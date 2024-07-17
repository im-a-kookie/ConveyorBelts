using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Tiles
{
    public struct Tile
    {
        /// <summary>
        /// An ID referencing the tile texture
        /// </summary>
        public short id;
        /// <summary>
        /// A color tint value
        /// </summary>
        public (byte r, byte g, byte b) color;

        

        public Tile(short id,  byte r, byte g, byte b)
        {
            this.id = id;
            this.color = (r, g, b);
        }



    }
}
