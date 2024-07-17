using ConveyorBeltExample;
using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphics;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorEngine.Blocks;
using ConveyorEngine.Graphing;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static ConveyorEngine.Items.SequentialInventory;

namespace ConveyorEngine.Graphics.Conveyors
{
    [Initializable(Stage=EngineStage.Game)]
    public class ConveyorHelper
    {
        public static PointRectangle[][][][] ConveyorBounds8;
        public static ConveyorSpriteManager BeltTextureCache;
        public static Effect ConveyorMask;
        public static EffectParameter MaskVal;

        /// <summary>
        /// A map of vertex buffer caches for rendering. Maps sprite index to the relevant buffer cache.
        /// </summary>
        public static Dictionary<int, (VertexBufferWrapper vb, double tick_rate)> vertex_caches = new();

        public static void Init()
        {
            Debug.WriteLine("Initializing Conveyor Drawing...");
            ConveyorBounds8 = BuildBounds8();
            ConveyorMask = Core.Instance.Content.Load<Effect>("pixel_mask");
            MaskVal = ConveyorMask.Parameters["Mask"];
            InitItemPosCache();
            ConveyorSpriteManager.AddMember("conveyor0");
            ConveyorSpriteManager.AddMember("conveyor1");
            ConveyorSpriteManager.AddMember("conveyor2");
        }
       
        internal static void DrawConveyors(SpriteBatch sb, World world, Camera c)
        {

            var i0 = SpriteManager.SpriteMappings["choc_chip"].R;

            //Start by getting the visible region
            var view = c.VisibleArea;
            int minx = (int)c.VisibleArea.X / 8 - 1;
            int miny = (int)c.VisibleArea.Y / 8 - 1;
            int maxx = ((int)c.VisibleArea.X + (int)c.VisibleArea.Z) / 8 + 1;
            int maxy = ((int)c.VisibleArea.Y + (int)c.VisibleArea.W) / 8 + 1;

            //compute the view rectangle and check if it's contained in the cached area
            Rectangle _view = new Rectangle(minx, miny, maxx - minx, maxy - miny);

            var viewport = Core.Instance.GraphicsDevice.Viewport;
            BasicEffect be = new BasicEffect(Core.Instance.GraphicsDevice);
            be.World = Matrix.Identity;
            be.View = Matrix.Identity;
            be.VertexColorEnabled = true;
            be.LightingEnabled = false;
            be.TextureEnabled = true;
            Matrix _projection;
            Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, 0f, -1f, out _projection);
            if (Core.Instance.GraphicsDevice.UseHalfPixelOffset)
            {
                _projection.M41 += -0.5f * _projection.M11;
                _projection.M42 += -0.5f * _projection.M22;
            }
            be.Projection = c.Transform * _projection;

            for (int i = World.FlooredIntDiv(_view.Left, Settings.Engine.CHUNK_SIZE); i <= World.FlooredIntDiv(_view.Right, Settings.Engine.CHUNK_SIZE); i++)
            {
                //check every chunk touched by the branch
                for (int j = World.FlooredIntDiv(_view.Top, Settings.Engine.CHUNK_SIZE); j <= World.FlooredIntDiv(_view.Bottom, Settings.Engine.CHUNK_SIZE); j++)
                {
                    Chunk chunk = world.GetChunkFromChunkPos(new KPoint(i, j), null);
                    while (chunk.IsQueued > 0) { }
                    foreach(var k in chunk.buffers)
                    {
                        if (chunk.AwaitingFinalization > 0) k.Value.vb.ConstructBuffer();
                        int frame = c.ZoomCurrent < 1 ? 0 : (int)(world.Tick / (k.Value.tickrate / 8f)) & 0x3;
                        be.Texture = ConveyorSpriteManager.Frames[frame];
                        Core.Instance.GraphicsDevice.SetVertexBuffer(k.Value.vb._buffer);
                        Core.Instance.GraphicsDevice.Indices = k.Value.vb._indices;
                        foreach (var p in be.CurrentTechnique.Passes)
                        {
                            p.Apply();
                            Core.Instance.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, k.Value.vb.tileCount * 2);
                        }
                    }
                    chunk.AwaitingFinalization = 0;
                }
            }
            
            c.StartBatch(sb, null);

            float depthYScaler = 1f / (maxy - miny);
            float depthXScaler = depthYScaler / (maxx - minx);
            for (int j = 0; j < world.Branches._assigned; ++j)
            {
                Branch b = world.Branches._cached[j];
                if (b.IsInViewCache && b != null && b.Inventory.Size() > 0)
                {
                    var si = b.Inventory.GetSequentialItems(); 
                    
                    //go through every single item
                    int track_pos = 0;
                    //for (int g = si.Items.Count - 1; g >= 0; --g)
                    for (int g = 0; g < si.Count; ++g)
                    {
                        track_pos += si[g].gap;
                        track_pos = int.Max(1, int.Min(b.Size * Settings.Conveyors.CONVEYOR_PRECISION - 1, track_pos));

                        //now we iterate for the size of the group
                        for (int index = 0; index < si[g].Count(); ++index)
                        {
                            Item n = si[g].Peek(index);
                            if ((index & 0x1) == 0 || c.ZoomCurrent > 1)
                            {
                                int block = (b.Size - (track_pos) / Settings.Conveyors.CONVEYOR_PRECISION) - 1;
                                int step;
                                if (block >= b.Size)
                                {
                                    block = b.Size - 1;
                                    step = Settings.Conveyors.CONVEYOR_PRECISION;
                                }
                                else step = ((b.Size - block) * Settings.Conveyors.CONVEYOR_PRECISION) - track_pos;
                                if (block >= 0 && block < b.Size && step >= 0 && step <= Settings.Conveyors.CONVEYOR_PRECISION)
                                {
                                    //for b.members[block] see if it needs additional rendering (it might)
                                    //in which case add it to a list


                                    Vector2 pos;
                                    if (block == 0)
                                        pos = ComputeItemPos((Dir)n.dir, b.members[block].Direction, step);
                                    else
                                        pos = ComputeItemPos(b.members[block - 1].Direction.Opposite(), b.members[block].Direction, step);

                                    var pp = new KPoint(b.members[block].pos);
                                    pos -= new Vector2(1f, 1f);
                                    sb.Draw(
                                        texture: SpriteManager.TexturePage,
                                        position: new Vector2(pp.x * 8 + pos.X * 8f, pp.y * 8 + pos.Y * 8f),
                                        sourceRectangle: i0,
                                        color: Color.White,
                                        rotation: 0f,
                                        origin: Vector2.Zero,
                                        scale: 1f,
                                        effects: SpriteEffects.None,
                                        layerDepth: (pp.y + pos.Y - miny) * depthYScaler + (pp.x + pos.X - minx) * depthXScaler);
                                }
                            };



                            track_pos += Settings.Conveyors.ITEM_WIDTH;

                        }
                    }
                    
                }
            }

            sb.End();

        }

        public static PointRectangle[][][][] BuildBounds8()
        {
            var b = new PointRectangle[4][][][]; ;
            foreach (Dir d0 in Enum.GetValues(typeof(Dir)))
            {
                b[(int)d0] = new PointRectangle[16][][];
                for (int j = 0; j < 16; j++)
                {
                    b[(int)d0][j] = new PointRectangle[4][];
                    for (int i = 0; i < 4; i++)
                    {
                        var n = GetBounds8(d0, j, i);
                        b[(int)d0][j][i] = n;
                    }
                }
            }
            return b;
        }

        /// <summary>
        /// Gets the rects
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static PointRectangle[] GetRects8(Dir d, int mask, int frame)
        {
            return ConveyorBounds8[(int)d][mask & 0xF][frame & 0x3];
        }

        public static float[] GetMasking(Dir d, int mask)
        {
            //it only masks if d and ccw or cw are set but not both
            int cw_i = (int)d + 1 & 0x3;
            int ccw_i = (int)d - 1 & 0x3;
            mask |= 1 << (int)d;
            if (mask == (1 << (int)d | 1 << cw_i))
            {
                if (((int)d & 0x1) != 0) return [1f, 1f];
                return [0f, 1f];
            }
            else if (mask == (1 << (int)d | 1 << ccw_i))
            {
                if (((int)d & 0x1) == 0) return [0f, 1f];
                return [1f, 1f];
            }
            return null;
        }

              /// <summary>
        /// Generates the initial 8x8 bounds for the conveyor belts
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="mask"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static PointRectangle[] GetBounds8(Dir direction, int mask, int frame)
        {
            //so we pick the direction first
            //then we just fill in or block off the thing next
            //1. Render the conveyor center component first
            var l = new List<PointRectangle>();
            mask |= 1 << (int)direction;
            frame = frame & 0x3;

            switch (direction)
            {
                case Dir.NORTH:
                    l.Add(new PointRectangle(new Point(0, 0), new Rectangle(0, frame, 8, 4)));
                    l.Add(new PointRectangle(new Point(0, 4), new Rectangle(0, frame, 8, 4)));
                    break;
                case Dir.SOUTH:
                    l.Add(new PointRectangle(new Point(0, 0), new Rectangle(0, 3 - frame, 8, 4)));
                    l.Add(new PointRectangle(new Point(0, 4), new Rectangle(0, 3 - frame, 8, 4)));
                    break;
                case Dir.EAST:
                    l.Add(new PointRectangle(new Point(0, 0), new Rectangle(11 - frame, 0, 4, 8)));
                    l.Add(new PointRectangle(new Point(4, 0), new Rectangle(11 - frame, 0, 4, 8)));
                    break;
                case Dir.WEST:
                    l.Add(new PointRectangle(new Point(0, 0), new Rectangle(8 + frame, 0, 4, 8)));
                    l.Add(new PointRectangle(new Point(4, 0), new Rectangle(8 + frame, 0, 4, 8)));
                    break;
            }

            int cw_i = (int)direction + 1 & 0x3;
            int ccw_i = (int)direction - 1 & 0x3;
            int op_i = (int)direction + 2 & 0x3;

            Dir dcw = (Dir)cw_i;
            Dir dccw = (Dir)ccw_i;
            Dir dop = (Dir)op_i;

            //now check each face
            for (int i = 0; i < 4; i++)
            {
                //we block if we are cw or ccw and mask&(1<<side) == 0
                //or if we are dop and mask(1<<(side0|side1)) != 0
                bool block = i == cw_i && (mask & 1 << cw_i) == 0
                    || i == ccw_i && (mask & 1 << ccw_i) == 0
                    || i == op_i && (mask & 1 << op_i) == 0 && (mask & 1 << ccw_i) != 0 | (mask & 1 << cw_i) != 0;

                if (block)
                {
                    var d = (Dir)i;
                    switch (d)
                    {
                        case Dir.NORTH: l.Add(new PointRectangle(0, 0, 0, 7, 8, 1)); break;
                        case Dir.SOUTH: l.Add(new PointRectangle(0, 7, 0, 7, 8, 1)); break;
                        case Dir.EAST: l.Add(new PointRectangle(7, 0, 15, 0, 1, 8)); break;
                        case Dir.WEST: l.Add(new PointRectangle(0, 0, 15, 0, 1, 8)); break;
                    }
                }
                else
                {
                    //we did not block the edge
                    //so we need to draw a speck in the corner instead
                    var d = (Dir)i;

                    if (d == Dir.SOUTH) l.Add(new PointRectangle(7, 7, 0, 7, 1, 1));

                    switch (d)
                    {
                        case Dir.NORTH: l.Add(new PointRectangle(0, 0, 0, 7, 1, 1)); break;
                        case Dir.SOUTH: l.Add(new PointRectangle(7, 7, 0, 7, 1, 1)); break;
                        case Dir.EAST: l.Add(new PointRectangle(7, 0, 0, 7, 1, 1)); break;
                        case Dir.WEST: l.Add(new PointRectangle(0, 7, 0, 7, 1, 1)); break;
                    }
                }




            }


            return l.ToArray();

        }



        /// <summary>
        /// A cache mapping conveyor step values to item positions
        /// </summary>
        public static Vector2[][][] _pos_cache = null;

        /// <summary>
        /// Initialize the position caching for conveyor items
        /// </summary>
        public static void InitItemPosCache()
        {
            if (_pos_cache == null)
            {
                _pos_cache = new Vector2[4][][];
                for (int i = 0; i < 4; ++i)
                {
                    _pos_cache[i] = new Vector2[4][];
                    for (int j = 0; j < 4; ++j)
                    {
                        _pos_cache[i][j] = new Vector2[Settings.Conveyors.CONVEYOR_RENDER_PRECISION];
                        for (int k = 0; k < Settings.Conveyors.CONVEYOR_RENDER_PRECISION; ++k)
                        {
                            _pos_cache[i][j][k] = Compute((Dir)i, (Dir)j, k);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Retrieves the location of the item on the conveyor.
        /// </summary>
        /// <param name="a">Face that the item comes from</param>
        /// <param name="b">Face that the item goes to</param>
        /// <param name="progress">The progress on the conveyor in CALCULATION precision</param>
        /// <returns></returns>
        public static Vector2 ComputeItemPos(Dir a, Dir b, int progress)
        {
            var p = progress * Settings.Conveyors.CONVEYOR_RENDER_PRECISION >> Settings.Conveyors.CONVEYOR_BITSHIFT;
            //now we are definitely within the range
            return _pos_cache[(int)a][(int)b][int.Min(p, Settings.Conveyors.CONVEYOR_RENDER_PRECISION - 1)];
        }

        public static Vector2 Compute(Dir a, Dir b, int step)
        {
            float frac = (float)step / Settings.Conveyors.CONVEYOR_RENDER_PRECISION;
            //Consider the item to fall on a path comprised of two vectors from A to Origin to B
            KPoint pa = new KPoint(0, 0).Step((int)a);
            KPoint pb = new KPoint(0, 0).Step((int)b);
            Vector2 A = new Vector2(0.5f + 0.5f * pa.x, 0.5f + 0.5f * pa.y);
            Vector2 O = new Vector2(0.5f, 0.5f);
            Vector2 B = new Vector2(0.5f + 0.5f * pb.x, 0.5f + 0.5f * pb.y);
            if (frac < 0.5f) return A + (O - A) * (2 * frac);
            else return O + (B - O) * 2 * (frac - 0.5f);
        }




    






    }
}
