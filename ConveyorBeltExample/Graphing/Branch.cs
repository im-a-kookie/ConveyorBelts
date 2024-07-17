using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Items;
using ConveyorEngine.Blocks;
using ConveyorEngine.Graphics;
using ConveyorEngine.Graphics.Conveyors;
using ConveyorEngine.Graphing;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static ConveyorEngine.Items.SequentialInventory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ConveyorBeltExample.Graphing
{
    public class Branch : Indexable
    {
        
        public static void RefreshViewableConveyorCache(World w, Chunk c, (Branch[] branches, int count) b)
        {
            
        }







        //the branch contains an array of the points in the branch
        public BranchBlock[] members;
        public int member_count = 0;
        /// <summary>
        /// Whether items are rendered
        /// </summary>
        public bool RendersItems = false;

        public void Trim()
        {
            if (members.Length > 32 && members.Length > 3 * member_count)
            {
                var _new = ArrayPool<BranchBlock>.Shared.Rent(member_count);
                Array.Copy(members, 0, _new, 0, member_count);
                ArrayPool<BranchBlock>.Shared.Return(members);
                members = _new;
            }
        }

        public BranchBlock Head => members[0];
        public BranchBlock Tail => members[member_count - 1];

        public BlockGraphed _cachedHead;
        public BlockGraphed _cachedTail;

        /// <summary>
        /// Cached color for science
        /// </summary>
        public Color ForScience = Color.White;

        public ref Color GetCol() => ref ForScience;

        /// <summary>
        /// Accesses the definition for the head block
        /// </summary>
        public BlockDefinition HeadDef => Head.definition;

        /// <summary>
        /// Accesses the definition for the tail block
        /// </summary>
        public BlockDefinition TailDef => Tail.definition;

        /// <summary>
        /// The latest time at which we can predict no halts
        /// </summary>
        public long NonHaltingTick = 0;

        /// <summary>
        /// The last tick at which outputs are resolved on this branch
        /// </summary>
        public long LastResolvedOutputTick = 0;

        /// <summary>
        /// The last tick at which inputs are resolved into this branch
        /// </summary>
        public long LastResolvedInputTick = 0;

        /// <summary>
        /// A thing for calculating stuff
        /// </summary>
        public float RenderWorkTicks = 0f;
        
        /// <summary>
        /// The inventory index for looking up inventory contents
        /// </summary>
        public Inventory Inventory => GetInventory();

        /// <summary>
        /// The number of item GROUPS on this conveyor
        /// </summary>
        public int ItemCount = 0;

        public bool IsInViewCache = false;
        public bool WithinUpdateBounds = false;

        public bool IsBuffered = false;
        public int IsQueued = 0;

        /// <summary>
        /// The world for this branch
        /// </summary>
        public World w;

        public Branch(World w)
        {
            this.w = w;
        }

        /// <summary>
        /// The number of ticks before we update again
        /// </summary>
        public int ticks_left;

        public void FlagImmediateUpdate()
        {
            ticks_left = 0;
        }
        public void FlagUpdate(int delay)
        {
            ticks_left = delay;
        }

        public int flagged = 0;

        public Deque<int>[] RawInputs = [new(), new(), new(), new()];
        public Deque<int>[] RawOutputs = [new(), new(), new(), new()];
        public int _inventory_index;

        public Deque<int> GetInputIds(Dir d) => GetInputIds((int)d);
        public Deque<int> GetInputIds(int d) => RawInputs[d & 0x3];

        public void GetInputs(Dir d, out Branch[] branches, out int count) => GetInputs((int)d, out branches, out count);
        public void GetInputs(int d, out Branch[] branches, out int count)
        {
            branches = ArrayPool<Branch>.Shared.Rent(1);
            count = 0;

            var g = GetInputIds(d);
            for (int j = g.headDeleted; j < g.head.Count; ++j)
            {
                if (g.head[j] > 0)
                {
                    if (count >= branches.Length)
                    {
                        Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                        Array.Copy(branches, n, branches.Length);
                        ArrayPool<Branch>.Shared.Return(branches);
                        branches = n;
                    }
                    branches[count++] = w.Branches[g.head[j]];
                }
            }
            for (int j = g.tailDeleted; j < g.tail.Count; ++j)
            {
                if (count >= branches.Length)
                {
                    Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                    Array.Copy(branches, n, branches.Length);
                    ArrayPool<Branch>.Shared.Return(branches);
                    branches = n;
                }
                branches[count++] = w.Branches[g.tail[j]];
            }
        }

        public int GetInputCount(Predicate<Branch> x = null)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                var g = GetInputIds(i);
                for (int j = g.headDeleted; j < g.head.Count; ++j)
                {
                    if (g.head[j] > 0)
                    {
                        if (x == null || x(w.Branches[g.head[j]])) ++count;
                    }
                }
                for (int j = g.tailDeleted; j < g.tail.Count; ++j)
                {
                    if (g.tail[j] > 0)
                    {
                        if (x == null || x(w.Branches[g.tail[j]])) ++count;
                    }
                }
            }
            return count;
        }

        public void GetInputs(out Branch[] branches, out int count)
        {
            branches = ArrayPool<Branch>.Shared.Rent(1);
            count = 0;
            for (int i = 0; i < 4; i++)
            {
                var g = GetInputIds(i);
                for (int j = g.headDeleted; j < g.head.Count; ++j)
                {
                    if (g.head[j] > 0)
                    {
                        if (count >= branches.Length)
                        {
                            Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                            Array.Copy(branches, n, branches.Length);
                            ArrayPool<Branch>.Shared.Return(branches);
                            branches = n;
                        }
                        branches[count++] = w.Branches[g.head[j]];
                    }
                }
                for (int j = g.tailDeleted; j < g.tail.Count; ++j)
                {
                    if (count >= branches.Length)
                    {
                        Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                        Array.Copy(branches, n, branches.Length);
                        ArrayPool<Branch>.Shared.Return(branches);
                        branches = n;
                    }
                    branches[count++] = w.Branches[g.tail[j]];
                }
            }
        }

        public Deque<int> GetOutputIds(Dir d) => GetOutputIds((int)d);
        public Deque<int> GetOutputIds(int d) => RawOutputs[d & 0x3];

        public void GetOutputs(Dir d, out Branch[] branches, out int count) => GetOutputs((int)d, out branches, out count);
        public void GetOutputs(int d, out Branch[] branches, out int count)
        {
            branches = ArrayPool<Branch>.Shared.Rent(1);
            count = 0;
            var g = GetOutputIds(d);
            for (int j = g.headDeleted; j < g.head.Count; ++j)
            {
                if (g.head[j] > 0)
                {
                    if (count >= branches.Length)
                    {
                        Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                        Array.Copy(branches, n, branches.Length);
                        ArrayPool<Branch>.Shared.Return(branches);
                        branches = n;
                    }
                    branches[count++] = w.Branches[g.head[j]];
                }
            }
            for (int j = g.tailDeleted; j < g.tail.Count; ++j)
            {
                if (count >= branches.Length)
                {
                    Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                    Array.Copy(branches, n, branches.Length);
                    ArrayPool<Branch>.Shared.Return(branches);
                    branches = n;
                }
                branches[count++] = w.Branches[g.tail[j]];
            }
            
        }
        public int GetOutputCount(Predicate<Branch> x = null)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                var g = GetOutputIds(i);
                for (int j = g.headDeleted; j < g.head.Count; ++j)
                {
                    if (g.head[j] > 0)
                    {
                        if (x == null || x(w.Branches[g.head[j]])) ++count;
                    }
                }
                for (int j = g.tailDeleted; j < g.tail.Count; ++j)
                {
                    if (g.tail[j] > 0)
                    {
                        if (x == null || x(w.Branches[g.tail[j]])) ++count;
                    }
                }
            }
            return count;
        }
        public void GetOutputs(out Branch[] branches, out int count)
        {
            branches = ArrayPool<Branch>.Shared.Rent(1);
            count = 0;
            for (int i = 0; i < 4; i++)
            {
                var g = GetOutputIds(i);
                for (int j = g.headDeleted; j < g.head.Count; ++j)
                {
                    if (g.head[j] > 0)
                    {
                        if (count >= branches.Length)
                        {
                            Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                            Array.Copy(branches, n, branches.Length);
                            ArrayPool<Branch>.Shared.Return(branches);
                            branches = n;
                        }
                        branches[count++] = w.Branches[g.head[j]];
                    }
                }
                for (int j = g.tailDeleted; j < g.tail.Count; ++j)
                {
                    if (count >= branches.Length)
                    {
                        Branch[] n = ArrayPool<Branch>.Shared.Rent(branches.Length + 1);
                        Array.Copy(branches, n, branches.Length);
                        ArrayPool<Branch>.Shared.Return(branches);
                        branches = n;
                    }
                    branches[count++] = w.Branches[g.tail[j]];
                }
            }
        }


        /// <summary>
        /// Inputs the given branch to this branch, and this branch as an output to that branch.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="d"></param>
        public void AddInput(Branch b, Dir d)
        {
            if (b == null || b.Index <= 0) return;

            if (RawInputs[(int)d].Contains(b.Index)) return;
            RawInputs[(int)d].AddFirst(b.Index);
            b.AddOutput(this, d.Opposite());
        }

        /// <summary>
        /// Adds the given branch as an output of this one, and this as an input to that branch.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="d"></param>
        public void AddOutput(Branch b, Dir d)
        {
            if (b == null || b.Index <= 0) return;
            if (RawOutputs[(int)d].Contains(b.Index)) return;
            RawOutputs[(int)d].AddFirst(b.Index);
            b.AddInput(this, d.Opposite());
        }

        public void RemoveInput(Branch b, Dir d)
        {
            if (RawInputs[(int)d].Remove(b.Index))
            {
                b.RemoveOutput(this, d.Opposite());
            }
        }

        /// <summary>
        /// Adds the given branch as an output of this one, and this as an input to that branch.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="d"></param>
        public void RemoveOutput(Branch b, Dir d)
        {
            if (RawOutputs[(int)d].Remove(b.Index))
            {
                b.RemoveInput(this, d.Opposite());
            }
        }

        /// <summary>
        /// Clears all inputs
        /// </summary>
        public void ClearInput()
        {
            for (int i = 0; i < 4; i++)
            {
                while (RawInputs[i].TryTakeFirst(out var n))
                {
                    if (n > 0)
                    {
                        Branch b = w.Branches[n];
                        if (b != null) b.RawOutputs[(int)((Dir)i).Opposite()].Remove(Index);
                    }
                }
            }
        }

        public void ClearOutput()
        {
            for (int i = 0; i < 4; i++)
            {
                while (RawOutputs[i].TryTakeFirst(out var n))
                {
                    if (n > 0)
                    {
                        Branch b = w.Branches[n];
                        if (b != null) b.RawInputs[(int)((Dir)i).Opposite()].Remove(Index);
                    }
                }
            }
        }

        public void ClearInOut()
        {
            ClearInput();
            ClearOutput();
        }

        /// <summary>
        /// Gets the inventory for this branch
        /// </summary>
        /// <returns></returns>
        public Inventory GetInventory()
        {
            return w.Inventories[GetInventoryIndex()];
        }

        /// <summary>
        /// Gets the inventory for this branch
        /// </summary>
        /// <returns></returns>
        public int GetInventoryIndex()
        {
            return _inventory_index;
        }

        
        /// <summary>
        /// The world bounds of this branch
        /// </summary>
        public Rectangle Bounds = new Rectangle(0, 0, 0, 0);

        public int StateFlag = 0;

        public Queue<Promise> promises_out = new Queue<Promise>();
        public List<Promise> promises_in = new List<Promise>();

        public int BranchProcessingTime()
        {
            //Basically, returns the number of ticks for one complete operation
            if (Head.id > 0)
            {
                return ((BlockGraphed)Head.definition).ProcessingTime() * member_count;
            }
            return 1;
        }


        /// <summary>
        /// The size of this branch
        /// </summary>
        public int Size => member_count;
        /// <summary>
        /// Gets the index of the given block in this branch
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public int this[KPoint pos]
        {
            get
            {
                for (int i = 0; i < Size; i++)
                { if (members[i].pos == pos) return i; }
                return -1;
            }
        }


        /// <summary>
        /// Get the Index of this branch
        /// </summary>
        public int Index { get; set; }

        public int UpdaterIndex { get; set; }

        /// <summary>
        /// Get the pool holding this branch
        /// </summary>
        public IPool parent { get; set; }


        /// <summary>
        /// Dirty, clears the head of this branch and reconnects it.
        /// This will do nothing if the head block remains in the block array.
        /// <para>Clears branch IO. ConnectEnds must be called after this method.</para>
        /// </summary>
        public ConveyorGroup[] DestroyHead(World w, Chunk cache)
        {
            ClearInOut();

            int n = member_count - 1;
            if (n <= 0)
            {

                if (members != null) ArrayPool<BranchBlock>.Shared.Return(members);
                members = null;
                member_count = 0;
                //we have nulled this branch so free us from the thing

                w.Branches.Free(this);
                IsBuffered = false;
                if (Inventory != null)
                {
                    var q = Inventory.GetSequentialItems();
                    w.Inventories.Free(Inventory);
                    if (q != null) return q.ToArray();
                }
                return null;
            }
            
            //remove this branch from first member
            members[0].branchindex = -1;
            _cachedHead = (BlockGraphed)HeadDef;
            _cachedTail = (BlockGraphed)TailDef;
            RendersItems = _cachedHead.RendersItems;

            w.GetChunkFromWorldCoords(members[0].pos).SetBranchIndex(members[0].pos, -1);
            //so now we can copy the array into itself?
            member_count -= 1;
            Array.Copy(members, 1, members, 0, member_count);
            Trim();

            //now destroy the head of the inventory
            if (Inventory is SequentialInventory ci)
            {
                //let the first items be lost
                var items = ci.Split(Settings.Conveyors.CONVEYOR_PRECISION);

                w.Inventories.Free(ci);
                w.Inventories.Assign(items);
                _inventory_index = items.Index;
                IsBuffered = false;
                //now see
                return ci.Items.ToArray();

                //TODO we can drop them on the ground maybe?
            }
            IsBuffered = false;
            return null;
        }

        /// <summary>
        /// Updates the bounds of everything within this branch
        /// </summary>
        public void UpdateBounds()
        {
            if (Size <= 0)
            {
                Bounds = new Rectangle(0, 0, 0, 0);
                return;
            }


            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            for(int i = 0; i < member_count; i++)
            {
                minX = int.Min(minX, members[i].pos.x);
                maxX = int.Max(maxX, members[i].pos.x);

                minY = int.Min(minY, members[i].pos.y);
                maxY = int.Max(maxY, members[i].pos.y);
            }
            Bounds = new Rectangle(minX, minY, 1 + maxX - minX, 1 + maxY - minY);
        }

        public static Color MakeColor(int index)
        {
            return Color.FromNonPremultiplied(
                127 + (((index * 27) ^ 287538253 >> 9) & 0x7F),
                127 + (((index * 12) ^ 385792837 >> 7) & 0x7F),
                127 + (((index * 53) ^ 123159321 >> 5) & 0x7F),
                255);

        }

        public void SetColor()
        {
            if (Index <= 0) ForScience = Color.White;
            else ForScience = MakeColor(Index);
        }

        /// <summary>
        /// Splits the branch so that we become a new branch starting with block B.
        /// All other blocks (if any), are separated into a new branch that inputs to B.
        /// </summary>
        /// <param name="b"></param>
        public bool SplitBranch(World w, Chunk cache, KPoint pos, ref BlockData data, int n = -1)
        {
            if (n < 0) n = this[pos];
            if (n <= 0)
            {
                return false;
            }

            //create a new branch for everything in the branch before "pos"
            Branch bN = new Branch(w);
            bN.LastResolvedInputTick = this.LastResolvedInputTick;
            bN.LastResolvedOutputTick = this.LastResolvedOutputTick;
            w.Branches.Assign(bN);
            w.BranchUpdater.FlagUpdate(bN);

            bN.SetColor();

            if (bN.members != null) ArrayPool<BranchBlock>.Shared.Return(bN.members);
            bN.members = ArrayPool<BranchBlock>.Shared.Rent(n);
            bN.member_count = n;
            bN.RenderWorkTicks = this.RenderWorkTicks;



            //copy the data into the things
            Array.Copy(members, 0, bN.members, 0, n);
            member_count -= n;
            Array.Copy(members, n, members, 0, member_count);
            Trim();

            bN.ClearInOut();
            ClearInOut();

            //update these graphs
            Chunk c = w.GetChunkFromWorldCoords(pos);
            for (int i = 0; i < Size; i++)
            {
                c = w.GetChunkFromWorldCoords(members[i].pos, c);
                members[i].branchindex = Index;
                int mask = 1 << members[i].dir;
                if (i > 0) mask |= (1 << ((members[i - 1].dir + 2) & 0x3));
                c.SetBranchAndData(members[i].pos, Index, (uint)mask);
                c.Flag(LayerManager.ConveyorLayer);
                members[i].mask = (byte)mask;
            }

            for (int i = 0; i < bN.Size; i++)
            {
                c = w.GetChunkFromWorldCoords(bN.members[i].pos, c);
                bN.members[i].branchindex = bN.Index;
                int mask = 1 << bN.members[i].dir;
                if (i > 0) mask |= (1 << ((bN.members[i - 1].dir + 2 ) & 0x3));
                c.SetBranchAndData(bN.members[i].pos, bN.Index, (uint)mask);
                c.Flag(LayerManager.ConveyorLayer);
                bN.members[i].mask = (byte)mask;
            }

            data.branchindex = Index;

            _cachedHead = (BlockGraphed)HeadDef;
            _cachedTail = (BlockGraphed)TailDef;
            bN._cachedHead = (BlockGraphed)bN.HeadDef;
            bN._cachedTail = (BlockGraphed)bN.TailDef;
            RendersItems = _cachedHead.RendersItems;
            bN.RendersItems = bN._cachedHead.RendersItems;


            //(1) contains everything before this block

            //and this now contains everything afterwards
            //So we need to split the inventory at the position
            if (Inventory is SequentialInventory preceding_items)
            {
                //Cut the front out of this inventory
                var subsequent_items = preceding_items.Split(n * Settings.Conveyors.CONVEYOR_PRECISION);
                //we captured the foremost part
                //ci was our inventory so now we have to switch it around
                bN._inventory_index = preceding_items.Index;
                preceding_items.PathLength = bN.Size * Settings.Conveyors.CONVEYOR_PRECISION;

                w.Inventories.Assign(subsequent_items);
                this._inventory_index = subsequent_items.Index;
                subsequent_items.PathLength = Size * Settings.Conveyors.CONVEYOR_PRECISION;

                this.ItemCount = subsequent_items.Size();
                bN.ItemCount = preceding_items.Size();


            }

            ConnectEnds(w, cache, true);
            bN.ConnectEnds(w, cache, true);

            IsBuffered = false;
            bN.IsBuffered = false;

            //update branch bounding
            bN.UpdateBounds();
            UpdateBounds();

            return true;
        }

        /// <summary>
        /// Merges this branch forwards and backwards
        /// </summary>
        public void MergeBranch(World w, Chunk cache)
        {

            //so we need to collect all of the contiguous branches
            List<Branch> available = [this];
            int our_index = 0;
            int our_arr_index = 0;
            int _size = 0;

            //move Forwards
            while (true && _size < Settings.Conveyors.MAX_BRANCH_LENGTH)
            {
                var bN = available.Last();
                _size += bN.Size;
                bN.GetOutputs(out var outputs, out var out_count);
                try
                {
                    if (out_count == 1 && outputs[0].Index != bN.Index && !available.Contains(outputs[0]) && outputs[0].GetInputCount() <= 1)
                    {
                        bN = outputs[0];
                        if (bN.Size + _size < Settings.Conveyors.MAX_BRANCH_LENGTH)
                            available.Add(bN);
                    }
                    else break;
                }
                finally
                {
                    ArrayPool<Branch>.Shared.Return(outputs);
                }
            }

            _size -= available[0].Size;
            //move backwards
            while (true && _size < Settings.Conveyors.MAX_BRANCH_LENGTH)
            {
                var bN = available[0];
                _size += bN.Size;
                bN.GetInputs(out var inputs, out var in_count);
                try
                {
                    if (in_count == 1 && inputs[0] != bN && !available.Contains(inputs[0]) && inputs[0].GetOutputCount() <= 1)
                    {
                        bN = inputs[0];
                        if (bN.Size + _size < Settings.Conveyors.MAX_BRANCH_LENGTH)
                        {
                            our_index += 1;
                            our_arr_index += bN.Size;
                            available.Insert(0, bN);
                        }
                        else break;
                    }
                    else break;
                }
                finally
                {
                    ArrayPool<Branch>.Shared.Return(inputs);
                }
            }


            if (available.Count <= 1) return;
            IsBuffered = false;

            //We have to merge the items before applying resizing logic
            if (Inventory is SequentialInventory si)
            {
                //merge them forwards into a new inventory
                SequentialInventory ni = new SequentialInventory();
                ni.Time = si.Time;
                w.Inventories.Assign(ni);
                //now we merge all of them to ni piece by piece
                for (int i = 0; i < available.Count; i++)
                {
                    ni.Append(available[i].Inventory);
                }
                _inventory_index = ni.Index;
                ItemCount = ni.Size();
            }

            var len = 0;
            foreach (var b in available)
                len += b.Size;

            //Get a new array from the pool
            //In theory we can skip this when the new array fits into our current array
            //But since we're already pooling, the benefit is negligible
            BranchBlock[] _new = ArrayPool<BranchBlock>.Shared.Rent(len);
            int n = 0;
            for (int i = 0; i < available.Count; i++)
            {
                Array.Copy(available[i].members, 0, _new, n, available[i].Size);
                n += available[i].Size;
            }
            if (members != null) ArrayPool<BranchBlock>.Shared.Return(members);
            members = _new;
            member_count = len;

            //Now update all of the branch indices in the world storage
            Chunk c = w.GetChunkFromWorldCoords(members[0].pos); //cache
            for (int i = 0; i < Size; i++)
            {
                c = w.GetChunkFromWorldCoords(members[i].pos, c); //update cache
                members[i].branchindex = Index; //update index in the branch member
                int mask = 1 << members[i].dir;
                if (i > 0) mask |= (1 << ((members[i - 1].dir + 2) & 0x3));
                c.SetBranchAndData(members[i].pos, Index, (uint)mask);
                c.Flag(LayerManager.ConveyorLayer);
                members[i].mask = (byte)mask;
            }

            //now remember to YEET the inventories in a moment

            //now connect our ends
            //for which the first step is to disconnect our ends
            for(int i = 0; i < available.Count; i++)
            {
                var b = available[i];
                b.ClearInOut();

                if (b != this)
                {
                    b.member_count = 0;
                    w.Branches.Free(b);
                    if (b.members != null) ArrayPool<BranchBlock>.Shared.Return(b.members);
                    //remember to yeet the inventories as well
                    if (b.Inventory != null) w.Inventories.Free(b.Inventory);
                }
            }



            //and then just connect I guess
            ConnectEnds(w, cache, true);

            UpdateBounds();

        }

        /// <summary>
        /// Recalculates the end connections of this branch
        /// </summary>
        public void ConnectEnds(World w, Chunk cache, bool skipMerge = false)
        {

            ClearInOut();

            //get our stuff
            BlockGraphed h = (BlockGraphed)HeadDef;
            BlockGraphed t = (BlockGraphed)TailDef;
            BlockData hd = w.GetBlock(this.Head.pos, cache);
            BlockData td = w.GetBlock(Tail.pos, cache);

            int hmask = 1 << Head.dir;

            h.AdjacentPoints(Head.pos, out var points, out int count);

            //connect the head to anything touching it
            for (int i = 0; i < count; i++)
            {
                var nextPos = points[i];
                var nextData = w.GetBlock(nextPos, cache);
                if (nextData.id > 0 && nextData.branchindex > 0 &&// nextData.branchindex != this.Index &&
                    nextData.block is BlockGraphed bg &&
                    h.ReceivesInput(w, Head.pos, hd, nextPos, nextData))
                {
                    var dir = i & 0x3;
                    var b = nextData.branch(w);
                    b.LastResolvedInputTick = w.Tick;
                    b.LastResolvedOutputTick = w.Tick;
                    if (b != null)
                    {
                        AddInput(b, (Dir)(dir));
                        hmask |= 1 << (dir);
                    }
                }
            }

            var c = w.GetChunkFromWorldCoords(Head.pos, cache);
            c.SetData(Head.pos, (uint)hmask);
            c.Flag(LayerManager.ConveyorLayer);

            members[0].mask = (byte)hmask;

            ArrayPool<KPoint>.Shared.Return(points);

            t.AdjacentPoints(Tail.pos, out points, out count);


            //connect the tail to anything touching it
            for (int i = 0; i < count; i++)
            {
                var nextPos = points[i];
                var nextData = w.GetBlock(nextPos, cache);
                if (nextData.id > 0 && nextData.branchindex > 0 &&// nextData.branchindex != this.Index &&
                    nextData.block is BlockGraphed bg && 
                    bg.ReceivesInput(w, nextPos, nextData, Tail.pos, td))
                {
                    Branch b = w.Branches[nextData.branchindex];
                    if (b != null)
                    {
                        int dir = i & 0x3;
                        //split the branch at nextPos
                        b.SplitBranch(w, cache, nextPos, ref nextData);
                        //now attach us as an output to its head
                        AddOutput(b, (Dir)(dir));
                        //it is necessarily a head or a tail
                        if (nextPos == b.Head.pos) b.members[0].mask |= (byte)(1 << ((dir + 2) & 0x3));
                        else if (nextPos == b.Tail.pos) b.members[0].mask |= (byte)(1 << ((dir + 2) & 0x3));

                        b.LastResolvedInputTick = w.Tick;
                        b.LastResolvedOutputTick = w.Tick;
                        c = w.GetChunkFromWorldCoords(nextPos, cache);
                        c.OrData(nextPos, (uint)(1 << ((dir + 2) & 0x3)));
                        c.Flag(LayerManager.ConveyorLayer);
                    }
                }
            }

            ArrayPool<KPoint>.Shared.Return(points);

            LastResolvedInputTick = w.Tick;
            LastResolvedOutputTick = w.Tick;

            //And try to merge the branches?
            //This might be redundant
            if (!skipMerge) MergeBranch(w, cache);
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
            p.owner.promises_out.Enqueue(p);
        }

    }
}
