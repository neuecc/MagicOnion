using System;

namespace Benchmark.ClientLib.Internal
{
    internal static class Durations
    {
        private const char DayChar = 'd';
        private const char HourChar = 'h';
        private const char MinuteChar = 'm';
        private const char SecondChar = 's';

        /// <summary>
        /// Convert 1d1h1m1s format to TimeSpan
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static TimeSpan FromString(string text)
        {
            var dResult = ExtractNumber(text, DayChar);
            var hResult = ExtractNumber(dResult.leastText, HourChar);
            var mResult = ExtractNumber(hResult.leastText, MinuteChar);
            var sResult = ExtractNumber(mResult.leastText, SecondChar);
            return TimeSpan.FromSeconds(dResult.number * 24 * 60 * 60 + hResult.number * 60 * 60 + mResult.number * 60 + sResult.number);
        }

        /// <summary>
        /// Convert TimeSpan to 1d1h1m1s format
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToString(TimeSpan input)
        {
            var text = "";
            if (input.Days != 0)
                text += $"{input.Days}{DayChar}";
            if (input.Hours != 0)
                text += $"{input.Hours}{HourChar}";
            if (input.Minutes != 0)
                text += $"{input.Minutes}{MinuteChar}";
            if (input.Seconds != 0)
                text += $"{input.Seconds}{SecondChar}";

            if (string.IsNullOrEmpty(text))
                text = "0";

            return text;
        }

        public static void Validate(string text) => FromString(text);

        private static (string leastText, int number) ExtractNumber(string text, char charactor)
        {
            if (string.IsNullOrEmpty(text))
                return ("", 0);

            var num = 0;
            var index = text.IndexOf(charactor);
            if (index != -1)
            {
                if (!int.TryParse(text.Substring(0, index), out num))
                    throw new FormatException($"Invalid Format detected at {text}.");
                text = text.Substring(index, text.Length - index).TrimStart(charactor);
            }
            return (text, num);
        }
    }
}
