using System.Buffers;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.PerformanceData;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GrassHelper
{
    public partial class Form1 : Form
    {

        Image img = null;

        int tick = 0;
        Rectangle[] Dirt = new Rectangle[4];
        Rectangle[][] Grass = new Rectangle[4][];


        //The Fertility data is stored in array of 4x4 tiles
        //
        // F1  F2  F3
        // F4  F5  F6
        // F7  F8  F9
        // Each fertility tile is discrete, but water, nutrients, etc, are diffused between them

        // 1  2  3  4
        //1 [a][A][b][ ]
        //2 [C][X][B][ ]
        //3 [c][D][d][ ]
        //4 [ ][ ][ ][e]
        //ABCD are lerps of each pair
        //X is a lerp of ABCD
        //
        //if aN is F5(1,1) and eN, F5(4,4) 
        //Then a5 considers F1, F2, and F4
        //We can refer to an expanded lookup
        //First we take the distances of the cells, which we can normalize
        // [32][25] | [18][17] | [  ][  ] | [  ][  ]
        // [25][18] | [13][10] | [  ][  ] | [  ][  ]
        // ---------#######################----------
        // [18][13] # [ 4][ 3]   [ 3][ 4] # [  ][  ]
        // [17][10] # [ 3][ 1]   [ 1][ 3] # [  ][  ]
        // ---------#          F          #----------
        // [  ][  ] # [ 3][ 1]   [ 1][ 3] # [  ][  ]
        // [  ][  ] # [ 4][ 3]   [ 3][ 4] # [  ][  ]
        // ---------#######################---------
        // [  ][  ]   [  ][  ] | [  ][  ]   [  ][  ]
        // [  ][  ]   [  ][  ] | [  ][  ]   [  ][  ]

        //Then we overlap the grids and are left with 4 floats for every cell
        //dictating the multiplier for each of the four neighboring fertility blocks
        struct Fertility
        {
            public byte Nutrient, Hydration, Sandiness, Toxicity, Salinity;

            public static int W = 4, H = 4;

            static Vector128<float>[,] dists;
            static int[][] indices = [[0, 1, 3], [1, 2, 5], [3, 6, 7], [5, 7, 8] ];
            static float[,][] dist_cache;
            static Vector256<float>[,] octDist;
            static float[,] inner_dist;
            public static void GenerateCache(int scale = 4)
            {
                W = H = scale;
                dists = new Vector128<float>[W, H];
                octDist = new Vector256<float>[W, H];
                var One = Vector256.Create(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);

                inner_dist = new float[W, H];

                Point[] pos = [new(-W, -H), new(0, -H), new(W, -H), new(-W, 0), new(0, 0), new(W, 0), new(-W, H), new(0, H), new(W, H)];
                //there are 9 of these stupid things so let's do Vector256
                for (int i = 0; i < W; ++i)
                {
                    for (int j = 0; j < H; ++j)
                    {

                      

                        float x = i - (W / 2) + 0.5f;
                        float y = j - (H / 2) + 0.5f;
                        float[] dist = new float[8];
                        for (int k = 0; k < 8; k++)
                        {
                            int n = k + (k / 4);
                            dist[k] = (pos[n].X - x) * (pos[n].X - x) + (pos[n].Y - y) * (pos[n].Y - y);
                        }
                        var result = Vector256.Create(dist);
                        result = Vector256.Divide(One, result);
                        
                        var ss = Vector256.Sum(result);
                        float d_here = 1f / (x * x + y * y);
                        ss += d_here;
                        result = Vector256.Divide(result, ss);
                        d_here /= ss;
                        inner_dist[i, j] = d_here;
                        octDist[i, j] = result;

                        int a = i / (W / 2);
                        int b = j / (H / 2);
                        int index = a + 2 * b;

                        //compute the thing
                        Point A = pos[indices[index][0]];
                        Point B = pos[indices[index][1]];
                        Point C = pos[indices[index][2]];

                        var f = (double x) => (float)x;

                        //compute the distances 
                        
                       

                        var vIn = Vector256.Create(A.X, B.X, C.X, x, A.Y, B.Y, C.Y, y);
                        var vDif = Vector256.Create(x, x, x, 0, y, y, y, 0);
                        vIn = Vector256.Subtract(vIn, vDif);
                        vIn = Vector256.Multiply(vIn, vIn);

                        var vOut = Vector128.Add(vIn.GetUpper(), vIn.GetLower());
                        vOut = Vector128.Divide(Vector128.Create(1f, 1f, 1f, 1f), vOut);

                        var sum = Vector128.Sum(vOut);
                        vOut = Vector128.Divide(vOut, sum);
                        dists[i, j] = vOut;
                    }
                }
            }

            //static Fertility()
            //{
                //GenerateCache(4);
            //}

            public byte[] Data => new byte[] { Nutrient, Hydration, Sandiness, Toxicity, Salinity };
            public float[] FData => new float[] { Nutrient, Hydration, Sandiness, Toxicity, Salinity };

            public ulong Long => BitConverter.ToUInt64(Data, 0);

            public Fertility(int nutrient = 0, int hydration = 0, int sandiness = 0, int toxicity = 0, int salinity = 128) : this((int[])[nutrient, hydration, sandiness, toxicity, salinity]) { }

            public Fertility((int nutrient, int hydration, int sandiness, int toxicity, int salinity) pars) : this((int[])[pars.nutrient, pars.hydration, pars.sandiness, pars.toxicity, pars.salinity]) { }

            public Fertility(Fertility f) : this(f.Data) { }

            public Fertility(double[] input)
            {
                Nutrient = (byte)input[0];
                Hydration = (byte)input[1];
                Sandiness = (byte)input[2];
                Toxicity = (byte)input[3];
                Salinity = (byte)input[4];
            }

            public Fertility(float[] input)
            {
                Nutrient = (byte)input[0];
                Hydration = (byte)input[1];
                Sandiness = (byte)input[2];
                Toxicity = (byte)input[3];
                Salinity = (byte)input[4];
            }

            public Fertility(byte[] input)
            {
                Nutrient = input[0];
                Hydration = input[1];
                Sandiness = input[2];
                Toxicity = input[3];
                Salinity = input[4];
            }

            public Fertility(int[] input)
            {
                Nutrient = (byte)input[0];
                Hydration = (byte)input[1];
                Sandiness = (byte)input[2];
                Toxicity = (byte)input[3];
                Salinity = (byte)input[4];
            }

            public Vector256<float> Pack => new Vector<float>(FData).AsVector256();

            public byte this[int x] => Data[x];


            public static Vector128<float> Vectorize(int index, Fertility A, Fertility B, Fertility C, Fertility D)
            {
                return Vector128.Create((float)A[index], (float)B[index], (float)C[index], (float)D[index]);
            }

            public static Vector256<float> Vectorize(int index, Fertility[] A)
            {
                return Vector256.Create((float)A[0][index], (float)A[1][index], (float)A[2][index], (float)A[3][index], (float)A[5][index], (float)A[6][index], (float)A[7][index], (float)A[8][index]);
            }

            public Fertility Interpolate(int x, int y, Fertility[] surrounds3x3)
            {
                int a = x / (W / 2);
                int b = y / (H / 2);
                int index = a + 2 * b;
                //compute the thing
                var A = surrounds3x3[indices[index][0]];
                var B = surrounds3x3[indices[index][1]];
                var C = surrounds3x3[indices[index][2]];
                var D = surrounds3x3[4];
                //now do the distance cache multiplier thing
                var dD = new float[Data.Length];
                for(int i = 0; i < Data.Length; i++)
                {
                    //dD[i] = Vector256.Sum(Vector256.Multiply(Vectorize(i, surrounds3x3), octDist[x, y]));
                    //dD[i] += Data[i] * inner_dist[x,y];
                   dD[i] = Vector128.Sum(Vector128.Multiply(dists[x, y], Vectorize(i, A, B, C, D)));
                }
                return new Fertility(dD);
            }

            public (int r, int g, int b) ToRGB(int X, int Y)
            {
                //Debug.WriteLine(X + ", " + Y + " => " + (Randomize(X, Y, 5423) % 30) + ", " + (Randomize(Y, X, 2362) % 30) + ", " + (Randomize(X, 7347, Y) % 30));

                return (int.Clamp((Randomize(X, Y, 152) & 0x1F) + Nutrient - 0xF, 0, 255), 
                    int.Clamp((Randomize(Y, X, 5121) & 0x1F) + Toxicity - 0xF, 0, 255), 
                    int.Clamp((Randomize(X , 1621, Y) & 0x1F) + Hydration - 0xF, 0, 255));
            }

        }


        int Scale = 16;

        int Rows = 8;
        Fertility[] DataArray;

        //let's justt do a thing where we set random values and they give us an RGB color
        void SetData()
        {
            Fertility.GenerateCache(Scale);
            Random r = new Random();
            byte[] n = new byte[5];
            DataArray = new Fertility[Rows * Rows];
            for (int i = 0; i < Rows * Rows; i++)
            {
                r.NextBytes(n);
                DataArray[i] = new Fertility(n);

            }


        }

        Fertility Get(int i, int j)
        {
            Fertility[] surround = ArrayPool<Fertility>.Shared.Rent(9);
            //get the position for the thing.....
            int xx = int.Clamp(i / Scale, 0, Rows - 1);
            int yy = int.Clamp(j / Scale, 0, Rows - 1);
            for (int ii = 0; ii < 3; ++ii)
            {
                for (int jj = 0; jj < 3; ++jj)
                {
                    int n = ii + (3 * jj);
                    int rx = int.Clamp(xx + ii - 1, 0, Rows - 1);
                    int ry = int.Clamp(yy + jj - 1, 0, Rows - 1);
                    surround[n] = DataArray[rx + Rows * ry];
                }
            }
            return DataArray[yy + Rows * xx].Interpolate(i - xx * Scale, j - yy * Scale, surround);
        }


        public void UpdateCell(int x, int y, int seed = 0)
        {
            var d0 = DataArray[y + Rows * x];


            //push through the thing
            int[][] difs = new int[4][];

            for (int d = 0; d < 4; d++)
            {
                //smoosh the directions
                var dir = (d + Randomize(x, y)) & 0x3;
                var xx = (x + ((2 - (dir &= 0x3)) * (dir & 0x1)));
                var yy = (y + (dir - 1) * (~dir & 0x1));

                if (xx < 0 || yy < 0 || xx >= Rows || yy >= Rows) continue;


                var dN = DataArray[yy + Rows * xx];
                difs[dir] = new int[dN.Data.Length];

                int h0 = d0.Hydration, hN = dN.Hydration, n0 = d0.Nutrient, nN = dN.Nutrient;

                //we move up to 10 units of water

                //a larger elevation difference increases the change

                int water = int.Clamp((d0.Hydration - dN.Hydration) / 2, -WATER_MOVE, WATER_MOVE);
                int nutrient = int.Clamp((d0.Nutrient - dN.Nutrient) / 2, -NUTRIENT_MOVE, NUTRIENT_MOVE);
                int toxin = int.Clamp((d0.Toxicity - dN.Toxicity) / 2, -POLLUTION_MOVE, POLLUTION_MOVE);
                int salinity = int.Clamp((d0.Salinity - dN.Salinity) / 2, -SALINITY_MOVE, SALINITY_MOVE);

                //we move a lot of nutrient and toxin when water moves
                nutrient += water / 2;
                toxin += water / 2;
                //and we move a LOT of salt
                salinity += (2 * water) / 3;

                //now set all of the values
                dN.Hydration = (byte)int.Clamp(dN.Hydration + water, 0, 255);
                dN.Nutrient = (byte)int.Clamp(dN.Nutrient + nutrient, 0, 255);
                dN.Toxicity = (byte)int.Clamp(dN.Toxicity + toxin, 0, 255);
                dN.Salinity = (byte)int.Clamp(dN.Toxicity + toxin, 0, 255);

                d0.Hydration = (byte)int.Clamp(d0.Hydration - water, 0, 255);
                d0.Nutrient = (byte)int.Clamp(d0.Nutrient - nutrient, 0, 255);
                d0.Toxicity = (byte)int.Clamp(d0.Toxicity - toxin, 0, 255);
                d0.Salinity = (byte)int.Clamp(d0.Toxicity - toxin, 0, 255);

                DataArray[yy + Rows * xx] = dN;



                //We need to optimize direction stepping, because we will do it a lot
                //val = (
                //    ((ulong)(p.x + ((2 - (dir &= 0x3)) * (dir & 0x1)) & 0xFFFFFFFF))
                //    | (((ulong)(p.y + (dir - 1) * (~dir & 0x1))) << 32)
                //);

            }
            DataArray[y + Rows * x] = d0;
        }

        static float[] lookup_sqrt;
        static float[] lookup_log;
        static float[] lookup_slight;

        public Form1()
        {
            InitializeComponent();

            SetData();

            this.Paint += Form1_Paint;
            this.Focus();
            this.BringToFront();

            lookup_sqrt = new float[256];
            lookup_log = new float[256];


            for (int i = 0; i < 256; i++)
            {
                lookup_sqrt[i] = float.Sqrt(i);
                lookup_log[i] = float.Log(i);
                lookup_slight[i] = float.Pow(i, 0.75f);
            }












            return;





            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Images | *.png" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    img = Image.FromStream(File.Open(ofd.FileName, FileMode.Open));
                }
                else
                {
                    Application.Exit();
                }
            }

            for (int i = 0; i < 4; ++i)
            {
                Dirt[i] = new Rectangle(0, 8 * i, 8, 8);
                Grass[i] = new Rectangle[4];
                for (int j = 0; j < 4; j++)
                {
                    Grass[i][j] = new Rectangle(8 + j * 8, i * 8, 8, 8);
                }
            }


        }


        public static int Randomize(int n)
        {
            return (((n * 121) ^ 16237481) >> 3) ^ 0x7E88ABDE;
        }

        public static int Randomize(int x, int y)
        {
            return ((((x + 155121 * y) * 241) ^ 63261214) >> 4) ^ 0x7EE81CD9;
        }

        public static int Randomize(int x, int y, int z)
        {
            //0x7e82ed9c
            //0x629e8935
            //0x2c6a4bcf
            //0x0fc5ef97
            //0x35306eca
            //0x7ad6d57b
            // Linear congruential generator parameters
            return (((((12515 * x) >> 3) + 512115 * y + 734733 * z) ^ 0x2c6a4bcf) >> 3) ^ 0x7ad6d57b;

        }


        void ComputeTile(Fertility f)
        {
            //computes this into a tile somehow
            //1. the sandiness value gives us one of a few values
            //
            





        }


        private void Form1_Paint(object? sender, PaintEventArgs e)
        {

            e.Graphics.ResetTransform();
            e.Graphics.ScaleTransform(512 / (Scale * Rows), (512 / (Scale * Rows)));
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            for (int i = 0; i < Scale * Rows; ++i)
            {
                for (int j = 0; j < Scale * Rows; ++j)
                {
                    var f = Get(i, j);
                    var c = f.ToRGB(i, j);
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(c.r, c.g, c.b)), new Rectangle(i, j, 1, 1));
                }
            }

            return;
            //draw the dirt tile
            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    Rectangle pos = new Rectangle(i * 8, j * 8, 8, 8);
                    int dirt_seed = Randomize(i, j);
                    int grass_seed = Randomize(i ^ j, i + j);

                    //increase the number of grass splooges based on tick

                    int splooge = tick * 4 + (grass_seed & 0x3);


                    e.Graphics.DrawImage(img, pos, Dirt[dirt_seed & 0x3], GraphicsUnit.Pixel);

                    for (int k = 0; k < splooge; k++)
                    {
                        int x = Randomize(i, j, k) % 5;
                        int y = Randomize(j, k, i) % 7;
                        e.Graphics.DrawImage(img, new Rectangle(pos.X + x - 2, pos.Y + y - 6, 8, 8), Grass[int.Min(3, k / 16)][(k % 16) / 4], GraphicsUnit.Pixel);
                    }


                    //we basically just draw f(tick) + variance pieces of grass of the first color
                    //then keep going with the next color
                    //and so on




                }
            }






        }


        static int WATER_MOVE = 6;
        static int NUTRIENT_MOVE = 3;
        static int POLLUTION_MOVE = 3;
        static int SALINITY_MOVE = 3;

        private void button1_Click(object sender, EventArgs e)
        {
            tick += 1;

            SetData();



            Refresh();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            tick += 1;
            for(int i = 0; i < Rows * Rows; i++)
            {
                if ((Randomize(i, tick) & 0x1) != 0)
                {
                    UpdateCell(i % Rows, i / Rows, Randomize(i, tick));
                }
            }
            Refresh();


        }
    }
}
