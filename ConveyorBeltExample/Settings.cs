using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample
{
    public class Settings
    {

        public static class Render
        {
            public static int CONV_NUM = 4;
            public static int CONV_IND = 0;


            public static int TOTAL_BUF = CONV_NUM;
        }


        public static class Engine
        {
            /// <summary>
            /// The number of ticks to perform per second
            /// </summary>
            public const int TICK_RATE = 60;
            /// <summary>
            /// A mask that gets applied to tick counts, causing time to wrap rather than overflow
            /// With a tick rate of 60 and a mas of 7FFFFFFF, we should  get around 1 year worth of days
            /// </summary>
            public const int TICK_MASK = 0x7FFFFFFF;

            public const int WORLD_SIZE_PO2 = 20;
            public const int WORLD_SIZE = 1 << WORLD_SIZE_PO2;

            /// <summary>
            /// The po2 size of chunks
            /// </summary>
            public const int CHUNK_SIZE_PO2 = 7;
            /// <summary>
            /// The actual size of chunks
            /// </summary>
            public const int CHUNK_SIZE = (1 << CHUNK_SIZE_PO2);
            /// <summary>
            /// A bit mask for chunks
            /// </summary>
            public const int CHUNK_MASK = CHUNK_SIZE - 1;

        }

        public static class Conveyors
        {

            /// <summary>
            /// Bitshift for conveyor item placement precision in 2^n
            /// </summary>
            public const int CONVEYOR_BITSHIFT = 7;


            /// <summary>
            /// The conveyor precision actual value
            /// </summary>
            public const int CONVEYOR_PRECISION = 1 << CONVEYOR_BITSHIFT;

            /// <summary>
            /// How precisely the item can be *rendered*. Precision is clamped.
            /// </summary>
            public const int CONVEYOR_RENDER_PRECISION = 64;

            /// <summary>
            /// A binary mask for the conveyor precision (e.g 0 < value & mask < precision)
            /// </summary>
            public const int CONVEYOR_MASK = CONVEYOR_PRECISION - 1;
            /// <summary>
            /// The width of items on conveyors. Can be considered as the spacing also
            /// </summary>
            public const int ITEM_WIDTH = (7 * CONVEYOR_PRECISION) / 8;
            /// <summary>
            /// The merge threshold for items on conveyor belts being smooshed together
            /// </summary>
            public const int CONVEYOR_MERGE_THRESHOLD = 1 + (CONVEYOR_PRECISION / 16);

            public const int MAX_BRANCH_LENGTH = 128;

        }

        public static class Ecology
        {
            /// <summary>
            /// The number of times to segment the chunk into fertility areas
            /// </summary>
            public const int CHUNK_FERILITY_SEGMENTATIONS = 4;

            public const int BLOCKS_PER_FERTILITY = Engine.CHUNK_SIZE >> CHUNK_FERILITY_SEGMENTATIONS;
        }



        public static class Graphing
        {
            public const int INITIAL_BRANCH_BUFFER = 100;
            public const float GROW_MULTIPLIER = 2;
            public const int GROW_STATIC = 100;

        }

    }



}
