#if NETCORE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Gehtsoft.Tools.TypeUtils
{
    class AssemblyResolver : IDisposable
    {
        private readonly ICompilationAssemblyResolver assemblyResolver;
        private readonly DependencyContext dependencyContext;
        private readonly AssemblyLoadContext loadContext;

        class AssemblyFolderResolver : ICompilationAssemblyResolver
        {
            private readonly string mAssemblyPath;

            public AssemblyFolderResolver(string assemblyPath)
            {
                mAssemblyPath = assemblyPath;
            }

            public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
            {
                foreach (var assembly in library.Assemblies)
                {
                    string path = Path.Combine(mAssemblyPath, assembly);
                    if (File.Exists(path))
                    {
                        assemblies.Add(path);
                    }
                }
                return assemblies.Count > 0;
            }
        }

        public AssemblyResolver(string path)
        {
            FileInfo fi = new FileInfo(path);
            this.Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            this.dependencyContext = DependencyContext.Load(this.Assembly);
            string nugetFolder = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), ".nuget\\packages");
            string nugetFallbackFolder = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet\\sdk\\NuGetFallbackFolder");

            this.assemblyResolver = new CompositeCompilationAssemblyResolver
            (new ICompilationAssemblyResolver[]
            {
                new ReferenceAssemblyPathResolver(),
                new ReferenceAssemblyPathResolver(fi.DirectoryName, new string[] {}),
                new PackageCompilationAssemblyResolver(nugetFolder),
                new PackageCompilationAssemblyResolver(nugetFallbackFolder),
                new AssemblyFolderResolver(fi.DirectoryName),
            });

            this.loadContext = AssemblyLoadContext.GetLoadContext(this.Assembly);
            this.loadContext.Resolving += OnResolving;
        }

        public Assembly Assembly { get; }

        public void Dispose()
        {
            this.loadContext.Resolving -= this.OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            bool NamesMatch(RuntimeLibrary runtime)
            {
                return string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase);
            }

            RuntimeLibrary library =
                this.dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);
            if (library != null)
            {
                var wrapper = new CompilationLibrary(
                    library.Type,
                    library.Name,
                    library.Version,
                    library.Hash,
                    library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                    library.Dependencies,
                    library.Serviceable);

                var assemblies = new List<string>();
                this.assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
                if (assemblies.Count > 0)
                {
                    return this.loadContext.LoadFromAssemblyPath(assemblies[0]);
                }
            }
            return null;
        }
    }
}
#endif