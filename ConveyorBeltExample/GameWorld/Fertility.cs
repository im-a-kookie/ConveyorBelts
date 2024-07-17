using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.GameWorld
{
    /// <summary>
    /// Stores low resolution details about soil and water table
    /// Generally there are several of these in each chunk, each
    /// covering areas several tiles large.
    /// </summary>
    public struct Fertility
    {
        public byte Nutrient, Hydration, Sandiness, Toxicity, Salinity;

        public static int W = 4, H = 4;

        static Vector128<float>[,] dists;
        static int[][] indices = [[0, 1, 3], [1, 2, 5], [3, 6, 7], [5, 7, 8]];
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
                    //find the coordinate of the cell in question
                    float x = i - (W / 2) + 0.5f;
                    float y = j - (H / 2) + 0.5f;
                    float[] dist = new float[8];
                    //now calculate its distances from each of the 8 surrounding fertility tiles, and the tile in 
                    for (int k = 0; k < 8; k++)
                    {
                        int n = k + (k / 4);
                        dist[k] = (pos[n].X - x) * (pos[n].X - x) + (pos[n].Y - y) * (pos[n].Y - y);
                    }
                    var result = Vector256.Create(dist);
                    result = One / result;

                    var ss = Vector256.Sum(result);
                    float d_here = 1f / (x * x + y * y);
                    ss += d_here;
                    result /= ss;
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
                    vIn -= vDif;
                    vIn = vIn * vIn;

                    var vOut = Vector128.Create(1f, 1f, 1f, 1f) / (vIn.GetUpper() + vIn.GetLower());

                    var sum = Vector128.Sum(vOut);
                    vOut /= sum;
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
            for (int i = 0; i < Data.Length; i++)
            {
                dD[i] = Vector128.Sum(dists[x, y] * Vectorize(i, A, B, C, D));
            }
            return new Fertility(dD);
        }


    }
}
