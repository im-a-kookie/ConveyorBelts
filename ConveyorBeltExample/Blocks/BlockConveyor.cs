using ConveyorBeltExample.Graphics;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Blocks
{
    internal class BlockConveyor : BlockGraphed
    {



        private static PointRectangle[][][][] ConveyorBounds;

        private static int ProgressPerTick = (Settings.CONVEYOR_PRECISION / Settings.TICK_RATE);

        static BlockConveyor()
        {
            ConveyorBounds = new PointRectangle[4][][][];
            foreach(Dir d0 in Enum.GetValues(typeof(Dir)))
            {
                ConveyorBounds[(int)d0] = new PointRectangle[16][][];
                for(int j = 0; j < 16; j++)
                {
                    ConveyorBounds[(int)d0][j] = new PointRectangle[4][];
                    for (int i = 0; i < 4; i++)
                    {
                        var n = GetBoundsOffsets(d0, j, i);
                        ConveyorBounds[(int)d0][j][i] = n;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the rects
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static PointRectangle[] GetRects(Dir d, int mask, int frame)
        {
            return ConveyorBounds[(int)d][mask & 0xF][frame & 0x3];
        }

        public static PointRectangle[] GetBoundsOffsets(Dir direction, int mask, int frame)
        {
            //generate the bounding offsets for a conveyor in the given direction, withj the given mask
            //where mask & (1 << CDir) == 1 if that direction is set
            //and the corresponding bit for "direction" is taken as the output
            //There are 8 states
            //output to EAST/WEST with NORTH, SOUTH, BOTH, or NO inputs
            //and the 90 degree rotation
            mask |= (1 << (int)direction);
            bool vertical = (((int)direction & 0x1) == 0);

            //this is the frame for the outside of the thing

            int[] slots = new int[4];
            for (int i = 0; i < 4; i++)
            {
                slots[i] = ((mask & (1 << i)) == 0) ? 0 : 1;
            }
            slots[(int)direction] = 2;

            //So first of all
            //let's draw the main body
            //we do this if the slot opposite the direction is given
            //or if no other slots are given

            Dir d_opposite = (Dir)(((int)direction + 2) & 0x3);
            Dir d_cw = (Dir)(((int)direction + 1) & 0x3);
            Dir d_ccw = (Dir)(((int)direction - 1) & 0x3);

            int m_op = 1 << (int)d_opposite;
            int m_cw = 1 << (int)d_cw;
            int m_ccw = 1 << (int)d_ccw;

            int vn = vertical ? (direction == Dir.NORTH ? 4 + (frame & 0x3) : 4 - (frame & 0x3)) : 0;
            int hn = vertical ? 0 : (direction == Dir.EAST ? 4 - (frame & 0x3) : 4 + (frame & 0x3));

            List<PointRectangle> pointRectangles = new List<PointRectangle>();

            if ((mask & m_op) != 0 || (mask & (m_cw | m_ccw)) == 0)
            {
                pointRectangles.Add(new PointRectangle(0, 0, hn, vn, SpriteManager.SPRITE_RES_PX, SpriteManager.SPRITE_RES_PX) { flag = !vertical });
            }
            else
            {
                int _x = direction == Dir.EAST ? 1 : 0;
                int _y = direction == Dir.SOUTH ? 1 : 0;
                pointRectangles.Add(new PointRectangle(
                    4 * _x,4 * _y,
                    hn + 4 * _x, vn + 4 * _y, vertical ? 16 : 12, vertical ? 12 : 16)
                { flag = !vertical });

                pointRectangles.Add(
                    new PointRectangle(
                    vertical ? 4 : 11 - 7 * _x, vertical ? 11 - 7 * _y : 4,
                    vertical ? 4 : 31, vertical ? 31 : 4, vertical ? 8 : 1, vertical ? 1 : 8)
                    { flag = !vertical });
            }

            //now atop this we add the d_cw and d_ccw if necessary
            if ((mask & m_cw) != 0)
            {
                int dx = 0, dy = 0;
                switch (d_cw)
                {
                    case Dir.NORTH: dx = 0; dy = 0; hn = 0; vn = 4 - (frame & 0x3); break;
                    case Dir.EAST: dx = 11; dy = 0; vn = 0; hn = 7 + (frame & 0x3); break;
                    case Dir.SOUTH: dx = 0; dy = 11; vn = 0; hn = 4 + (frame & 0x3); break;
                    case Dir.WEST: dx = 0; dy = 0; vn = 0; hn = 4 - (frame & 0x3); break;
                }

                pointRectangles.Add(
                    new PointRectangle(dx, dy, hn, vn, vertical ? 5 : 16, vertical ? 16 : 5) { flag = vertical }
                    );

            }

            //now atop this we add the d_cw and d_ccw if necessary
            if ((mask & m_ccw) != 0)
            {
                int dx = 0, dy = 0;
                switch (d_ccw)
                {
                    case Dir.NORTH: dx = 0; dy = 0; hn = 0; vn = 4 - (frame & 0x3); break;
                    case Dir.EAST: dx = 11; dy = 0; vn = 0; hn = 4 + (frame & 0x3); break;
                    case Dir.SOUTH: dx = 0; dy = 11; vn = 0; hn = 4 + (frame & 0x3); break;
                    case Dir.WEST: dx = 0; dy = 0; vn = 0; hn = 4 - (frame & 0x3); break;
                }

                pointRectangles.Add(
                    new PointRectangle(dx, dy, hn, vn, vertical ? 5 : 16, vertical ? 16 : 5) { flag = vertical }
                    );

            }

            return pointRectangles.ToArray();
        }


        public override int UpdateTick(int time)
        {
            int delta_t = (time - branch.TailTime) & Settings.TICK_MASK;
            if (delta_t > (Settings.TICK_RATE * 60 * 60)) return time;
            if (delta_t == 0) return time;

            //this is the length of the conveyor stack
            int max_distance = branch.Size * Settings.CONVEYOR_PRECISION;

            if (branch.items is ConveyorInventory ci)
            {

                //1. See if we had any promises consumed
                int promises_consumed = 0;
                for(int i = 0; i < branch.promises_out.Count; i++)
                {
                    if (branch.promises_out[i].time == -1) promises_consumed += 1;
                    else break;
                }

                //consume everything up to here then delete the next index
                for(int i = 0; i < ci.Items.Count && promises_consumed > 0; i++)
                {
                    int n = int.Min(ci.Items[i].Length, promises_consumed);
                    promises_consumed -= n;
                    if (ci.Items[i].Length - n <= 0)
                    {
                        Item[] _new = new Item[ci.Items[i].Length - n];
                        Array.Copy(ci.Items[i], n, _new, 0, n);
                        ci.Items[i] = _new;
                        ci.progresses[i] += n * Settings.CONVEYOR_SPACING;
                    }
                    else
                    {
                        ci.Items.RemoveAt(i--);
                    }
                }
                
                //halt events are signalled by promise rejection
                //when a promise is rejected, the first item is locked to the end of the conveyor
                //everything behind it is then moved forwards
                //and we stop updating until we receive a new update
                if (branch.promises_out.Count > 0 && branch.promises_out[0].time == -2)
                {
                    branch.promises_out.RemoveAt(0);
                    if (ci.Items.Count > 0)
                    {
                        //we tick this to the end of the conveyor
                        ci.progresses[0] = max_distance;
                        ci.Items[0][0].time += 1;
                        for(int i = 1; i < ci.Items.Count; ++i)
                        {
                            int last_pos = ci.progresses[i-1] + ci.Items[i-1].Length * Settings.CONVEYOR_SPACING + Settings.CONVEYOR_SPACING;
                            //find how many ticks
                            int idelta = time - ci.Items[i][0].time;
                            int dist = idelta * ProgressPerTick;
                            ci.progresses[i] = int.Min(last_pos, ci.progresses[i] + dist);
                            if (last_pos - ci.progresses[i]  < Settings.CONVEYOR_MERGE_THRESHOLD)
                            {
                                //merge the groups
                                Item[] _new = new Item[ci.Items[i].Length + ci.Items[i - 1].Length];
                                Array.Copy(ci.Items[i - 1], 0, _new, 0, ci.Items[i - 1].Length);
                                Array.Copy(ci.Items[i], 0, _new, ci.Items[i - 1].Length, ci.Items[i].Length);
                                ci.Items[i - 1] = _new;
                                ci.Items.RemoveAt(i--);
                            }
                        }
                    }
                }


                //1. See how far forwards we can go
                if (ci.Items.Count == 0)
                {
                    branch.TailTime = time;
                }
                else
                {

                    //we can tick this item up to the last tick where it remains on the conveyor
                    int full_ticks_left = (max_distance - ci.progresses[0]) / ProgressPerTick;
                    //we consume this many ticks
                    int ticks_consume = int.Min(delta_t, full_ticks_left);
                    //so this is the best tick we can reach
                    int best_tick = (branch.TailTime + ticks_consume) & Settings.TICK_MASK;
                    //so this is the distance that the conveyor can push items ahead before a halt event
                    int new_distance = ticks_consume * ProgressPerTick;
                    for (int i = 0; i < ci.Items.Count; ++i)
                    {
                        //move it forwards as far as we can go
                        ci.progresses[i] += new_distance;
                        ci.Items[i][0].time = best_tick;
                    }

                    //now see if we can insert any promises
                    for(int i = 0; i < branch.promises_in.Count; ++i)
                    {
                        int pos_est = branch.promises_in[i].ext;
                    }


                    //now if we have outputs
                    if (branch.outputs.Count > 0)
                    {
                        //now everything is moved forwards, we need to make our promises
                        int projected = delta_t * ProgressPerTick;
                        for (int i = 0; i < ci.Items.Count; i++)
                        {
                            //find the projected location of the stack
                            int n = ci.progresses[i] + projected;
                            int overshoot = (n - max_distance);
                            //now grab items out of the stack
                            int index = 0;
                        PromiseLoop:
                            if (overshoot < 0) break;
                            if (index >= ci.Items[i].Length) continue;

                            //inherent round robining, though I don't think it's important for conveyors
                            //it is important to establish the round robin behaviour
                            branch.StateFlag = ((branch.StateFlag & Settings.TICK_MASK) + 1) % branch.outputs.Count;
                            Branch target = branch.outputs[branch.StateFlag];
                            if (branch.promises_out.Count > 0 && branch.promises_out.Last().owner != target) break;

                            int dist_left = max_distance - ci.progresses[i] - index * Settings.CONVEYOR_SPACING;
                            Promise p = new Promise()
                            {
                                owner = branch,
                                item = ci.Items[i][index],
                                time = (best_tick + (dist_left / ProgressPerTick)) & Settings.TICK_MASK, //ticks we can progress past the end
                                ext = dist_left
                            };
                            target.PromiseInput(p);

                            overshoot -= Settings.CONVEYOR_SPACING;
                            index += 1;
                            goto PromiseLoop;
                        }
                    }



                }
            }
            return time;
        }


        public override int ProcessingTime()
        {
            return branch.Size * Settings.CONVEYOR_PRECISION / ProgressPerTick;
        }

    }
}
