using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Tests
{
    public class PointBenches
    {

struct PPoint
{
    public int x, y;
    public PPoint(int x, int y)
    {
        this.x = x; this.y = y;
    }
}

struct LPoint
{
    public ulong val;
    public LPoint(int _x, int _y)
    {
        val = (ulong)(((ulong)_x & 0xFFFFFFFF) | (((ulong)_y) << 32));
    }
    public LPoint(ulong l)
    {
        val = l;
    }
    public int x => (int)(val & 0xFFFFFFFF);
    public int y => (int)(val >> 32);
}

        public static void Perform()
        {


            int[][] vals = [[0, 0], [10, 20], [-11, -124]];
            foreach (int[] i in vals)
            {
                var pp = new PPoint(i[0], i[1]);
                var lp = new LPoint(i[0], i[1]);
                Console.WriteLine("Setting Point: " + i[0] + ", " + i[1] + ", (" + string.Format("{0:X}", i[0]) + ", " + string.Format("{0:X}", i[1]) + ")");
                Console.WriteLine("PP: " + pp.x + ", " + pp.y);
                Console.WriteLine("LP: " + lp.x + ", " + lp.y + ",   " + string.Format("{0:X}", lp.val));
                Console.WriteLine("");
            }

            for (int i = 0; i < 4; i++)
            {
                RunTests(100, false);
            }
            RunTests(300, true);
            RunTests(300, true);

        }

        public static void RunTests(int size = 300, bool log = true)
        {
            Stopwatch s = new Stopwatch();
            PPoint[] _pp = new PPoint[size * size];
            LPoint[] _lp = new LPoint[size * size];

            s.Restart();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var point = new PPoint(i, j);
                    _pp[i + size * j] = point;
                }
            }
            s.Stop();
            if (log) Console.WriteLine("PPoint Create: " + s.Elapsed.TotalMicroseconds);

            s.Restart();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var lpoint = new LPoint(i, j);
                    _lp[i + size * j] = lpoint;
                }
            }
            s.Stop();
            if (log) Console.WriteLine("LPoint Create: " + s.Elapsed.TotalMicroseconds);

            s.Start();
            int xs = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    xs += _pp[i + size * j].x + _pp[i + size * j].y;
                }
            }
            s.Stop();
            if (log) Console.WriteLine("PPoint Getter: " + s.Elapsed.TotalMicroseconds + ", " + xs);


            s.Restart();
            xs = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    xs += _lp[i + size * j].x + _lp[i + size * j].y;
                }
            }
            s.Stop();
            if (log) Console.WriteLine("LPoint Getter: " + s.Elapsed.TotalMicroseconds + ", " + xs);
            if (log) Console.WriteLine("");
        }
    }
}
