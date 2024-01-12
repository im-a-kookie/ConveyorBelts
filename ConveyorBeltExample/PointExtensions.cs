using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample
{
    public static class PointExtensions
    {
        public static int[][] dirs = new int[][] { new int[] { 0, -1 }, new int[] { 1, 0 }, new int[] { 0, 1 }, new int[] { -1, 0 } };

        public static Point Step(this Point point, Dir direction) => new Point(point.X + dirs[(int)direction & 0x3][0], point.Y + dirs[(int)direction & 0x3][1]);
        
        public static List<Point> GetAdjacent(this Point point) =>  new List<Point>()
        {
            Step(point, Dir.NORTH), Step(point, Dir.EAST), Step(point, Dir.SOUTH), Step(point, Dir.WEST)
        };

        

    }
}
