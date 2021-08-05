using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Gehtsoft.Tools2.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Gehtsoft.Tools2.Reflection
{
    /// <summary>
    /// The class that finds the types by various conditions
    /// </summary>
    public static class TypeFinder
    {
        private static WeakReference<Assembly[]> mEveryWhere = null;

        /// <summary>
        /// Returns enumeration of all types in the list of assemblies
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static IEnumerable<Type> InAssemblies(params Assembly[] assemblies)
        {
            for (int i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes();
                for (int j = 0; j < types.Length; j++)
                    yield return types[j];
            }
        }

        /// <summary>
        /// Returns the enumeration of all available types
        /// </summary>
        public static IEnumerable<Type> EveryWhere
        {
            get
            {
                if (mEveryWhere == null || !mEveryWhere.TryGetTarget(out Assembly[] assemblies))
                {
                    assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    mEveryWhere = new WeakReference<Assembly[]>(assemblies);
                }
                return InAssemblies(assemblies);
            }
        }

        /// <summary>
        /// Returns all types in the assembly specified.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IEnumerable<Type> InAssembly(Assembly assembly) => InAssemblies(assembly);

        /// <summary>
        /// Returns all types located in the same assembly as the types specified.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> NearType(Type type) => InAssembly(type.Assembly);

        /// <summary>
        /// Filters only the types that implements the attribute (specified as an argument).
        /// </summary>
        /// <param name="types"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        public static IEnumerable<Type> WhichHaveAttribute(this IEnumerable<Type> types, Type attributeType)
        {
            if (!attributeType.IsAssignableFrom(typeof(Attribute)))
                throw new ArgumentException($"The type must be derived from the {nameof(Attribute)} type.", nameof(attributeType));

            return types.Where(t => t.GetCustomAttribute(attributeType) != null);
        }

        /// <summary>
        /// Filters only the types that implements the attribute (specified as a generic parameter).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="types"></param>
        /// <returns></returns>
        public static IEnumerable<Type> WhichHaveAttribute<T>(this IEnumerable<Type> types)
            where T : Attribute
            => WhichHaveAttribute(types, typeof(T));

        /// <summary>
        /// Filters only the types that implements the interface (specified as an argument).
        ///
        /// If the generic interface definition is specified, e.g. `typeof(IEnumerable<>)`,
        /// the implementation with any parameter will match the condition.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static IEnumerable<Type> WhichImplements(this IEnumerable<Type> types, Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("The type must be an interface.", nameof(interfaceType));

            if (interfaceType.IsGenericTypeDefinition)
                return types.Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType));
            else
                return types.Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(interfaceType)));
        }

        /// <summary>
        /// Filters only the types that implements the interface (specified as a generic parameter).
        ///
        /// If the generic interface definition is specified, e.g. `typeof(IEnumerable<>)`,
        /// the implementation with any parameter will match the condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="types"></param>
        /// <returns></returns>
        public static IEnumerable<Type> WhichImplements<T>(this IEnumerable<Type> types)
            where T : class
            => WhichImplements(types, typeof(T));

        /// <summary>
        /// Filters only the types that implements the interface or a derived from the type (specified as an argument).
        /// </summary>
        /// <param name="types"></param>
        /// <param name="interfaceType"></param>
        public static IEnumerable<Type> WhichDerivedFrom(this IEnumerable<Type> types, Type parentType)
        {
            if (!parentType.IsClass)
                throw new ArgumentException("The type must be a reference type or an interface.", nameof(parentType));

            if (parentType.IsGenericTypeDefinition)
                if (parentType.IsInterface)
                    return WhichImplements(types, parentType);
                else
                    throw new ArgumentException("Only generic interface definitions are supported, but the type is a generic class definition", nameof(parentType));

            return types.Where(t => t.IsAssignableFrom(parentType));
        }

        /// <summary>
        /// Filters only the types that implements the interface or a derived from the type (specified as an argument).
        /// </summary>
        /// <param name="types"></param>
        /// <param name="interfaceType"></param>
        public static IEnumerable<Type> WhichDerivedFrom<T>(this IEnumerable<Type> types)
            where T : class
            => WhichDerivedFrom(types, typeof(T));

        /// <summary>
        /// Registers the types in the service collection.
        ///
        /// The method registers all the types in the service collection
        /// with the lifeTime specified. The types may be registered "as is" or
        /// as an implementation of an interface (e.g. `IEnumerable`) or
        /// a generic interface definition (e.g. `IEnumerable<>`).
        /// </summary>
        /// <param name="types"></param>
        /// <param name="serviceCollection"></param>
        /// <param name="lifeTime"></param>
        /// <param name="registerAs">The interface or generic interface definition to register the type as.</param>
        public static void AddToServiceCollection(this IEnumerable<Type> types, IServiceCollection serviceCollection, ServiceLifetime lifeTime, Type registerAs = null)
            => types.ForAll(type => serviceCollection.Add(new ServiceDescriptor(registerAs == null ? type : type.ExtractImplementation(registerAs), type, lifeTime)));
    }
}
