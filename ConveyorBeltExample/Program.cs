

using ConveyorEngine.Tests;
using ConveyorEngine.Util;
using System.Diagnostics;
using System.Threading;

TestBed.GetAndPerformTestables();


using var game = new ConveyorBeltExample.Core();
game.Run();
