using System;
using System.Collections.Generic;

namespace Gehtsoft.ResourceManager
{
    public interface IResourcePoolResolver
    {
        TextMessagePool GetPoolInstance { get; }
    }

    public class SingletonPoolResolver : IResourcePoolResolver
    {
        private static TextMessagePool gPool = new TextMessagePool();

        public TextMessagePool GetPoolInstance => gPool;
    }

    public class TheadPoolResolver : IResourcePoolResolver
    {
        [ThreadStatic] private static TextMessagePool gPool;

        public TextMessagePool GetPoolInstance => gPool ?? (gPool = new TextMessagePool());
    }

    public static class ResourceManager
    {
        public static IResourcePoolResolver ResourcePoolResolver { get; set; } = new SingletonPoolResolver();

        private static List<string> mComponents = new List<string>();

        public static TextMessageBlockPool Resources { get; } = new TextMessageBlockPool();

        public static TextMessagePool Messages => ResourcePoolResolver.GetPoolInstance;

        public static void AddResources(IEnumerable<TextMessageBlock> blocks)
        {
            foreach (TextMessageBlock block in blocks)
            {
                if (!mComponents.Contains(block.Component))
                    mComponents.Add(block.Component);
                Resources.Add(block);
            }
        }

        public static bool HasPool => ResourcePoolResolver.GetPoolInstance != null && ResourcePoolResolver.GetPoolInstance.Count > 0;

        public static void Initialize()
        {
            Initialize("en");
        }

        public static void Initialize(string language)
        {
            bool enus = language == "en";
            
            foreach (string component in mComponents)
                ResourcePoolResolver.GetPoolInstance.Add(Resources[component, "en"]);

            if (!enus)
            {
                foreach (string component in mComponents)
                    ResourcePoolResolver.GetPoolInstance.Add(Resources[component, language]);
            }
        }
    }
}
