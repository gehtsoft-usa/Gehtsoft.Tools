using System;
using System.Collections;
using System.Collections.Generic;

namespace Gehtsoft.ResourceManager
{
    public class TextMessage
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class TextMessageBlock : IEnumerable<TextMessage>
    {
        public string Component { get; set; }
        public string Language { get; set; }
        private List<TextMessage> mMessages = new List<TextMessage>();

        public int Count => mMessages.Count;
        public TextMessage this[int index] => mMessages[index];

        public void Add(TextMessage textMessage)
        {
            mMessages.Add(textMessage);
        }


        public IEnumerator<TextMessage> GetEnumerator()
        {
            return mMessages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) mMessages).GetEnumerator();
        }
    }


    public class TextMessagePool
    {
        private Dictionary<string, string> mMessages = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                string s;
                if (mMessages.TryGetValue(key, out s))
                    return s;
                throw new ArgumentException("Resources is not found:" + key);
            }
        }

        public string Format(string key, params object[] parameters)
        {
            return string.Format(this[key], parameters);
        }

        public void Add(TextMessageBlock block)
        {
            foreach (TextMessage message in block)
                mMessages[message.Name] = message.Value;
        }

        public void Clear()
        {
            mMessages.Clear();
        }

        public int Count => mMessages.Count;
    }

    public class TextMessageBlockPool
    {
        Dictionary<string, TextMessageBlock> mBlocks = new Dictionary<string, TextMessageBlock>();

        public TextMessageBlock this[string component]
        {
            get { return this[component, "en"]; }
        }

        public TextMessageBlock this[string component, string language]
        {
            get
            {
                TextMessageBlock block;
                if (mBlocks.TryGetValue($"{component}.{language}", out block))
                    return block;
                if (language != "en")
                    return this[component, "en"];
                return null;
            }
        }

        public void Add(TextMessageBlock block)
        {
            mBlocks[$"{block.Component}.{block.Language}"] = block;
        }
    }


}
