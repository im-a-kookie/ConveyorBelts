using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphics;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorBeltExample;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Reflection.Metadata.Ecma335;
using System.Buffers;

namespace ConveyorEngine.Graphics.Conveyors
{
    internal class SequentialItemRender
    {
        public static DynamicVertexBufferWrapper vertex_buffers;

        /// <summary>
        /// Rebuilds the vertex buffer for items on the conveyor belt
        /// </summary>
        /// <param name="w"></param>
        /// <param name="b"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static void RebuildConveyorCaches(World w, (Branch[] branches, int count) b)
        {
            //We need to rebuild and reset the buffer at this point since... yeah.
            if (vertex_buffers == null) vertex_buffers = new DynamicVertexBufferWrapper(SpriteManager.TexturePage.Width);
            vertex_buffers.Reset();

            //there's actually a fair bit happening here, which makes me wonder if thread delegation could be faster
            //But we can still only populate the buffers from the render thread
            //So the gains are small and this is therefore sorta low priority
            for (int i = 0; i < b.count; i++)
            {
                Branch branch = b.branches[i];
                //now we can do the thing
                //skip casting and do a null check only
                var si = branch.Inventory.GetAsSequential().Items;
                //go through every single item
                int track_pos = 0;
                //for (int g = si.Items.Count - 1; g >= 0; --g)
                for (int g = 0; g < si.Count; ++g)
                {
                    track_pos += si[g].gap;
                    track_pos = int.Max(1, int.Min(branch.Size * Settings.Conveyors.CONVEYOR_PRECISION - 1, track_pos));

                    //now we iterate for the size of the group
                    for (int index = 0; index < si[g].Count(); ++index)
                    {
                        Item n = si[g].Peek(index); //check the next item
                        //we only render alternate items at high zoom
                        //Strictly this actually breaks down, and we should flag the item groups with a flipflop toggle
                        //then use that instead to maintain some degree of consistency, but this is nonurgent.
                        if ((index & 0x1) == 0 || Core.Instance.myCam.ZoomCurrent > 1)
                        {
                            int block = (branch.Size - (track_pos) / Settings.Conveyors.CONVEYOR_PRECISION) - 1;
                            int step;
                            if (block >= branch.Size)
                            {
                                block = branch.Size - 1;
                                step = Settings.Conveyors.CONVEYOR_PRECISION;
                            }
                            else step = ((branch.Size - block) * Settings.Conveyors.CONVEYOR_PRECISION) - track_pos;
                            if (block >= 0 && block < branch.Size && step >= 0 && step <= Settings.Conveyors.CONVEYOR_PRECISION)
                            {
                                Vector2 pos;
                                if (block == 0)
                                    pos = ConveyorHelper.ComputeItemPos((Dir)n.dir, branch.members[block].Direction, step);
                                else
                                    pos = ConveyorHelper.ComputeItemPos(branch.members[block - 1].Direction.Opposite(), branch.members[block].Direction, step);
                                var pp = branch.members[block].pos;
                                //now add the tile to the cache
                                vertex_buffers.AddTile(pp.x * 8 + pos.X * 8f - 8, pp.y * 8 + pos.Y * 8f - 8, ItemManager.TextureCache[n.id], Color.White, false, false);
                            };
                        }
                        track_pos += Settings.Conveyors.ITEM_WIDTH;

                    }
                }
                
            }
            vertex_buffers.ConstructBuffer();
        }

        public static bool RenderConveyorItems(World w, (Branch[] branches, int count) b, BasicEffect e)
        {
            if (vertex_buffers._buffer != null && vertex_buffers._indices != null && vertex_buffers.tileCount > 0)
            {
                e.Texture = SpriteManager.TexturePage;
                Core.Instance.GraphicsDevice.SetVertexBuffer(vertex_buffers._buffer);
                Core.Instance.GraphicsDevice.Indices = vertex_buffers._indices;
                foreach (var p in e.CurrentTechnique.Passes)
                {
                    p.Apply();
                    Core.Instance.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertex_buffers.tileCount * 2);
                }
                //Returning true indicates that we have finished rendering
                return true;
                //A return of "false" indicates that we now need to call the basic effect render step
            }
            return true;
        }

    }
}
