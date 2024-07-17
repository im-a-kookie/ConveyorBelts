using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{
    public abstract class Byteable
    {
        /// <summary>
        /// All Byteables must be instantiable from a byte array
        /// </summary>
        /// <param name="data"></param>
        public Byteable(byte[] data) { }

        /// <summary>
        /// All byteables must be convertible to a byte array, which must be consistent with ToBytes
        /// </summary>
        /// <returns></returns>
        public abstract byte[] ToBytes();

    }
}
