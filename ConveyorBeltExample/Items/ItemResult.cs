using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Items
{
    [StructLayout(LayoutKind.Sequential)]
    internal class ItemResult
    {
        public short id;
        public int ext;
        public int progress;
        public ItemResult(short id, int ext, int progress)
        {
            this.id = id;
            this.ext = ext;
            this.progress = progress;
        }   
    }
}
