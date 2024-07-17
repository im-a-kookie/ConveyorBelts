using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorEngine.Blocks;
using ConveyorEngine.Graphics;
using ConveyorEngine.Graphing;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using static ConveyorEngine.Items.SequentialInventory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ConveyorBeltExample.Blocks
{
    public class BlockGraphed : BlockDefinition
    {

        /// <summary>
        /// Might have to replace with predicate or function. Gets the time taken for this block to do one action (convert an input to an output)
        /// </summary>
        public virtual int ProcessingTime() => 1;


        public bool RendersItems = true;

        public void AdjacentPoints(KPoint pos, out KPoint[] vals, out int count)
        {
            vals = ArrayPool<KPoint>.Shared.Rent(Bounds.Width * 2 + Bounds.Height * 2);
            count = 0;
            //every point north of top edge and south of bottom
            for(int i = 0; i < Bounds.Width; i++)
            {
                vals[count++] = new KPoint(pos.x + i, pos.y).Step(0);
                vals[count++] = new KPoint(pos.x + Bounds.Width - 1, pos.y + i).Step(1);
                vals[count++] = new KPoint(pos.x + i, pos.y + Bounds.Width - 1).Step(2);
                vals[count++] = new KPoint(pos.x, pos.y + i).Step(3);
            }


        }

        public virtual Inventory GetDefaultInventory()
        {
            return new SequentialInventory();
        }

        public virtual float RenderWorkRate() => 1f;

        /// <summary>
        /// Called when this block is broken. Not called when *replaced* by a BlockGraphed
        /// </summary>
        public virtual void Destroyed_NoReplace(World w, Chunk cache, KPoint pos, BlockData data)
        {
            //in this case interesting things happen
            //basically I guess all we need to do is split the branch and stuff
            data.branch(w).ClearInOut();
            data.branch(w).SplitBranch(w, cache, pos, ref data, -1);
            data.branch(w).DestroyHead(w, cache);
            data.branch(w).ConnectEnds(w, cache);
            List<Branch> things = new();

            //some care required....
            foreach (var pA in pos.Adjacent)
            {
                if (w.GetBlock(pos, cache).block is BlockGraphed)
                {
                    if (!things.Contains(data.branch(w))) things.Add(data.branch(w));
                }
            }

            foreach (Branch br in things)
            {
                //update it
                //and do stuff
                br.MergeBranch(w, cache);
            }

        }

        /// <summary>
        /// This is called when World.SetBlock(BlockGraphed) is called
        /// World.SetBlock(B) => Gets the old block, sets the block array, calls this method
        /// </summary>
        /// <param name="placing"></param>
        public void UpdatePlacementImmediate(World world, Chunk cache, KPoint pos, BlockData newData, BlockData oldData, BlockGraphed oldBlock)
        {

            //we actually log this place change with the world
            //and then do it after the branch updater has updated

            //1. If we are deleting a BlockGraphed
            // then we need to remove it from the branches to which it connects
            List<Branch> affectedBranches = new ();

            //TODO conveyors will keep their items if you replace them with conveyors
            //but generally they will lose the items
            //If the old block isn't graphed, then it doesn't matter
            ConveyorGroup[] _old = null;
            if (oldBlock != null && oldData.branchindex > 0)
            {
                //remove it from the branch
                var bb = oldData.branch(world);
                bb.SplitBranch(world, cache, pos, ref oldData, -1);
                _old = bb.DestroyHead(world, cache);
            }

            //just make a branch for the new block
            var b = new Branch(world) { members = ArrayPool<BranchBlock>.Shared.Rent(1) };

            b.Bounds = new Rectangle(pos.x, pos.y, 1, 1);

            b.RenderWorkTicks = ((BlockGraphed)newData.block).RenderWorkRate();

            b.member_count = 1;

            //Now assign it into the pool (yay)
            world.Branches.Assign(b);
            newData.branchindex = b.Index;
            b.members[0] = new(pos, newData);
            cache.SetBranchIndex(pos, newData.branchindex);

            b.SetColor();

            //Grab any fallen inventory items
            Inventory ii = GetDefaultInventory();
            ii.parent = world.Inventories;
            
            ii.parent.Assign(ii);
            ii.Time = world.Tick;
            b._inventory_index = ii.Index;
            if (ii is SequentialInventory x)
            {
                x.PathLength = Settings.Conveyors.CONVEYOR_PRECISION;
                x.SpaceAtHead = x.PathLength;
            }

            if (_old != null && ii is SequentialInventory si)
            {
                //try to put the items
                for(int i = 0; i < _old.Length && i < 1 + Settings.Conveyors.CONVEYOR_PRECISION / Settings.Conveyors.ITEM_WIDTH; ++i)
                {
                   si.Items.AddLast(_old[i]);
                }
            }

            //Now grab the adjacent branches
            if (!affectedBranches.Contains(b)) affectedBranches.Add(b);
            AdjacentPoints(pos, out var points, out var count);

            for(int i = 0; i < count; ++i)
            {
                var p = points[i];
                BlockData neighbor = world.GetBlock(p, cache);
                if (neighbor.id > 0 && neighbor.branchindex > 0 && neighbor.block is BlockGraphed neighborGraph)
                {
                    var nb = neighbor.branch(world);
                    if (nb == null)
                        Debug.WriteLine("What in the effffff?)");
                    if (!affectedBranches.Contains(nb)) affectedBranches.Add(nb);
                }
            }

            //Now we need to reconnect all of these branches
            foreach (Branch branch in affectedBranches)
            {
                if (branch.Index > 0)
                {
                    branch._cachedHead = (BlockGraphed)branch.HeadDef;
                    branch._cachedTail = (BlockGraphed)branch.TailDef;
                    branch.RendersItems = branch._cachedHead.RendersItems;
                    //we have to do this, so we can only set immediate changes when the graph is locally up to date
                    branch.LastResolvedInputTick = world.Tick;
                    branch.LastResolvedOutputTick = world.Tick;

                    branch.IsBuffered = false;
                    branch.ItemCount = branch.Inventory.Size();

                    world.BranchUpdater.FlagUpdate(branch);
                    branch.ConnectEnds(world, cache);

                    if (branch.Inventory is SequentialInventory ci) ci.PathLength = branch.Size * Settings.Conveyors.CONVEYOR_PRECISION;

                    branch.UpdateBounds();

                }

            }


        }


        /// <summary>
        /// Gets all neighbors that input into us
        /// </summary>
        public BranchBlock[] GetInputBlocks(World w, Chunk cache, KPoint pos, BlockData data)
        {
            //we need a simple reduction
            //of positions
            //where the array element will be empty if there is no input
            BranchBlock[] result = new BranchBlock[4];
            for(int i = 0; i < 4; i++)
            {
                var nextPos = pos.Step(i);
                var nextData = w.GetBlock(nextPos, cache);
                if (nextData.id > 0 &&
                nextData.block is BlockGraphed nextG &&
                ReceivesInput(w, pos, data, nextPos, nextData))
                {
                    result[i] = new BranchBlock
                    {
                        branchindex = nextData.branchindex,
                        id = nextData.id,
                        pos = nextPos
                    };
                }
            }
            return result;
        }


        /// <summary>
        /// Gets all neighbors that input into us
        /// </summary>
        public BranchBlock[] GetOuputBlocks(World w, Chunk cache, KPoint pos, BlockData data)
        {

            AdjacentPoints(pos, out var points, out var count);

            //we need a simple reduction
            //of positions
            //where the array element will be empty if there is no input
            BranchBlock[] result = new BranchBlock[count];
            for (int i = 0; i < count; i++)
            {
                var nextPos = points[i];
                var nextData = w.GetBlock(nextPos, cache);
                if (nextData.id > 0 &&
                nextData.block is BlockGraphed nextGraph &&
                nextGraph.ReceivesInput(w, nextPos, nextData, pos, data))
                {
                    result[i] = new BranchBlock
                    {
                        branchindex = nextData.branchindex,
                        id = nextData.id,
                        pos = nextPos,
                        mask = (byte)nextData.Int,
                        dir = nextData.dir
                    };
                }
            }
            ArrayPool<KPoint>.Shared.Return(points);
            return result;
        }

        /// <summary>
        /// Gets all neighbors that input into us
        /// </summary>
        public BranchBlock[] GetConnectedBlocks(World w, Chunk cache, KPoint pos, BlockData data)
        {
            //we need a simple reduction
            //of positions
            //where the array element will be empty if there is no input

            AdjacentPoints(pos, out var points, out var count);


            BranchBlock[] result = new BranchBlock[count];
            for (int i = 0; i < count; i++)
            {
                var nextPos = points[i];
                var nextData = w.GetBlock(nextPos, cache);
                if (nextData.id > 0 && nextData.block is BlockGraphed nextGraph &&
                    (ReceivesInput(w, pos, data, nextPos, nextData) ||
                    nextGraph.ReceivesInput(w, nextPos, nextData, pos, data)))
                {
                    result[i] = new BranchBlock
                    {
                        branchindex = nextData.branchindex,
                        id = nextData.id,
                        pos = nextPos,
                        mask = (byte)nextData.Int,
                        dir = nextData.dir
                    };
                }
            }
            ArrayPool<KPoint>.Shared.Return(points);

            return result;
        }

        /// <summary>
        /// Whether this block (this definition) can take inputs from the provider
        /// </summary>
        /// <param name="graph_querying"></param>
        /// <returns></returns>
        public virtual bool ReceivesInput(World world, KPoint pos, BlockData data, KPoint providerPos, BlockData providerData)
        {
            return (providerData.block is BlockGraphed provider_graph &&
                provider_graph.ProvidesOutput(world, providerPos, providerData, pos, data));
        }

        /// <summary>
        /// Whether the first block (this definition) can output into the receiver
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual bool ProvidesOutput(World world, KPoint pos, BlockData data, KPoint receiverPos, BlockData receiverData)
        {
            return pos.Step(data.dir) == receiverPos;
        }

        /// <summary>
        /// Whether a given block can form a homogenous entity.
        /// Generally this is no, except for things like conveyor belts
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual bool IsHomogenous(World w, Chunk cache, KPoint pos, BlockData data)
        {
            if (data.block is BlockPart) return IsHomogenous(w, cache, pos, w.GetBlock(pos - data.Point, cache));
            else return false;
        }

        /// <summary>
        /// Performs as much work as possible, up to "time" units of time.
        /// </summary>
        /// <param name="time">The timestamp that we are trying to update this block to.</param>
        /// <returns>The amount of time that we were able to progress</returns>
        public virtual long UpdateTick(BranchUpdater u, World w, long time, Branch b)
        {
            return time;
        }

        public override int GetLayer()
        {
            return LayerManager.ConveyorLayer;
        }




    }
}
