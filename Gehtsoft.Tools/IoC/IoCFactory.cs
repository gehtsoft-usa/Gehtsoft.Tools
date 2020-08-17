using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gehtsoft.Tools.IoC.Tools;
#if HASSERVICEPROVIDER
using Microsoft.Extensions.DependencyInjection;
#endif


namespace Gehtsoft.Tools.IoC
{
    public interface IIoCFactory : IServiceProvider, IClassRegistry
    {
        object GetService(Type type, object[] args);
    }

    #if HASSERVICEPROVIDER
    public class IoCFactory : IIoCFactory
    {
        private IServiceProvider mProvider;
        private ServiceCollection mServiceCollection = new ServiceCollection();

        private IServiceProvider Provider => mProvider ?? (mProvider = mServiceCollection.BuildServiceProvider());

        public object GetService(Type serviceType)
        {
            return Provider.GetService(serviceType) ?? ActivatorUtilities.CreateInstance(Provider, serviceType);
        }

        public object GetService(Type type, object[] args)
        {
            foreach (var e in mServiceCollection)
            {
                if (e.ServiceType == type && e.ImplementationType != type)
                {
                    type = e.ImplementationType;
                    break;
                }
            }
            return ActivatorUtilities.CreateInstance(Provider, type, args);
        } 

        public void Add(Type registryType, Type implementationType, RegistryMode mode) => Add(registryType, implementationType, mode, null);

        private void Add(Type registryType, Type implementationType, RegistryMode mode, object instance)
        {
            mProvider = null;
            switch (mode)
            {
                case RegistryMode.Singleton:
                {
                    if (instance == null)
                        mServiceCollection.AddSingleton(registryType, implementationType);
                    else
                        mServiceCollection.AddSingleton(registryType, provider => instance);
                }
                    break;
                case RegistryMode.Cached:
                    mServiceCollection.AddScoped(registryType, implementationType);
                    break;
                case RegistryMode.CreateEveryTime:
                    mServiceCollection.AddTransient(registryType, implementationType);
                    break;
            }
        }

        public void Add<T>() => Add(typeof(T), typeof(T), RegistryMode.CreateEveryTime);

        public void AddSingleton<T>() => Add(typeof(T), typeof(T), RegistryMode.Singleton);

        public void Add<T, TI>() where TI : T  => Add(typeof(T), typeof(TI), RegistryMode.CreateEveryTime);

        public void AddSingleton<T, TI>() where TI : T => Add(typeof(T), typeof(TI), RegistryMode.Singleton); 

        public void AddSingleton<T>(T instance) => Add(typeof(T), instance.GetType(), RegistryMode.Singleton, instance);

        public T GetService<T>() where T : class => GetService(typeof(T)) as T; 

        public T GetService<T>(params object[] args) where T : class => GetService(typeof(T), args) as T; 
    }
    #else
    public class IoCFactory : IIoCFactory
    {
        class FactoryItem
        {
            public Type ImplementationType { get; set; }
            public bool IsSignletone { get; set; }
            public object SignletonObject { get; set; }
        }

        private Dictionary<Type, FactoryItem> mTypes = new Dictionary<Type, FactoryItem>();

        public object GetService(Type serviceType) => GetService(serviceType, null);

        public object GetService(Type type, object[] args)
        {
            if (mTypes.TryGetValue(type, out FactoryItem item))
            {
                if (item.IsSignletone)
                {
                    if (item.SignletonObject == null)
                        item.SignletonObject = TypeTools.ForcedConstructUsingFactory(item.ImplementationType, this, args);
                    return item.SignletonObject;
                }
                return TypeTools.ForcedConstructUsingFactory(item.ImplementationType, this, args);
            }

            if (type.GetTypeInfo().IsValueType || !type.GetTypeInfo().IsAbstract)
                return TypeTools.ForcedConstructUsingFactory(type, this, args);

            return null;
        }

        public void Add(Type registryType, Type implementationType, RegistryMode mode) => Add(registryType, implementationType, mode, null);

        private void Add(Type registryType, Type implementationType, RegistryMode mode, object instance)
        {
            FactoryItem item = new FactoryItem() {ImplementationType = implementationType, IsSignletone = mode == RegistryMode.Singleton, SignletonObject = instance};
            mTypes[registryType] = item;
        }

        public void Add<T>() => Add(typeof(T), typeof(T), RegistryMode.CreateEveryTime);

        public void AddSingleton<T>() => Add(typeof(T), typeof(T), RegistryMode.Singleton);

        public void Add<T, TI>() where TI : T  => Add(typeof(T), typeof(TI), RegistryMode.CreateEveryTime);

        public void AddSingleton<T, TI>() where TI : T => Add(typeof(T), typeof(TI), RegistryMode.Singleton); 

        public void AddSingleton<T>(T instance) => Add(typeof(T), instance.GetType(), RegistryMode.Singleton, instance);

        public T GetService<T>() where T : class => GetService(typeof(T)) as T; 

        public T GetService<T>(params object[] args) where T : class => GetService(typeof(T), args) as T; 
    }    
#endif
}
