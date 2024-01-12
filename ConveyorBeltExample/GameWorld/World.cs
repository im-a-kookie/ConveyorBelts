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
    internal class World
    {
        public Dictionary<Point, Chunk> LoadedChunks = new Dictionary<Point, Chunk>();

        public HashSet<Graph> TransportGraphs = new HashSet<Graph>();

        public WorldManager Manager;

        public string Dimension = "main";

        /// <summary>
        /// The path to this world storage
        /// </summary>
        public string WorldPath
        {
            get
            {
                string s = Manager.WorldPath;
                s += "Dimension\\";
                Directory.CreateDirectory(s);
                return s;
            }
        }

        public World(WorldManager wm, string dimensionName)
        {
            this.Manager = wm;
            this.Dimension = dimensionName;
        }


        public Block? GetBlock(Point p)
        {
            Chunk c = GetChunkFromWorldCoords(p);
            if (c == null) return null;
            return c.GetBlock(p.X & 127, p.Y & 127);
        }

        public Chunk GetChunkFromWorldCoords(Point p)
        {
            return GetChunk(new Point(p.X & 127, p.Y & 127));
        }

        public Chunk GetChunk(Point point, bool forceLoad = true)
        {
            if (LoadedChunks.ContainsKey(point)) return LoadedChunks[point];
            if (!forceLoad || Manager == null) return null;

            Chunk c = new Chunk(this, point);
            LoadedChunks.Add(point, c);
            return c;

            //we need to be able to load and store chunks, but that's a problem for later
            if (File.Exists(WorldPath + "[" + point.X + "," + point.Y + "].chunk"))
            {
                //load the chunk from the file
                //return Chunk.FromBytes(this, File.ReadAllBytes(WorldPath + "[" + point.X + "," + point.Y + "].chunk"));
            }

            //generate the chunk from the generator
            return new Chunk(this, point);
        }




    }
}
