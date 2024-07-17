using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphics;
using ConveyorBeltExample.Graphing;
using ConveyorBeltExample.Items;
using ConveyorEngine.Blocks;
using ConveyorEngine.Graphics.Conveyors;
using ConveyorEngine.Items;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using static System.Net.Mime.MediaTypeNames;

namespace ConveyorBeltExample
{
    public class Core : Game
    {
        public static Core Instance;
        public GraphicsDeviceManager _graphics;
        public SpriteBatch _spriteBatch;
        public Camera myCam;
        public Stopwatch UpdateWatch = Stopwatch.StartNew();
        public Stopwatch RenderWatch = Stopwatch.StartNew();

        World world;

        public static RenderTarget2D[] Targets = [ null ];
        public static RenderTarget2D GetTarget(int index = 0)
        {
            if (index >= Targets.Length)
            {
                var n = new RenderTarget2D[index + 1];
                for(int i = 0; i < Targets.Length; i++) n[i] = Targets[i];
                Targets = n;
            }

            if (Targets[index] == null)
            {
                Targets[index] = new RenderTarget2D(Core.Instance.GraphicsDevice,
                                            Core.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth,
                                            Core.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight);
            }
            return Targets[index];
        }


        public Core()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            lock (this)
            {
                if (Instance == null)
                    Instance = this;
            }
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();
            Targets[0] = new RenderTarget2D(GraphicsDevice,
                                            GraphicsDevice.PresentationParameters.BackBufferWidth,
                                            GraphicsDevice.PresentationParameters.BackBufferHeight,
                                            false,
                                            GraphicsDevice.PresentationParameters.BackBufferFormat,
                                            DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);

            TargetElapsedTime = TimeSpan.FromTicks((long)(TimeSpan.TicksPerSecond / 60L));
            _graphics.SynchronizeWithVerticalRetrace = false;


            _spriteBatch = new SpriteBatch(GraphicsDevice);
            myCam = new Camera(GraphicsDevice.Viewport);

            ///Initialize and finalize all of the Game initializers
            Initialization.InitializeGame(EngineStage.Game);
            Initialization.FinalizeStuff(EngineStage.Game);

            world = new World("test_world");

            //Stupid testing thing
            if (false)
            {
                //basically it just picks random points in the rectangle and continues until 10K blocks are set?
                int set = 0;
                Random r = new Random(1252515);
                KPoint _last = new KPoint(0, 0);
                while (set < 20000)
                {
                    //pick a point
                    KPoint p = new KPoint(r.Next(2560 / 8), r.Next(1440 / 8));
                    while (set < 20000 && _last != p)
                    {
                        KPoint dd = p - _last;
                        Dir d = dd.Direction();
                        //set this block to that direction and then step to it
                        var _data = world.GetBlock(_last);
                        if (_data.id != 0)
                        {
                            ++set;
                        }
                        world.SetBlockImmediate(_last, new(1, d));
                        _last = _last.Step((int)d);
                    }
                }
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // TODO: use this.Content to load your game content here
        }

        int n = 0;
        double update_avg = 0f;
        double render_avg = 0f;
        double factor = 10;
        Dir dir = Dir.SOUTH;


        bool clicked = false;
        bool placed = false;
        bool locked_axis_x = false;
        int locked_x_val = 0;
        bool locked_axis_y = false;
        int locked_y_val = 0;
        Dir drag_dir = Dir.SOUTH;
        KPoint? LastPlace = null;

        Deque<double> rtimes = new Deque<double>();
        Deque<double> utimes = new Deque<double>();
        public int MaxCount = 200;

        /// <summary>
        /// Handles when the mouse is dragged.
        /// Realistically this should be moved to an Input controller bound to the loaded world,
        /// or some kind of client session provider.
        /// </summary>
        /// <param name="lock_axis"></param>
        public void HandleMouseDrags(bool lock_axis = false)
        {
            var wp = new KPoint(myCam.ScreenPointToGrid(Mouse.GetState().Position));

            //axis locking so we can do lines
            if (!lock_axis)
            {
                locked_axis_x = false;
                locked_axis_y = false;
            }

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                //If the button was *just* pressed, then we need to react, otherwise it's dragged
                if (!clicked)
                {
                    //place a block here
                    clicked = true;
                    var d = world.GetBlock(wp);
                    if (d.id != 2 || d.dir != (byte)dir)
                    {
                        //we're just using id==2 as a placeholder for testing, since we know this is a conveyor
                        //We could also get the block by id from a string name,
                        //Which will be sometimes okay, but indexing is WAY faster than dictionary lookup
                        //So this is something to be mindful of in certain scenarios
                        world.SetBlockDeferred(wp, new BlockData(2, dir, -1, 0));
                        placed = true;
                        LastPlace = wp;
                    }
                }
                else
                {
                    //Calculate the target of the mouse
                    //Ordinarily this is any block, but with axis locking it needs to slide in a straight line
                    var target = new KPoint(locked_axis_x ? locked_x_val : wp.x, locked_axis_y ? locked_y_val : wp.y);
                    if (LastPlace == null || target != LastPlace.Value)
                    {
                        //We've moved to a new place
                        if (LastPlace != null)
                        {
                            List<KPoint> steps = new List<KPoint>();
                            List<Dir> dirs = new List<Dir>();

                            //find an appropriate path from the last place to the target
                            KPoint dp = target - LastPlace.Value;
                            dir = dp.Direction();

                            //urgh axis locking
                            //we need to determine which axis on the fly
                            if (lock_axis && !locked_axis_x && !locked_axis_y)
                            {
                                    if (((int)dir & 0x1) == 0) { locked_axis_x = true; locked_x_val = wp.x; }
                                    else { locked_axis_y = true; locked_y_val = wp.y; }
                            }
                            

                            steps.Add(LastPlace.Value);
                            dirs.Add(dir);
                            //var d = world.GetBlock(LastPlace.Value);
                            //if (d.id != 1 || d.dir != (byte)dir)
                            //{
                            //world.SetBlockDeferred(LastPlace.Value, new BlockData(1, dir, -1, 0));
                            //    placed = true;
                            //}

                            //Let's find a path to this place
                            LastPlace = LastPlace.Value.Step((int)dir);
                            while(LastPlace.Value != target)
                            {
                                steps.Add(LastPlace.Value);
                                dp = target - LastPlace.Value;
                                dir = dp.Direction();
                                dirs.Add(dir);
                                LastPlace = LastPlace.Value.Step((int)dir);
                            }
                            
                            //Now we go through all the steps and set the blocks
                            for (int i = 0; i < steps.Count; i++)
                            {
                                var d = world.GetBlock(steps[i]);
                                if (d.id != 2 || d.dir != (byte)dirs[i])
                                {
                                    world.SetBlockDeferred(steps[i], new BlockData(2, dirs[i], -1, 0));
                                    placed = true;
                                    LastPlace = steps[i];
                                }
                            
                            }
                        }
                        else //we do not have a previous place
                        {
                            var d = world.GetBlock(wp);
                            if (d.id != 2 || d.dir != (byte)dir)
                            {
                                world.SetBlockDeferred(wp, new BlockData(2, dir, -1, 0));
                                placed = true;
                            }
                            LastPlace = wp;

                        }
                        clicked = true;
                    }
                }
            }
            else if (Mouse.GetState().LeftButton == ButtonState.Released)
            {

                if (clicked)
                {
                    clicked = false;
                    var target = new KPoint(locked_axis_x ? locked_x_val : wp.x, locked_axis_y ? locked_y_val : wp.y);
                    if (LastPlace == null || target != LastPlace.Value)
                    {
                        if (LastPlace != null)
                        {
                            KPoint dp = target - LastPlace.Value;
                            dir = dp.Direction();
                        }
                        var d = world.GetBlock(target);
                        if (d.id != 2)
                        {
                            world.SetBlockDeferred(target, new BlockData(2, dir, -1, 0));
                            LastPlace = target;
                        }
                    }
                }
                LastPlace = null;
                placed = false;
                locked_axis_x = false;
                locked_axis_y = false;
            }
        }



        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            n += (int)gameTime.ElapsedGameTime.TotalMicroseconds;
            if (n > 300 * 1000)
            {
                n -= 1000 * 1000;

                double worstDraw = 0;
                for (int i = 0; i < rtimes.Count; i++) worstDraw = Math.Max(worstDraw, rtimes[i]);

                double worstUpdate = 0;
                for (int i = 0; i < utimes.Count; i++) worstUpdate = Math.Max(worstUpdate, utimes[i]);

                Window.Title = "Debug. Render: " + Math.Round(render_avg * 10) / 10f + "ms, Update:: " + Math.Round(update_avg * 10f) /10f + "ms, Mem: " + Math.Round(((GC.GetTotalMemory(true) / 1024) / 102.4f))/10f + "MB, Wd: " + Math.Round(worstDraw * 10) + " , WU:" + Math.Round(worstUpdate * 10);
               
                

            }

            UpdateWatch.Restart();

            // TODO: Add your update logic here
            myCam.UpdateCamera(GraphicsDevice.Viewport, gameTime);

            world.MarkNeighborUpdates(new KPoint(myCam.ScreenPointToGrid(Mouse.GetState().Position)));
            
            if (Keyboard.GetState().IsKeyDown(Keys.Up)) dir = Dir.NORTH;
            if (Keyboard.GetState().IsKeyDown(Keys.Down)) dir = Dir.SOUTH;
            if (Keyboard.GetState().IsKeyDown(Keys.Left)) dir = Dir.WEST;
            if (Keyboard.GetState().IsKeyDown(Keys.Right)) dir = Dir.EAST;

            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl))
            {
                HandleMouseDrags(Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift));
            }

            if (Mouse.GetState().LeftButton == ButtonState.Pressed && Keyboard.GetState().IsKeyDown(Keys.LeftAlt))
            {
                //get the x/y in the world
                var p = Mouse.GetState().Position;
                var wp = myCam.ScreenPointToGrid(p);
                
                var d = world.GetBlock(new KPoint(wp));
                if (d.id == 2 && d.block is BlockConveyor bc && d.branch(world) != null)
                {
                    //try to put an item
                    if (d.branch(world).Inventory != null)
                    {

                        Item i = new Item() { id = 1 };
                        //sledgehammer
                        d.branch(world).Inventory.Time = world.Tick;
                        d.branch(world).LastResolvedOutputTick = world.Tick;
                        d.branch(world).LastResolvedInputTick = world.Tick;
                        d.branch(world).Inventory.Put(i, Settings.Conveyors.CONVEYOR_PRECISION / 2, false);
                        world.BranchUpdater.FlagUpdate(d.branch(world));
                        placed = true;
                    }
                }
                
            }


            bool thing = false;
            if (thing)
            {
                GC.Collect();
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                if (!_rplop)
                {
                    _rplop = true;
                    Debug.WriteLine(myCam.ScreenPointToBounds(Mouse.GetState().Position) + ", " + myCam.CameraPosition);
                    BlockData d = world.GetBlock(new KPoint(myCam.ScreenPointToGrid(Mouse.GetState().Position)));
                    Debug.WriteLine("Block: " + BlockManager.BlockIndexMap[d.id] + " => " + d.ToDebugString());

                    var b = d.branch(world);
                    if (b != null)
                    {
                        Debug.WriteLine("Branch:  " + b.Index + ", Items: " + b.ItemCount + ", In: " + b.GetInputCount() + ", Out: " + b.GetOutputCount());

                    }


                    Debug.WriteLine("");

                }
            }
            else _rplop = false;

            if (Mouse.GetState().LeftButton == ButtonState.Released)
            {
                LastPlace = null;
                placed = false;
            }


            world.Update(gameTime);

            base.Update(gameTime);



            utimes.AddFirst(UpdateWatch.Elapsed.TotalMilliseconds);
            if (utimes.Count > MaxCount) utimes.TakeLast();

            update_avg = ((update_avg * factor) + UpdateWatch.Elapsed.TotalMilliseconds) / (factor + 1);

        }
        bool _rplop = false;

        double d = 0;

        int item_placing = 0;

        protected override void Draw(GameTime gameTime)
        {
            RenderWatch.Restart();

            var rt = GraphicsDevice.GetRenderTargets();
            GraphicsDevice.SetRenderTarget(GetTarget());
            GraphicsDevice.Clear(Color.CornflowerBlue);

           


           
            world.Draw(gameTime, this);

            //get the x/y in the world
            var p = Mouse.GetState().Position;
            var grid = myCam.ScreenPointToGrid(p);

            var selector = SpriteManager.SpriteMappings["highlight_8px"];
            myCam.StartBatch(_spriteBatch);
            _spriteBatch.Draw(SpriteManager.TexturePage, new Rectangle(grid.X * 8, grid.Y * 8, 8, 8), new Rectangle(selector.X, selector.Y + 8, 8, 8), Color.White);
            _spriteBatch.End();

            //GraphicsDevice.SetRenderTargets(rt);
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(GetTarget(), 
                new Rectangle(0, 0, 
                GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight),
                Color.White);


            //now render UI stuff I guess
            switch (item_placing)
            {
                case 1:
                    
                    break;
                default: break;

            }


            _spriteBatch.End();

            base.Draw(gameTime);

            rtimes.AddFirst(RenderWatch.Elapsed.TotalMilliseconds);
            if (rtimes.Count > MaxCount) rtimes.TakeLast();
            render_avg = ((render_avg * factor) + RenderWatch.Elapsed.TotalMilliseconds) / (factor + 1);


        }
    }
}
