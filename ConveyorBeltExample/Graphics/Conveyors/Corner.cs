using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Graphics.Conveyors
{

    /// <summary>
    /// public struct that holds some data for rendering conveyor corners
    /// </summary>
    public struct Corner
    {
        public readonly int x, y, branch;
        public readonly byte mask, frame;
        public readonly Dir d;
        public Corner(int x, int y, Dir d, int mask, int frame, int branch)
        {
            this.x = x;
            this.y = y;
            this.mask = (byte)mask;
            this.frame = (byte)frame;
            this.d = d;
            this.branch = branch;
        }
    }




}
