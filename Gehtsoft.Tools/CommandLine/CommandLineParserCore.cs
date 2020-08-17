using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.Structures;

namespace Gehtsoft.Tools.CommandLine
{
    public class CommandLineParserCore
    {
        protected class KeyInfo
        {
            private CommandLineParser.KeyDescription mDescription;

            public bool HasParameters => mDescription.Parameters?.Length > 0;

            public CommandLineParser.KeyDescription Description
            {
                get => mDescription;
                set
                {
                    mDescription = value;
                    mValues = HasParameters ? new List<object>() : null;
                }
            }
            
            public int Occurrence { get; private set; } = 0;
                       
            private List<object> mValues;
            
            public List<object> Values => mValues;

            public KeyInfo()
            {

            }
            
            public KeyInfo(CommandLineParser.KeyDescription description)
            {
                Description = description;
            }

            public void Reset()
            {
                Occurrence++;
                mValues?.Clear();
            }
        }

        private KeyInfo[] mKeys;
        private KeyInfo mCurrentKey;

        private KeyInfo ClassifyString(string value)
        {
            for (int i = 0; i < mKeys.Length; i++)
            {
                if (value == mKeys[i].Description.KeyName)
                    return mKeys[i];
            }
            return null;
        }

        private CommandLineParser mParser;

        public void Process(CommandLineParser parser, string[] args)
        {
            mParser = parser;

            mKeys = new KeyInfo[parser.Keys.Count];
            for (int i = 0; i < parser.Keys.Count; i++)
                mKeys[i] = new KeyInfo(parser.Keys[i]);

            mCurrentKey = ClassifyString(null);
            mCurrentKey?.Reset();
            
            foreach (string s in args)
            {
                KeyInfo classificator = ClassifyString(s);
                if (classificator != null)
                {
                    if (mCurrentKey != null)
                        if (!CloseCurrentKey())
                            return ;

                    mCurrentKey = classificator;
                    mCurrentKey.Reset();
                }
                else
                {
                    int value = mCurrentKey.Values?.Count ?? 0;
                    object v = null;
                    try
                    {
                        switch (mCurrentKey.Description.GetParameterType(value))
                        {
                            case null:
                                parser.InvokeOnCommandLineError(CommandLineParser.CommandLineError.UnknownKey, null, s);
                                return;
                            case CommandLineParser.ParameterType.String:
                                v = s;
                                break;
                            case CommandLineParser.ParameterType.Integer:
                                v = Convert.ChangeType(s, TypeCode.Int32);
                                break;
                            case CommandLineParser.ParameterType.Number:
                                v = Convert.ChangeType(s, TypeCode.Double);
                                break;
                            case CommandLineParser.ParameterType.Date:
                                v = Convert.ChangeType(s, TypeCode.DateTime);
                                break;
                            case CommandLineParser.ParameterType.Boolean:
                                v = Convert.ChangeType(s, TypeCode.Boolean);
                                break;

                        }
                    }
                    catch (Exception )
                    {
                        parser.InvokeOnCommandLineError(CommandLineParser.CommandLineError.CantParseValue, mCurrentKey.Description, s);
                        return;
                    }

                    mCurrentKey.Values?.Add(v);
                }
            }

            if (mCurrentKey != null)
                if (!CloseCurrentKey())
                    return ;

            foreach (KeyInfo i  in mKeys)
            {
                if (i.Occurrence == 0 && !i.Description.Optional)
                    parser.InvokeOnCommandLineError(CommandLineParser.CommandLineError.KeyNotSet, i.Description, null);
            }
        }

        private bool CloseCurrentKey()
        {
            if (!mCurrentKey.Description.IsParameterCountCorrect(mCurrentKey.Values?.Count ?? 0))
            {
                mParser.InvokeOnCommandLineError(CommandLineParser.CommandLineError.WrongNumberOfValues, mCurrentKey.Description, null);
                return false;
            }
            object[] values = mCurrentKey.Values?.ToArray();
            mCurrentKey.Description.InvokeOnKey(values);
            mParser.InvokeOnKey(mCurrentKey.Description, values);
            return true;

        }
    }
}
