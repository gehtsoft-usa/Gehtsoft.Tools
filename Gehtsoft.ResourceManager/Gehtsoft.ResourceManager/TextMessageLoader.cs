using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;

namespace Gehtsoft.ResourceManager
{
    public static partial class TextMessageLoader
    {
        public static TextMessageBlock[] LoadXml(string component, Assembly assembly, string manifestResourceName, Encoding encoding = null)
        {
            XmlDocument document = new XmlDocument();
            using (Stream stream = assembly.GetManifestResourceStream(manifestResourceName))
                document.Load(stream);

            List<TextMessageBlock> list = new List<TextMessageBlock>();
            Dictionary<string, TextMessageBlock> index = new Dictionary<string, TextMessageBlock>();
            ScanXmlGroup(document.DocumentElement, component, null, list, index);
            return list.ToArray();
        }

        public static TextMessageBlock[] LoadXml(string component, string xmlFile)
        {
            XmlDocument document = new XmlDocument();
            document.Load(xmlFile);
            List<TextMessageBlock> list = new List<TextMessageBlock>();
            Dictionary<string, TextMessageBlock> index = new Dictionary<string, TextMessageBlock>();
            ScanXmlGroup(document.DocumentElement, component, null, list, index);
            return list.ToArray();
        }

        static void ScanXmlGroup(XmlNode groupRoot, string component, string prefix, List<TextMessageBlock> list, Dictionary<string, TextMessageBlock> index)
        {
            foreach (XmlNode messageElement in groupRoot.ChildNodes)
            {
                if (messageElement.NodeType == XmlNodeType.Element && messageElement.Name == "message")
                {
                    StringBuilder defaultText = new StringBuilder();
                    bool hasEnUsText = false;
                    string id;

                    if (messageElement.Attributes["id"] == null)
                        continue;

                    if (prefix != null)
                        id = $"{prefix}.{messageElement.Attributes["id"].Value}";
                    else
                        id = messageElement.Attributes["id"].Value;

                    foreach (XmlNode messageChildren in messageElement.ChildNodes)
                    {
                        if (messageChildren.NodeType == XmlNodeType.Text)
                            defaultText.Append(messageChildren.Value);
                        else if (messageChildren.NodeType == XmlNodeType.Element && messageChildren.Name == "text")
                        {
                            string language = null;
                            if (messageChildren.Attributes["language"] != null)
                                language = messageChildren.Attributes["language"].Value;
                            else if (messageChildren.Attributes["lang"] != null)
                                language = messageChildren.Attributes["lang"].Value;
                            if (language == null)
                                language = "en";
                            string value = messageChildren.InnerText.Trim();

                            TextMessageBlock block;
                            if (!index.TryGetValue(language, out block))
                            {
                                block = new TextMessageBlock() {Component = component, Language = language};
                                list.Add(block);
                                index[language] = block;
                            }
                            block.Add(new TextMessage() {Name = id, Value = value});
                            if (language == "en")
                                hasEnUsText = true;
                        }
                    }
                    if (!hasEnUsText)
                    {
                        TextMessageBlock block;
                        if (!index.TryGetValue("en", out block))
                        {
                            block = new TextMessageBlock() {Component = component, Language = "en"};
                            list.Add(block);
                            index["en"] = block;
                        }
                        block.Add(new TextMessage() {Name = id, Value = defaultText.ToString().Trim()});
                    }
                }
                else if (messageElement.NodeType == XmlNodeType.Element && messageElement.Name == "group")
                {
                    string id;

                    if (messageElement.Attributes["id"] == null)
                        continue;

                    if (prefix != null)
                        id = $"{prefix}.{messageElement.Attributes["id"].Value}";
                    else
                        id = messageElement.Attributes["id"].Value;

                    ScanXmlGroup(messageElement, component, id, list, index);
                }
            }
        }

        private static List<CultureInfo> mSupportedCultures = null;

        public static void AddSupportedCulture(string name)
        {
            CultureInfo info = new CultureInfo(name);
            if (mSupportedCultures == null)
            {
                mSupportedCultures = new List<CultureInfo>();
                mSupportedCultures.Add(new CultureInfo(""));
            }
            foreach (CultureInfo info1 in mSupportedCultures)
                if (info1.Name == name)
                    return;
            mSupportedCultures.Add(info);
        }

        public static TextMessageBlock[] LoadResource(string component, string baseName, string assemblyFile)
        {
            List<TextMessageBlock> blocks = new List<TextMessageBlock>();
            Assembly assembly = GetAssembly(assemblyFile);
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(baseName, assembly);
            CultureInfo[] cultures = mSupportedCultures?.ToArray() ?? CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (CultureInfo culture in cultures)
            {
                try
                {
                    ResourceSet rs = rm.GetResourceSet(culture, true, false);
                    if (rs != null)
                    {
                        TextMessageBlock block = new TextMessageBlock() {Component = component, Language = culture.Name};
                        if (block.Language == "")
                            block.Language = "en";
                        foreach (DictionaryEntry entry in rs)
                        {
                            if (entry.Value is string)
                                block.Add(new TextMessage() {Name = entry.Key.ToString(), Value = entry.Value.ToString()});
                        }
                        if (block.Count > 0)
                            blocks.Add(block);
                    }
                }
                catch (CultureNotFoundException )
                {

                }
            }
            return blocks.ToArray();
        }

        private static Assembly GetAssembly(string assemblyFile)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            string s = assemblyFile.ToUpper();
            foreach (Assembly assembly in assemblies)
            {
                if (GetAssemblyPath(assembly).ToUpper() == s)
                    return assembly;
            }
            return Assembly.LoadFile(assemblyFile);
        }

        private static string GetAssemblyPath(Assembly assembly)
        {
            UriBuilder uri = new UriBuilder(assembly.CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetFullPath(path);

        }
    }
}
