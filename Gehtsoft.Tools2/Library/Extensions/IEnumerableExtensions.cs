using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gehtsoft.Tools2.Extensions
{
    /// <summary>
    /// IEnumerable generic type extensions
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Calls the action specified for each element of the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="action"></param>
        public static void ForAll<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (var value in values)
                action(value);
        }

        /// <summary>
        /// Finds the index of the element which matches the predicate.
        /// 
        /// Please note that some collections do not support consistent elements order, e.g. dictionaries or hash sets. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static int IndexOf<T>(this IEnumerable<T> values, Func<T, bool> action)
        {
            int ix = -1;
            foreach (T value in values)
            {
                ++ix;
                if (action(value))
                    return ix;
            }
            return ix;
        }

        /// <summary>
        /// Finds the index of the specified element.
        /// 
        /// Please note that some collections do not support consistent elements order, e.g. dictionaries or hash sets. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int IndexOf<T>(this IEnumerable<T> values, T value)
            where T : IEquatable<T>
            => ((object)value == null) ?
                IndexOf<T>(values, v => (object)v == null) :
                IndexOf<T>(values, v => value.Equals(v));
    }
}
