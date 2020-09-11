using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.IoC.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Gehtsoft.Tools.IoC.Tools
{
    public static class TypeTools
    {
        internal static object[] BuildArguments(MemberInfo memberInfo, IServiceProvider factory, object[] additionalArguments = null)
        {
            ParameterInfo[] parameters = null;

            if (memberInfo.MemberType == MemberTypes.Constructor)
            {
                ConstructorInfo constructorInfo = (ConstructorInfo) memberInfo;
                parameters = constructorInfo.GetParameters();
            }
            else if (memberInfo.MemberType == MemberTypes.Method)
            {
                MethodInfo methodInfo = (MethodInfo) memberInfo;
                parameters = methodInfo.GetParameters();
            }

            if (parameters == null || parameters.Length == 0)
                return null;

            object[] arguments = new object[parameters.Length];
            foreach (ParameterInfo parameter in parameters)
            {
                object value = null;
                if (additionalArguments != null)
                    foreach (object arg in additionalArguments)
                        if (arg != null)
                            if (parameter.ParameterType.GetTypeInfo().IsAssignableFrom(arg.GetType()))
                                value = arg;

                if (value == null)
                    value = factory?.GetService(parameter.ParameterType);

                if (value == null && parameter.HasDefaultValue)
                    value = parameter.DefaultValue;

                arguments[parameter.Position] = value;
            }

            return arguments;
        }

        public static void InjectProperties(object obj, IServiceProvider factory)
        {
            foreach (PropertyInfo propertyInfo in obj.GetType().GetTypeInfo().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance))
            {
                InjectAttribute attribute = propertyInfo.GetCustomAttribute<InjectAttribute>();
                if (attribute != null)
                {
                    var r = factory.GetService(propertyInfo.PropertyType);
                    if (attribute.Required && r == null)
                        throw new InvalidOperationException($"Property {propertyInfo.Name} of {obj.GetType().Name} requires {propertyInfo.PropertyType.Name} to be injected but the type is not registered in the service provider");
                    propertyInfo.SetValue(obj, r);
                }
            }
        }

        public static void InjectFields(object obj, IServiceProvider factory)
        {
            foreach (FieldInfo fieldInfo in obj.GetType().GetTypeInfo().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance))
            {
                InjectAttribute attribute = fieldInfo.GetCustomAttribute<InjectAttribute>();
                if (attribute != null)
                {
                    var r = factory.GetService(fieldInfo.FieldType);
                    if (attribute.Required && r == null)
                        throw new InvalidOperationException($"Field {fieldInfo.Name} of {obj.GetType().Name} requires {fieldInfo.FieldType.Name} to be injected but the type is not registered in the service provider");
                    fieldInfo.SetValue(obj, r);
                }
            }
        }

        private static object InjectEverything(object obj, IServiceProvider factory)
        {
            InjectProperties(obj, factory);
            InjectFields(obj, factory);
            return obj;
        }

        internal static object ConstructUsingFactory(Type type, IServiceProvider factory, object[] args = null)
        {
            object obj = factory.GetService(type);
            return obj ?? ForcedConstructUsingFactory(type, factory, args);
        }

        internal static object ForcedConstructUsingFactory(Type type, IServiceProvider factory, object[] args = null)
        {
            return ActivatorUtilities.CreateInstance(factory, type, args);
        }
    }
}
