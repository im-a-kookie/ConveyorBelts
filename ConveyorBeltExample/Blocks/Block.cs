using ConveyorBeltExample.GameWorld;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Blocks
{
    internal class Block
    {
        public Point Location;

        public byte[] ExtData;

        public Dir Direction;

        public double UpdateTime;

        public float Work = 0f;

        public World world;

        public Block() { }

        /// <summary>
        /// Called when the block is set
        /// </summary>
        /// <param name="w"></param>
        /// <param name="c"></param>
        /// <param name="p"></param>
        public virtual void OnSet(World w, Chunk c, Point p)
        {
            //doesn't do anything normally
        }

        /// <summary>
        /// Called when the block is removed.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="c"></param>
        /// <param name="p"></param>
        public virtual int OnRemove(World w, Chunk c, Point p, bool forced)
        {
            if (forced) return 0;
            return 0;
        }

    }
}
