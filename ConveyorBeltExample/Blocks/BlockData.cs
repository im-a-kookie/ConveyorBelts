using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using ConveyorEngine.Blocks;
using ConveyorEngine.Graphing;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Blocks
{
    //a block is an id byte
    //a branch index int32
    //int data
    //int data
    public struct BlockData
    {
        /// <summary>
        /// An ID ref for this block data (links back to the block collector. IDs are consistent.
        /// </summary>
        public short id;
        /// <summary>
        /// The block definition represented by this data object
        /// </summary>
        public BlockDefinition block => BlockManager.BlockIndexMap[id];

        /// <summary>
        /// The branch index of this block. Can be -1 if the block is not graphable
        /// </summary>
        public int branchindex;
        /// <summary>
        /// The branch referenced by this block
        /// </summary>
        public Branch branch(World w) => branchindex < 0 ? null : w.Branches[branchindex];
        /// <summary>
        /// The data stored in this block
        /// </summary>
        public uint data;

        /// <summary>
        /// The direction of the block stored as a byte
        /// </summary>
        public byte dir;
        /// <summary>
        /// The direction of the block using the enum
        /// </summary>
        public Dir direction => (Dir)dir;

        public static BlockData EMPTY = new(0, Dir.NORTH, 0, 0);

        public BlockData(int id, Dir dir, int branchindex = -1, uint data = 0)
        {
            this.id = (byte)id;
            this.dir = (byte)dir;
            this.branchindex = branchindex;
            this.data = data;
        }

        /// <summary>
        /// Generates a stub block data for other methods. Does not contain ext data,
        /// basically just direction
        /// </summary>
        /// <param name="stub"></param>
        public BlockData(BranchBlock stub)
        {
            id = stub.id;
            branchindex = stub.branchindex;
            dir = stub.dir;
        }

        public BlockData(byte[] bytes)
        {
            if (Marshal.SizeOf(typeof(BlockData)) != bytes.Length) throw new Exception("Serialization Failure!");
            id = BitConverter.ToInt16(bytes, 0);
            branchindex = BitConverter.ToInt16(bytes, 2);
            dir = bytes[6];
            data = BitConverter.ToUInt32(bytes, 7);
        }

        public string ToDebugString()
        {
            return "[id=" + id + ",d=" + dir + ",b=" + branchindex + ",v=" + data +"]";
        }

        public byte[] ToBytes()
        {
            byte[] output = new byte[Marshal.SizeOf(typeof(BlockData))];
            BitConverter.GetBytes(id).CopyTo(output, 0);
            BitConverter.GetBytes(branchindex).CopyTo(output, 2);
            output[6] = dir;
            BitConverter.GetBytes(data).CopyTo(output, 7);
            return output;
        }


        #region Block Data Accessors
        /// <summary>  float representation of block storage data  </summary>
        public float Float { get => GetFloat(); set => SetFloat(value); }
        /// <summary>  float[2] array representation of block storage data  </summary>
        /// <summary>  byte[4] array representation of block storage data  </summary>
        public byte[] Bytes { get => GetBytes(); set => SetBytes(value); }
        /// <summary>  int representation of block storage data  </summary>
        public int Int { get => GetInt(); set => SetInt(value); }
        /// <summary>  int[2] array representation of block storage data  </summary>
        public short Short { get => GetShort(); set => SetShort(value); }
        /// <summary>  short[4] representation of block storage data  </summary>
        public short[] Shorts { get => GetShorts(); set => SetShorts(value); }
        /// <summary>  KPoint representation of block storage data  </summary>
        public KIntPoint Point { get => GetPoint(); set => SetPoint(value); }

        /// <summary>
        /// Gets the data stored here as a byte[]
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes() => BitConverter.GetBytes(data);
        /// <summary>
        /// Sets the data stored here as a byte[8]
        /// </summary>
        /// <param name="b"></param>
        public void SetBytes(byte[] b) => BitConverter.ToUInt32(b);
        /// <summary>
        /// Gets the data stored here as double
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// Sets the data stored here as float
        /// </summary>
        /// <param name="f"></param>
        public void SetFloat(float f) => data = (uint)BitConverter.SingleToUInt32Bits(f);
        /// <summary>
        /// Gets the data stored here as float
        /// </summary>
        /// <returns></returns>
        public float GetFloat() => BitConverter.UInt32BitsToSingle(data);

        /// <summary>
        /// Sets the data stored here as an int
        /// </summary>
        /// <param name="f"></param>
        public void SetInt(int f) => data = (uint)f;
        /// <summary>
        /// Gets the data stored here as an int
        /// </summary>
        /// <returns></returns>
        public int GetInt() => (int)data;

        /// <summary>
        /// Sets the data stored herea as a short
        /// </summary>
        /// <param name="f"></param>
        public void SetShort(short f) => data = (uint)(f);
        /// <summary>
        /// Gets the data stored here as a short
        /// </summary>
        /// <returns></returns>
        public short GetShort() => (short)data;
        /// <summary>
        /// Gets the data stored here as a short[3]
        /// </summary>
        /// <returns></returns>
        public short[] GetShorts() => [(short)data, (short)(data >> 16)];
        /// <summary>
        /// Sets the data stored here as a short[4]
        /// </summary>
        /// <param name="f"></param>
        public void SetShorts(short[] f) => data = (
            ((((uint)f[1]) <<  0) & 0x000000000000FFFF) |
            ((((uint)f[1]) << 16) & 0x00000000FFFF0000) 
            );
        /// <summary>
        /// Gets the data stored here as a Point (x,y)
        /// </summary>
        /// <returns></returns>
        public KIntPoint GetPoint() => new KIntPoint(data);
        /// <summary>
        /// Sets the data stored here as a Point (x,y)
        /// </summary>
        /// <param name="k"></param>
        public void SetPoint(KIntPoint k) => data = k.val;
        #endregion

    }


}
