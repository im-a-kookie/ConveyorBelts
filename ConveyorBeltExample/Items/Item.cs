using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Items
{
    /// <summary>
    /// The Item class. Mostly used for... stuff.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Item
    {

        public static bool Flagged = true;

        public static Item Empty = new Item(0, 0, 0, 0);

        public short id;
        public int ext;
        public short stack;
        public byte dir;
        public bool flagged = false;

        public Item() 
        {
            flagged = Flagged;
            Flagged = !flagged;
        }

        public Item(short id, int ext, short stack, byte dir)
        {
            this.ext = ext; 
            this.stack = stack;
            this.dir = dir;
            this.id = id;
            //always be flaggering
            flagged = Flagged;
            Flagged = !flagged;
        }

        public Item Copy()
        {
            return new Item(id, ext, stack, dir) { flagged = this.flagged };
        }

        public static bool operator ==(Item left, Item right)
        {
            return (left.id == right.id && left.ext == right.ext);
        }

        public static bool operator !=(Item left, Item right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return (obj is Item i && i == this);
        }
    }
}
