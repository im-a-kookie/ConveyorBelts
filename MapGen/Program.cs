
using Microsoft.Xna.Framework;
using System.Reflection;

var func = () => { return new Rectangle(1, 2, 3, 4); };
Rectangle r = func();

/* 
 *		Rectangle r = new Rectangle(1, 2, 3, 4);
		IL_0000: ldc.i4.1
		IL_0001: ldc.i4.2
		IL_0002: ldc.i4.3
		IL_0003: ldc.i4.4
		IL_0004: newobj instance void [MonoGame.Framework]Microsoft.Xna.Framework.Rectangle::.ctor(int32, int32, int32, int32)
		IL_0009: pop
 */






using var game = new MapGen.Game1();
game.Run();
