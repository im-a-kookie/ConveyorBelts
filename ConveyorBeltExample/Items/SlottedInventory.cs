using ConveyorBeltExample.Items;
using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConveyorEngine.Items.SequentialInventory;

namespace ConveyorEngine.Items
{
    public class SlottedInventory : Inventory
    {
        
        public Item[] Items;


        public readonly int SlotsForOuput;
        public readonly int SlotsForInput;

        public SlottedInventory(int inputs, int outputs)
        {
            Items = new Item[inputs + outputs];
            SlotsForOuput = outputs;
            SlotsForInput = inputs;
        }

        public int Slots { get => Items.Length; }

        public Item Get(int slot)
        { 
            return Items[slot];
        }

        public void Set(int slot, Item item)
        {
            Items[slot] = item;
        }

        /// <summary>
        /// Tries to add an item to the inventory. Returns the number of items added from the item stack.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int Add(Item item)
        {
            //try to add it sequentially to the slots
            //see if we can stack iwth SlotsForInput
            ItemDefinition id0 = ItemManager.ItemRegistry[item.id];
            for (int i = 0; i < SlotsForInput; i++)
            {
               if (Items[i] == item)
               {
                    short n = item.stack;
                    short d = (short)(Items[i].stack + n);
                    short over = short.Max(0, (short)(d - id0.StackLimit));
                    Items[i].stack = d;
                    item.stack = over;
                    return n - over;
               }
            }

            //we failed to add to an existing thing so
            for (int i = 0; i < SlotsForInput; i++)
            {
                if (Items[i].id <= 0)
                {
                    Items[i] = item;
                    return item.stack;
                }
            }

            return 0;
        }

        /// <summary>
        /// Grabs the "first" or an otherwise random output item from the inventory
        /// </summary>
        /// <param name="outputOnly"></param>
        /// <returns></returns>
        public Item TakeOutput(short amount = 1, bool outputOnly = true)
        {
            Item val = new();
            for (int i = outputOnly ? SlotsForInput : 0; i < SlotsForInput + SlotsForOuput && amount > 0; i++)
            {
                //Make sure it has an item, and there are items stacked up, and we match the val item if it's set
                if (Items[i].id > 0 && Items[i].stack > 0 && (val.id < 0 || val == Items[i]))
                {
                    //we can grab up to stack_limit minus the current stack amount
                    short max = (short)(ItemManager.ItemRegistry[Items[i].id].StackLimit - val.stack);
                    short n = short.Min(Items[i].stack, (short)amount); //but also capped to the "amount" we want
                    n = short.Min(n, max);
                    //deduct the items
                    Items[i].stack -= n;
                    if (Items[i].stack <= 0) Items[i].id = 0;
                    amount -= n;
                    val = Items[i];
                    val.stack += n;
                    if (n == max || val.stack > ItemManager.ItemRegistry[val.id].StackLimit) return val;
                }
                
            }
            return val;
        }

        public override bool RemoveFromInventory(Item item, int slot, long ext)
        {
            //see if we can take an item, kinda like TakeOutput but with a specific item
            for (int i = 0; i < SlotsForInput + SlotsForOuput; i++)
            {
                //Make sure it has an item, and there are items stacked up, and we match the val item if it's set
                if (Items[i].id > 0 && Items[i].stack > 0 && Items[i] == item)
                {
                    //take one from this thing
                    Items[i].stack -= 1;
                    if (Items[i].stack <= 0) Items[i].id = -1;
                    return true;
                }
            }
            return false;
        }

        public override int GetItemSlots()
        {
            return Items.Length;
        }

        public override void Join(Inventory i)
        {
            throw new NotImplementedException();
        }

        public override Item TakeFirst(int amount, int mode = 0)
        {
            if ((mode & 0x1) == 0)
            { 
                for (int i = SlotsForInput; i < SlotsForOuput + SlotsForInput; i++)
                {
                    if (Items[i].id > 0)
                    {
                        Items[i].stack -= 1;
                        if (Items[i].stack <= 0) Items[i].id = -1;
                        return new Item() { id = Items[i].id, ext = Items[i].ext, stack = 1 };
                    }
                }
            }
            if (mode == 0 || mode == 1)
            {
                for (int i = 0; i < SlotsForInput; i++)
                {
                    if (Items[i].id > 0)
                    {
                        Items[i].stack -= 1;
                        if (Items[i].stack <= 0) Items[i].id = -1;
                        return new Item() { id = Items[i].id, ext = Items[i].ext, stack = 1 };
                    }
                }
            }
            return default;
        }

        public override bool Put(Item i, int ext,bool forced = true)
        {
            throw new NotImplementedException();
        }

        public override int Size()
        {
            return 0;
        }

        public override Deque<ConveyorGroup> GetSequentialItems()
        {
            return null;
        }

        public override SequentialInventory GetAsSequential()
        {
            return null;
        }
    }


}
