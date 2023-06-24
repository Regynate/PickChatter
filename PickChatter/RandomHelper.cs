using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    public static class RandomHelper
    {
        private static readonly Random random = new Random();

        public static float Float(float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }

        public static float Float(float max)
        {
            return Float(0, max);
        }

        public static int Int(int min, int max)
        {
            return random.Next(min, max);
        }

        public static int Int(int max)
        {
            return random.Next(max);
        }

        public static bool Bool()
        {
            return random.Next(2) == 0;
        }

        public static T Random<T>(this IEnumerable<T> values)
        {
            return values.ElementAt(random.Next(0, values.Count()));
        }

        public static T Choice<T>(T first, T second)
        {
            return Random(new List<T> { first, second });
        }

        public static List<T> Random<T>(this IEnumerable<T> values, int count)
        {
            List<T> result = new List<T>();

            for (int i = 0; i < count; ++i)
            {
                result.Add(Random(values));
            }

            return result;
        }

        public static T Random<T>(this IEnumerable<T> values, Func<T, float> getWeight)
        {
            float sum = 0;

            foreach (T value in values)
            {
                sum += getWeight(value);
            }

            float chosen = Float(sum);

            sum = 0;

            foreach (T value in values)
            {
                sum += getWeight(value);
                if (sum > chosen)
                {
                    return value;
                }
            }

            return values.Last();
        }

        public static char Char()
        {
            return "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray().Random();
        }

        public static string String(int length)
        {
            StringBuilder sb = new();

            for (int i = 0; i < length; i++)
            {
                sb.Append(Char());
            }

            return sb.ToString();
        }
    }
}
