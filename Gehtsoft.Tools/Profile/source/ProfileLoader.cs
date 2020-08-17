using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.ConfigurationProfile
{
    public static class ProfileLoader
    {
        private static Regex mParser1 = new Regex(@"^\s*\[([^]]*)\]\s*$");
        private static Regex mParser2 = new Regex(@"^\s*([^=]+)\s*=\s*(\S.*)?$");

        public static Profile LoadProfile(string fileName)
        {
            Profile profile = new Profile();

            using (StreamReader reader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8))
            {
                string currSection = "";
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    Match m;
                    m = mParser1.Match(line);
                    if (m.Success)
                        currSection = m.Groups[1].Value.Trim();
                    else
                    {
                        m = mParser2.Match(line);
                        if (m.Success)
                            profile.Set(currSection, m.Groups[1].Value.Trim(), m.Groups[2].Value);
                    }
                }
            }
            return profile;
        }

        public static void SaveProfile(string fileName, Profile profile)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    foreach (string section in profile.GetSections())
                    {
                        writer.WriteLine($"[{section}]");
                        foreach (string key in profile.GetKeys(section))
                            writer.WriteLine($"{key}={profile.Get(section, key)}");
                    }
                }
            }
        }
    }
}
