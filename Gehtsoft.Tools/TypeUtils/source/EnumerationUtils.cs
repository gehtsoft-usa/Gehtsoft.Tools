using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public static class EnumerationUtils
    {
        public static void ForEach<T>(this T[] array, Action<T, int> predicate)
        {
            int l = array.Length;
            for (int i = 0; i < array.Length; i++)
                predicate(array[i], i);
        }

        public static void ForEach<T>(this IList<T> array, Action<T, int> predicate)
        {
            int l = array.Count;
            for (int i = 0; i < array.Count; i++)
                predicate(array[i], i);
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> predicate)
        {
            foreach (T t in enumerable)
                predicate(t);
        }

    }
}
