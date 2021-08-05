using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Gehtsoft.Tools2.Extensions
{
    /// <summary>
    /// Type extensions
    /// </summary>
    public static class TypeExtension
    {
        /// <summary>
        /// Returns the file name of the assembly file which contains the type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ContainerFileName(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            Assembly assembly = type.Assembly;
            UriBuilder uri = new UriBuilder(assembly.CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Returns the folder name where the assembly file, which contains the type, is located
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string TypeFolder(Type type) => Path.GetDirectoryName(ContainerFileName(type));

        /// <summary>
        /// Returns the type specified if the target type implements it.
        ///
        /// If the type specified is a generic type definition (e.g. `typeof(IEnumerable<>)`) it
        /// will return the first implementation of that generic type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericType"></param>
        /// <returns></returns>
        public static Type ExtractImplementation(this Type type, Type genericType)
        {
            if (genericType.IsGenericTypeDefinition)
            {
                if (genericType.IsInterface)
                {
                    var x = Array.Find(type.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);
                    if (x == null)
                        throw new ArgumentException($"Type {type} does not implement {genericType}", nameof(genericType));
                    return x;
                }
                else
                {
                    var t = type;
                    while (t != null)
                    {
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == genericType)
                            return t;
                        t = t.BaseType;
                    }
                    throw new ArgumentException($"Type {type} is not derived from {genericType}", nameof(genericType));
                }
            }
            else
            {
                if (!type.IsAssignableFrom(genericType))
                    throw new ArgumentException($"Type {type} is not derived from {genericType} nor it implements it", nameof(genericType));
                return genericType;
            }
        }
    }
}
