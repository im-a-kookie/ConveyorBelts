using ConveyorBeltExample.GameWorld;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static ConveyorEngine.Items.SequentialInventory;

namespace ConveyorBeltExample.Items
{
    public abstract class Inventory : Indexable
    {

        public long Time;

        public int Index { get; set; }
        public IPool parent { get; set; }

        public abstract int Size();

        public abstract Deque<ConveyorGroup> GetSequentialItems();

        public abstract SequentialInventory GetAsSequential();


        //An inventory is a thing which contains items
        //An inventory tends to have slots I guess?
        //idfk, but we will use the inventory for all the things

        public abstract bool RemoveFromInventory(Item i, int slot, long ext);

        /// <summary>
        /// Gets the number of slots in this inventory
        /// </summary>
        /// <returns></returns>
        public abstract int GetItemSlots();

        /// <summary>
        /// Joins I onto the end of this inventory
        /// </summary>
        /// <param name="i"></param>
        public abstract void Join(Inventory i);

        /// <summary>
        /// Inserts the item into this. Assume ordering.
        /// <para>ext indicates "progress" from a conveyor.</para>
        /// </summary>
        /// <param name="i"></param>
        public abstract bool Put( Item i, int ext, bool forced = true);

        /// <summary>
        /// Takes the first item from this inventory.
        /// </summary>
        /// <param name="mode">0 = any item prioritizing output channels. 1 = inputs only, 2 = outputs only. </param>
        public abstract Item TakeFirst(int maxAmount = 1, int mode = 0);

    }
}
