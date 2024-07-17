using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Tests
{
    public class Benchmarks
    {
        public static void Run()
        {
            int loops = 10;
            int iterations = 1000000;

            double[] doubletimes = new double[loops];
            double[] floattimes = new double[loops];
            double[] inttimes = new double[loops];
            double[] longtimes = new double[loops];

            Stopwatch s = new Stopwatch();

            for (int i = 0; i < loops; i++)
            {

                s.Restart();
                double d = TestDouble(iterations);
                s.Stop();
                doubletimes[i] = s.Elapsed.TotalMicroseconds;

                s.Restart();
                var f = TestFloat(iterations);
                s.Stop();
                floattimes[i] = s.Elapsed.TotalMicroseconds;

                s.Restart();
                var x = TestInt(iterations);
                s.Stop();
                inttimes[i] = s.Elapsed.TotalMicroseconds;

                s.Restart();
                var y = TestLong(iterations);
                s.Stop();
                longtimes[i] = s.Elapsed.TotalMicroseconds;

            }

            doubletimes[0] = 0;
            floattimes[0] = 0;
            inttimes[0] = 0;

            for (int i = 1; i < loops; i++)
            {
                doubletimes[0] += doubletimes[i];
                floattimes[0] += floattimes[i];
                inttimes[0] += inttimes[i];
                longtimes[0] += inttimes[i];
            }

            doubletimes[0] /= loops - 1;
            floattimes[0] /= loops - 1;
            inttimes[0] /= loops - 1;
            longtimes[0] /= loops - 1;

            Debug.WriteLine("Double: " + Math.Round(doubletimes[0]));
            Debug.WriteLine("Float: " + Math.Round(floattimes[0]));
            Debug.WriteLine("Int: " + Math.Round(inttimes[0]));
            Debug.WriteLine("Int: " + Math.Round(longtimes[0]));

        }

        public static double TestDouble(int iterations)
        {
            double d = 1d;
            for (int i = 0; i < iterations; i++) if ((i & 0x1) == 0) d *= 2; else d /= 2;
            return d;
        }

        public static float TestFloat(int iterations)
        {
            float d = 1f;
            for (int i = 0; i < iterations; i++) if ((i & 0x1) == 0) d *= 2; else d /= 2;
            return d;
        }

        public static int TestInt(int iterations)
        {
            int d = 1;
            for (int i = 0; i < iterations; i++) if ((i & 0x1) == 0) d <<= 1; else d >>= 1;
            return d;
        }


        public static long TestLong(int iterations)
        {
            long d = 1;
            for (int i = 0; i < iterations; i++) if ((i & 0x1) == 0) d <<= 1; else d >>= 1;
            return d;
        }

    }
}
