using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Gehtsoft.Tools2.Extensions
{
    /// <summary>
    /// Extensions for the object type
    /// </summary>
    public static class DependencyInjectionObjectExtension
    {
        /// <summary>
        /// Populates all fields and properties attributed with `InjectAttribute` using the factory specified.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="factory"></param>
        public static void PopulateMembers(this object obj, Func<Type, object> factory)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            var type = obj.GetType();

            type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
              .Where(field => field.GetCustomAttribute<InjectAttribute>() != null)
              .ForAll(field => field.SetValue(obj, factory(field.FieldType)));

            type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
              .Where(property => property.GetCustomAttribute<InjectAttribute>() != null &&
                                 property.SetMethod != null)
              .ForAll(property => property.SetValue(obj, factory(property.PropertyType)));
        }

        /// <summary>
        /// Populates all fields and properties attributed with `InjectAttribute` using the service provider.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="provider"></param>
        public static void PopulateMembers(this object obj, IServiceProvider provider)
            => PopulateMembers(obj, t => ActivatorUtilities.CreateInstance(provider, t));
    }
}
