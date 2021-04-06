using System.IO;
using System;

namespace Donut
{
    class Program
    {
        static void Main(string[] args)
        {
            OutputToTerminal();
            // OutputToFile();
        }

        static void OutputToTerminal()
        {
            Donut.Helpers.DonutSettings settings = new()
            {
            };
            Donut.Helpers.Donut donut = new(settings);
            double aAngle = 1;
            double bAngle = 1;
            double aAngleSpacing = 0.07;
            double bAngleSpacing = 0.03;
            while (true)
            {
                settings.A = aAngle;
                settings.B = bAngle;
                var result = donut.GetAsciiDonut();
                Console.WriteLine(result);
                int consoleTop = Console.CursorTop - settings.TerminalScreenHeight - 1;
                if (consoleTop < 0)
                {
                    // the console is too small in height.
                    consoleTop = Console.WindowHeight - 1;
                }
                Console.SetCursorPosition(0, consoleTop);

                aAngle = aAngle + aAngleSpacing % (3.14 * 2);
                bAngle = bAngle + bAngleSpacing % (3.14 * 2);
            }
        }

        static void OutputToFile()
        {
            Donut.Helpers.DonutSettings settings = new()
            {
                A = 3.14 / 6,
                B = 3.14 / 6,
            };
            Donut.Helpers.Donut donut = new(settings);
            var result = donut.GetAsciiDonut();
            string filePath = "./out.txt";
            File.Delete(filePath);
            using var fileStream = File.OpenWrite(filePath);
            using var streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(result);
        }
    }
}
