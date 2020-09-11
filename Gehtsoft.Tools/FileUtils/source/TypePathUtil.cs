using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.FileUtils
{
    public static class TypePathUtil
    {
        public static string ApplicationName()
        {
            FileInfo fi = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
            return fi.FullName;
        }
        public static string ApplicationFolder()
        {
            FileInfo fi = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
            return fi.Directory.FullName;
        }

        public static string TypeFileName(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            Assembly assembly;
            assembly = type.Assembly;
            UriBuilder uri = new UriBuilder(assembly.CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetFullPath(path);
        }

        public static string TypeFolder(Type type)
        {
            return Path.GetDirectoryName(TypeFileName(type));
        }
    }
}

