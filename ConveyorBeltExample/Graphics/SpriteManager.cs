using ConveyorEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphics
{
    
    public class SpriteManager
    {


        /// <summary>
        /// The spritesheet for this instance.
        /// We strictly limit the engine to one texture page, since with 16x16 graphics at 4096x4096 limits
        /// We just have so much space it's kinda dumb
        /// </summary>
        public static Texture2D TexturePage;

        /// <summary>
        /// A collection of string->sprite mappings. We're just going to map the string to their list index for now.
        /// </summary>
        internal static Dictionary<string, SpriteBounds> SpriteMappings = new();

        internal static Dictionary<string, int> SpriteIndexMapping = new();

        public static List<SpriteBounds> SpritesByIndex = new();

        public static List<Texture2D> SubTextureCollection = new();

        /// <summary>
        /// The sprites are loaded from a resource pack
        /// Which subsequently, can be used to generate all of the game content
        /// 
        /// e.g Fuel_Ignitable
        /// Burn_Output = 
        /// 
        /// </summary>
        public static void LoadSpritesAndGeneratePage(string path = "")
        {
            if (path != null)
            {
                if (!path.EndsWith(Path.DirectorySeparatorChar))
                {
                    path = path + Path.DirectorySeparatorChar;
                }

                //Let's try to read all of the sprites out of the "sprites" folder first
                List<string> ids = new List<string>();
                string SpritePath = path + "sprites";
                if (Directory.Exists(SpritePath))
                {
                    foreach (string s in Directory.EnumerateFiles(SpritePath, "*.png", SearchOption.AllDirectories))
                    {
                        Texture2D tex = Texture2D.FromFile(Core.Instance.GraphicsDevice, s);
                        tex.Tag = Path.GetFileNameWithoutExtension(s);
                        SubTextureCollection.Add(tex);
                    }
                }
            }

            if (SubTextureCollection.Count == 0) return;

            TextureBundle tp = new TextureBundle(SubTextureCollection);
            if (TexturePage != null) TexturePage.Dispose();
            TexturePage = tp.GeneratePage(Core.Instance.GraphicsDevice);

            //now we need to run an algorithm to slot every rectangle into a new bucket
            SpriteMappings.Clear();
            SpriteIndexMapping.Clear();
            SpritesByIndex.Clear();

            //now map the things
            foreach (var t in tp.Textures)
            {
                SpriteBounds r = new (t.Value.X, t.Value.Y, t.Value.Width, t.Value.Height);
                SpriteMappings.Add((string)t.Key.Tag, r);
                SpriteIndexMapping.Add((string)t.Key.Tag, SpritesByIndex.Count);
                SpritesByIndex.Add(r);
            }


        }

        /// <summary>
        /// Finalizes the texture builder properly
        /// </summary>
        public static void FinalizeTextures()
        {
            //and dispose all of the textures
            foreach (var t in SubTextureCollection) t.Dispose();
        }


        /// <summary>
        /// An public class for smooshing a mix of sprites into a texture page
        /// </summary>
        public class TextureBundle
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
                RenderTarget2D result = new RenderTarget2D(g, 1 << Size, 1 << Size);
                g.SetRenderTarget(result);
                g.Clear(Color.FromNonPremultiplied(0, 0, 0, 0));
                SpriteBatch sb = new SpriteBatch(g);
                sb.Begin();
                
                foreach(var n in Textures)
                {
                    sb.Draw(n.Key, new Rectangle(n.Value.X , n.Value.Y, n.Key.Width, n.Key.Height), Color.White);
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
            /// Marks the indices of the map with the flag, for every cell within the rectangle.
            /// For simplicity, the map is reduced to an 8x8 grid
            /// </summary>
            /// <param name="map"></param>
            /// <param name="n"></param>
            /// <param name="flag"></param>
            internal void Mark(bool[,] map, Rectangle n, bool flag = true)
            {

                int x0 = (n.X / 8);
                int y0 = (n.Y / 8);
                int x1 = x0 + 1 + (n.Width - 1) / 8;
                int y1 = y0 + 1 + (n.Height - 1) / 8;

                for (int x = x0; x < x1; x++)
                {
                    for(int y = y0; y < y1; y++)
                    {
                        map[x, y] = flag;
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
                int x0 = (n.X / 8);
                int y0 = (n.Y / 8);
                int x1 = x0 + 1 + (n.Width - 1) / 8;
                int y1 = y0 + 1 + (n.Height - 1) / 8;

                if (x1 >= ((1 << Size) / 8)) return true;
                if (y1 >= ((1 << Size) / 8)) return true;

                for (int x = x0; x < x1; x++)
                {
                    for (int y = y0; y < y1; y++)
                    {
                        if (map[x, y]) return true;
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
                Size = 1;

                //clear empty things
                workingList.RemoveAll(x => x.Width == 0 || x.Height == 0);

                while(workingList.Count > 0)
                {
                    var f = (Texture2D t) => int.Max(t.Width, t.Height);

                    int bestIndex = 0;
                    int bestF = -1;
                    for(int i = 0; i < workingList.Count; i++)
                    {
                        int x = f(workingList[i]);
                        if (x > bestF){
                            bestF = x;
                            bestIndex = i;
                        }
                    }

                    sumArea += workingList[bestIndex].Width * workingList[bestIndex].Height;
                    sorted.Add(workingList[bestIndex]);
                    workingList.RemoveAt(bestIndex);
                }

                //the total covered number of pixels is like this
                //sumArea
                //so
                while ((1 << Size) * (1 << Size) < sumArea) ++Size;

                while (true)
                {
                    //Calculate the dimensions of the things
                    int dim = 1 << Size;
                    if (dim > 4096) throw new Exception("Too Many Textures!");

                    bool[,] occupied = new bool[(dim / 8), (dim / 8)];
                    int success = 0;
                    
                    //now fill the rectangles in order
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        bool has = false;
                        for(int x = 0; x < dim && !has; x+=8)
                        {
                            for(int y = 0; y < dim && !has; y+=8)
                            {
                                //get the width and height and check the overlapping state
                                Rectangle r = new Rectangle(x, y, sorted[i].Width, sorted[i].Height);
                                
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
