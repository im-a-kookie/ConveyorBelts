using ConveyorBeltExample;
using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks.BlockDefinitions
{
    internal class BlockSplitter : BlockGraphed
    {

        public int ProgressPerTick = 1;

        public override Inventory GetDefaultInventory()
        {
            return new SequentialInventory();
        }

        /// <summary>
        /// The router is strictly directional
        /// receives input from the back, and splits output to the front and sides.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        /// <param name="data"></param>
        /// <param name="receiverPos"></param>
        /// <param name="receiverData"></param>
        /// <returns></returns>
        public override bool ProvidesOutput(World world, KPoint pos, BlockData data, KPoint receiverPos, BlockData receiverData)
        {
            if (pos.Step(data.dir) == receiverPos) return true;
            if (pos.Step(data.dir + 2) == receiverPos) return true;
            if (pos.Step(data.dir - 2) == receiverPos) return true;
            return false;
        }


        public override long UpdateTick(BranchUpdater u, World w, long time, Branch b)
        {
            if (b.Inventory is  SequentialInventory inv)
            {
                //1. Resolve input tick field
                b.GetInputs(out var inputs, out var in_count);
                b.GetOutputs(out var outputs, out var out_count);
                inv.PathLength = Settings.Conveyors.CONVEYOR_PRECISION;
                b.ItemCount = inv.Size();
                inv.Time = b.LastResolvedOutputTick;

                var _t = b.LastResolvedOutputTick;
                var _h = inv.SpaceAtHead;

                try
                {
                    //check our inputs
                    if (in_count < 0)
                    {
                        //we can exit early
                        b.LastResolvedInputTick = time;
                        if (b.ItemCount <= 0)
                        {
                            b.LastResolvedOutputTick = time;
                            b.ticks_left = int.MaxValue;
                            return time;
                        }
                    }
                    else
                    {
                        b.LastResolvedInputTick = time;
                        //So it will be resolved here
                        for (int i = 0; i < in_count; i++)
                        {
                            //inputs with no items can skip to the future
                            if (inputs[i].ItemCount <= 0)
                                inputs[i].LastResolvedOutputTick = long.Min(time, inputs[i].LastResolvedInputTick + inputs[i].BranchProcessingTime());
                            b.LastResolvedInputTick = long.Min(inputs[i].LastResolvedOutputTick, b.LastResolvedInputTick);
                            //There is a case where that branch needs to be updated
                            if (inputs[i].LastResolvedOutputTick < time) u.FlagUpdate(inputs[i]);
                        }
                    }


                    //now we can push items forwards
                    //We cannot exceed the time of our last promise
                    if (b.promises_out.TryPeek(out Promise firstP)) b.LastResolvedOutputTick = firstP.time - 1;
                    //Now we can jam the items up to this tick
                    inv.ForceToTicksUnaware(b.LastResolvedOutputTick, ProgressPerTick);

                    //now resolve our promises
                    int promises_taken = 0;
                    for (int i = 0; i < b.promises_in.Count; i++)
                    {
                        var p = b.promises_in[i];
                        if (p.time < 0) { ++promises_taken; continue; }
                        //Now make sure we know what happens at our head up until this tick (e.g let other inputs catch up)
                        int p_dt = (int)(p.time - b.LastResolvedInputTick);
                        if (p_dt <= 1)
                        {
                            //we can take this promise
                            ++promises_taken;
                            p.owner.promises_out.Dequeue(); //it is necessarily the first for the owner of the promise
                            //And we can resolve the promising branch to the tick specified in the promise                               
                            p.owner.LastResolvedOutputTick = long.Min(time, p.resolved_tick);
                            b.LastResolvedInputTick = long.Min(b.LastResolvedInputTick, p.resolved_tick);
                            u.FlagUpdate(p.owner); //the owner should be updated
                            //we can now resolve our input tick
                            //Now calculate where to place the promise
                            if (inv.Put(p.item, (int)(p.time - b.LastResolvedOutputTick) * ProgressPerTick, true))
                            {
                                inv.Items.PeekLast().front.dir = p.item.dir;
                                p.owner.Inventory.RemoveFromInventory(p.item, p.slot, -1);
                                p.owner.ItemCount = p.owner.Inventory.Size();

                            }
                            else break; //we are full so there's no need to continue
                        }
                        else break; //we overshot our present state
                    }

                    if (promises_taken > 0)
                    {
                        b.promises_in.RemoveRange(0, promises_taken);
                        b.ItemCount = inv.Items.Count;
                    }


                    //calculate how far forwards we can safely predict
                    var NonHaltingTick = long.Min(time, b.LastResolvedInputTick + inv.PathLength / ProgressPerTick);
                    int delta = (int)(NonHaltingTick - b.LastResolvedOutputTick);

                    if (delta <= 0)
                    {
                        b.ticks_left = b.WithinUpdateBounds ? 1 : int.Max(1, (out_count == 0 ? inv.PathLength : inv.UnitsFromTail) / ProgressPerTick);
                        return time;
                    }

                    //now move items forwards
                    if (b.ItemCount <= 0)
                    {
                        //Our next output is resolved to the non-halting tick
                        //and we don't need to update again until that happens
                        b.ticks_left = int.Max(1, inv.UnitsFromTail / ProgressPerTick);
                        b.LastResolvedOutputTick = NonHaltingTick;
                    }
                    //we have items and no promises, meaning we need to move items forwards
                    else if (b.promises_out.Count <= 0)
                    {
                        //move items forwards on the conveyor
                        int furthest = inv.AdvanceTicks(delta, ProgressPerTick);
                        int remain = delta - furthest;
                        b.LastResolvedOutputTick += furthest;



                        if (remain > 0)
                        {
                            //we should only ever have one of these
                            if (out_count == 0)
                            {
                                //force us to the remain time
                                inv.ForceToTicksUnaware(time, ProgressPerTick);
                                b.LastResolvedOutputTick = time;
                            }
                            else //make promises to our outputs
                            {
                                b.StateFlag = (b.StateFlag + 1) & 0x3FFFFFFF;

                                //calculate the projection thing
                                int track_length = 0;
                                int items_fed = 0;
                                int projection = remain * ProgressPerTick;
                                Promise _p = null;
                                int best_group_index = -1;
                                int items_left_in_best = -1;
                                for (int i = 0; i < inv.Items.Count && remain > 0; ++i)
                                {
                                    best_group_index = i;
                                    items_left_in_best = inv.Items[i].Count();
                                    //project
                                    //start is at si.Items[i].gap + track_length
                                    track_length += inv.Items[i].gap;
                                    remain -= inv.Items[0].gap / ProgressPerTick; //consume the gap
                                    int p_proj = projection - track_length;
                                    int n = 0;
                                    while (p_proj > 0)
                                    {
                                        if (_p != null) _p.resolved_tick = b.LastResolvedOutputTick + track_length / ProgressPerTick;

                                        var p = new Promise()
                                        {
                                            item = inv.Items[i].Peek(n),
                                            time = b.LastResolvedOutputTick + remain,
                                            owner = b,
                                            ext = items_fed,
                                            target = outputs[b.StateFlag % out_count]
                                        };
                                        p.item.dir = (byte)((2 + b.Tail.dir) & 0x3);
                                        outputs[b.StateFlag % out_count].PromiseInput(p);
                                        outputs[b.StateFlag % out_count].ticks_left = 1;
                                        ++items_fed;
                                        p_proj -= Settings.Conveyors.ITEM_WIDTH;
                                        projection -= Settings.Conveyors.ITEM_WIDTH;
                                        track_length += Settings.Conveyors.ITEM_WIDTH;
                                        remain = projection / ProgressPerTick;
                                        _p = p;
                                        items_left_in_best -= 1;
                                    }
                                }
                                //now we know how far the items were projected
                                //we can see if this leaves us within the first item
                                if (_p != null)
                                {
                                    if (best_group_index >= inv.Items.Count - 1 && items_left_in_best <= 0) 
                                        _p.resolved_tick = b.LastResolvedOutputTick + inv.PathLength / ProgressPerTick;
                                    else _p.resolved_tick = b.LastResolvedOutputTick + track_length / ProgressPerTick;
                                    b.ticks_left = b.WithinUpdateBounds ? 1 : int.Max(1, inv.UnitsFromTail / ProgressPerTick);
                                }
                            }
                        }
                    }

                    inv.Time = b.LastResolvedOutputTick;

                    //calculate the best halting time
                    if (b.ticks_left <= 0)
                    {
                        if (b.ItemCount <= 0) int.Max(1, inv.UnitsFromTail / ProgressPerTick);
                        else if (b.WithinUpdateBounds) b.ticks_left = 1;
                        else b.ticks_left = int.Max(1, inv.UnitsFromTail / ProgressPerTick);
                    }
                    if (b.promises_out.TryPeek(out var po)) { u.FlagUpdate(po.target); u.FlagUpdate(b); }
                    if (b.LastResolvedOutputTick < time && (_t != b.LastResolvedOutputTick || b.promises_in.Count > 0)) u.FlagUpdate(b);
                    if (_h != inv.SpaceAtHead && _h < 0 && inv.SpaceAtHead >= 0)
                    {
                        for (int i = 0; i < in_count; i++) u.FlagUpdate(inputs[i]);
                    }
                }
                finally
                {
                    ArrayPool<Branch>.Shared.Return(inputs);
                    ArrayPool<Branch>.Shared.Return(outputs);
                }

            }
            return time;
        }

        public override bool IsHomogenous(World w, Chunk cache, KPoint pos, BlockData data)
        {
            return false;
        }


    }
}
