using ConveyorBeltExample;
using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using ConveyorEngine.Blocks;
using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Graphing
{
    public struct BranchBlock
    {
        public KPoint pos;
        public short id;
        public int branchindex;
        public byte dir;
        public byte mask;

        public Dir Direction => (Dir)(dir & 0x3);


        public BranchBlock(KPoint pos, BlockData data)
        {
            this.pos = pos;
            this.id = data.id;
            this.branchindex = data.branchindex;
            this.dir = (byte)(data.dir);
            this.mask = (byte)data.Int;
        }
        public BlockDefinition definition => BlockManager.BlockIndexMap[this.id];
        public Branch branch(World w) => w.Branches[branchindex];
        public BlockDefinition def => BlockManager.BlockIndexMap[id];

        public override string ToString()
        {
            return pos.ToString() + "=" + (def == null ? id : def.name) + "↑→↓←"[dir] + ", " + branchindex;
        }

    }
}
