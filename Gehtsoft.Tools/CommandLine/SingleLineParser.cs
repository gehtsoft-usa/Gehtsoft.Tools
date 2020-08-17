using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.Structures;

namespace Gehtsoft.Tools.CommandLine
{
    public class SingleLineParser : FastDFA<SingleLineParser.State, SingleLineParser.SignalId>
    {
        public enum SignalId
        {
            Space = 0,
            Quote = 1,
            Character = 2,
        }

        public enum State
        {
            Nothing = 0,
            Value = 1,
            InQuoteValue = 2,
        }

        private object mMutex = new object();

        public object SyncRoot => mMutex;

        private int mStart;
        private int mCurrent;
        private string mValue;
        private List<string> mElements = new List<string>();

        public SingleLineParser()
        {       
            AddTransition(State.Nothing, State.Nothing, SignalId.Space);
            AddTransition(State.Nothing, State.Value, SignalId.Character, StartSimpleValue);
            AddTransition(State.Nothing, State.InQuoteValue, SignalId.Quote, StartQuotedValue);

            AddTransition(State.Value,State.Nothing, SignalId.Space, EndSimpleValue);
            AddTransition(State.Value, State.Value, SignalId.Character);
            AddTransition(State.Value, State.InQuoteValue, SignalId.Quote, SwitchToQuotedValue);

            AddTransition(State.InQuoteValue, State.InQuoteValue, SignalId.Space);
            AddTransition(State.InQuoteValue, State.InQuoteValue, SignalId.Character);
            AddTransition(State.InQuoteValue, State.Nothing, SignalId.Quote, EndQuotedValue);
        }

        private SignalId ToSignal(char c)
        {
            if (char.IsWhiteSpace(c))
                return SignalId.Space;
            else if (c == '"')
                return SignalId.Quote;
            else
                return SignalId.Character;
        }

        private void StartSimpleValue()
        {
            mStart = mCurrent;
        }

        private void EndSimpleValue()
        {
            int lenght = mCurrent - mStart;
            if (lenght > 0)
                mElements.Add(mValue.Substring(mStart, lenght));
            else
                mElements.Add("");
        }

        private void StartQuotedValue()
        {
            mStart = mCurrent;
        }

        private void EndQuotedValue()
        {
            int lenght = mCurrent - mStart - 1;
            if (lenght > 0)
                mElements.Add(mValue.Substring(mStart + 1, lenght));
            else
                mElements.Add("");
        }

        private void SwitchToQuotedValue()
        {
            EndSimpleValue();
            StartQuotedValue();
        }


        public string[] Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            
            lock (mMutex)
            {
                mValue = value;
                mCurrent = 0;
                if (mElements.Count > 0)
                    mElements.Clear();

                CurrentState = State.Nothing;

                foreach (char c in value)
                {
                    Signal(ToSignal(c));
                    mCurrent++;
                }

                if (CurrentState == State.Value)
                    Signal(SignalId.Space);

                if (CurrentState == State.InQuoteValue)
                    throw new ArgumentException("Quote is not closed", nameof(value));

                return mElements.ToArray();
            }
        }

    }
}
