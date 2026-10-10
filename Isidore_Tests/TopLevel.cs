using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Isidore_Tests
{
    class TopLevel
    {
        static int Main(string[] args)
        {
            
            // Closes any open MatLab figure windows
            MLApp.MLApp matlab = new MLApp.MLApp();
            String strAppDir = new FileInfo(System.Windows.Forms.Application.ExecutablePath).DirectoryName;
            String res = matlab.Execute("clear;close all");

            // This isn't part of the test suite, but instead provides examples
            // of common programming techniques that aren't used in 
            // scientific programming or scripting
            //CodingExamples.Run();

            Stopwatch totTime = new Stopwatch();
            totTime.Start();

            // error monitors
            List<bool> passed = new List<bool>();

            // Maths Tests
            passed.Add(Maths.Run());

            // Maths Tests
            passed.Add(MatlabTests.Run());

            // Loader Tests
            passed.Add(Loader.Run());

            // Render Tests
            passed.Add(RenderTests.Run());

            // Models Tests
            passed.Add(ModelsTests.Run());

            totTime.Stop();
            Console.WriteLine("\nSuite total time = {0}s\n",
                totTime.ElapsedMilliseconds / 1000.0);

            //Checks if all passed assigns string
            string passStr = "All tests passed.";
            foreach (bool pass in passed)
                if (!pass) passStr = "At least one test failed.";
            Console.WriteLine(passStr);

            //Aids.WaitForKey("\n" + passStr + "\nPress any key to exit");
            return passed.TrueForAll(pass => pass) ? 0 : 1;
        }
    }

    public class Aids
    {
        public static bool WaitForKey(String message)
        {
            Console.Error.Write(message);
            while (!Console.KeyAvailable)
            {
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(500);
            }
            Console.ReadKey();
            return true;
        }

        public static double perErr(double val, double truth, double minVal = 0.000001)
        {
            if (truth == 0)
                return Math.Abs(val) <= minVal ? 0.0 : double.PositiveInfinity;
            return Math.Abs((val - truth) / truth);
        }
    }
}
