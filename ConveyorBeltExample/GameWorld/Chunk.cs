using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.Graphics;
using ConveyorBeltExample.Graphing;
using ConveyorEngine.Blocks;
using ConveyorEngine.GameWorld;
using ConveyorEngine.Graphics;
using ConveyorEngine.Graphics.Conveyors;
using ConveyorEngine.Tiles;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ConveyorEngine.Graphics.Conveyors.ConveyorHelper;

namespace ConveyorBeltExample.GameWorld
{
    public class Chunk : Byteable
    {
        /// <summary>
        /// Flags for whether the chunk is dirty at any given index
        /// </summary>
        public bool[] dirty;

        /// <summary>
        /// The world owning this chunk
        /// </summary>
        public World world;

        /// <summary>
        /// The location of this chunk in chunk coordinates
        /// </summary>
        public KPoint Location;

        /// <summary>
        /// The block storage array
        /// </summary>
        public BlockData[] Blocks;

        /// <summary>
        /// A map of the fertility subregions in this chunk
        /// </summary>
        public Fertility[] FertilityMap;

        /// <summary>
        /// A map of the tiles in this chunk
        /// </summary>
        public Tile[] TileMap;
        


        /// <summary>
        /// The bounds of this chunk in world coordinates
        /// </summary>
        public Rectangle Bounds => new Rectangle(Location.x * Settings.Engine.CHUNK_SIZE, Location.y * Settings.Engine.CHUNK_SIZE, Settings.Engine.CHUNK_SIZE, Settings.Engine.CHUNK_SIZE);

        public Chunk(byte[] data) : base(data)
        { }

        public Chunk (World w, KPoint pos) : this(null)
        {
            this.world = w;
            this.Location = pos;
            Blocks = new BlockData[Settings.Engine.CHUNK_SIZE * Settings.Engine.CHUNK_SIZE];
            dirty = new bool[LayerManager.RenderLayers.Count];

        }

        /// <summary>
        /// Gets the block at the given position. Worldpos is wrapped. Returns a value copy.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public BlockData GetBlockDirect(KPoint p)
        {
            return Blocks[(p.x & Settings.Engine.CHUNK_MASK) | ((p.y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2)];
        }

        /// <summary>
        /// Gets the block at the given x/y (world pos is wrapped). Returns a value copy.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public BlockData GetBlockDirect(int x, int y)
        {
            return Blocks[(x & Settings.Engine.CHUNK_MASK) | ((y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2)];
        }

        /// <summary>
        /// Sets the block at the given index of the block storage. Returns a value copy
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BlockData GetBlockDirect(int index)
        {
            return Blocks[index & (Settings.Engine.CHUNK_MASK | (Settings.Engine.CHUNK_SIZE << Settings.Engine.CHUNK_SIZE_PO2))];
        }

        /// <summary>
        /// Sets the data in the block storage using the given BlockData parcel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="b"></param>
        public void SetBlockDirect(int x, int y, BlockData b)
        {
            Blocks[(x & Settings.Engine.CHUNK_MASK) | ((y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2)] = b;
        }
        /// <summary>
        /// Sets the data in the block storage using the given BlockData parcel
        /// </summary>
        /// <param name="p"></param>
        /// <param name="b"></param>
        public void SetBlockDirect(KPoint p, BlockData b)
        {
            Blocks[(p.x & Settings.Engine.CHUNK_MASK) | ((p.y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2)] = b;
        }

        /// <summary>
        /// Updates the branch index in the underlying block storage.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public void SetBranchIndex(KPoint p, int n)
        {
            Blocks[(p.x & Settings.Engine.CHUNK_MASK) | ((p.y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2)].branchindex = n;
        }


        /// <summary>
        /// Updates the branch index in the underlying block storage.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public void SetData(KPoint p, uint data)
        {
            Blocks[(p.x & Settings.Engine.CHUNK_MASK) | ((p.y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2)].data = data;
        }

        /// <summary>
        /// Updates the branch index in the underlying block storage.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public void OrData(KPoint p, uint data)
        {
            Blocks[(p.x & Settings.Engine.CHUNK_MASK) | ((p.y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2)].data |= data;
        }


        /// <summary>
        /// Updates the branch index in the underlying block storage.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public void SetBranchAndData(KPoint p, int branch, uint data)
        {
            int i = (p.x & Settings.Engine.CHUNK_MASK) | ((p.y & Settings.Engine.CHUNK_MASK) << Settings.Engine.CHUNK_SIZE_PO2);
            Blocks[i].data = data;
            Blocks[i].branchindex = branch;
        }

        /// <summary>
        /// Casts this blocks in this chunk to an array of bytes
        /// </summary>
        /// <returns></returns>
        public override byte[] ToBytes()
        {
            Span<byte> s = MemoryMarshal.Cast<BlockData, byte>(Blocks);
            return s.ToArray();
        }

        /// <summary>
        /// Populates the blocks in this chunk from an array of bytes
        /// </summary>
        /// <param name="b"></param>
        public void FromBytes(byte[] b)
        {
            var vectorSpan = MemoryMarshal.Cast<byte, BlockData>(b);
            Blocks = vectorSpan.ToArray();

        }

        #region DrawCaching

        /// <summary>
        /// the buffers in this chunk
        /// </summary>
        public Dictionary<int, (VertexBufferWrapper vb, double tickrate)> buffers = new();

        ///After branches have been finalized, the worker threads start working
        ///1. The chunk rebuilders are constantly looking for dirty branches
        ///2. But they are excluded during the update tick
        ///3. When a dirty branch is found, we take every chunk touched by the branch and add it to the rebuild queue
        ///4. The rebuilder rebuilds any chunk added to the rebuild queue
        ///5. The renderer waits until the viewable chunks are rebuilt


        public int IsQueued = 0;
        public int AwaitingFinalization = 0;

        public void Flag(int layer = -1)
        {
            world.LayerManager.FlagChunk(this, layer);
        }

        public static void RebuildTerrainCache(World w, Chunk c)
        {

        }

        public static bool RenderTerrainCache(World w, Chunk c, BasicEffect e)
        {
            return true;
        }

        public static void RebuildDecoratorCache(World w, Chunk c)
        {

        }

        public static bool RenderDecoratorCache(World w, Chunk c, BasicEffect e)
        {
            return true;
        }


        ///Rebuilds all of the buffers (needs to be redone as rebuilding terrain and graphable buffers)
        public static void RebuildConveyorBuffers(World w, Chunk c)
        {
            //Clear all of the buffers
            foreach(var k in c.buffers.Keys)
            {
                if (c.buffers[k].vb != null) c.buffers[k].vb.Dispose();
            }
            c.buffers.Clear();

            //recache the chunk
            //and then mark it as having been dequeued
            for (int _x = 0; _x < Settings.Engine.CHUNK_SIZE; ++_x)
            {
                for (int _y = 0; _y < Settings.Engine.CHUNK_SIZE; ++_y)
                {
                    //regenerate the vertex caches from the blocks
                    int p = _x | (_y << Settings.Engine.CHUNK_SIZE_PO2);
                    BlockData d = c.Blocks[p];

                    int x = c.Location.x * Settings.Engine.CHUNK_SIZE + _x;
                    int y = c.Location.y * Settings.Engine.CHUNK_SIZE + _y;

                    //now we're going to do lots of conveyor drawer stuff
                    //first let's make sure we have the correct buffer
                    if (d.block is BlockConveyor b)
                    {
                        d.branch(w).IsInViewCache = true;

                        //figure out which one idfk
                        int index = SpriteManager.SpriteIndexMapping["conveyor0"];
                        if (!c.buffers.ContainsKey(index))
                        {
                           c. buffers.Add(index, (new VertexBufferWrapper(ConveyorSpriteManager.Size), (float)b.RenderWorkRate()));
                        }
                        var val = c.buffers[index];
                        //now we just do the thing
                        val.vb.AddTile(x << 3, y << 3, ConveyorSpriteManager.GetUV(index, d.dir, (int)d.data), Branch.MakeColor(d.branchindex), false, false);
                    }
                }
            }
            Interlocked.CompareExchange(ref c.AwaitingFinalization, 1, 0);
        }

        public static bool RenderConveyorBuffer(World w, Chunk c, BasicEffect e)
        {
            //Iterate over each conveyor layer
            foreach(var k in c.buffers)
            {
                if (k.Value.vb.tileCount <= 0) continue;
                if (c.AwaitingFinalization > 0) k.Value.vb.ConstructBuffer();

                int frame = Core.Instance.myCam.ZoomCurrent < 1 ? 0 : (int)(w.Tick / (k.Value.tickrate / 8f)) & 0x3;
                e.Texture = ConveyorSpriteManager.Frames[frame];
                Core.Instance.GraphicsDevice.SetVertexBuffer(k.Value.vb._buffer);
                Core.Instance.GraphicsDevice.Indices = k.Value.vb._indices;
                foreach (var p in e.CurrentTechnique.Passes)
                {
                    p.Apply();
                    Core.Instance.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, k.Value.vb.tileCount * 2);
                }
            }
            //Returning true indicates that we have finished rendering
            return true;
            //A return of "false" indicates that something is borkered
        }

        #endregion




    }
}
