using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphing
{
    internal class Branch : IComparable
    {
        public BlockGraphed[] members;
        public List<Branch> inputs;
        public List<Branch> outputs;
        public Inventory items;

        /// <summary>
        /// The entity currently updating the branch
        /// </summary>
        public BranchUpdater updater;
        

        public int Value;
        public int Index = -1;
        public bool HasRoot = false;

        public int StateFlag = 0;

        /// <summary>
        /// The current updated time at the head of this branch
        /// </summary>
        public int HeadTime = 0;
        /// <summary>
        /// The next tick where a halting event is possible at the tail of this branch.
        /// <para>Children cannot process past the halting awareness of this branch</para>
        /// 
        /// <para>i.e a branch in state t promises to t+P, meaning children can update to t+P.</para>
        /// </summary>
        public int NonHaltingTime = 0;
        /// <summary>
        /// The current time at the tail of this branch..
        /// 
        /// <para>This represents the current state of the branch!</para>
        /// </summary>
        public int TailTime = 0;

        public int MinInputTime = -1;

        public List<Promise> promises_out = new List<Promise>();
        public List<Promise> promises_in = new List<Promise>();

        public double BranchProcessingTime()
        {
            //this is a bit fucky
            //processing blocks will have to do it differently
            if (Head != null)
            {
                return Head.ProcessingTime();
            }
            return -1d;
        }


        /// <summary>
        /// The size of this branch
        /// </summary>
        public int Size => members.Length;
        /// <summary>
        /// Gets the index of the given block in this branch
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public int this[BlockGraphed b] { get => Array.IndexOf(members, b); }
        /// <summary>
        /// The block at the head of this graph (inputs)
        /// </summary>
        public BlockGraphed Head => members[0];
        /// <summary>
        /// The block at the tail of this graph (outputs)
        /// </summary>
        public BlockGraphed Tail => members[Size - 1];

        /// <summary>
        /// Dirty, clears the head of this branch and reconnects it.
        /// This will do nothing if the head block remains in the block array
        /// </summary>
        public void DestroyHead(bool remerge = true)
        {
            int n = Size - 1;
            if (n <= 0)
            {
                members = null;
                return;
            }
            BlockGraphed[] _new = new BlockGraphed[Size - 1];
            Array.Copy(members, 1, _new, 0, Size - 1);
            members[0].branch = null;
            if (remerge) ConnectEnds();
            else inputs.Clear();
        }

        /// <summary>
        /// Appends a branch to our end
        /// </summary>
        /// <param name="b"></param>
        public void AppendBranch(Branch b)
        {
            if (!outputs.Contains(b)) {  outputs.Add(b); }
            if (!b.inputs.Contains(this)) { b.inputs.Add(this); }

        }

        /// <summary>
        /// Attaches a branch to our front
        /// </summary>
        /// <param name="b"></param>
        public void AppendToBranch(Branch b)
        {
            if (!inputs.Contains(b)) { inputs.Add(b); }
            if (!b.outputs.Contains(this)) { b.outputs.Add(this); }
        }

        /// <summary>
        /// Internal use only. Unsafe. Connects a new branch to our head, and reconnects our inputs to it.
        /// </summary>
        /// <param name="b"></param>
        internal void InjectHead(Branch b)
        {
            foreach (var i in inputs)
            {
                i.outputs.Remove(this);
                i.outputs.Add(b);
                b.inputs.Add(this);
            }
            inputs.Clear();
            inputs.Add(b);
            b.outputs.Add(this);
        }

        /// <summary>
        /// Unsafe. Internal use only. Connects a new branch to our tail, and shifts all of our outputs to it
        /// </summary>
        /// <param name="b"></param>
        internal void InsertTail(Branch b)
        {
            foreach (var i in outputs)
            {
                i.inputs.Remove(this);
                i.inputs.Add(b);
                b.outputs.Add(this);
            }
            outputs.Clear();
            outputs.Add(b);
            b.inputs.Add(this);
        }

        public bool AttachBranch(Branch branch, BlockGraphed b, bool merges = true)
        {
            //1. Split the branch
            int n = this[b];
            if (n < 0) return false;
            SplitBranch(b, n);
            //now do the thing ja
            if (!inputs.Contains(branch)) inputs.Add(branch);
            if (!branch.outputs.Contains(this)) branch.outputs.Add(this);
            if (merges) MergeBranch();
            return true;
        }

        /// <summary>
        /// Splits the branch so that we become a new branch starting with block B.
        /// All other blocks (if any), are separated into a new branch that inputs to B.
        /// </summary>
        /// <param name="b"></param>
        public bool SplitBranch(BlockGraphed b, int n = -1)
        {
            if (n < 0) n = this[b];
            if (n <= 0) return false;
            //create a new branch with everything we need
            Branch bN = new Branch();
            bN.members = new BlockGraphed[n];
            Array.Copy(members, 0, bN.members, 0, n);
            for (int i = 0; i < n; i++) bN.members[i].branch = bN;
            var mH = new BlockGraphed[Size - n];
            Array.Copy(members, n, mH, 0, mH.Length);
            members = mH;
            bN.inputs = inputs;
            foreach(var i in bN.inputs)
            {
                i.outputs.Remove(this);
                i.outputs.Add(bN);
            }
            bN.outputs.Add(this);
            inputs = new List<Branch>() { bN };
            return true;
        }

        /// <summary>
        /// Merges this branch up and down with every branch we can merge with
        /// </summary>
        public void MergeBranch()
        {
            var bN = this;
            var len = Size;
            while(bN.inputs.Count == 1 
                && bN.inputs.First().outputs.Count == 1 
                && bN.inputs.First().outputs[0] != this)
            {
                bN = bN.inputs[0];
                len += bN.Size;
            }
            //now merge forwards
            var bM = this;
            while (bM.outputs.Count == 1 
                && bM.outputs.First().inputs.Count == 1 
                && bM.outputs.First().inputs[0] != this)
            {
                bM = bM.outputs[0];
                len += bM.Size;
            }

            BlockGraphed[] _new = new BlockGraphed[len];
            int n = 0;
            while(true)
            {
                Array.Copy(bN.members, 0, _new, n, bN.Size);
                n += bN.Size;
                bN.members = null;
                if (bN == bM) break;
                bN = bN.outputs[0];
            }

            members = _new;

            //now update our inputs and outputs appropriately
            foreach (var m in members) m.branch = this;
            if (bN != this)
            {
                inputs = bN.inputs;
                foreach(var i in inputs)
                {
                    i.outputs.Remove(bN);
                    i.outputs.Add(this);
                }
            }

            if (bM != this)
            {
                outputs = bM.outputs;
                foreach(var o in outputs)
                {
                    o.inputs.Remove(bM);
                    o.outputs.Add(this);
                }
            }

        }

        /// <summary>
        /// Recalculates the end connections of this branch
        /// </summary>
        public void ConnectEnds()
        {
            outputs.Clear();
            inputs.Clear();

            //connect the head to any blocks that can output into it
            foreach (var i in Head.InputBlocks)
            {
                this.AttachBranch(i.branch, Head, false);
            }

            //and connect the tail to any blocks that it outputs into
            foreach (var o in Tail.OutputBlocks)
            {
                o.branch.AttachBranch(this, o, false);
            }

            //And try to merge the branches?
            //This might be redundant
            MergeBranch();
        }

        public int CompareTo(object obj)
        {
            if (obj is Branch b)
            {
                return b.Value.CompareTo(this.Value);
            }
            return 0;
        }

        /// <summary>
        /// Promises an input to this branch. The promise must state the tick
        /// in which it was promised to this branch (so will reflect t+P for a branch in state t).
        /// </summary>
        /// <param name="p"></param>
        public virtual void PromiseInput(Promise p)
        {
            int n = Promise.GetInsertionIndex(promises_in, p);
            if (n <= 0) promises_in.Add(p);
            else promises_in.Insert(n, p);

            n = Promise.GetInsertionIndex(p.owner.promises_out, p);
            if (n <= 0) p.owner.promises_out.Add(p);
            else p.owner.promises_out.Insert(n, p);

            //we need to flag the updates into the branch updater
            //which is inherently stored in the owner of p
            //Only one branch updater can exist in a tick, so use the owner's updater


            //graph.FlagUpdate(this);
            //p.Owner.graph.FlagUpdate(p.Owner);

        }

    }
}
