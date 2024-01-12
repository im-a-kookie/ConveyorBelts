using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Blocks
{
    internal class BlockGraphed : Block
    {

        public Branch branch;
        
        /// <summary>
        /// Might have to replace with predicate or function. Gets the time taken for this block to do one action (convert an input to an output)
        /// </summary>
        public virtual int ProcessingTime() => branch.Size;

        /// <summary>
        /// This is called when World.SetBlock(BlockGraphed) is called
        /// World.SetBlock(B) => Gets the old block, sets the block array, calls this method
        /// </summary>
        /// <param name="placing"></param>
        public void UpdatePlacement(BlockGraphed? oldBlock = null)
        {
            //1. If we are deleting a BlockGraphed
            // then we need to remove it from the branches to which it connects
            List<Branch> affectedBranches = new List<Branch>();

            if (oldBlock != null)
            {
                //remove it from the branch
                oldBlock.branch.SplitBranch(oldBlock);
                oldBlock.branch.DestroyHead(false);
            }

            //now see if the new block requires a branch
            if (branch == null)
            {
                branch = new Branch()
                {
                    members = [this]
                };
            }

            //Grab every branch that's adjacent to us
            if(!affectedBranches.Contains(branch)) affectedBranches.Add(branch);
            foreach(var p in Location.GetAdjacent())
            {
                Block _b = world.GetBlock(p);
                if (_b != null && _b is BlockGraphed b)
                {
                    if (!affectedBranches.Contains(b.branch)) affectedBranches.Add(b.branch);
                }
            }

            //Now we need to reconnect all of these branches
            foreach(Branch b in affectedBranches)
            {
                if (b.members != null && b.Size > 0)
                {
                    b.ConnectEnds();
                    b.MergeBranch();
                }
            }

            //now we need to repopulate the affected branch collection
            //keeping in mind that the connect/merge step may have eradicated some branches
            //we don't need to care much for the graphs of these branches, since we will clean the graphs later
            affectedBranches = new List<Branch>(
                affectedBranches.Where(x => x.members != null && x.members.Length > 0)
                );
            

        }


        /// <summary>
        /// Gets all neighbors that input into us
        /// </summary>
        public List<BlockGraphed> InputBlocks 
        {
            get
            {
                List<BlockGraphed> results = new List<BlockGraphed>();
                foreach(var p in Location.GetAdjacent())
                {
                    Block? bN = world.GetBlock(p);
                    if (bN != null && bN is BlockGraphed g && ReceivesInput(g))
                    {
                        results.Add(g);
                    }
                }
                return results;
            }
        }
        
        /// <summary>
        /// Gets all neighbors that we output into
        /// </summary>
        public List<BlockGraphed> OutputBlocks
        {
            get
            {
                List<BlockGraphed> results = new List<BlockGraphed>();
                foreach (var p in Location.GetAdjacent())
                {
                    Block? bN = world.GetBlock(p);
                    if (bN != null && bN is BlockGraphed g && g.ReceivesInput(this))
                    {
                        results.Add(g);
                    }
                }
                return results;
            }
        }

        /// <summary>
        /// Gets all neighbors that are connected to us (input or output)
        /// </summary>
        public List<BlockGraphed> ConnectedBlocks
        {
            get
            {
                List<BlockGraphed> results = new List<BlockGraphed>();
                foreach (var p in Location.GetAdjacent())
                {
                    Block? bN = world.GetBlock(p);
                    if (bN != null && bN is BlockGraphed g && (ReceivesInput(g) || g.ReceivesInput(this)))
                    {
                        results.Add(g);
                    }
                }
                return results;
            }
        }

        /// <summary>
        /// Whether we receive input from the given block
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual bool ReceivesInput(BlockGraphed b)
        {
            return (b is BlockGraphed g && b.ProvidesOutput(this));
        }

        /// <summary>
        /// Whether we receive output from the given block
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual bool ProvidesOutput(BlockGraphed b)
        {
            return Location.Step(Direction) == b.Location;
        }

        /// <summary>
        /// Whether a given block can form a congituous graph edge with this one
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual bool IsContiguous(BlockGraphed b)
        {
            return b.GetType() == this.GetType();
        }

        /// <summary>
        /// Performs as much work as possible, up to "time" units of time.
        /// </summary>
        /// <param name="time">The timestamp that we are trying to update this block to.</param>
        /// <returns>The amount of time that we were able to progress</returns>
        public virtual int UpdateTick(int time)
        {
            return time;
        }



        


    }
}
