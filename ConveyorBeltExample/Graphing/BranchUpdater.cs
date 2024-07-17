using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphing
{
    public class BranchUpdater
    {
        public World owner;

        //branches cannot be modified during the update sweep.
        List<Branch> branches = new List<Branch>();
        ConcurrentQueue<Branch> updates = new ConcurrentQueue<Branch>();
        public long previous_tick;

        /// <summary>
        /// Flags an update into the thing Branches are made visible via this approach.
        /// </summary>
        /// <param name="b"></param>
        public void FlagUpdate(Branch b)
        {
            if (b.Index <= 0) return;
            if (Interlocked.CompareExchange(ref b.flagged, 1, 0) == 0)
            {
                updates.Enqueue(b);
            }
        }

        public void Update(long new_tick)
        {
            if (owner == null || owner.Branches.Items == null) return;

            long delta = new_tick - previous_tick;
            //cap the update rate to 1 tick, for now
            new_tick = previous_tick + delta;

            if (delta != 0)
            {

                //or is it quicker to maintain a separate array and SIMD?
                //iterate every branch and deduct its timer
                for (int i = 0; i < owner.Branches._assigned; i++)
                {
                    if (owner.Branches._cached[i] == null || owner.Branches._cached[i].Index <= 0) continue;
                    owner.Branches._cached[i].ticks_left -= (int)delta;
                    if (owner.Branches._cached[i].ticks_left <= 0)
                    {
                        FlagUpdate(owner.Branches._cached[i]);
                    }
                }

                //Now repeatedly attempt to dequeue updates until everything is done
                //Remember that some stuff may circulate, and this enables kind of...
                //A hard piecewise solution
                //Which is ultimately critical to the algorithm, since we can't arbitrarily advance beyond projected halts,
                //but we can also use this to update by smaller fixed intervals if we so choose.
                while (updates.TryDequeue(out Branch b))
                {
                    if (b.Index <= 0) continue;
                    b.flagged = 0;
                    //remember, we can't update any branches into the future
                    if (b.LastResolvedOutputTick < new_tick || b.LastResolvedInputTick < new_tick)
                    {
                        b._cachedHead.UpdateTick(this, owner, new_tick, b);
                    }
                    //things with no ticks left are updated to match their processing time
                    //Though this isn't strictly necessary
                    if (b.ticks_left <= 0) b.ticks_left = b.BranchProcessingTime();
                }
            }

            previous_tick = new_tick;
        }


    }


}
