using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks
{
    /// <summary>
    /// The core class that defines a block type, and its behaviour
    /// </summary>
    public class BlockDefinition
    {

        public string name;

        public short id;

        public Rectangle Bounds = new Rectangle(0, 0, 1, 1);


        public BlockDefinition() { }
        public BlockDefinition(string name) { this.name = name; }

        /// <summary>
        /// Called when the block is set
        /// </summary>
        /// <param name="w"></param>
        /// <param name="c"></param>
        /// <param name="p"></param>
        public virtual void OnSet(World w, Chunk c, KPoint p, BlockData data)
        {

        }

        /// <summary>
        /// Called when a block is removed.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <param name="data"></param>
        /// <param name="occured">Whether the removal has already occurred.</param>
        /// <returns>1 to deny the removal when it has not already occurred</returns>
        public virtual int OnRemove(World w, Chunk c, KPoint p, BlockData data, bool occured = true)
        {
            if (occured) return 0;
            return 0;
        }

        public virtual int GetLayer()
        {
            return 1;
        }




    }
}
