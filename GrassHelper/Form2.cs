using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrassHelper
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Generate(512);
        }
        public class RNG
        {
            private const int a = 16807;
            private const int m = 2147483647;
            private const int q = 127773;
            private const int r = 2836;
            private int seed;

            /// <summary>
            /// converts a given seed into a psuedorandom value
            /// </summary>
            /// <param name="seed"></param>
            /// <returns></returns>
            public static double Randomize(int seed)
            {
                int hi = seed / q;
                int lo = seed % q;
                seed = (a * lo) - (r * hi);
                if (seed <= 0)
                    seed = seed + m;
                return (seed * 1.0) / m;
            }

            public RNG(int seed)
            {
                if (seed <= 0 || seed == int.MaxValue)
                    throw new Exception("Bad seed");
                this.seed = seed;
            }
            public double Next()
            {
                int hi = seed / q;
                int lo = seed % q;
                seed = (a * lo) - (r * hi);
                if (seed <= 0)
                    seed = seed + m;
                return (seed * 1.0) / m;
            }

            public double NextInt(int min, int max)
            {
                int hi = seed / q;
                int lo = seed % q;
                seed = (a * lo) - (r * hi);
                if (seed <= 0)
                    seed = seed + m;
                return int.Min(min, int.Max(max, seed));
            }

        }

        //basically we make a big square
        //which becomes 4 smaller squares
        //and so on
        public class Division
        {

            public Division parent;
            public float scale = 1f;
            public Vector2 Origin;
            public Vector2 Size;
            public Division[] Subdivisions;
            public Vector2 Offset;
            public int flag = -1;

            public Division(Vector2 origin, Vector2 size, RNG r)
            {
                Origin = origin;
                Size = size;
                Offset = new Vector2((Origin + Size * (float)(r.Next() * 0.8 + 0.1)).X, ((Origin + Size * (float)(r.Next() * 0.8 + 0.1)).Y));
            }

            public void GenerateDivisions(RNG r)
            {
                if (Subdivisions != null) return;
                Subdivisions = new Division[4];
                Subdivisions[0] = new Division(Origin, Size / 2, r);
                Subdivisions[1] = new Division(Origin + new Vector2(Size.X / 2, 0), Size / 2, r);
                Subdivisions[3] = new Division(Origin + new Vector2(0, Size.Y / 2), Size / 2, r);
                Subdivisions[2] = new Division(Origin + Size / 2, Size / 2, r);
                for (int i = 0; i < 4; i++)
                {
                    Subdivisions[i].scale = scale / 2;
                    Subdivisions[i].parent = this;
                }

            }

            public override bool Equals(object? obj)
            {
                return (obj is Division d && d.Origin == Origin && d.Size == Size);
            }

            public override int GetHashCode()
            {
                return Origin.GetHashCode() ^ Size.GetHashCode();
            }

            public void Draw(Graphics g, float scale, bool offset = true)
            {
                g.DrawRectangle(new Pen(Color.Black, 1f / scale), Origin.X, Origin.Y, Size.X, Size.Y);
                if (offset) g.FillEllipse(Brushes.Green, Offset.X - (2f / scale), Offset.Y - (2f / scale), 4f / scale, 4f / scale);
            }


            public Division GetSubdivision(Vector2 point)
            {
                var p = (PointF)point;
                RectangleF bounds = new RectangleF((PointF)Origin, (SizeF)Size);
                if(!bounds.Contains(p))  return null;
                if (Subdivisions == null) return this;
                //check the quadrant
                for(int i = 0; i < 4; i++)
                {
                    var d = Subdivisions[i].GetSubdivision(point);
                    if (d != null) return d;
                }
                return this;
            }


        }

        public Bitmap Generate(int size = 256)
        {
            RNG r = new RNG((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
            r = new RNG(1235);

            Division d = new Division(Vector2.Zero, Vector2.One, r);

            d.GenerateDivisions(r);

            //lets make sure the offsets are 


            Bitmap b = new Bitmap(size + 1, size + 1);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                g.ScaleTransform(size, size);
                d.Draw(g, size, false);


                //now we have four corners, connect them
                for (int i = 0; i < 4; i++)
                {
                    //push the offset corners in a bit
                    d.Subdivisions[i].Draw(g, size, true);
                }

                List<Division> l0 = new();
                List<Division> l1 = new();

                for (int i = 0; i < 4; i++)
                    l0.Add(d.Subdivisions[i]);

                //1. We select a random point in each division in the list
                //2. Subdivide the division in that point
                //3. Get the subcell under that point
                // 
                //4. Draw a path through the points in order
                //5. Grab every cell under the lines at the current scale
                //6. Add them to a new list in path order
                //7. Repeat from the first step


                for(int i = 0; i < 1; i++)
                {
                    RecursivelyDivide(r, d, l0, l1);
                    (l0, l1) = (l1, l0);
                }

                for (int i = 0; i < l0.Count; i++)
                {
                    var d0 = l0[i];
                    var d1 = l0[(i + 1) % l0.Count];
                    d0.Draw(g, size, true);
                    //g.FillRectangle(Brushes.Black, new RectangleF((PointF)d0.Origin, (SizeF)d0.Size));

                    g.DrawLine(new Pen(Color.Red, 1f / size), new PointF(d0.Offset.X, d0.Offset.Y), new PointF(d1.Offset.X, d1.Offset.Y));
                }




            }
            return b;
        }

        public void RecursivelyDivide(RNG r, Division parent, List<Division> input, List<Division> output)
        {
            


        }



    }
}
