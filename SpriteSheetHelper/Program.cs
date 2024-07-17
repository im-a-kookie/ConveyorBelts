using System.Collections.Concurrent;
using System.Diagnostics;

namespace SpriteSheetHelper
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {


            TestBed.GetAndPerformTestables();

            Namer.Build();

            var ss = Namer.GenerateAll();
            Debug.WriteLine("Part Count: " + ss.Count() + "\nExamples: ");

            for(int i = 0; i < 10; i++)
            {
                Debug.WriteLine(Namer.Generate(3));
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Editor());
        }
    }
}