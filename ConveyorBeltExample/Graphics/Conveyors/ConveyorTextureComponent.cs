using ConveyorBeltExample.Graphics;
using ConveyorBeltExample;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ConveyorEngine.Graphics.Conveyors
{

    /// <summary>
    /// Soooooo the conveyor textures are very minimal for artistic simplicity.
    /// <para>However, they are used to construct a complicated, animated, multidirectional conveyor.</para>
    /// <para>It's quite expensive to build this on the fly, so we instead build and cache every frame here.</para>
    /// </summary>
    public class ConveyorTextureComponent
    {
        //the buffer is actually quite interesting, as we have 4 of them, one per frame
        //so we don't have to update any UV masks
        public RenderTarget2D[] buffers;
        internal int sprite = -1;

        /// <summary>
        /// Cache all the parts
        /// </summary>
        public static PointRectangle[][][][] ConveyorParts;

        /// <summary>
        /// Generate the texture buffer.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="texture"></param>
        /// <param name="ConveyorBounds"></param>
        public void GenerateBuffer(SpriteBatch sb, Rectangle texture, PointRectangle[][][][] ConveyorBounds)
        {
            //1. Prepare the buffer sizing, we need to calculate the sizing for the buffer

            buffers = new RenderTarget2D[4];
            for (int frame = 0; frame < 4; frame++)
            {
                buffers[frame] = new RenderTarget2D(sb.GraphicsDevice, 64, 64,
                                            false, SurfaceFormat.Color,
                                            DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
                sb.GraphicsDevice.SetRenderTarget(buffers[frame]);
                //remember we will draw all of this shit
                //and then draw the corners also ugh
                //so we have a collection of corners that needs to be thingimied
                List<Corner> corners = new List<Corner>();
                Dictionary<int, PointRectangle> uniques = new Dictionary<int, PointRectangle>();
                sb.Begin();
                for (int dir = 0; dir < ConveyorBounds.Length; dir++)
                {
                    for (int mask = 0; mask < ConveyorBounds[dir].Length; mask++)
                    {
                        int posX = (dir | (mask << 2)) & 7;
                        int posY = (dir | (mask << 2)) / 8;
                        //the x/y/z is equal to 4x16x4 
                        if (IsMaskableCorner((Dir)dir, mask))
                        {
                            corners.Add(new Corner(posX, posY, (Dir)dir, mask, frame, -1));
                        }

                        for (int part = 0; part < ConveyorBounds[dir][mask][frame].Length; part++)
                        {
                            var r = ConveyorBounds[dir][mask][frame][part];
                            //draw the thing now
                            sb.Draw(SpriteManager.TexturePage,
                                new Rectangle(r.P.X + (posX << 3), r.P.Y + (posY << 3), r.R.Width, r.R.Height),
                                new Rectangle(texture.X + r.R.X, texture.Y + r.R.Y, r.R.Width, r.R.Height),
                                Color.White);
                        }
                    }
                }
                sb.End();
                //now draw the corners onto the texture
                var corner_tex = new RenderTarget2D(sb.GraphicsDevice, 64, 64);
                sb.GraphicsDevice.SetRenderTarget(corner_tex);
                sb.GraphicsDevice.Clear(Color.FromNonPremultiplied(0, 0, 0, 0));
                sb.Begin();

                //Corners are simple enough. We found them all before,
                //But they have two animated components :O
                //To solve this, we will stencil half of one direction over the other direction.
                foreach (var corner in corners)
                {
                    //
                    var rr = GetMaskedTrack(corner.d, corner.mask, corner.frame);
                    foreach (var r in rr)
                    {
                        sb.Draw(SpriteManager.TexturePage,
                            new Rectangle(corner.x * 8 + r.P.X, corner.y * 8 + r.P.Y, r.R.Width, r.R.Height),
                            new Rectangle(texture.X + r.R.X, texture.Y + r.R.Y, r.R.Width, r.R.Height),
                            Color.White);
                        continue;
                    }
                }
                sb.End();

                //Now we go back and stamp out half of it
                var mask_tex = new RenderTarget2D(sb.GraphicsDevice, 64, 64);
                sb.GraphicsDevice.SetRenderTarget(mask_tex);
                sb.GraphicsDevice.Clear(Color.FromNonPremultiplied(0, 0, 0, 0));
                sb.Begin();

                foreach (var corner in corners)
                {
                    //draw it
                    var me = GetMaskingEffect(corner.d, corner.mask);
                    sb.Draw(texture: SpriteManager.TexturePage,
                                destinationRectangle: new Rectangle(corner.x * 8, corner.y * 8, 8, 8),
                                sourceRectangle: new Rectangle(texture.X, texture.Y + 8, 8, 8),
                                origin: new Vector2(0f, 0f),
                                effects: me ?? SpriteEffects.None,
                                color: Color.White,
                                rotation: 0f,
                                layerDepth: 0f
                                );
                }
                sb.End();

                //now we can take the stencil from before and simply splat it over the top
                sb.GraphicsDevice.SetRenderTarget(buffers[frame]);
                ConveyorHelper.MaskVal.SetValue(mask_tex);
                sb.Begin(effect: ConveyorHelper.ConveyorMask);
                sb.Draw(
                    corner_tex,
                    new Rectangle(0, 0, 64, 64),
                    new Rectangle(0, 0, 64, 64),
                    Color.White);
                sb.End();

                //and reset stuff
                Core.Instance.GraphicsDevice.SetRenderTarget(Core.GetTarget());
                corner_tex.Dispose();
                mask_tex.Dispose();
            }
        }

        /// <summary>
        /// Determines if the given tile is a corner that requires masking.
        /// <para>Remember, the mask describes directions which have an inputting belt</para>
        /// <para>But a corner only thingies when it has a single side input</para>
        /// </summary>
        /// <param name="d"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static bool IsMaskableCorner(Dir d, int mask)
        {
            int cw_i = 1 << ((int)d + 1 & 0x3);
            int ccw_i = 1 << ((int)d - 1 & 0x3);
            int cd_i = 1 << (int)d;
            mask |= cd_i;
            return mask == (cd_i | cw_i) || mask == (cd_i | ccw_i);
        }

        /// <summary>
        /// Generates the initial 8x8 bounds for the conveyor belts
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="mask"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static PointRectangle[] GetMaskedTrack(Dir d, int mask, int frame)
        {
            //so we pick the direction first
            //then we just fill in or block off the thing next
            //1. Render the conveyor center component first
            var l = new List<PointRectangle>();
            mask |= 1 << (int)d;
            frame = frame & 0x3;

            int cw_i = (int)d + 1 & 0x3;
            int ccw_i = (int)d - 1 & 0x3;

            //find the actual compass directions of concern
            Dir dcw = (Dir)cw_i;
            Dir dccw = (Dir)ccw_i;
            Dir target = dcw;

            //make sure we are a valid corner, and get the cornering direction
            //this provides our output
            if (mask == (1 << (int)d | 1 << cw_i))
            {
                target = dcw;
            }
            else if (mask == (1 << (int)d | 1 << ccw_i))
            {
                target = dccw;
            }
            else return null;

            //Now get the direction of the track that we need
            switch (target)
            {
                case Dir.SOUTH:
                    l.Add(new PointRectangle(new Point(1, 0), new Rectangle(1, frame, 6, 4)));
                    l.Add(new PointRectangle(new Point(1, 4), new Rectangle(1, frame, 6, 4)));
                    break;
                case Dir.NORTH:
                    l.Add(new PointRectangle(new Point(1, 0), new Rectangle(1, 3 - frame, 6, 4)));
                    l.Add(new PointRectangle(new Point(1, 4), new Rectangle(1, 3 - frame, 6, 4)));
                    break;
                case Dir.WEST:
                    l.Add(new PointRectangle(new Point(0, 1), new Rectangle(11 - frame, 1, 4, 6)));
                    l.Add(new PointRectangle(new Point(4, 1), new Rectangle(11 - frame, 1, 4, 6)));
                    break;
                case Dir.EAST:
                    l.Add(new PointRectangle(new Point(0, 1), new Rectangle(8 + frame, 1, 4, 6)));
                    l.Add(new PointRectangle(new Point(4, 1), new Rectangle(8 + frame, 1, 4, 6)));
                    break;
            }

            return l.ToArray();

        }

        /// <summary>
        /// Returns the masking texture, which we use to mask out the corner pieces
        /// </summary>
        /// <param name="d"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static SpriteEffects? GetMaskingEffect(Dir d, int mask)
        {
            //we block off the output direction
            mask |= 1 << (int)d;
            switch (d)
            {
                case Dir.NORTH:
                    if (mask == (1 << (int)Dir.NORTH | 1 << (int)Dir.EAST))
                    {
                        return SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;
                    }
                    else if (mask == (1 << (int)Dir.NORTH | 1 << (int)Dir.WEST))
                    {
                        return SpriteEffects.FlipVertically;
                    }
                    break;
                case Dir.EAST:
                    if (mask == (1 << (int)Dir.EAST | 1 << (int)Dir.NORTH))
                    {
                        return SpriteEffects.None;
                    }
                    else if (mask == (1 << (int)Dir.EAST | 1 << (int)Dir.SOUTH))
                    {
                        return SpriteEffects.FlipVertically;
                    }
                    break;
                case Dir.SOUTH:
                    if (mask == (1 << (int)Dir.SOUTH | 1 << (int)Dir.EAST))
                    {
                        return SpriteEffects.FlipHorizontally;
                    }
                    else if (mask == (1 << (int)Dir.SOUTH | 1 << (int)Dir.WEST))
                    {
                        return SpriteEffects.None;
                    }
                    break;
                case Dir.WEST:
                    if (mask == (1 << (int)Dir.WEST | 1 << (int)Dir.NORTH))
                    {
                        return SpriteEffects.FlipHorizontally;
                    }
                    else if (mask == (1 << (int)Dir.WEST | 1 << (int)Dir.SOUTH))
                    {
                        return SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;
                    }
                    break;

            }
            return null;
        }


    }





}
