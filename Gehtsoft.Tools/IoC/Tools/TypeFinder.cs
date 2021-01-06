using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Gehtsoft.Tools.IoC.Tools
{
    public static class TypeFinder
    {
        public static IEnumerable<Assembly> NearClass(Type type)
        {
            yield return type.Assembly;
        }

        public static IEnumerable<Assembly> NearClasses(params Type[] types)
        {
            HashSet<Assembly> assemblies = new HashSet<Assembly>();
            foreach (Type type in types)
            {
                if (assemblies.Contains(type.Assembly))
                    continue;
                assemblies.Add(type.Assembly);
                yield return type.Assembly;
            }
        }

        public static IEnumerable<Assembly> NearClass<T>() => NearClass(typeof(T));

        public static IEnumerable<Assembly> InAllAssemblies()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
                yield return assemblies[i];
        }

        public static IEnumerable<Assembly> ExceptSystem(this IEnumerable<Assembly> enumerator)
        {
            return enumerator.Where(assembly =>
            {
                string fullName = assembly.FullName;
                return !(assembly.FullName.StartsWith("mscorlib,", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("netstandard,", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("System,", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("Newtonsoft.Json,", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("Newtonsoft.Json.", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("Windows,", StringComparison.OrdinalIgnoreCase) ||
                         assembly.FullName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase));
            });
        }

        public static IEnumerable<Type> GetTypes(this IEnumerable<Assembly> enumerator)
        {
            foreach (Assembly assembly in enumerator)
            {
                foreach (Type type in assembly.GetTypes())
                    yield return type;
            }
        }

        public static IEnumerable<Type> GetTypes(this IEnumerable<Type> enumerator) => enumerator;

        public static IEnumerable<Type> Which(this IEnumerable<Assembly> enumerator, Func<Type, bool> predicate) => enumerator.GetTypes().Where(predicate);

        public static IEnumerable<Type> Which(this IEnumerable<Type> enumerator, Func<Type, bool> predicate) => enumerator.Where(predicate);

        public static IEnumerable<Type> WhichIsClass(this IEnumerable<Assembly> enumerable) => enumerable.GetTypes().WhichIsClass();

        public static IEnumerable<Type> WhichIsClass(this IEnumerable<Type> enumerable) => enumerable.Which(type => !type.IsInterface && !type.IsAbstract && !type.IsGenericTypeDefinition);

        public static IEnumerable<Type> WhichImplements(this IEnumerable<Assembly> enumerable, Type implementsInterface) => enumerable.GetTypes().WhichImplements(implementsInterface);

        public static IEnumerable<Type> WhichImplements(this IEnumerable<Type> enumerable, Type implementsInterface) =>
            enumerable.Which(type =>
            {
                foreach (Type interfaceType in type.GetTypeInfo().ImplementedInterfaces)
                {
                    if (implementsInterface.IsGenericTypeDefinition)
                    {
                        TypeInfo interfaceTypeInfo = interfaceType.GetTypeInfo();
                        if (interfaceTypeInfo.IsGenericType && interfaceTypeInfo.GetGenericTypeDefinition() == implementsInterface)
                            return true;
                    }
                    else
                    {
                        if (interfaceType == implementsInterface)
                            return true;
                    }
                }

                return false;
            });

        public static IEnumerable<Type> WhichImplements<T>(this IEnumerable<Assembly> enumerable) => enumerable.GetTypes().WhichImplements<T>();

        public static IEnumerable<Type> WhichImplements<T>(this IEnumerable<Type> enumerable) => WhichImplements(enumerable, typeof(T));

        public static IEnumerable<Type> WhichHasAttribute(this IEnumerable<Assembly> enumerable, Type attributeType) => enumerable.GetTypes().WhichHasAttribute(attributeType);

        public static IEnumerable<Type> WhichHasAttribute<T>(this IEnumerable<Assembly> enumerable) where T : System.Attribute => WhichHasAttribute(enumerable, typeof(T));

        public static IEnumerable<Type> WhichHasAttribute(this IEnumerable<Type> enumerable, Type attributeType) => Which(enumerable, type => type.GetCustomAttribute(attributeType) != null);

        public static IEnumerable<Type> WhichHasAttribute<T>(this IEnumerable<Type> enumerable) where T : System.Attribute => WhichHasAttribute(enumerable, typeof(T));

        public static void ForAll(this IEnumerable<Assembly> enumerable, Action<Type> action) => enumerable.GetTypes().ForAll(action);

        public static void ForAll(this IEnumerable<Type> enumerable, Action<Type> action)
        {
            foreach (Type type in enumerable)
                action(type);
        }

        public static void InvokeForAll(this IEnumerable<Type> enumerable, string methodName, IServiceProvider provider, params object[] additionalArguments)
            => enumerable.ForAll(type =>
            {

                TypeInfo typeInfo = type.GetTypeInfo();
                object obj = null;
                MethodInfo memberInfo = type.GetMethod(methodName, BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                if (memberInfo == null)
                    throw new ArgumentException($"Method {methodName} is not found in {type.Name}");

                if (!memberInfo.IsStatic)
                {
                    obj = provider != null ? ActivatorUtilities.CreateInstance(provider, type, additionalArguments) : Activator.CreateInstance(type);
                }
                memberInfo.Invoke(obj, TypeTools.BuildArguments(memberInfo, provider, additionalArguments));
            });


        public static void RegisterAll(this IEnumerable<Type> enumerable, Type asInterfaceType, Action<Type, Type> registerAction)
            => enumerable.ForAll(type =>
            {
                Type registerAs = null;
                if (asInterfaceType != null)
                {
                    TypeInfo typeInfo = type.GetTypeInfo();
                    foreach (Type interfaceType in typeInfo.ImplementedInterfaces)
                    {
                        if (asInterfaceType.IsGenericTypeDefinition)
                        {
                            TypeInfo interfaceTypeInfo = interfaceType.GetTypeInfo();
                            if (interfaceTypeInfo.IsGenericType && interfaceTypeInfo.GetGenericTypeDefinition() == asInterfaceType)
                                registerAs = interfaceType;
                        }
                        else
                        {
                            if (interfaceType == asInterfaceType)
                                registerAs = interfaceType;
                        }

                        if (registerAs != null)
                            break;
                    }
                }
                else
                    registerAs = type;

                if (registerAs == null)
                    throw new ArgumentException($"Type {type.Name} does not implement interface {asInterfaceType?.Name}");

                registerAction(registerAs, type);
            });

        public static void RegisterAll(this IEnumerable<Type> enumerable, IClassRegistry registry, RegistryMode mode = RegistryMode.CreateEveryTime, Type asInterfaceType = null)
            => enumerable.RegisterAll(asInterfaceType, (registerAs, type) => registry.Add(registerAs, type, mode));

        public static void RegisterAll(this IEnumerable<Type> enumerable, IServiceCollection collection, RegistryMode mode, Type asInterfaceType)
        {
            switch (mode)
            {
                case RegistryMode.Singleton:
                    enumerable.RegisterAllAsSingleton(collection, asInterfaceType);
                    break;
                case RegistryMode.Cached:
                    enumerable.RegisterAllAsScoped(collection, asInterfaceType);
                    break;
                case RegistryMode.CreateEveryTime:
                    enumerable.RegisterAllAsTransient(collection, asInterfaceType);
                    break;
            }
        }

        public static void RegisterAllAsSingleton(this IEnumerable<Type> enumerable, IServiceCollection collection, Type asInterfaceType)
           => enumerable.RegisterAll(asInterfaceType, (registerAs, type) => collection.AddSingleton(registerAs, type));

        public static void RegisterAllAsScoped(this IEnumerable<Type> enumerable, IServiceCollection collection, Type asInterfaceType)
            => enumerable.RegisterAll(asInterfaceType, (registerAs, type) => collection.AddScoped(registerAs, type));

        public static void RegisterAllAsTransient(this IEnumerable<Type> enumerable, IServiceCollection collection, Type asInterfaceType)
            => enumerable.RegisterAll(asInterfaceType, (registerAs, type) => collection.AddTransient(registerAs, type));
    }
}
