using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphics
{
    internal class SpriteManager
    {
        public const int SPRITE_RES_PO2 = 4;
        public const int SPRITE_RES_PX = 16;
        public const int RES_MASK = SPRITE_RES_PX - 1;


        /// <summary>
        /// The spritesheet for this instance.
        /// We strictly limit the engine to one texture page, since with 16x16 graphics at 4096x4096 limits
        /// We just have so much space it's kinda dumb
        /// </summary>
        public static Texture2D TexturePage;

        /// <summary>
        /// A collection of string->sprite mappings. We're just going to map the string to their list index for now.
        /// </summary>
        internal static Dictionary<string, Rectangle> SpriteMappings = new Dictionary<string, Rectangle>();


        //we can render via rectangle or via string
        public static void Draw(GraphicsDevice g, IEnumerable<Layer> layers, Vector2 position)
        {

        }


        /// <summary>
        /// The sprites are loaded from a resource pack
        /// Which subsequently, can be used to generate all of the game content
        /// 
        /// e.g Fuel_Ignitable
        /// Burn_Output = 
        /// 
        /// </summary>
        public static void LoadSpritesAndGeneratePage(GraphicsDevice g, string path)
        {

            if (!path.EndsWith(Path.DirectorySeparatorChar))
            {
                path = path + Path.DirectorySeparatorChar;
            }

            //Let's try to read all of the sprites out of the "sprites" folder first
            List<string> ids = new List<string>();
            List<Texture2D> SubParts = new List<Texture2D>();
            string SpritePath = path + "sprites";
            if (Directory.Exists(SpritePath))
            {
                foreach(string s in Directory.EnumerateFiles(SpritePath, "*.png", SearchOption.AllDirectories))
                {
                    Texture2D tex = Texture2D.FromFile(g, s);
                    tex.Tag = Path.GetFileNameWithoutExtension(s);
                    SubParts.Add(tex);
                }
                //now we need to run an algorithm to slot every rectangle into a new bucket

                TextureBundle tp = new TextureBundle(SubParts);
                TexturePage = tp.GeneratePage(g);

                //now map the things
                foreach (var t in tp.Textures)
                {
                    SpriteMappings.Add((string)t.Key.Tag, new Rectangle(t.Value.X << SPRITE_RES_PO2, t.Value.Y << SPRITE_RES_PO2, t.Value.Width << SPRITE_RES_PO2, t.Value.Height << SPRITE_RES_PO2));
                }

                //and dispose all of the textures
                foreach (var t in SubParts) t.Dispose();

            }

        }

        /// <summary>
        /// An internal class for smooshing a mix of sprites into a texture page
        /// </summary>
        internal class TextureBundle
        {
            int Size = 1;
            public Dictionary<Texture2D, Rectangle> Textures = new Dictionary<Texture2D, Rectangle>();
            
            public TextureBundle(IEnumerable<Texture2D> images = null, int initial_size = 1)
            {
                this.Size = initial_size;
                foreach (Texture2D tex in images)
                { 
                    Textures.Add(tex, new Rectangle(0, 0, tex.Width, tex.Height));
                }
            }

            /// <summary>
            /// Generates a texture page from this bundle
            /// </summary>
            /// <param name="g"></param>
            /// <returns></returns>
            public Texture2D GeneratePage(GraphicsDevice g)
            {
                Arrange();
                RenderTargetBinding[] originRenderTargets = g.GetRenderTargets();
                RenderTarget2D result = new RenderTarget2D(g, (1 << SPRITE_RES_PO2) << Size, (1 << SPRITE_RES_PO2) << Size);
                g.SetRenderTarget(result);
                g.Clear(Color.FromNonPremultiplied(0, 0, 0, 0));
                SpriteBatch sb = new SpriteBatch(g);
                sb.Begin();
                
                foreach(var n in Textures)
                {
                    sb.Draw(n.Key, new Rectangle(n.Value.X << SPRITE_RES_PO2, n.Value.Y << SPRITE_RES_PO2, n.Key.Width, n.Key.Height), Color.White);
                }
                sb.End();
                g.SetRenderTargets(originRenderTargets);
                g.Clear(Color.FromNonPremultiplied(0, 0, 0, 0));

                return result;
            }

            internal bool Overlaps(List<Rectangle> rects, Rectangle n)
            {
                foreach (Rectangle r in rects) if (n.Intersects(r)) return true;
                return false;
            }

            /// <summary>
            /// Marks the indices of the map with the flag, for every cell within the rectangle
            /// </summary>
            /// <param name="map"></param>
            /// <param name="n"></param>
            /// <param name="flag"></param>
            internal void Mark(bool[,] map, Rectangle n, bool flag = true)
            {
                for(int i = 0; i < n.Width; i++)
                {
                    if (i + n.X >= (1 << Size)) continue;
                    for(int j = 0; j < n.Height; j++)
                    {
                        if (j + n.Y >= (1 << Size)) continue;
                        map[i + n.X, j + n.Y] = flag;
                    }
                }
            }

            /// <summary>
            /// Checks whether a rectangle can fit into the bool map without intersecting a "true" value
            /// </summary>
            /// <param name="map"></param>
            /// <param name="n"></param>
            /// <returns></returns>
            internal bool Overlaps(bool[,] map, Rectangle n)
            {
                for (int i = 0; i < n.Width; i++)
                {
                    if (i + n.X >= (1 << Size)) return true;
                    for (int j = 0; j < n.Height; j++)
                    {
                        if (j + n.Y >= (1 << Size)) return true;
                        if (map[i + n.X, j + n.Y]) return true;
                    }
                }
                return false;
            }

            public void Arrange()
            {
                //create a sorted list of textures by their size
                //and insert the largest ones first
                List<Texture2D> sorted = new List<Texture2D>();
                List<Texture2D> workingList = [.. Textures.Keys];
                int sumArea = 0;

                while(workingList.Count > 0)
                {
                    int best = 0;
                    int bestA = -1;
                    for(int i = 0; i < workingList.Count; i++)
                    {
                        int a = (workingList[i].Width >> SPRITE_RES_PO2) * (workingList[i].Height >> SPRITE_RES_PO2);
                        if (a > bestA)
                        {
                            bestA = a;
                            best = i;
                        }
                    }
                    sumArea += bestA;
                    sorted.Add(workingList[best]);
                    workingList.RemoveAt(best);
                }
                //now scale until we can definitely fit everything
                while ((1 << (Size << 1)) < sumArea) ++Size;
                while (true)
                {
                    //Calculate the dimensions of the things
                    int dim = 1 << Size;
                    if (((1 << SPRITE_RES_PO2) << Size) > 4096) throw new Exception("Too Many Textures!");

                    bool[,] occupied = new bool[dim, dim];
                    int success = 0;
                    
                    //now full the rectangles from the working list
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        bool has = false;
                        for(int x = 0; x < dim && !has; x++)
                        {
                            for(int y = 0; y < dim && !has; y++)
                            {
                                Rectangle r = new Rectangle(x, y, sorted[i].Width >> SPRITE_RES_PO2, sorted[i].Height >> SPRITE_RES_PO2);
                                if (Overlaps(occupied, r)) continue;
                                Mark(occupied, r, true);
                                Textures[sorted[i]] = r;
                                has = true;
                                break;
                            }
                        }

                        //Check that we were able to fit the texture
                        if (!has) break;
                        ++success;
                    }

                    //We were able to fit everything, which is winner
                   if (success == sorted.Count)
                   {
                        break;
                   }
                   //we were not able to fit everything
                   //So increase the texture page dimensions and try again
                   else
                   {
                        ++Size;
                        continue;
                   }
                }
            }
        }







    }
}
