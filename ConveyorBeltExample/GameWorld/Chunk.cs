using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.Graphing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.GameWorld
{
    internal class Chunk
    {
        public HashSet<Graph> Graphs = new HashSet<Graph>();

        public World world;
        public Point Location;
        public Block[,] Blocks;

        public Chunk (World w, Point pos)
        {
            this.world = w;
            this.Location = pos;
            Blocks = new Block[128, 128];

        }

        public Block GetBlock(int x, int y)
        {
            return Blocks[x, y];
        }

        public byte[] ToBytes()
        {
            //1. Map the blocks
            Dictionary<Type, int> idMap = new Dictionary<Type, int>();
            List<int> idVals = new List<int>();
            List<string> typeVals = new List<string>();

            List<int> idList = new List<int>();
            List<short> coordList = new List<short>();
            List<Dir> dirList = new List<Dir>();
            List<double> workList = new List<double>();
            List<byte[]> dataList = new List<byte[]>();

            foreach (Block b in Blocks)
            {
                int bid = 0;
                if (b != null && !(b is BlockAir))
                {
                    if (idMap.TryAdd(b.GetType(), idMap.Count + 1))
                    {
                        idVals.Add(idMap.Count);
                        typeVals.Add(b.GetType().Assembly.FullName + "|" + b.GetType().FullName);
                    }
                    bid = idMap[b.GetType()];
                    idList.Add(bid);
                    coordList.Add((short)((b.Location.X & 127) | ((b.Location.Y & 127) << 8)));
                    dirList.Add(b.Direction);
                    workList.Add(b.UpdateTime);
                    dataList.Add(b.ExtData);
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {

                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(idVals.Count);
                    for (int i = 0; i < idVals.Count; i++)
                    {
                        bw.Write(idVals[i]);
                        byte[] str = System.Text.Encoding.UTF8.GetBytes(typeVals[i]);
                        bw.Write(str.Length);
                        bw.Write(str);
                    }

                    //now we shall write the graph, so that it doesn't need to be rebuilt
                    foreach(Graph g in Graphs)
                    {
                        //


                    }

                    bw.Write(idList.Count);
                    for (int i = 0; i < idList.Count; i++)
                    {
                        //capture our size as a thing
                        bw.Write(coordList[i]);
                        bw.Write(idList[i]);
                        bw.Write((byte)dirList[i]);
                        bw.Write(workList[i]);
                        if (dataList[i] != null)
                        {
                            bw.Write(dataList[i].Length);
                            bw.Write(dataList[i]);
                        }
                        else
                        {
                            bw.Write(0);
                        }
                    }
                }
                return ms.ToArray();
            }
            

            //1. we need to write the id map





        }



    }
}
