using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.Graphics;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorEngine;
using ConveyorEngine.Blocks;
using ConveyorEngine.Graphics;
using ConveyorEngine.Graphics.Conveyors;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConveyorBeltExample.GameWorld
{
    /// <summary>
    /// The world wrapper. Worlds are indexed into the world manager.
    /// </summary>
    public class World
    {

        //this can be done with a chunked array
        //of say 16x16 = 256 chunks
        //If we travel 130K blocks (2^17) then we have 512 x 16 = 8192 chunks loaded
        //where one chunk is 128x128x16-ish bytes or 256kb
        //which is no bueno hey
        //since like loading 1000 chunks will mean 256mb data...
        //So we probably need to chunk the block storage
        /// <summary>
        /// The chunk array
        /// </summary>
        public ConcurrentDictionary<KPoint, Chunk> LoadedChunks = new ConcurrentDictionary<KPoint, Chunk>();

        /// <summary>
        /// A collection of dirty chunks based on the applicable render layer
        /// </summary>
        public List<ConcurrentQueue<Chunk>> Dirty = new();

        /// <summary>
        /// The branches in this pool thing
        /// </summary>
        internal IndexedPool<Branch> Branches = new IndexedPool<Branch>();

        /// <summary>
        /// The inventories in this world thing
        /// </summary>
        internal IndexedPool<Inventory> Inventories = new IndexedPool<Inventory>();

        public BranchUpdater BranchUpdater;

        /// <summary>
        /// Probably not a big deal idk
        /// </summary>
        public WorldDataLayer DataManager;

        public LayerManager LayerManager;

        public List<Particle> Particles = new List<Particle>();


        /// <summary>
        /// The name of the dimension
        /// </summary>
        public string Dimension = "main";
        public int WorldID = -1;

        public double lifetime = 0d;
        public long Tick = 0L;
        public bool BlockChanged = false;

        /// <summary>
        /// The bounds of the world. This is stored in world coordinates which are then wrapped to a 1<<24 area
        /// </summary>
        public Rectangle Bounds;

        public KPoint Origin;

        public World (string name, short id)
        {
            Dimension = name;
            Index = id;

            LayerManager = new(this, Core.Instance.myCam, Core.Instance.GraphicsDevice.Viewport);

            var x = (id >> 8) - 128;
            var y = (id & 0xFF) - 128;
            //Compute the world bounds
            //remember the origin of the world is at Bounds-X
            Bounds = new Rectangle(
                x << (1 + Settings.Engine.WORLD_SIZE_PO2), 
                y << (1 + Settings.Engine.WORLD_SIZE_PO2), 
                Settings.Engine.WORLD_SIZE, 
                Settings.Engine.WORLD_SIZE
            );
            Origin = new KPoint(
                Bounds.X + Settings.Engine.WORLD_SIZE >> 1, 
                Bounds.Y + Settings.Engine.WORLD_SIZE >> 1
            );

            BranchUpdater = new BranchUpdater() { owner = this };

        }

        /// <summary>
        /// The path to this world storage
        /// </summary>
        public string WorldPath
        {
            get
            {
                string s = DataManager.WorldPath;
                s += "Dimension\\";
                Directory.CreateDirectory(s);
                return s;
            }
        }

        public int Index { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public World(string dimensionName)
        {
            this.Dimension = dimensionName;
            BranchUpdater = new BranchUpdater() { owner = this };
            LayerManager = new(this, Core.Instance.myCam, Core.Instance.GraphicsDevice.Viewport);
        }

        /// <summary>
        /// Gets the chunk at the given location in the chunk array. Threadsafe. If "force" is true,
        /// then the chunk will be created if it is not currently loaded.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="forceLoad"></param>
        /// <returns></returns>
        public Chunk GetChunkFromChunkPos(KPoint point, Chunk _cache = null, bool forceLoad = true)
        {
            if (_cache != null && _cache.Location == point) return _cache;

            Chunk c = null;
            if (LoadedChunks.TryGetValue(point, out c))
            {
                return c;
            }
            if (LoadedChunks.TryAdd(point, null))
            {
                c = new Chunk(this, point);
                if (!LoadedChunks.TryUpdate(point, c, null)) 
                    LoadedChunks.TryGetValue(point, out c);
                {
                    return c;
                }
            }
            else
            {
                while (!LoadedChunks.TryGetValue(point, out c) || c == null) { }
                return c;
            }
        }

        
        /// <summary>
        /// Gets the chunk from the given position in the world
        /// </summary>
        /// <param name="point"></param>
        /// <param name="forceLoad"></param>
        /// <returns></returns>
        public Chunk GetChunkFromWorldCoords(KPoint point, Chunk c = null, bool forceLoad = true)
        {
            return GetChunkFromChunkPos(new KPoint(point.x >> Settings.Engine.CHUNK_SIZE_PO2, point.y >> Settings.Engine.CHUNK_SIZE_PO2), c, forceLoad);
        }

        public Chunk GetChunkFromWorldCoords(ulong index, Chunk c = null, bool forceLoad = true)
        {
            return GetChunkFromChunkPos(new KPoint((int)index >> Settings.Engine.CHUNK_SIZE_PO2, (int)(index >> (32 + Settings.Engine.CHUNK_SIZE_PO2))), c, forceLoad);
        }

        
        public BlockData GetBlock(KPoint p, Chunk cache = null)
        {
            Chunk c = GetChunkFromWorldCoords(p, cache);
            if (c == null) return default;
            BlockData b = c.GetBlockDirect(p);
            //BlockPart always refers us back to the parent
            if (b.id == 1) return GetBlock(p - b.GetPoint(), cache);
            return b;
        }


        /// <summary>
        /// Deletes every part of a given block, directly in raw data. Use with caution.
        /// </summary>
        /// <param name="def"></param>
        /// <param name="p"></param>
        /// <param name="chunk"></param>
        public void ClearBlockPartsUnsafe(BlockDefinition def, KPoint p, Chunk chunk = null)
        {
            //now set all of our "part" blocks
            for (int i = -def.Bounds.X; i < p.x + def.Bounds.Width; ++i)
            {
                for (int j = -def.Bounds.Y; j < p.y + def.Bounds.Height; ++j)
                {
                    //set it as a block part
                    Chunk c = GetChunkFromWorldCoords(p, chunk);
                    c.SetBlockDirect(p, BlockData.EMPTY);
                }
            }
        }

        /// <summary>
        /// Safely breaks every component part of a given block and notifies appropriately.
        /// </summary>
        /// <param name="def"></param>
        /// <param name="p"></param>
        /// <param name="chunk"></param>
        public void ClearBlockPartsSafe(BlockDefinition def, KPoint p, Chunk chunk = null)
        {
            //now set all of our "part" blocks
            for (int i = -def.Bounds.X; i < p.x + def.Bounds.Width; ++i)
            {
                for (int j = -def.Bounds.Y; j < p.y + def.Bounds.Height; ++j)
                {
                    //set it as a block part
                    BreakBlock(p, false, chunk);
                }
            }
        }


        public void SetBlockPartsUnsafe(BlockDefinition def, KPoint p, Chunk chunk = null)
        {
            //now set all of our "part" blocks
            for (int i = -def.Bounds.X; i < p.x + def.Bounds.Width; ++i)
            {
                for (int j = -def.Bounds.Y; j < p.y + def.Bounds.Height; ++j)
                {
                    if (i != p.x && j != p.y)
                    {
                        //set it as a block part
                        Chunk c = GetChunkFromWorldCoords(p, chunk);
                        c.SetBlockDirect(p, new BlockData(1, Dir.NORTH, 0, new KIntPoint(i - p.x, j - p.y).val));
                    }
                }
            }
        }


        /// <summary>
        /// Sets the given block into the world immediately.
        /// </summary>
        /// <param name="newData"></param>
        public Chunk SetBlockImmediate(KPoint p, BlockData newData, Chunk cache = null)
        {

            Chunk c = GetChunkFromWorldCoords(p, cache);
            if (c == null) return null;

            

            BlockData oldData = c.GetBlockDirect(p);

            if (newData.id == oldData.id && newData.dir == oldData.dir) 
                return c;

            var newDef = newData.block;
            if (newData.id > 0) c.dirty[newDef.GetLayer()] = true;

            //flag the chunk to rebuild the vertex cache for the block being placed

            /*
            There are various cases
            1. If the block here is a perfectly overlapping Graphed Block and we are a Graphed Block
                    We replace it and finish
                    
            2. Otherwise, we remove every block underneath us then place. It's annoyering.
               But we can chain (2) to (1)

            It gets messy because multiblocks but I think it's okay
            */
            if (oldData.id > 1)
            {
                var oldDef = oldData.block;
                c.dirty[oldDef.GetLayer()] = true;
                if (oldDef is BlockGraphed og)
                {
                    if (newDef is BlockGraphed ng)
                    {
                        if (oldDef.Bounds == ng.Bounds)
                        {
                            //erase it from here
                            ClearBlockPartsUnsafe(oldDef, p, c);
                            //and now put the new blocks in the correct place
                            SetBlockPartsUnsafe(newDef, p, c);
                            c.SetBlockDirect(p, newData);
                            ng.UpdatePlacementImmediate(this, c, p, newData, oldData, og);
                            //if we actually changed the block, then call the on remove etc stuff
                            if (oldDef != newDef)
                            {
                                oldDef.OnRemove(this, c, p, oldData, true);
                                newDef.OnSet(this, c, p, newData);
                            }

                            return c;
                        }
                    }
                }
            }

            //now break everything underneath us
            ClearBlockPartsSafe(newDef, p, c);
            SetBlockPartsUnsafe(newDef, p, c);
            c.SetBlockDirect(p, newData);
            c.dirty[newData.block.GetLayer()] = true;

            if (newDef is BlockGraphed newGraph)
            {
                newGraph.UpdatePlacementImmediate(this, c, p, newData, oldData, null);
            }

            newDef.OnSet(this, c, p, newData);

            return c;
        }

        /// <summary>
        /// Breaks a block.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="suppressBlockLogic"></param>
        public void BreakBlock(KPoint p, bool suppressBlockLogic = false, Chunk cache = null)
        {
            //Get the chunk for the thing
            Chunk c = GetChunkFromWorldCoords(p, cache);

            if (suppressBlockLogic)
            {
                c.SetBlockDirect(p, BlockData.EMPTY);
                return;
            }

            var b = c.GetBlockDirect(p);
            BlockDefinition bdef;

            if (b.id == 1)
            {
                //move to the home pos
                p = p - b.Point;
                b = GetBlock(p, c);
            }

            if (b.id > 0)
            {
                bdef = b.block;
                c.dirty[bdef.GetLayer()] = true;
                ClearBlockPartsSafe(bdef, p, c);
                if (bdef is BlockGraphed graph)
                    graph.Destroyed_NoReplace(this, c, p, b);
                bdef.OnRemove(this, c, p, b, true);

            }


        }

        long timer = 0;

        /// <summary>
        /// A deferred queue of block updates to perform later (ideally, next frame/update).
        /// </summary>
        public ConcurrentQueue<(KPoint p, BlockData d, Chunk c)> _deferred = new ConcurrentQueue<(KPoint, BlockData, Chunk)>();
        /// <summary>
        /// Sets a block after the next/current update tick has completed its cycle.
        /// <para>This is generally favorable for performance, and should be used wherever practical.</para>
        /// </summary>
        /// <param name="p"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public void SetBlockDeferred(KPoint p, BlockData b, Chunk c = null, bool updates = true)
        {
            //flag neighbor updates
            if(updates) MarkNeighborUpdates(p, c);
            _deferred.Enqueue((p, b, c));
        }

        /// <summary>
        /// This tells neighbors to update themselves. 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="c"></param>
        public void MarkNeighborUpdates(KPoint pos, Chunk c = null)
        {
            for(int i = 0; i < 4; i++)
            {
                var p = pos.Step(i);
                var _c = GetChunkFromWorldCoords(pos, c);
                var d = _c.GetBlockDirect(p);
                if (d.branchindex > 0) BranchUpdater.FlagUpdate(Branches[d.branchindex]);
            }
        }


        static volatile int UrgentChunkRebuilds = 0;
        static volatile int LazyChunkRebuilds = 0;

        static bool WaitRebuild = false;
        static ConcurrentQueue<Chunk> RebuildImmediate = new();
        static ConcurrentQueue<Chunk> RebuildLazy = new();
        static TaskChannel tc = new TaskChannel(4);

        public static int FlooredIntDiv(int a, int b)
        {
            return (a / b - Convert.ToInt32(((a < 0) ^ (b < 0)) && (a % b != 0)));
        }

        int slowcounter = 0;
        int slowfactor = 1;

        public void Update(GameTime t)
        {
            //check the caches
            if (Branches.IsDirty) Branches.RebuildCache();

            //the tick increases based on elapsed time
            //lifetime += t.ElapsedGameTime.TotalSeconds;
            //long _tick = Tick;
            //Tick = (long)(lifetime * Settings.Engine.TICK_RATE);
            slowcounter += 1;
            if (slowcounter >= slowfactor)
            {
                slowcounter -= slowfactor;
                Tick += 1;
                BranchUpdater.Update(Tick);

            }

            //Complete deferred block changes
            while (_deferred.TryDequeue(out var result))
            {
                SetBlockImmediate(result.p, result.d, result.c);
            }


            //now get the visible area of the world
            //this is given in grid coordinates
            var view = new Rectangle(
                Core.Instance.myCam.VisibleInt.X / 8, Core.Instance.myCam.VisibleInt.Y / 8, 
                Core.Instance.myCam.VisibleInt.Width / 8, Core.Instance.myCam.VisibleInt.Height / 8);
            view.Inflate(16, 16);

            //now we need to rebuild the caches, which can be multithreaded because yes

            //Check if we need to rebuild the branch cache
            if (Branches.IsDirty)
            {
                Branches.RebuildCache();
            }

            //we need to check every branch in the cache
            //whenever blocks are changed
            //which is kinda annoying but... yeah
            LayerManager.UpdateCaches(view);



        }

        public void Draw(GameTime t, Core c)
        {

            //now get the visible area of the world
            //this is given in grid coordinates
            var view = new Rectangle(
                Core.Instance.myCam.VisibleInt.X / 8, Core.Instance.myCam.VisibleInt.Y / 8,
                Core.Instance.myCam.VisibleInt.Width / 8, Core.Instance.myCam.VisibleInt.Height / 8);
            view.Inflate(16, 16);

            LayerManager.RenderCaches(view, c.myCam, c.GraphicsDevice.Viewport);


            //ConveyorHelper.DrawConveyors(c._spriteBatch, this, c.myCam);

            c.myCam.StartBatch(Core.Instance._spriteBatch, null, false);

            Core.Instance._spriteBatch.Draw(SpriteManager.TexturePage, new Rectangle(-266, -256, SpriteManager.TexturePage.Width, SpriteManager.TexturePage.Height), SpriteManager.TexturePage.Bounds, Color.White);



            //this is cool just use world tick over something
            int f = (int)((Tick / 4) & 0b111);
            var fur_chim = SpriteManager.SpriteMappings["furnace_chimney"];
            var fur_body = SpriteManager.SpriteMappings["furnace"];
            Core.Instance._spriteBatch.Draw(SpriteManager.TexturePage, new Rectangle(-80, -80, 16, 16), fur_body[1].R, Color.White);

            Core.Instance._spriteBatch.Draw(SpriteManager.TexturePage, new Rectangle(-80, -82, 16, 16), fur_chim[f].R, Color.White);
            if (f == 0) Particles.Add(new ConveyorEngine.Particle("smoke", new Vector2(-73, -80), new Vector2(-0.01f, -5f), 4));

            Core.Instance._spriteBatch.End();

            //Particles get drawn at the end
            c.myCam.StartBatch(Core.Instance._spriteBatch, null);
            for(int i = 0; i < Particles.Count; i++)
            {
                var p = Particles[i];
                p.pos += p.velocity * (float)t.ElapsedGameTime.TotalSeconds;
                p.life = float.Max(0, p.life - (float)t.ElapsedGameTime.TotalSeconds);
                var r = SpriteManager.SpritesByIndex[Particles[i].sprite];
                var rr = new Rectangle(r.X + 16 * (int)((r.Width / 16) * (0.999f - p.life / p._start_life)), r.Y, 16, 16);
                Core.Instance._spriteBatch.Draw(SpriteManager.TexturePage, Particles[i].pos - new Vector2(8, 8), rr, Color.White);
            }
            Core.Instance._spriteBatch.End();
            Particles.RemoveAll(x => x.life <= 0);
        }


    }
}
