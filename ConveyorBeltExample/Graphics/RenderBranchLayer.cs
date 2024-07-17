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
    /// A render layer object for graph/branches
    /// </summary>
    internal class RenderBranchLayer : RenderLayer
    {

        public Func<World, (Branch[] branches, int count), BasicEffect, bool> Render;

        public Action<World, (Branch[] branches, int count)> RebuildCaches;

        public RenderBranchLayer(string name, int priority, Action<World, (Branch[] branches, int count)> rebuildCaches, Func<World, (Branch[] branches, int count), BasicEffect, bool> render) : base(name, priority)
        {
            this.Render = render;
            this.RebuildCaches = rebuildCaches;
        }

        public override bool Draw(World w, Chunk c, (Branch[] branches, int count) b, BasicEffect e)
        {
            return Render(w, b, e);
        }

        public override void Rebuild(World w, Chunk c, (Branch[] branches, int count) b)
        {
            RebuildCaches(w, b);
        }
    }
}
