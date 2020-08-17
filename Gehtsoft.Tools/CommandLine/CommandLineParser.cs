using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Gehtsoft.Tools.CommandLine
{
    public class CommandLineParser
    {
        public enum ParameterType
        {
            String,
            Integer,
            Number,
            Date,
            Boolean,
            Vararg,
        }

        
        public delegate void OnKeyDelegate(KeyDescription key, object[] parameters);

        public enum CommandLineError
        {
            CantParseValue,                     //key, value
            WrongNumberOfValues,                //key?
            UnknownKey,                         //key
            KeyNotSet,                          //key
        }

        public delegate void OnCommandLineErrorDelegate(CommandLineError error, KeyDescription key, string value);

        public event OnCommandLineErrorDelegate OnCommandLineError;
        public event OnKeyDelegate OnKey;


        internal void InvokeOnCommandLineError(CommandLineError error, KeyDescription key, string value) => OnCommandLineError?.Invoke(error, key, value);
        internal void InvokeOnKey(KeyDescription description, object[] parameters) => OnKey?.Invoke(description, parameters);

        public class KeyDescription
        {
            public string KeyName { get; set; }
            public bool Optional { get; set; }
            public ParameterType[] Parameters { get; set; }
            public delegate void OnKeyDelegate(string key, object[] parameters);
            public event OnKeyDelegate OnKey;

            private readonly int mMinParams;
            private readonly bool mVarArg;

            public ParameterType? GetParameterType(int index)
            {
                if (index >= mMinParams)
                {
                    if (mVarArg)
                        return ParameterType.String;
                    else
                        return null;
                }
                else
                    return Parameters[index];
            }

            public bool IsParameterCountCorrect(int count)
            {
                if (mVarArg)
                    return count >= mMinParams;
                else
                    return count == mMinParams;
            }

            public KeyDescription(string keyName) : this(keyName, true, null)
            {

            }

            public KeyDescription(string keyName, bool optional) : this(keyName, optional, null)
            {

            }

            public KeyDescription(string keyName, ParameterType[] parameters) : this(keyName, true, parameters)
            {

            }

            public KeyDescription(string keyName, bool optional, ParameterType[] parameters)
            {
                KeyName = keyName;
                Optional = optional;
                Parameters = parameters;

                int lenght = Parameters?.Length ?? 0;
                bool varylenght = false;
                for (int i = 0; i < lenght; i++)
                {
                    if (parameters[i] == ParameterType.Vararg)
                    {
                        lenght = i;
                        varylenght = true;
                        break;
                    }
                }

                mMinParams = lenght;
                mVarArg = varylenght;
            }

            internal void InvokeOnKey(object[] parameters) => OnKey?.Invoke(KeyName, parameters);
        }

        internal List<KeyDescription> Keys { get; } = new List<KeyDescription>();

        public virtual KeyDescription AddKey(KeyDescription description)
        {
            Keys.Add(description);
            return description;
        }

        public KeyDescription AddKey(ParameterType[] parameters) => AddKey(new KeyDescription(null, parameters));

        public KeyDescription AddKey(string name, params ParameterType[] parameters) => AddKey(new KeyDescription(name, parameters));

        public KeyDescription AddKey(string name, bool optional, params ParameterType[] parameters) => AddKey(new KeyDescription(name, optional, parameters));

        public void Parse(string[] args)
        {
            CommandLineParserCore core = new CommandLineParserCore();
            core.Process(this, args);
        }

        public void Parse(string line)
        {
            SingleLineParser parser = new SingleLineParser();
            Parse(parser.Parse(line));
        }
    }
}
