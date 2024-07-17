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
    internal class RenderChunkLayer : RenderLayer
    {
       

        public Func<World, Chunk, BasicEffect, bool> Render;

        public Action<World, Chunk> RebuildCaches;

        public RenderChunkLayer(string name, int priority, Action<World, Chunk> rebuildCaches, Func<World, Chunk, BasicEffect, bool> render) : base(name, priority)
        {
            this.Render = render;
            this.RebuildCaches = rebuildCaches;
        }

        public override bool Draw(World w, Chunk c, (Branch[] branches, int count) b, BasicEffect e)
        {
            return Render(w, c, e);
        }

        public override void Rebuild(World w, Chunk c, (Branch[] branches, int count) b)
        {
            RebuildCaches(w, c);
        }

    }
}
