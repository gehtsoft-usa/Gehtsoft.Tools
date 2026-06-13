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

            var resolvers = new List<ICompilationAssemblyResolver>
            {
                new ReferenceAssemblyPathResolver(),
                new ReferenceAssemblyPathResolver(fi.DirectoryName, new string[] {}),
            };

            string nugetFolder = AssemblyUtils.NuGetGlobalPackagesFolder();
            if (nugetFolder != null)
                resolvers.Add(new PackageCompilationAssemblyResolver(nugetFolder));

            //NuGetFallbackFolder only ever existed on Windows (and was dropped after .NET Core 3).
            string programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(programFiles))
                resolvers.Add(new PackageCompilationAssemblyResolver(Path.Combine(programFiles, "dotnet", "sdk", "NuGetFallbackFolder")));

            resolvers.Add(new AssemblyFolderResolver(fi.DirectoryName));

            this.assemblyResolver = new CompositeCompilationAssemblyResolver(resolvers.ToArray());

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
