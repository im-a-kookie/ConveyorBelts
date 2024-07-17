using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MapGen
{
    internal class DivisionGenerator
    {
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
            public Vector2 Origin;
            public Vector2 Size;
            public Division[] Subdivisions;
            public Vector2 Offset;

            public Division(Vector2 origin, Vector2 size, RNG r)
            {
                Origin = origin;
                Size = size;
                Offset = new Vector2((float)r.Next(), (float)r.Next());
            }

            public void GenerateDivisions(RNG r)
            {
                Subdivisions = new Division[4];
                Subdivisions[0] = new Division(Origin, Size / 2, r);
                Subdivisions[1] = new Division(Origin + new Vector2(Size.X / 2, 0), Size / 2, r);
                Subdivisions[2] = new Division(Origin + new Vector2(0, Size.Y / 2), Size / 2, r);
                Subdivisions[3] = new Division(Origin + Size / 2, Size / 2, r);

             }

            public override bool Equals(object obj)
            {
                return (obj is Division d && d.Origin == Origin && d.Size == Size);
            }

            public override int GetHashCode()
            {
                return Origin.GetHashCode() ^ Size.GetHashCode();
            }

        }

        







    }
}
