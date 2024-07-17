using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MapGen
{
    internal class Generator
    {


        public byte[] randoms;
        Vector2[] VertexMap;


        /// <summary>
        /// gets a random integer from the randomized entropy thing
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetRandomInt(int x, int y, int z)
        {
            return randoms[x & 1023] | (randoms[y & 1023] << 8) | (randoms[z & 1023] << 16) | (randoms[(x ^ y ^ z) & 1023] << 24);
        }

        /// <summary>
        /// gets a random integer from the randomized entropy thing
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetRandomInt(int x, int y)
        {
            return randoms[x & 1023] | (randoms[y & 1023] << 8) | (randoms[(x + y) & 1023] << 16) | (randoms[(x ^ y) & 1023] << 24);
        }

        /// <summary>
        /// gets a random integer from the randomized entropy thing
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetRandomInt(int x)
        {
            return randoms[x & 1023] | (randoms[(x + 124) & 1023] << 8) | (randoms[(x + 613) & 1023] << 16) | (randoms[(x + 173) & 1023] << 24);
        }

         
        public Generator(uint seed = 0, int dimension = 128)
        {
            Random r;
            if (seed == 0) r = new Random();
            else r = new Random((int)seed);
            //fill the random thing with random data
            randoms = new byte[1024];
            r.NextBytes(randoms);


        }


        //the map generates based on a perlin noise that is elevated in the center



        /// <summary>
        /// Generic foating point thing using floats, just realized Vector2 exists lol
        /// </summary>
        class FPoint
        {
            public float X, Y;
            public FPoint(double x, double y) { (X, Y) = ((float)x, (float)y); }
            public FPoint(float x, float y) { (X, Y) = (x, y); }
            public FPoint(int x, int y) { (X, Y) = (x, y); }

            public static FPoint operator +(FPoint a, FPoint b) => new FPoint(a.X + b.X, a.Y + b.Y);
            public static FPoint operator -(FPoint a, FPoint b) => new FPoint(a.X - b.X, a.Y - b.Y);
            public static bool operator ==(FPoint a, FPoint b) => a.X == b.X && a.Y == b.Y;
            public static bool operator !=(FPoint a, FPoint b) => !(a == b);

            public static FPoint operator +(FPoint a, float n) => new FPoint(a.X + n, a.Y + n);
            public static FPoint operator -(FPoint a, float n) => new FPoint(a.X - n, a.Y - n);
            public static FPoint operator *(FPoint a, float n) => new FPoint(a.X * n, a.Y * n);
            public static FPoint operator /(FPoint a, float n) => new FPoint(a.X / n, a.Y / n);
            public static FPoint operator %(FPoint a, float n) => new FPoint(a.X % n, a.Y % n);
            public static FPoint operator ^(FPoint a, float n) => new FPoint((float)Math.Pow(a.X, n), (float)Math.Pow(a.Y, n));

            public static FPoint operator +(FPoint a, double n) => a + (float)n;
            public static FPoint operator -(FPoint a, double n) => a - (float)n;
            public static FPoint operator *(FPoint a, double n) => a * (float)n;
            public static FPoint operator /(FPoint a, double n) => a / (float)n;
            public static FPoint operator %(FPoint a, double n) => a % (float)n;
            public static FPoint operator ^(FPoint a, double n) => a ^ (float)n;

            public static FPoint operator +(FPoint a, int n) => a + (float)n;
            public static FPoint operator -(FPoint a, int n) => a - (float)n;
            public static FPoint operator *(FPoint a, int n) => a * (float)n;
            public static FPoint operator /(FPoint a, int n) => a / (float)n;
            public static FPoint operator %(FPoint a, int n) => a % (float)n;
            public static FPoint operator ^(FPoint a, int n) => a ^ (float)n;

        }


    }
}
