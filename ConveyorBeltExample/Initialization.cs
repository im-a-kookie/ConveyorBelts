using ConveyorBeltExample.Graphics;
using ConveyorEngine.Blocks;
using ConveyorEngine.Graphics.Conveyors;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample
{
    /// <summary>
    /// Handles most of the game initialization stuff, redirected from the default Monogame Core class
    /// </summary>
    internal class Initialization
    {


        public static void InitializeGame(EngineStage stage = EngineStage.Game)
        {

            //The chunks then just look up the index of the render layer
            SpriteManager.LoadSpritesAndGeneratePage("Content");

            //Dynamically find everything that needs to be Initialized during this stage of the engine
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var a = (Initializable)Attribute.GetCustomAttribute(t, typeof(Initializable));
                if (a != null && a.Stage == stage)
                {
                    //see if T has a "FinalizeObject" method and a "Finalized" variable
                    var m = t.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    m?.Invoke(null, null);
                }
            }

            //TODO: this should be loaded from the content manager
            //Doing it here is okay but it breaks the design concept
            if (stage == EngineStage.Game)
            {
                BlockManager.RegisterBlock(0, new BlockDefinition("empty"));
                BlockManager.RegisterBlock(1, new BlockPart());

                BlockManager.RegisterBlock(2, new BlockConveyor("conveyor0", 2));
                BlockManager.RegisterBlock(3, new BlockConveyor("conveyor1", 2));

                ItemManager.RegisterItem("cookie", new ItemDefinition("Cookie", 100, 1, "choc_chip"));
            }
        }

        /// <summary>
        /// Finalizes everything that needs finalizating after initialization
        /// </summary>
        public static void FinalizeStuff(EngineStage stage = EngineStage.Game)
        {
            //find every automatically initializing class that matches the current engine stage
            foreach(var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var a =  (Finalizable)Attribute.GetCustomAttribute(t, typeof(Finalizable));
                if (a != null && a.Stage == stage)
                {
                    //see if T has a "FinalizeObject" method and a "Finalized" variable
                    var m = t.GetMethod("FinalizeObject", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    m?.Invoke(null, null);
                }
            }
        }









    }
}
