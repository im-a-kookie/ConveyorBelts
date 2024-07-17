using ConveyorBeltExample;
using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework.Input;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks
{
    public class BlockConveyor : BlockGraphed
    {
        /// <summary>
        /// Ticks for this block to move items by one space forwards
        /// </summary>
        public int ProcessingTimePerBlock = Settings.Engine.TICK_RATE;

        public int ProgressPerTick = Settings.Conveyors.CONVEYOR_PRECISION / Settings.Engine.TICK_RATE;

        public BlockConveyor(string name, double seconds_per_block)
        {
            this.name = name;
            ProcessingTimePerBlock = (int.Max(1, (int)(Settings.Engine.TICK_RATE * seconds_per_block)));
            ProgressPerTick = Settings.Conveyors.CONVEYOR_PRECISION / ProcessingTimePerBlock;
        }

        public override int ProcessingTime()
        {
            return ProcessingTimePerBlock;
        }

        public override float RenderWorkRate()
        {
            return Settings.Conveyors.CONVEYOR_PRECISION / (float)ProgressPerTick;
        }

        public override bool IsHomogenous(World w, Chunk cache, KPoint pos, BlockData data)
        {
            return data.block is BlockConveyor bc && bc.ProcessingTimePerBlock == ProcessingTimePerBlock;
        }

        public override bool ReceivesInput(World world, KPoint pos, BlockData data, KPoint providerPos, BlockData providerData)
        {
            //we can't receive input from our facing direction
            return pos.Step(data.dir) != providerPos && base.ReceivesInput(world, pos, data, providerPos, providerData);
        }

        /// <summary>
        /// The main conveyor belt algorithm.
        /// <para>This may appear to be unseemingly complicated. And it is. But.</para>
        /// <para>Most conveyors will be offscreen once performance becomes problematic, where this approach allows us to forget them.</para>
        /// </summary>
        /// <param name="u"></param>
        /// <param name="w"></param>
        /// <param name="time"></param>
        /// <param name="branch"></param>
        /// <returns></returns>
        public override long UpdateTick(BranchUpdater u, World w, long time, Branch branch)
        {
            if (branch.LastResolvedInputTick == time && branch.LastResolvedOutputTick == time)
            {
                while (branch.promises_out.TryDequeue(out var p))
                {
                    if (p.owner.Index > 0)
                    {
                        p.owner.LastResolvedInputTick = p.resolved_tick;
                    }
                }
                return time;
            }

            //delta always greater than 0
            if (branch.Inventory is SequentialInventory inv0)
            {
                branch.ItemCount = inv0.Items.Count;
                //in a loose sense, we complete a branch when last resolved output tick is equal to time
                long _t = branch.LastResolvedOutputTick;

                inv0.PathLength = branch.Size * Settings.Conveyors.CONVEYOR_PRECISION;
                inv0.Time = branch.LastResolvedOutputTick;

                //inv0.RecalculateGaps();
                long _h = inv0.SpaceAtHead;

                //We need to quickly check our input and output ticks
                branch.GetInputs(out var inputs, out var in_count);
                branch.GetOutputs(out var outputs, out var out_count);


                //If there are no inputs, then this branch is very easy to resolve
                if (in_count <= 0)
                {
                    //There are no inputs, so all input ticks are resolved
                    branch.LastResolvedInputTick = time;
                    if (branch.ItemCount == 0)
                    {
                        branch.LastResolvedOutputTick = time;
                        branch.ticks_left = branch.IsInViewCache ? 1 : int.Max(1, inv0.UnitsFromTail / ProgressPerTick);
                        ArrayPool<Branch>.Shared.Return(inputs);
                        ArrayPool<Branch>.Shared.Return(outputs);
                        return time;
                    }
                    else
                    {
                        //if we also have no promises out, then we don't need to update until the item reaches the head
                        if(branch.promises_out.Count > 0)
                        {
                            branch.ticks_left = int.Max(1, inv0.UnitsFromTail / ProgressPerTick);
                            branch.LastResolvedOutputTick = long.Min(time, branch.ticks_left);
                        }
                    }
                }
                else
                {
                    //There are inputs, so we need to figure out our best resolved input tick
                    //Which is generally, the oldest state of any of our inputs
                    //and can never exceed the current time
                    branch.LastResolvedInputTick = branch.LastResolvedOutputTick;

                    //If we have an input promise
                    //Then the promisor is stuck at the time of that promise
                    //So it will be resolved here
                    for (int i = 0; i < in_count; i++)
                    {
                        if (inputs[i].ItemCount <= 0)
                            inputs[i].LastResolvedOutputTick = long.Min(time, inputs[i].LastResolvedInputTick + inputs[i].Size * inputs[i].BranchProcessingTime());
                        branch.LastResolvedInputTick = long.Min(inputs[i].LastResolvedOutputTick, branch.LastResolvedInputTick);
                        //There is a case where that branch needs to be updated
                        if (inputs[i].LastResolvedOutputTick < time) u.FlagUpdate(inputs[i]);
                    }
                }

                //Now check if we have items
                if (branch.ItemCount == 0)
                {
                    //Since we have no items, we know that nothing can happen on this branch until we receive an input
                    //and we know that input cannot leave for the processing time of the branch
                    //So we can make a quick prediction here
                    branch.NonHaltingTick = long.Min(time, branch.LastResolvedInputTick + inv0.PathLength / ProgressPerTick);
                    if (in_count <= 0)
                    {
                        //we have no inputs, or items, so we havee resolved this branch
                        branch.LastResolvedOutputTick = time;
                        //If we also have no promises
                        if (branch.promises_in.Count == 0)
                        {
                            //Then we won't update until an input visits us
                            branch.ticks_left = 999999; //int.Max(1, (int)(time - branch.NonHaltingTick) - 1); //branch.Size * ProcessingTimePerBlock;
                            ArrayPool<Branch>.Shared.Return(inputs);
                            ArrayPool<Branch>.Shared.Return(outputs);
                            return branch.LastResolvedOutputTick;
                        }
                    }
                    else
                    {
                        //we have inputs, and they could possibly tick, so we can't update past our last resolvable input tick
                        branch.LastResolvedOutputTick = branch.NonHaltingTick;
                    }
                }

                //We cannot exceed the time of our last promise
                if (branch.promises_out.TryPeek(out Promise firstP)) branch.LastResolvedOutputTick = firstP.time - 1;

                //Now we can jam the items up to this tick
                inv0.ForceToTicksUnaware(branch.LastResolvedOutputTick, ProgressPerTick);

                //This creates as much space as we can get at the head of this branch right now
                //So we should process our inputs next
                if(branch.promises_in.Count > 0)
                {
                    int taken = 0;
                    for(int i = 0; i < branch.promises_in.Count ; ++i)
                    {
                        //take the next promise and see if it's valid
                        Promise p = branch.promises_in[i];
                        if (p.time < 0) { ++taken; continue; }
                        //Now make sure we know what happens at our head up until this tick (e.g let other inputs catch up)
                        int p_dt = (int)(p.time - branch.LastResolvedInputTick);
                        if (p_dt <= 1)
                        {
                            //we can take this promise
                            ++taken;
                            p.owner.promises_out.Dequeue(); //it is necessarily the first for the owner of the promise
                            //And we can resolve the promising branch to the tick specified in the promise                               
                            p.owner.LastResolvedOutputTick = long.Min(time, p.resolved_tick);
                            u.FlagUpdate(p.owner); //the owner should be updated

                            //p.owner.ticks_left = 1;
                            
                            //we can now resolve our input tick
                            branch.LastResolvedInputTick = p.owner.LastResolvedOutputTick;
                            for (int j = 0; j < in_count; j++)
                            {
                                branch.LastResolvedInputTick = long.Min(inputs[j].LastResolvedOutputTick, branch.LastResolvedInputTick);
                            }

                            //now calculate where to place this promise
                            //Basically we know when the promise is made
                            //and we know where our last item is with respect to time
                            //So we can simply push it ahead based on that time
                            int onboarding_dt = (int)(p.time - branch.LastResolvedOutputTick);
                            int dist = onboarding_dt * ProgressPerTick; //and the distance
                            //now see if we can put the item into ourselves
                            if (inv0.Put(p.item, dist, true))
                            {
                                inv0.Items.PeekLast().front.dir = p.item.dir;
                                //and try to remove it from the inventory
                                p.owner.Inventory.RemoveFromInventory(p.item, p.slot, -1);
                                p.owner.ItemCount = p.owner.Inventory.Size();
                                //see if we should update stuff
                                //if (branch.WithinUpdateBounds) branch.ticks_left = 1;
                                //else branch.ticks_left = inv0.UnitsFromTail / ProgressPerTick;

                            }
                            else break; //we are full so there's no need to continue
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (taken > 0)
                    {
                        branch.promises_in.RemoveRange(0, taken);
                        branch.ItemCount = inv0.Items.Count;
                    }
                }

                //now we can estimate the furthest possible future point until this branch loses predictability
                //which is the first of: the current game time, and the time at which an item could leave the conveyor if it is inserted 1 tick from now
                branch.NonHaltingTick = long.Min(time, branch.LastResolvedInputTick + inv0.PathLength / ProgressPerTick);

                //so we can now push this far into the future
                int delta = (int)(branch.NonHaltingTick - branch.LastResolvedOutputTick);


                if (branch.ItemCount <= 0)
                {
                    //calculate the next best time
                    //TODO this should set the next update time to the branch.Size * ProcessingTimePerBlock
                    branch.ticks_left = int.Max(1, inv0.UnitsFromTail / ProgressPerTick);
                    //no items so no output promises
                    //so our output is our best halting time
                    branch.LastResolvedOutputTick = branch.NonHaltingTick;
                    ArrayPool<Branch>.Shared.Return(inputs);
                    ArrayPool<Branch>.Shared.Return(outputs);

                    return branch.LastResolvedOutputTick;
                }
                //we can only do this if we have no output promises
                else if (branch.promises_out.Count == 0)
                {

                    //items, and no promises, but also no time
                    if (delta <= 0)
                    {
                        branch.ticks_left = branch.WithinUpdateBounds ? 1 : int.Max(1, (out_count == 0 ? inv0.PathLength : inv0.UnitsFromTail) / ProgressPerTick);
                        ArrayPool<Branch>.Shared.Return(inputs);
                        ArrayPool<Branch>.Shared.Return(outputs);
                        return time;
                    }

                    int furthest = inv0.AdvanceTicks(delta, ProgressPerTick);
                    int remain = delta - furthest;

                    branch.LastResolvedOutputTick += furthest;
                    //we have ticks left.....


                    if (remain > 0)
                    {
                        //we should only ever have one of these
                        if (out_count == 0)
                        {
                            //force us to the remain time
                            inv0.ForceToTicksUnaware(time, ProgressPerTick);
                            branch.LastResolvedOutputTick = time;
                        }
                        else //make promises to our outputs
                        {
                            branch.StateFlag = (branch.StateFlag + 1) & 0x3FFFFFFF;

                            //calculate the projection thing
                            int track_length = 0;
                            int items_fed = 0;
                            int projection = remain * ProgressPerTick;
                            Promise _p = null;
                            int best_stack = -1;
                            int best_left = -1;
                            for (int i = 0; i < inv0.Items.Count && remain > 0; ++i)
                            {
                                best_stack = i;
                                best_left = inv0.Items[i].Count();
                                //project
                                //start is at si.Items[i].gap + track_length
                                track_length += inv0.Items[i].gap;
                                remain -= inv0.Items[0].gap / ProgressPerTick; //consume the gap
                                //and calculate the projected overflow distance
                                int p_proj = projection - track_length;
                                int n = 0;
                                //now we need to resolve the projected overflow
                                while (p_proj > 0)
                                {
                                    //If we already have a promise, then this promise will be advanced somewhat
                                    if (_p != null) _p.resolved_tick = branch.LastResolvedOutputTick + track_length / ProgressPerTick;

                                    //Now we can make a new promise
                                    var p = new Promise()
                                    {
                                        item = inv0.Items[i].Peek(n),
                                        time = branch.LastResolvedOutputTick + remain,
                                        owner = branch,
                                        ext = items_fed,
                                        target = outputs[branch.StateFlag % out_count]
                                    };
                                    //We have to give it a direction and promise it forwards
                                    p.item.dir = (byte)((2 + branch.Tail.dir) & 0x3);
                                    outputs[branch.StateFlag % out_count].PromiseInput(p);
                                    outputs[branch.StateFlag % out_count].ticks_left = 1;
                                    ++items_fed;
                                    //now we can consume from the projected future
                                    p_proj -= Settings.Conveyors.ITEM_WIDTH;
                                    projection -= Settings.Conveyors.ITEM_WIDTH;
                                    track_length += Settings.Conveyors.ITEM_WIDTH;
                                    remain = projection / ProgressPerTick;
                                    _p = p;
                                    best_left -= 1;
                                }
                            }
                            //now we know how far the items were projected
                            //we can see if this leaves us within the first item
                            if (_p != null)
                            {
                                //basically this is really really annoying
                                //but we need to resolve the current timepoint
                                if (best_stack >= inv0.Items.Count - 1 && best_left <= 0)
                                    _p.resolved_tick = branch.LastResolvedOutputTick + inv0.PathLength / ProgressPerTick;
                                else _p.resolved_tick = branch.LastResolvedOutputTick + track_length / ProgressPerTick;
                                branch.ticks_left = branch.WithinUpdateBounds ? 1 : int.Max(1, inv0.UnitsFromTail / ProgressPerTick);
                            }
                        }
                    }
                }
                //now the inventory can also be resolved to the branch tick
                inv0.Time = branch.LastResolvedOutputTick;

                //calculate the best halting time
                if (branch.ticks_left <= 0)
                {
                    if (branch.ItemCount <= 0) int.Max(1, inv0.UnitsFromTail / ProgressPerTick);
                    else if (branch.WithinUpdateBounds) branch.ticks_left = 1;
                    else branch.ticks_left = int.Max(1, inv0.UnitsFromTail / ProgressPerTick);
                }
                if (branch.promises_out.TryPeek(out var po)) { u.FlagUpdate(po.target); u.FlagUpdate(branch); }

                if (branch.LastResolvedOutputTick < time && (_t != branch.LastResolvedOutputTick || branch.promises_in.Count > 0)) u.FlagUpdate(branch);

                //If there is head space, then we need to tell our inputs that they may be able to do stuff
                if (_h != inv0.SpaceAtHead && _h < 0 && inv0.SpaceAtHead >= 0)
                {
                    for(int i = 0; i < in_count; i++) u.FlagUpdate(inputs[i]);
                }

                //and we can return these stupid things too
                ArrayPool<Branch>.Shared.Return(inputs);
                ArrayPool<Branch>.Shared.Return(outputs);
            }


            return time;





        }

        



    }
}
