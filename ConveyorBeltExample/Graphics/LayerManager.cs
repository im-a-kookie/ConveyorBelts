using ConveyorBeltExample;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using ConveyorEngine.Graphics.Conveyors;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorEngine.Graphics
{
    [Finalizable(Stage = EngineStage.Game)]
    [Initializable(Stage = EngineStage.Game)]
    public class LayerManager
    {

        BasicEffect effect;

        public static int BuildingLayer = -1;
        public static int PlantLayer = -1;
        public static int TerrainLayer = -1;
        public static int ConveyorLayer = -1;
        public static int ItemLayer = -1;


        private static bool Finalized = false;

        public static List<RenderLayer> RenderLayers = [];

        public static Dictionary<string, RenderLayer> RenderLayerMap = [];

        public static int FlooredIntDiv(int a, int b)
        {
            return (a / b - Convert.ToInt32(((a < 0) ^ (b < 0)) && (a % b != 0)));
        }

        public World w;

        public KPoint view_wh;
        Matrix _projection;
        ConcurrentQueue<Chunk> immediate_chunks = new();
        ConcurrentQueue<Chunk> pending_chunks = new();

        /// <summary>
        /// Creates a new layer manager with the given camera and viewport
        /// </summary>
        /// <param name="w"></param>
        /// <param name="c"></param>
        /// <param name="v"></param>
        public LayerManager(World w, Camera c, Viewport v)
        {
            //set up the projection matrices so that it can render correct.y
            this.w = w;
            effect = new BasicEffect(Core.Instance.GraphicsDevice);
            effect.World = Matrix.Identity;
            effect.View = Matrix.Identity;
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = false;
            effect.TextureEnabled = true;

            Matrix.CreateOrthographicOffCenter(0f, v.Width, v.Height, 0f, 0f, -1f, out _projection);
            if (Core.Instance.GraphicsDevice.UseHalfPixelOffset)
            {
                _projection.M41 += -0.5f * _projection.M11;
                _projection.M42 += -0.5f * _projection.M22;
            }
            view_wh = new(v.Width, v.Height);

            effect.Projection = c.Transform * _projection;
        }

        /// <summary>
        /// Flags the given chunk to be updated at the specified layer.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="layer"></param>
        public void FlagChunk(Chunk c, int layer = -1)
        {
            if (layer == -1) layer = ConveyorLayer;
            c.dirty[layer] = true;
            if (c.IsQueued > 0) return;
            pending_chunks.Enqueue(c);
            c.IsQueued = 1;
        }

        /// <summary>
        /// Updates caches within the given view area
        /// </summary>
        /// <param name="view"></param>
        public void UpdateCaches(Rectangle view)
        {
            //1. Every dirty chunk is identified
            int count;
            lock (this)
            {
                count = pending_chunks.Count;
                for (int i = 0; i < count; ++i)
                {
                    if (pending_chunks.TryDequeue(out var c))
                    {
                        if (c.Bounds.Intersects(view)) immediate_chunks.Enqueue(c);
                        else pending_chunks.Enqueue(c);
                    }
                }
                //the count determines the number of threads we use? idk
            }


            //now we need to work through the chunks
            while(immediate_chunks.TryDequeue(out var c))
            {
                c.IsQueued = 0;
                for(int i = 0; i < c.dirty.Length; ++i)
                {
                    if (c.dirty[i])
                    {
                        RenderLayers[i].Rebuild(w, c, (null, -1));
                        c.dirty[i] = false;
                    }
                }
            }

            //now we need to work through the chunks
            while (pending_chunks.TryDequeue(out var c))
            {
                c.IsQueued = 0;
                for (int i = 0; i < c.dirty.Length; ++i)
                {
                    if (c.dirty[i])
                    {
                        RenderLayers[i].Rebuild(w, c, (null, -1));
                        c.dirty[i] = false;
                    }
                }
            }
        }

        /// <summary>
        /// Renders all of the caches in this layer to the given camera and viewport.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="c"></param>
        /// <param name="v"></param>
        public void RenderCaches(Rectangle view, Camera c, Viewport v)
        {
            Chunk[] chunks = ArrayPool<Chunk>.Shared.Rent((2 + (view.Width / Settings.Engine.CHUNK_SIZE)) * (2 + (view.Height / Settings.Engine.CHUNK_SIZE)));
            int count = 0;

            //Grab all of the chunks that fall within the current viewport
            for (int i = World.FlooredIntDiv(view.Left, Settings.Engine.CHUNK_SIZE); i <= World.FlooredIntDiv(view.Right, Settings.Engine.CHUNK_SIZE); i++)
            {
                //check every chunk touched by the branch
                for (int j = World.FlooredIntDiv(view.Top, Settings.Engine.CHUNK_SIZE); j <= World.FlooredIntDiv(view.Bottom, Settings.Engine.CHUNK_SIZE); j++)
                {

                    chunks[count++] = w.GetChunkFromChunkPos(new KPoint(i, j));
                }
            }

            //check if we need to reset the view projection on the render effect
            if (view_wh.x != v.Width || view_wh.y != v.Height)
            {
                Matrix.CreateOrthographicOffCenter(0f, v.Width, v.Height, 0f, 0f, -1f, out _projection);
                if (Core.Instance.GraphicsDevice.UseHalfPixelOffset)
                {
                    _projection.M41 += -0.5f * _projection.M11;
                    _projection.M42 += -0.5f * _projection.M22;
                }
                view_wh = new(v.Width, v.Height);
            }

            //translate it to the camera
            effect.Projection = c.Transform * _projection;


            //build a collection of the applicable branches also
            Branch[] branches = ArrayPool<Branch>.Shared.Rent(1);
            int bc = 0;
            //get the branch
            for (int i = 0; i < w.Branches._assigned; ++i)
            {
                Branch b = w.Branches._cached[i];
                b.IsInViewCache = b.Bounds.Intersects(view);// || b.Bounds.Contains(view) || view.Contains(b.Bounds);
                if (!b.WithinUpdateBounds && b.IsInViewCache) b.ticks_left = 1;
                b.WithinUpdateBounds = b.IsInViewCache;

                //flag it to be updated
                if (b != null && b.IsInViewCache && b.RendersItems && b.ItemCount > 0)
                {
                    if (bc >= branches.Length)
                    {
                        Branch[] _n = ArrayPool<Branch>.Shared.Rent((3 * bc) / 2);
                        Array.Copy(branches, _n, branches.Length);
                        ArrayPool<Branch>.Shared.Return(branches);
                        branches = _n;
                    }
                    branches[bc++] = b;
                }
            }

            //now go through and draw each layer
            for (int layer = 0; layer < RenderLayers.Count; ++layer)
            {

                if (RenderLayers[layer] is RenderBranchLayer)
                {
                    
                    RenderLayers[layer].Rebuild(w, null, (branches, bc));
                    RenderLayers[layer].Draw(w, null, (branches, bc), effect);
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        bool b = RenderLayers[layer].Draw(w, chunks[i], (null, -1), effect);
                        if (!b)
                        {
                            //... should never happen

                        }
                    }
                }
            }

            ArrayPool<Chunk>.Shared.Return(chunks);

        }

        public static void Init() 
        {
            Debug.WriteLine("Initializing Render Manager...");

            //First we need to render the LOD
            //which is comprised of several textures
            //The first is simply a single texture that renders the entire map at a resolution of 1 pixel per chunk
            //then 2x2 pixels per chunk
            //then 4x4 pixels
            //and so on
            //This requires every single chunk for the world to be generated
            //but doesn't require all of the chunks to be populated at this time???
            //RegisterLayer("lod", 10, Chunk.RebuildTerrainCache, Chunk.RenderTerrainCache);


            //First we render all of the terrain
            RegisterLayer("terrain", 10, Chunk.RebuildTerrainCache, Chunk.RenderTerrainCache);

            //Then we render harvestables
            RegisterLayer("plants", 20, Chunk.RebuildDecoratorCache, Chunk.RenderDecoratorCache);

            //Then we render conveyors and their items
            RegisterLayer("conveyors", 30, Chunk.RebuildConveyorBuffers, Chunk.RenderConveyorBuffer);
            //and the items
            RegisterLayer("items", 35, SequentialItemRender.RebuildConveyorCaches, SequentialItemRender.RenderConveyorItems);

            //then buildings
            //RegisterLayer("static tiles", 5, Chunk.RebuildConveyorBuffers);

            //calls chunks to rebuild conveyor buffers
            //RegisterLayer("conveyors", 10, Chunk.RebuildConveyorBuffers);

            //Items
            //RegisterLayer("items", 11, Chunk.RebuildConveyorBuffers);

            //Building layers
            //RegisterLayer("buildings", 12, Chunk.RebuildConveyorBuffers);

        }

        /// <summary>
        /// Registers a render layer. All chunks are cached, but only visible chunks will Render.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <param name="callback"></param>
        /// <exception cref="Exception"></exception>
        public static void RegisterLayer(string name, int priority, Action<World, Chunk> rebuild_caches, Func<World, Chunk, BasicEffect, bool> render)
        {
            if (Finalized) throw new Exception("Cannot access finalized resource");
            if (RenderLayerMap.ContainsKey(name)) throw new Exception("Render Layer: " + name + " is already registered!");
            RenderLayerMap.Add(name, new RenderChunkLayer(name, priority, rebuild_caches, render));

        }

        /// <summary>
        /// Registers a branch render layer. This is typically only used for branches in the current view.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <param name="callback"></param>
        /// <exception cref="Exception"></exception>
        public static void RegisterLayer(string name, int priority, Action<World, (Branch[] branches, int count)> rebuild_caches, Func<World, (Branch[] branches, int count), BasicEffect, bool> render)
        {
            if (Finalized) throw new Exception("Cannot access finalized resource");
            if (RenderLayerMap.ContainsKey(name)) throw new Exception("Render Layer: " + name + " is already registered!");
            RenderLayerMap.Add(name, new RenderBranchLayer(name, priority, rebuild_caches, render));

        }

        /// <summary>
        /// Finalizes and builds the render layer manager class
        /// </summary>
        public static void FinalizeObject()
        {
            foreach(var k in RenderLayerMap)
            {
                bool added = false;
                for(int i = 0; i < RenderLayers.Count; i++)
                {
                    if (RenderLayers[i].Priority > k.Value.Priority)
                    {
                        RenderLayers.Insert(i, k.Value);
                        added = true;
                        break;
                    }
                }
                if (!added) RenderLayers.Add(k.Value);
            }
            for(int i = 0; i < RenderLayers.Count; i++)
            {
                RenderLayers[i].Index = i;
            }

            //BuildingLayer = RenderLayerMap["buildings"].Index;
            PlantLayer = RenderLayerMap["plants"].Index;
            TerrainLayer = RenderLayerMap["terrain"].Index;
            ConveyorLayer = RenderLayerMap["conveyors"].Index;
            ItemLayer = RenderLayerMap["items"].Index;





            Finalized = true;

        }








    }

}
