using ConveyorBeltExample;
using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorEngine.Graphics;
using ConveyorEngine.Graphing;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks.BlockDefinitions
{
    public class BlockDrill : BlockGraphed
    {


        public BlockDrill()
        {
            Bounds = new(0, 0, 2, 2);
        }

        public override int GetLayer()
        {
            return LayerManager.BuildingLayer;
        }

        public override bool ProvidesOutput(World world, KPoint pos, BlockData data, KPoint receiverPos, BlockData receiverData)
        {
            return true;
        }

        public override long UpdateTick(BranchUpdater u, World w, long time, Branch b)
        {
            //1. Figure out how much time is passed
            //2. Empty out all complete promises
            //3. Take all promises that fit
            //4. Do as much work as we can before jamming
            //5. See how much more work we can do
            //6. Make promises forwards
            //get the data in each member of the branch and attempt to produce things from them
            Chunk c = null;
            for(int i = 0; i < b.member_count; ++i)
            {
                BranchBlock bl = b.members[i];
                c = w.GetChunkFromWorldCoords(bl.pos, c);
                BlockData d = w.GetBlock(bl.pos, c);
                //increment it
                //the drill takes a while to spin up
                //and you can see this in the animation?
                //nah who cares for now
                float stored_time = d.Float + (float)(time - b.LastResolvedOutputTick) / Settings.Engine.TICK_RATE;
                b.GetInputs(out var inputs, out var in_count);

                while (stored_time > 0)
                {
                    bool put = false;
                    //try to produce an item and output it from the branch outputs
                    for(int j = 0; j < in_count; j++)
                    {
                        if (inputs[j].GetInventory().Put(new Item(1, 0, 0, (byte)(inputs[j].Head.pos - bl.pos).Direction()), 0, true))
                        {
                            stored_time -= 1;
                            put = true;
                            break;
                        }
                    }
                    if (!put) stored_time = 0;
                }
                c.SetData(bl.pos, BitConverter.SingleToUInt32Bits(stored_time));
                ArrayPool<Branch>.Shared.Return(inputs);
            }
            return base.UpdateTick(u, w, time, b);
        }

        public override bool IsHomogenous(World w, Chunk cache, KPoint pos, BlockData data)
        {
            return false;
        }

    }
}
