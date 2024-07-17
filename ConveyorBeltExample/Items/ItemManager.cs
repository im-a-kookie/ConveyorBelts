using ConveyorBeltExample.Graphics;
using ConveyorBeltExample.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConveyorEngine.Items
{
    public class ItemManager
    {


        public static List<ItemDefinition> ItemRegistry = new List<ItemDefinition>();
        public static Dictionary<string, ItemDefinition> ItemMap = new();

        public static Rectangle[] TextureCache;


        public static void LoadItems()
        {
            ItemRegistry.Clear();
            //idfk but let's just do the thing
        }

        public static void RegisterItem(string id, ItemDefinition item)
        {
            if (ItemMap.ContainsKey(id))
                throw new Exception("Could not register item: " + item + "." + id + " is already defined by: " + ItemMap[id].Name);
            ItemRegistry.Add(item);
            item.ID = ItemRegistry.Count;
            item.sId = id;
            ItemMap.Add(id, item);

            if (TextureCache == null) TextureCache = new Rectangle[100];
            if (item.ID > TextureCache.Length)
            {
                var _n = new Rectangle[item.ID + 1];
                Array.Copy(TextureCache, _n, 0);
                TextureCache = _n;
            }
            TextureCache[item.ID] = SpriteManager.SpriteMappings[item.Texture].R;
        }

        /// <summary>
        /// Returns whether b can merge into a
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool CanStack(Item a, Item b)
        {
            var ar = ItemRegistry[a.id];
            var br = ItemRegistry[b.id];
            return ar == br && (a.stack + b.stack < ar.StackLimit);
        }

        /// <summary>
        /// Stacks the most of "b" into "a" as possible
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void StackBIntoA(ref Item a, ref Item b)
        {
            var ar = ItemRegistry[a.id];
            var br = ItemRegistry[b.id];
            if (ar != br) return;
            a.stack += b.stack;
            b.stack = 0;
            if (a.stack > ar.StackLimit)
            {
                b.stack += (short)(a.stack - ar.StackLimit);
                a.stack = ar.StackLimit;
            }
            //mark b for removal
            if (b.stack <= 0) b.id = -1;
            
        }

    }
}
