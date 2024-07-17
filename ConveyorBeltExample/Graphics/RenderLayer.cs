using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Graphics
{
    /// <summary>
    /// The Render Layer describes a slice of the world, generally at a given depth,
    /// In the sense that certain layers are assumed to always be above others (e.g buildings above terrain)
    /// </summary>
    public abstract class RenderLayer
    {
        public int Index = -1;

        public int Priority;

        public string Name;

        public RenderLayer(string name, int priority)
        {
            this.Name = name;
            this.Priority = priority;
        }
        
        /// <summary>
        /// Rebuild this layer
        /// </summary>
        /// <param name="w"></param>
        /// <param name="c"></param>
        /// <param name="b"></param>
        public abstract void Rebuild(World w, Chunk c, (Branch[] branches, int count) b);

        /// <summary>
        /// Draw this layer
        /// </summary>
        /// <param name="w"></param>
        /// <param name="c"></param>
        /// <param name="b"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public abstract bool Draw(World w, Chunk c, (Branch[] branches, int count) b, BasicEffect e);



    }
}
