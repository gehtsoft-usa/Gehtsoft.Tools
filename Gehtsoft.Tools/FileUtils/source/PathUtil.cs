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
    public static class PathUtil
    {
        public static string RelativePath(string absoluteBasePath, string absolutePathToConvert)
        {
            string[] absDirs = absoluteBasePath.Split('\\');
            string[] relDirs = absolutePathToConvert.Split('\\');

            // Get the shortest of the two paths
            int len = absDirs.Length < relDirs.Length ? absDirs.Length : relDirs.Length;

            // Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < len; index++)
            {
                if (absDirs[index] == relDirs[index]) lastCommonRoot = index;
                else break;
            }

            // If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
            {
                throw new ArgumentException("Paths do not have a common base");
            }

            // Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            // Add on the ..
            for (index = lastCommonRoot + 1; index < absDirs.Length; index++)
            {
                if (absDirs[index].Length > 0) relativePath.Append("..\\");
            }

            // Add on the folders
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
            {
                relativePath.Append(relDirs[index] + "\\");
            }
            relativePath.Append(relDirs[relDirs.Length - 1]);

            return relativePath.ToString();
        }

        public static bool DeleteDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!Directory.Exists(path))
                return true;

            string[] content;
            bool success = true;

            content = Directory.GetFiles(path);

            foreach (string file in content)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    success = false;
                }
                catch (UnauthorizedAccessException)
                {
                    success = false;
                }
            }

            content = Directory.GetDirectories(path);
            foreach (string folder in content)
            {
                success &= DeleteDirectory(folder);
            }

            if (success)
            {
                try
                {
                    Directory.Delete(path);
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }
            return success;
        }
    }
}

