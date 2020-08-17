using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if NETCORE
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
#endif

namespace Gehtsoft.Tools.TypeUtils
{
    public static class TypeUtils
    {
        public static Type ExtractImplementation(this Type type, Type implementsInterface)
        {
            if (implementsInterface.IsGenericTypeDefinition)
                throw new ArgumentException("The interface requested must not be a generic interface definition", nameof(implementsInterface));
            foreach (Type interfaceType in type.GetTypeInfo().ImplementedInterfaces)
            {
                if (interfaceType == implementsInterface)
                    return interfaceType;
            }

            return null;
        }

        public static Type ExtractGenericImplementation(this Type type, Type genericInterface, params Type[] typeParams)
        {
            if (!genericInterface.IsGenericTypeDefinition)
                throw new ArgumentException("The interface requested must be a generic interface definition", nameof(genericInterface));

            if (typeParams == null || typeParams.Length == 0)
                throw new ArgumentException("The type parameters must be listed", nameof(genericInterface));

            foreach (Type interfaceType in type.GetTypeInfo().ImplementedInterfaces)
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericInterface)
                {
                    if (interfaceType.GenericTypeArguments.Length == typeParams.Length)
                    {
                        bool allMatch = true;
                        for (int i = 0; i < typeParams.Length && allMatch; i++)
                            allMatch = allMatch & (typeParams[i] == interfaceType.GenericTypeArguments[i]);
                        if (allMatch)
                            return interfaceType;
                    }
                }
            }
            return null;
        }
    }

#if NETCORE
    public static class AssemblyUtils
    {
        public static Assembly LoadAssemblyFromPath(string path)
        {
            using (AssemblyResolver r = new AssemblyResolver(path))
                return r.Assembly;
        }

        private static string AssemblyPath(Assembly assembly)
        {
            if (assembly == null)
                return null;
            UriBuilder uri = new UriBuilder(assembly.CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            FileInfo fi = new FileInfo(Path.GetFullPath(path));
            return fi.DirectoryName;
        }

        private static bool ExistIn(string path, string file) => File.Exists(Path.Combine(path, file));

        private static string ResolveInRepository(string repoPath, string assemblyName)
        {
            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                assemblyName = assemblyName.Substring(0, assemblyName.Length - 4);
            DependencyContext context = DependencyContext.Default;
            var libs = context.RuntimeLibraries;
            RuntimeLibrary library = context.RuntimeLibraries.FirstOrDefault(runtime => string.Equals(runtime.Name, assemblyName, StringComparison.OrdinalIgnoreCase));
            if (library == null)
                return null;
            var wrapper = new CompilationLibrary(
                library.Type,
                library.Name,
                library.Version,
                library.Hash,
                library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                library.Dependencies,
                library.Serviceable);

            List<string> assemblies = new List<string>();
            PackageCompilationAssemblyResolver resolver = new PackageCompilationAssemblyResolver(repoPath);

            if (resolver.TryResolveAssemblyPaths(wrapper, assemblies))
                return assemblies[0];
            return null;

        }

        public static Assembly FindAssembly(string assemblyName)
        {
            //if parameter is a path
            if (assemblyName.Contains('/') || assemblyName.Contains('\\'))
            {
                if (!Path.IsPathRooted(assemblyName))
                    assemblyName = Path.GetFullPath(assemblyName);
                return LoadAssemblyFromPath(assemblyName);
            }

            //scan callers and try to find it near a caller
            HashSet<string> tested = new HashSet<string>();
            
            StackTrace trace = new StackTrace();
            foreach (StackFrame frame in trace.GetFrames())
            {
                string path = AssemblyPath(frame.GetMethod().DeclaringType?.Assembly);
                if (path == null)
                    continue;
                if (tested.Contains(path))
                    continue;
                tested.Add(path);
                if (ExistIn(path, assemblyName))
                    return LoadAssemblyFromPath(Path.Combine(path, assemblyName));
            }

            string nugetFolder = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), ".nuget\\packages");
            string resolvedPath = ResolveInRepository(nugetFolder, assemblyName);

            if (resolvedPath != null)
                return LoadAssemblyFromPath(resolvedPath);
            
            string nugetFallbackFolder = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet\\sdk\\NuGetFallbackFolder");
            resolvedPath = ResolveInRepository(nugetFallbackFolder, assemblyName);

            if (resolvedPath != null)
                return LoadAssemblyFromPath(resolvedPath);
            
            return null;
        }
    }

#endif
}
