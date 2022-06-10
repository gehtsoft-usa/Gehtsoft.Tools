using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools2.Extensions
{
    /// <summary>
    /// Random 
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Returns next random item from an array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T Next<T>(this Random r, T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Length == 0)
                throw new ArgumentException("Array is empty", nameof(array));
            if (array.Length == 1)
                return array[0];
            return array[r.Next(array.Length)];
        }

        /// <summary>
        /// Returns next random item from a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T Next<T>(this Random r, IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (list.Count == 0)
                throw new ArgumentException("List is empty", nameof(list));
            if (list.Count == 1)
                return list[0];
            return list[r.Next(list.Count)];
        }

        /// <summary>
        /// Returns a next random item from a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T Next<T>(this Random r, ICollection<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0)
                throw new ArgumentException("Array is empty", nameof(collection));
            if (collection.Count == 1)
                return collection.First();
            return collection.ElementAt(r.Next(collection.Count));
        }

        /// <summary>
        /// Returns next normally distributed double in the specified range (min, max]
        /// </summary>
        /// <param name="r"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double NextDouble(this Random r, double min, double max) => min + r.NextDouble() * (max - min);

        /// <summary>
        /// Returns next normally distributed double in range (o, max]
        /// </summary>
        /// <param name="r"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double NextDouble(this Random r, double max) => r.NextDouble() * max;

        /// <summary>
        /// Returns normally distributed integer in range (0, max]
        /// </summary>
        /// <param name="r"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Next(this Random r, int min, int max) => min + r.Next(max - min);

        /// <summary>
        /// Returns normally distributed data in range (from, to]
        /// </summary>
        /// <param name="r"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static DateTime Next(this Random r, DateTime from, DateTime to) => from.AddSeconds(r.NextDouble((to - from).TotalSeconds));

        /// <summary>
        /// Generate a chance
        /// </summary>
        /// <param name="r"></param>
        /// <param name="chance">A change of event between 0 (never) and (1) always</param>
        /// <returns>true if events with the chance specified is happened</returns>
        public static bool IsChance(this Random r, double chance) => chance > r.NextDouble();

        /// <summary>
        /// Generates normally distributed number.
        ///
        /// Each operation makes two Gaussians for the price of one, and apparently they can be cached or something for better performance, but who cares.
        /// </summary>
        /// <param name="r"></param>
        /// <param name = "mu">Mean of the distribution</param>
        /// <param name = "sigma">Standard deviation</param>
        /// <returns></returns>
        public static double NextGaussian(this Random r, double mu = 0, double sigma = 1)
        {
            var u1 = r.NextDouble();
            var u2 = r.NextDouble();

            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2);

            var rand_normal = mu + sigma * rand_std_normal;

            return rand_normal;
        }

        /// <summary>
        ///   Generates values from a triangular distribution.
        /// </summary>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Triangular_distribution for a description of the triangular probability distribution and the algorithm for generating one.
        /// </remarks>
        /// <param name="r"></param>
        /// <param name = "a">Minimum</param>
        /// <param name = "b">Maximum</param>
        /// <param name = "c">Mode (most frequent value)</param>
        /// <returns></returns>
        public static double NextTriangular(this Random r, double a, double b, double c)
        {
            var u = r.NextDouble();

            return u < (c - a) / (b - a)
                       ? a + Math.Sqrt(u * (b - a) * (c - a))
                       : b - Math.Sqrt((1 - u) * (b - a) * (b - c));
        }

        /// <summary>
        /// Selects a random event
        /// </summary>
        /// <param name="r"></param>
        /// <param name="probabilities">The weight of the event (any range, just use the same scale for all events). More weight - higher propbability for the event</param>
        /// <returns></returns>
        public static int Event(this Random r, params int[] probabilities)
        {
            if (probabilities == null || probabilities.Length == 0)
                return -1;
            if (probabilities.Length == 1)
                return 0;

            double[] chance = new double[probabilities.Length];

            double sum = 0;
            for (int i = 0; i < probabilities.Length; i++)
                sum += probabilities[i];

            double prior = 0;
            for (int i = 0; i < probabilities.Length; i++)
                prior = chance[i] = prior + (1.0 * probabilities[i]) / sum;

            double v = r.NextDouble();

            for (int i = 0; i < probabilities.Length; i++)
                if (v <= chance[i])
                    return i;

            throw new InvalidOperationException("Sum of probabilities appears to be more than 100%");
        }
    }
}
