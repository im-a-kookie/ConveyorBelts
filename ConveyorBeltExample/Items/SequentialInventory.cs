using ConveyorBeltExample;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Items;
using ConveyorEngine.Util;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ConveyorEngine.Items
{
    /// <summary>
    /// A strictly ordered inventory, used for conveyor belts and the like.
    /// <para>Accepts random insertion and extraction, but is optimized for head/tail usage.</para>
    /// </summary>
    public class SequentialInventory : Inventory
    {

        /// <summary>
        /// Items go brr
        /// </summary>
        public Deque<ConveyorGroup> Items = new Deque<ConveyorGroup>();

        /// <summary>
        /// The amount of space at the head of the inventory, in belt units
        /// </summary>
        public int SpaceAtHead = 0;
        /// <summary>
        /// The total length that the inventory represents, in belt units
        /// </summary>
        public int PathLength = 0;

        /// <summary>
        /// The number of belt units of the first item from the end of the belt
        /// </summary>
        public int UnitsFromTail => Items.Count == 0 ? PathLength : Items[0].gap;

        /// <summary>
        /// The number of item GROUPS in this inventory
        /// </summary>
        /// <returns></returns>
        public override int Size() => Items.Count;

        /// <summary>
        /// Gets this as a sequential item thing.
        /// <para>This gives some flexibility, and the profiler also says that it's faster than casting, so ???</para>
        /// </summary>
        /// <returns></returns>
        public override Deque<ConveyorGroup> GetSequentialItems()
        {
            return Items;
        }

        /// <summary>
        /// Gets this as a sequential inventory.
        /// <para>See <seealso cref="GetSequentialItems"/> for reasoning.</para>
        /// </summary>
        /// <returns></returns>
        public override SequentialInventory GetAsSequential()
        {
            return this;

        }
        /// <summary>
        /// A genetic group of matching items
        /// </summary>
        public class ConveyorGroup
        {
            /// <summary>
            /// The item for this group
            /// </summary>
            public Item front;
            /// <summary>
            /// The number of these items in this group
            /// </summary>
            public int number;
            /// <summary>
            /// The number of items in this group
            /// </summary>
            /// <returns></returns>
            public int Count() => number;
            /// <summary>
            /// Attempts to peek the item at the Nth position in this group.
            /// </summary>
            /// <param name="n"></param>
            /// <returns>The item, if there is one</returns>
            public Item Peek(int n) => (n >= Count() ? Item.Empty : front);

            /// <summary>
            /// The gap in front of this item on the current belt. This is where the magic happens.
            /// </summary>
            public int gap;
            /// <summary>
            /// The size that this group occupies on the belt. Gap+Length yields the tail position of this group.
            /// </summary>
            public int BeltLength => number * Settings.Conveyors.ITEM_WIDTH;

            //this is not ideal with a list ughhhhh
            public int AddEnd(Deque<Item> items)
            {
                int n = items.Count;
                //int matches = 0;
                //for (int i = 0; i < items.Count; i++) if (items[i] == front) ++matches; else break;
                //number += matches;
                //if (matches > 0) items.RemoveRange(0, matches);
                while (items.PeekFirst() == front) { ++number; items.TakeFirst(); }
                return n - items.Count;
            }

            /// <summary>
            /// Takes the first n items out of this group
            /// </summary>
            /// <param name="count"></param>
            /// <returns></returns>
            public Item[] TakeFront(int count)
            {
                if (count == 0) return null;
                count = int.Min(count, Count());
                Item[] result = new Item[count];
                Array.Fill(result, front);
                //unflip the flip
                if ((count & 0x1) != 0) front.flagged = !front.flagged;
                number -= count;
                gap += count * Settings.Conveyors.ITEM_WIDTH;
                return result;
            }

            /// <summary>
            /// Joins this group into the group ahead
            /// </summary>
            /// <param name="ahead"></param>
            public void Join(ConveyorGroup ahead)
            {
                //We can trivially join a Like group
                if(front == ahead.front)
                {
                    ahead.number += number;
                    number = 0;
                    gap = -1;
                }
                
            }
        }

        /// <summary>
        /// Sledgehammer gap recalculation. Needs to be done sometimes.
        /// </summary>
        public void RecalculateGaps()
        {
            SpaceAtHead = PathLength;
            for(int i = 0; i < Items.Count; i++)
            {
                SpaceAtHead -= Items[i].gap + Items[i].BeltLength;
            }
        }

        /// <summary>
        /// Advances the items in the inventory by the given units.
        /// </summary>
        /// <param name="time">The number of ticks to advance</param>
        /// <param name="steps_per_tick">The belt units to move with each tick</param>
        /// <returns>The number of WHOLE ticks consumed</returns>
        public int ForceToTicksUnaware(long time, int steps_per_tick)
        {
            int ticks = (int)(time - Time);
            if (ticks <= 0) return 0;
            int dist = ticks * steps_per_tick; //the furthest we can move
            for(int i = 0; i < Items.Count && dist > 0; i++)
            {
                //we move the item forwards and clamp it
                int d_lost = dist - Items[i].gap;
                Items[i].gap = int.Max(0, Items[i].gap - dist);
                if (Items[i].gap < Settings.Conveyors.CONVEYOR_MERGE_THRESHOLD && i > 0 && Items[i].front == Items[i - 1].front)
                {
                    Items[i].Join(Items[i - 1]);
                    if (Items[i].number == 0) Items.TakeArbitrary(i--);
                }
                SpaceAtHead += dist;
                dist = d_lost;
            }
            Time = time;
            return ticks;
        }

        /// <summary>
        /// Advances the items in the inventory by the given units
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="force">Whether to force all items forwards</param>
        /// <returns>The number of WHOLE ticks consumed</returns>
        public int AdvanceTicks(int ticks, int steps_per_tick)
        {
            if (Items.Count == 0) return ticks;
            int furthest = int.Min(ticks, (Items[0].gap / steps_per_tick));
            Items[0].gap -= furthest * steps_per_tick;
            SpaceAtHead += furthest * steps_per_tick;
            return furthest;
        }


        /// <summary>
        /// Advances the items in the inventory by the given units
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="force">Whether to force all items forwards</param>
        /// <returns>The actual distance advanced</returns>
        public int AdvanceUnits(int distance, bool force)
        {
            if (Items.Count == 0) return distance;
            if(force)
            {
                for(int i = 0; i < Items.Count && distance > 0; ++i)
                {
                    int n = int.Max(0, distance - Items[i].gap);
                    Items[i].gap = int.Max(0, Items[i].gap - distance);
                    distance -= n;
                    SpaceAtHead += n;
                }
                return distance;
            }
            else
            {
                //splonk it to the front
                //remember that the front item is technically offset by half of an item width
                int n = int.Max(0, distance - Items[0].gap);
                int _g = Items[0].gap;
                Items[0].gap = int.Max(0, Items[0].gap - distance);
                SpaceAtHead += Items[0].gap - _g;
                return n;
            }
        }




        public override int GetItemSlots()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Splits and returns a new inventory From the given pos (input=0) to the end (output=pathlength) of the belt
        /// </summary>
        /// <param name="pos">Position from the head of the conveyor</param>
        /// <returns>The new inventory, chopped off </returns>
        public SequentialInventory Split(int pos)
        {

            //1. Ensure pos is block rounded? Nah it will already do that
            RecalculateGaps();

            //the easiest is if we fall in the empty head
            if (pos < SpaceAtHead)
            {
                SequentialInventory si = new SequentialInventory() { Time = this.Time };
                si.Items = Items;
                Items = new Deque<ConveyorGroup>();
                return si;
            }
            

            //we do not fall within the head space so we do have to split
            //start from the start
            //start at the end of the track and move backwards
            int track = PathLength;
            for(int i = 0; i < Items.Count; i++)
            {
                int step_start = track - Items[i].gap;
                int step_end = step_start - Items[i].BeltLength;

                if (pos > step_start)
                {
                    //  [     ] ->    gap    |CUT|  ???   [    Item    ] -> gap    .....
                    // End  Start             pos       Track                  PathLength
                    //remove everything before index i
                    SequentialInventory si = new SequentialInventory() { Time = this.Time };
                    for (int n = 0; n < i; n++)
                    {
                        si.Items.AddLast(Items.TakeFirst());
                    }

                    //now adjust item i gap to point correctly
                    si.PathLength = this.PathLength - pos;
                    si.SpaceAtHead = track - pos;
                    Items[0].gap = pos - step_start;
                    this.PathLength = pos;
                    track = step_end;
                    return si;
                }
                else if (pos > step_end)
                {
                    //cut index i at the position
                    //  [     |CUT|            ] ->    gap       [    Item    ] -> gap    .....
                    // End     pos           Start             Track

                    //so we take every item whose front exceeds pos
                    //which we can determine index-wise based on
                    int items = 1 + (step_start - pos) / Settings.Conveyors.ITEM_WIDTH;
                    ConveyorGroup g = new ConveyorGroup() { front = Items[i].front, number = items, gap = Items[i].gap };
                    //remove this many from the front of the thing
                    var ii = Items[i].TakeFront(items);
                    if (Items[i].number <= 0)
                    {
                        //pluck it out of the collection
                        Items.TakeArbitrary(i);
                        if(i > Items.Count) Items[i].gap = pos - (step_start - Settings.Conveyors.ITEM_WIDTH * items);
                        else SpaceAtHead = pos - (step_start - Settings.Conveyors.ITEM_WIDTH * items);
                    }
                    else
                    {
                        Items[i].gap = pos - (step_start - Settings.Conveyors.ITEM_WIDTH * items);
                    }
                    SequentialInventory si = new SequentialInventory() { Time = this.Time };
                    for (int n = 0; n < i; n++)  si.Items.AddLast(Items.TakeFirst());
                    si.Items.AddLast(g);
                    si.PathLength = this.PathLength - pos;
                    this.PathLength = pos;
                    si.SpaceAtHead = step_start - Settings.Conveyors.ITEM_WIDTH * items;
                    return si;
                }
                else
                {
                    track = step_end;
                    continue;
                }
            }

            return new SequentialInventory() { Time = this.Time };
        }


        /// <summary>
        /// Appends i to the end of this inventory (e.g extending a conveyor forwards
        /// </summary>
        /// <param name="i"></param>
        public void Append(Inventory i)
        {
            if (i is SequentialInventory si)
            {
                RecalculateGaps();
                si.RecalculateGaps();
                if (Items.Count > 0) Items[0].gap += si.SpaceAtHead;

                Items.AddRangeFirst(si.Items);
                si.Items.Clear();
                Time = si.Time;
                PathLength += si.PathLength;
            }
        }

        public override void Join(Inventory i)
        {
            //we can smash
            if (i is SequentialInventory s)
            {
                //1. Calculate the new path length
                int n = PathLength + s.PathLength;
                //ummmm so like
                //we need to increase the path length of the first item in s to our tail space
                if (s.Items.Count > 0) s.Items[0].gap += SpaceAtHead;
                //now we can just add it all
                Items.AddRangeLast(s.Items);
                s.Items.Clear();
            }
        }

        public override bool Put(Item i, int ext, bool pushed = true)
        {
            RecalculateGaps();
            //Puts a new item
            if (pushed)
            {
                if(SpaceAtHead >= 0)
                {
                    int best = int.Min(SpaceAtHead, ext);

                    int gap = SpaceAtHead - best;
                    if (gap < Settings.Conveyors.CONVEYOR_MERGE_THRESHOLD && Items.Count > 0 && Items.PeekLast().front == i)
                    {
                        Items.PeekLast().number += 1;
                        SpaceAtHead -= gap + Settings.Conveyors.ITEM_WIDTH;
                    }
                    else
                    {

                        ConveyorGroup g = new ConveyorGroup { front = i, number = 1, gap = SpaceAtHead - best };
                        Items.AddLast(g);
                        SpaceAtHead -= g.gap + g.BeltLength;
                    }
                    //We put it into a space at the front
                    //
                    return true;
                }
                return false;
            }
            else
            {
                if (Items.Count == 0) SpaceAtHead = PathLength;

                //unforced means we are trying to place the item *at* the given position
                if (Items.Count == 0 || (ext < SpaceAtHead && SpaceAtHead >= Settings.Conveyors.ITEM_WIDTH))
                {
                    //simple placement
                    ConveyorGroup g = new ConveyorGroup { front = i, number = 1, gap = SpaceAtHead - ext };
                    Items.AddLast(g);
                    SpaceAtHead -= ext;
                    return true;
                }
                else if (Items[0].gap > Settings.Conveyors.ITEM_WIDTH && ext > PathLength - Items[0].gap + Settings.Conveyors.ITEM_WIDTH)
                {
                    ConveyorGroup g = new ConveyorGroup { front = i, number = 1, gap = PathLength - ext };
                    Items.AddFirst(g);
                    Items[0].gap -= (g.gap + g.BeltLength);
                    return true;
                }
                else
                {
                    //complex placement
                    int track_length = PathLength - (Items[0].gap + Items[0].BeltLength);
                    for(int pos = 1; pos < Items.Count; pos++)
                    {
                        //see if we fit in front of the item
                        if (Items[pos].gap >= Settings.Conveyors.ITEM_WIDTH && ext > track_length - Items[pos].gap + Settings.Conveyors.ITEM_WIDTH)
                        {
                            ConveyorGroup g = new ConveyorGroup { front = i, number = 1, gap = PathLength - ext  };
                            Items[pos].gap -= (g.gap + g.BeltLength);
                            Items.Insert(pos, g);
                            return true;
                        }
                        track_length -= Items[pos].gap + Items[pos].BeltLength;
                    }
                }
            }
            return false;
        }

        public void TakeGroup(int n)
        {
            var g = Items.TakeArbitrary(n);
            if (n < Items.Count) Items[n].gap += (g.gap + g.BeltLength);
            else SpaceAtHead += (g.gap + g.BeltLength);
        }

        public override bool RemoveFromInventory(Item i, int slot, long ext)
        {
            //removes the first matching item 
            for(int n = 0; n < Items.Count; ++n)
            {
                ConveyorGroup g = Items[n];
                if (g.Count() > 0)
                {
                    Item ki = g.Peek(0);
                    if (ki == i)
                    {
                        g.TakeFront(1);
                        if (g.Count() <= 0)
                        {
                            TakeGroup(n);
                        }
                        return true;
                    }
                    return false;
                }
                else
                {
                    TakeGroup(n);
                }
            }
            return false;
        }

        public override Item TakeFirst(int maxAmount = 1, int mode = 0)
        {

            //take the first item out of here
            while (true)
            {
                if (Items.Count == 0) return new Item() { id = 0 };
                var g = Items.PeekFirst();
                if (g.Count() > 0)
                {
                    Items.TakeFirst();
                    continue;
                }
                Item i = g.TakeFront(1)[0];
                if (g.Count() <= 0) Items.TakeFirst();
                if (Items.Count > 0)
                {
                    Items[0].gap += g.gap;
                    //change the flag status of the front item?
                    //Items[0].front.flagged = !Items[0].front.flagged;
                }
                return i;
            }


        }
    }
}
