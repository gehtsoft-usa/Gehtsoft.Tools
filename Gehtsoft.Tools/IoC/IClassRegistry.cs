using System;

namespace Gehtsoft.Tools.IoC
{
    public enum RegistryMode
    {
        CreateEveryTime = 1,
        Singleton = 2,
        Cached = 3,
    }

    public interface IClassRegistry
    {
        void Add(Type registryType, Type implementationType, RegistryMode mode);
    }
}