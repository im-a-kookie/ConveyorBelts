using ConveyorBeltExample.Blocks;
using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks
{
    public static class BlockManager
    {

        public static BlockDefinition[] BlockIndexMap = new BlockDefinition[1024];

        public static Dictionary<string, BlockDefinition> BlockRegistry = new Dictionary<string, BlockDefinition>();

        /// <summary>
        /// Registers the given block
        /// </summary>
        /// <param name="b"></param>
        public static int RegisterBlock(int id, BlockDefinition b)
        {
            if (BlockIndexMap[id] != null) 
                throw new Exception("Block ID: " + id + " is already registered to " + BlockIndexMap[id].name + "!");
            if (BlockRegistry.ContainsKey(b.name)) 
                throw new Exception("Block " + b.name + " is already registered to ID " + BlockRegistry[b.name].id + "(" + BlockRegistry[b.name] .GetType().FullName + ")" + "!");
            BlockIndexMap[id] = b;
            b.id = (short)id;
            BlockRegistry.Add(b.name, b);
            return id;
        }

        


    }
}
