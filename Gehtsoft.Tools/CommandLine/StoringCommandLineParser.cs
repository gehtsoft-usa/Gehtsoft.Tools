using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gehtsoft.Tools.CommandLine
{
    public class StoringCommandLineParser : CommandLineParser
    {
        public class Argument
        {
            public KeyDescription Key { get; internal set; }
            public object[] Arguments { get; internal set; }

            public int ParametersCount => Arguments?.Length ?? 0;

            public T GetParameter<T>(int index) => (T) Convert.ChangeType(Arguments[index], typeof(T));

            public Argument(KeyDescription key, object[] arguments)
            {
                Key = key;
                Arguments = arguments;
            }
        }

        public class ArgumentCollection : IEnumerable<Argument>
        {
            private List<Argument> mArguments = new List<Argument>();

            public int Count => mArguments.Count;

            public Argument this[int index] => mArguments[index];

            public Argument this[string key, int occurrence = 1]
            {
                get
                {
                    if (occurrence < 1)
                        throw new ArgumentException("Value must be >= 1", nameof(occurrence));

                    for (int i = 0; i < mArguments.Count; i++)
                    {
                        if (mArguments[i].Key.KeyName == key)
                        {
                            occurrence--;
                            if (occurrence == 0)
                                return mArguments[i];
                        }
                    }

                    return null;
                }
            }

            public Argument this[KeyDescription key, int occurrence = 1]
            {
                get
                {
                    if (occurrence < 1)
                        throw new ArgumentException("Value must be >= 1", nameof(occurrence));

                    for (int i = 0; i < mArguments.Count; i++)
                    {
                        if (mArguments[i].Key == key)
                        {
                            occurrence--;
                            if (occurrence == 0)
                                return mArguments[i];
                        }
                    }

                    return null;
                }
            }


            protected internal void Add(Argument argument) => mArguments.Add(argument);

            public IEnumerator<Argument> GetEnumerator() => mArguments.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => mArguments.GetEnumerator();

            internal void Clear() => mArguments.Clear();
        }

        protected ArgumentCollection mArguments = new ArgumentCollection();

        public ArgumentCollection Arguments => mArguments;

        protected void OnKeyHandler(KeyDescription key, object[] parameters) => mArguments.Add(new Argument(key, parameters));

        public class Error
        {
            public CommandLineError ErrorCode { get; }
            public KeyDescription Key { get; }
            public string Value { get; }

            public Error(CommandLineError errorCode, KeyDescription key, string value)
            {
                ErrorCode = errorCode;
                Key = key;
                Value = value;
            }
        }

        public class ErrorCollection : IEnumerable<Error>
        {
            private List<Error> mErrors = new List<Error>();

            public int Count => mErrors.Count;

            public Error this[int index] => mErrors[index];

            protected internal void Add(Error error) => mErrors.Add(error);

            public IEnumerator<Error> GetEnumerator() => mErrors.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => mErrors.GetEnumerator();

            internal void Clear() => mErrors.Clear();
        }

        private ErrorCollection mErrors = new ErrorCollection();
        public ErrorCollection Errors => mErrors;

        protected void OnError(CommandLineError errorCode, KeyDescription key, string value) => mErrors.Add(new Error(errorCode, key, value));


        public StoringCommandLineParser()
        {
            base.OnCommandLineError += OnError;
            base.OnKey += OnKeyHandler;
        }

        public void Reset()
        {
            mArguments.Clear();
            mErrors.Clear();
        }
    }
}
