using System.Text;
using System;
using System.Reflection.Emit;
using static System.Math;

namespace Donut.Helpers
{
    // eyes are in the origin (0, 0, 0)
    public class DonutSettings
    {
        // radius of the circle
        public double R1 { get; set; } = 1;
        // distance between y axis and the middle of the circle
        public double R2 { get; set; } = 2;
        public int ScreenWidth { get; set; } = 60;
        public int ScreenHeight { get; set; } = 40;
        public int TerminalScreenHeight
        {
            get => this.ScreenHeight / 2;
        }
        public double ThetaSpacing { get; set; } = 0.07;
        public double PhiSpacing { get; set; } = 0.02;
        // distance between eyes and screen
        public double K1 { get; set; } = 4.8;
        // distance between eyes and donut
        public double K2 { get; set; } = 5;
        public DotInSpace LightLocation { get; set; } = new() { X = 0, Y = 1, Z = -1 };
        // rotation about the x-axis by A
        public double A { get; set; } = 0;
        // rotation about the z-axis by B
        public double B { get; set; } = 0;
        public double ScaleUpRate = 7;
    }

    public class DotInSpace
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public class Donut
    {
        private readonly DonutSettings _settings;
        public Donut(DonutSettings settings)
        {
            _settings = settings;
        }
        private readonly string _pixels = ".,-~:;=!*#$@";

        public string GetAsciiDonut()
        {
            char[] screenBitmap = new char[_settings.TerminalScreenHeight * _settings.ScreenWidth];
            int i;
            for (i = 0; i < screenBitmap.Length; i++)
            {
                screenBitmap[i] = ' ';
            }

            double[] bitmap = new double[_settings.ScreenHeight * _settings.ScreenWidth];
            double[] depthBitmap = new double[_settings.ScreenHeight * _settings.ScreenWidth];
            for (i = 0; i < depthBitmap.Length; i++)
            {
                depthBitmap[i] = double.MaxValue;
            }

            double theta;
            double phi;
            for (phi = 0; phi < PI * 2; phi += _settings.PhiSpacing)
            {
                for (theta = 0; theta < PI * 2; theta += _settings.ThetaSpacing)
                {
                    var dotOnDonutSurface = GetDotOnDonutSurface(theta, phi);
                    var dotOnScreen = GetDotOnScreen(dotOnDonutSurface);
                    // scale up
                    dotOnScreen.X *= _settings.ScaleUpRate;
                    dotOnScreen.Y *= _settings.ScaleUpRate;
                    // locate donut to the center of the screen {
                    dotOnScreen.X += _settings.ScreenWidth / 2;
                    // Y is overturned
                    dotOnScreen.Y = _settings.ScreenHeight / 2 - dotOnScreen.Y;
                    // }
                    // fill up the bitmap
                    if (dotOnScreen.X >= 0 && dotOnScreen.X < _settings.ScreenWidth &&
                        dotOnScreen.Y >= 0 && dotOnScreen.Y < _settings.ScreenHeight)
                    {
                        int bitmapIndex =
                            (int)dotOnScreen.X + (int)dotOnScreen.Y * _settings.ScreenWidth;
                        if (dotOnScreen.Z < depthBitmap[bitmapIndex])
                        {
                            bitmap[bitmapIndex] = GetLuminance(theta, phi);
                            // bitmap[bitmapIndex] = 1;
                            depthBitmap[bitmapIndex] = dotOnScreen.Z;
                        }
                    }
                }
            }

            // render
            int x;
            int y;
            for (y = 0; y < _settings.ScreenHeight; y += 2)
            {
                for (x = 0; x < _settings.ScreenWidth; x++)
                {
                    double luminanceMean;
                    if (y + 1 < _settings.ScreenHeight)
                    {
                        luminanceMean = (bitmap[x + y * _settings.ScreenWidth] + bitmap[x + (y + 1) * _settings.ScreenWidth]) / 2;
                    }
                    else
                    {
                        luminanceMean = bitmap[x + y * _settings.ScreenWidth];
                    }
                    int luminance = (int)(8 * luminanceMean + 0.5);
                    char pixel = ' ';
                    if (luminance > 0)
                    {
                        pixel = _pixels[luminance];
                    }
                    screenBitmap[x + y / 2 * _settings.ScreenWidth] = pixel;
                }
            }

            StringBuilder result = new();
            for (i = 0; i < _settings.TerminalScreenHeight; i++)
            {
                result.Append(screenBitmap[(i * _settings.ScreenWidth)..((i + 1) * _settings.ScreenWidth)]);
                result.Append('\n');
            }
            return result.ToString();
        }

        // https://zs.symbolab.com/solver/matrix-multiply-calculator/%5Cbegin%7Bpmatrix%7Dcos%20T%26sin%20T%260%5Cend%7Bpmatrix%7D%5Cbegin%7Bpmatrix%7Dcos%20P%260%26sin%20P%5C%5C%200%261%260%5C%5C%20-sin%20P%260%26cos%20P%5Cend%7Bpmatrix%7D%5Cbegin%7Bpmatrix%7D1%260%260%5C%5C%200%26cos%20A%26sin%20A%5C%5C%200%26-sin%20A%26cos%20A%5Cend%7Bpmatrix%7D%5Cbegin%7Bpmatrix%7Dcos%20B%26sin%20B%260%5C%5C%20-sin%20B%26cos%20B%260%5C%5C%200%260%261%5Cend%7Bpmatrix%7D
        private double GetLuminance(double theta, double phi)
        {
            var a = _settings.A;
            var b = _settings.B;
            return
                _settings.LightLocation.X * (
                    Cos(theta) * Cos(phi) * Cos(b) -
                    Sin(b) * (
                        Sin(theta) * Cos(a) -
                        Cos(theta) * Sin(phi) * Sin(a))
                ) +
                _settings.LightLocation.Y * (
                    Cos(theta) * Cos(phi) * Sin(b) +
                    Cos(b) * (
                        Sin(theta) * Cos(a) -
                        Cos(theta) * Sin(phi) * Sin(a))
                ) +
                _settings.LightLocation.Z * (
                    Sin(theta) * Sin(a) +
                    Cos(theta) * Sin(phi) * Cos(a)
                );
        }

        private DotInSpace GetDotOnScreen(DotInSpace dot)
        {
            var k1 = _settings.K1;
            var k2 = _settings.K2;
            return new DotInSpace()
            {
                X = k1 * dot.X / k2,
                Y = k1 * dot.Y / k2,
                Z = k1 * dot.Z / k2,
            };
        }

        private DotInSpace GetDotOnDonutSurface(double theta, double phi)
        {
            var r1 = _settings.R1;
            var r2 = _settings.R2;
            var a = _settings.A;
            var b = _settings.B;
            // return new DotInSpace()
            // {
            //     X = (r2 + r1 * Cos(theta)) * Cos(phi),
            //     Y = r1 * Sin(theta),
            //     Z = -(r2 + r1 * Cos(theta)) * Sin(phi),
            // };
            return new DotInSpace()
            {
                X = (r2 + r1 * Cos(theta)) * (Cos(b) * Cos(phi) + Sin(a) * Sin(b) * Sin(phi)) -
                    r1 * Cos(a) * Sin(b) * Sin(theta),
                Y = (r2 + r1 * Cos(theta)) * (Sin(b) * Cos(phi) - Sin(a) * Cos(b) * Sin(phi)) +
                    r1 * Cos(a) * Cos(b) * Sin(theta),
                Z = Cos(a) * (r2 + r1 * Cos(theta)) * Sin(phi) +
                    r1 * Sin(a) * Sin(theta),
            };
        }
    }
}
