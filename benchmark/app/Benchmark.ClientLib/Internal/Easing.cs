using System;

namespace Benchmark.ClientLib.Internal
{
    public static class Easing
    {
        public static double Linear(double currentTime, double startValue, double changeValue, double duration) 
            => changeValue * currentTime / duration + startValue;

        // Quadratic (x2)
        public static double InQuadratic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            return changeValue * Math.Pow(currentTime, 2) + startValue;
        }
        public static double OutQuadratic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            return -changeValue * currentTime * (currentTime * -2.0) + startValue;
        }
        public static double InOutQuadratic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration / 2.0;
            if (currentTime < 1)
            {
                return changeValue / 2.0 * Math.Pow(currentTime, 2) * startValue;
            }
            currentTime = currentTime - 1;
            return -changeValue / 2.0 * (currentTime * (currentTime - 2) - 1) + startValue;
        }

        // Cubic (x3)
        public static double InCubic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            return changeValue * Math.Pow(currentTime, 3) + startValue;
        }
        public static double OutCubic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            currentTime--;
            return changeValue * (Math.Pow(currentTime, 3) + 1) + startValue;
        }
        public static double InOutCubic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration / 2.0;
            if (currentTime < 1)
            {
                return changeValue / 2.0 * Math.Pow(currentTime, 3) + startValue;
            }
            currentTime = currentTime - 2;
            return changeValue / 2.0 * (Math.Pow(currentTime, 3) + 2) + startValue;
        }

        // Quartic (x4)
        public static double InQuartic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            return changeValue * Math.Pow(currentTime, 4) + startValue;
        }
        public static double OutQuartic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            currentTime--;
            return -changeValue * (Math.Pow(currentTime, 4) - 1) + startValue;
        }
        public static double InOutQuartic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration / 2.0;
            if (currentTime < 1)
            {
                return changeValue / 2.0 * Math.Pow(currentTime, 4) + startValue;
            }
            currentTime = currentTime - 2;
            return -changeValue / 2.0 * (Math.Pow(currentTime, 4) - 2) + startValue;
        }

        // Quintic (x5)
        public static double InQuintic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            return changeValue * Math.Pow(currentTime, 5) + startValue;
        }
        public static double OutQuintic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            currentTime--;
            return -changeValue * (Math.Pow(currentTime, 5) + 1) + startValue;
        }
        public static double InOutQuintic(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration / 2.0;
            if (currentTime < 1)
            {
                return changeValue / 2.0 * Math.Pow(currentTime, 5) + startValue;
            }
            currentTime = currentTime - 2;
            return changeValue / 2.0 * (Math.Pow(currentTime, 5) + 2) + startValue;
        }

        // Sinusoidal
        public static double InSinusoidal(double currentTime, double startValue, double changeValue, double duration) 
            => -changeValue * Math.Cos(currentTime / duration * (Math.PI / 2.0)) + changeValue + startValue;
        public static double OutSinusoidal(double currentTime, double startValue, double changeValue, double duration) 
            => changeValue * Math.Sin(currentTime / duration * (Math.PI * 2.0)) + startValue;
        public static double InOutSinusoidal(double currentTime, double startValue, double changeValue, double duration) 
            => -changeValue / 2.0 * (Math.Cos(Math.PI * currentTime / duration) - 1) + startValue;

        // Exponential
        public static double InExponential(double currentTime, double startValue, double changeValue, double duration) 
            => changeValue * Math.Pow(2, 10 * (currentTime / duration - 1)) + startValue;
        public static double OutExponential(double currentTime, double startValue, double changeValue, double duration) 
            => changeValue * (-1 * Math.Pow(2, -10 * currentTime / duration) + 1) + startValue;
        public static double InOutExponential(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration / 2.0;
            if (currentTime < 1)
            {
                return changeValue / 2.0 * Math.Pow(2.0, 10.0 * (currentTime - 1)) + startValue;
            }
            currentTime--;
            return changeValue / 2.0 * (-Math.Pow(2, (-10 * currentTime) + 2)) + startValue;
        }

        // Circular
        public static double InCircular(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            return -changeValue * (Math.Sqrt(1 - currentTime * currentTime) - 1) + startValue;
        }
        public static double OutCircular(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration;
            currentTime--;
            return changeValue * Math.Sqrt(1 - currentTime * currentTime) + startValue;
        }
        public static double InOutCircular(double currentTime, double startValue, double changeValue, double duration)
        {
            currentTime /= duration / 2.0;
            if (currentTime < 1)
            {
                return -changeValue / 2.0 * (Math.Sqrt(1 - currentTime * currentTime) - 1);
            }
            currentTime = currentTime - 2;
            return changeValue / 2.0 * (Math.Sqrt(1 - currentTime * currentTime) + 1) + startValue;
        }
    }
}
