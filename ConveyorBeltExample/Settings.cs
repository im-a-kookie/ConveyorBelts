using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample
{
    internal class Settings
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
        /// <summary>
        /// Bitshift for conveyor item placement precision
        /// </summary>
        public const int CONVEYOR_BITSHIFT = 16;
        /// <summary>
        /// Density of items on conveyors (how many items in a line fit in a block)
        /// </summary>
        public const int CONVEYOR_ITEM_DENSITY = 3;
        /// <summary>
        /// The conveyor precision actual value
        /// </summary>
        public const int CONVEYOR_PRECISION = 1 << CONVEYOR_BITSHIFT;
        /// <summary>
        /// A binary mask for the conveyor precision (e.g 0 < value & mask < precision)
        /// </summary>
        public const int CONVEYOR_MASK = CONVEYOR_PRECISION - 1;
        /// <summary>
        /// The spacing value for items on conveyor belts
        /// </summary>
        public const int CONVEYOR_SPACING = CONVEYOR_PRECISION / CONVEYOR_ITEM_DENSITY;
        /// <summary>
        /// The merge threshold for items on conveyor belts being smooshed together
        /// </summary>
        public const int CONVEYOR_MERGE_THRESHOLD = 1 + (CONVEYOR_PRECISION / 64);
        


    }



}
