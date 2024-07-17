using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{

    public static class DirExtensions
    {
        public static Dir Clockwise(this Dir d) => (Dir)(((int)d + 1) & 0x3);
        public static Dir Anticlockwise(this Dir d) => (Dir)(((int)d - 1) & 0x3);
        public static Dir Opposite(this Dir d) => (Dir)(((int)d + 2) & 0x3);
        public static int Mask(this Dir d) => 1 << (int)d;
        public static bool Aligns(this Dir d, int mask) => (mask & d.Mask()) != 0;
        public static bool PointsTo(this Dir d, int mask) => Aligns(d.Opposite(), mask);
    }

    public enum Dir
    {
        NORTH = 0, EAST = 1, SOUTH = 2, WEST = 3


    }
}
