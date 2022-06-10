using System;
using System.Collections.Generic;
using System.Text;
using Gehtsoft.Tools2.Algorithm.DFA;

namespace Gehtsoft.Tools2.UnitTest
{
    public class FastDFATestStringParser
    {
        public static class State
        {
            public const int None = 0;
            public const int Lexem = 1;
            public const int SqString = 2;
            public const int DqString = 3;
            public const int SqEscape = 4;
            public const int DqEscape = 5;
            public const int Error = 6;
            public const int MAX = 7;
        }

        public static class Signal
        {
            public const int Character = 0;
            public const int Space = 1;
            public const int SingleQuote = 2;
            public const int DoubleQuote = 3;
            public const int Escape = 4;
            public const int EOL = 5;

            public const int MAX = 7;
        }

        public enum ElementType
        {
            Lexem,
            String,
            Space
        }

        private readonly List<Tuple<ElementType, string>> mElements = new List<Tuple<ElementType, string>>();

        public List<Tuple<ElementType, string>> Elements => mElements;

        private readonly FastDFA mDFA;

        public FastDFATestStringParser()
        {
            mDFA = new FastDFA(State.MAX, Signal.MAX);
            SetupDfa(mDFA);
        }

        private char mCurrentChar;
        private readonly StringBuilder mCurrentElement = new StringBuilder();
        private ElementType mCurrentElementType = ElementType.Space;

        private void StartLexem(int from, int signal, int to)
        {
            mCurrentElement.Clear();
            mCurrentElement.Append(mCurrentChar);
            mCurrentElementType = ElementType.Lexem;
        }

        private void StartString(int from, int signal, int to)
        {
            if (mCurrentElementType != ElementType.Space)
                EndLexem(from, signal, to);
            mCurrentElement.Clear();
            mCurrentElementType = ElementType.String;
        }

        private void AddCharacter(int from, int signal, int to)
        {
            mCurrentElement.Append(mCurrentChar);
        }

        private void AddEscapeCharacter(int from, int signal, int to)
        {
            if (mCurrentChar == 't')
                mCurrentElement.Append('\t');
            else if (mCurrentChar == 'r')
                mCurrentElement.Append('\r');
            else if (mCurrentChar == 'n')
                mCurrentElement.Append('\n');
        }

        private void EndLexem(int from, int signal, int to)
        {
            mElements.Add(new Tuple<ElementType, string>(mCurrentElementType, mCurrentElement.ToString()));
            mCurrentElement.Clear();
            mCurrentElementType = ElementType.Space;
        }

        private void Error(int from, int signal, int to)
        {
            throw new InvalidOperationException($"Unexpected character {signal} in state {from}");
        }

        private void SetupDfa(FastDFA dfa)
        {
            dfa.InitialState = State.None;
            dfa.AddTransition(State.None, Signal.Space, State.None);
            dfa.AddTransition(State.None, Signal.EOL, State.None);
            dfa.AddTransition(State.None, Signal.Character, State.Lexem, StartLexem);
            dfa.AddTransition(State.None, Signal.SingleQuote, State.SqString, StartString);
            dfa.AddTransition(State.None, Signal.DoubleQuote, State.DqString, StartString);

            dfa.AddTransition(State.Lexem, Signal.Space, State.None, EndLexem);
            dfa.AddTransition(State.Lexem, Signal.EOL, State.None, EndLexem);
            dfa.AddTransition(State.Lexem, Signal.Character, State.Lexem, AddCharacter);
            dfa.AddTransition(State.Lexem, Signal.Escape, State.Error, Error);
            dfa.AddTransition(State.Lexem, Signal.SingleQuote, State.DqString, StartString);
            dfa.AddTransition(State.Lexem, Signal.DoubleQuote, State.DqString, StartString);

            dfa.AddTransition(State.SqString, Signal.Escape, State.SqEscape);
            dfa.AddTransition(State.SqString, Signal.Character, State.SqString, AddCharacter);
            dfa.AddTransition(State.SqString, Signal.Space, State.SqString, AddCharacter);
            dfa.AddTransition(State.SqString, Signal.DoubleQuote, State.SqString, AddCharacter);
            dfa.AddTransition(State.SqString, Signal.SingleQuote, State.None, EndLexem);
            dfa.AddTransition(State.SqString, Signal.EOL, State.Error, Error);

            dfa.AddTransition(State.DqString, Signal.Escape, State.DqEscape);
            dfa.AddTransition(State.DqString, Signal.Character, State.DqString, AddCharacter);
            dfa.AddTransition(State.DqString, Signal.Space, State.DqString, AddCharacter);
            dfa.AddTransition(State.DqString, Signal.SingleQuote, State.DqString, AddCharacter);
            dfa.AddTransition(State.DqString, Signal.DoubleQuote, State.None, EndLexem);
            dfa.AddTransition(State.DqString, Signal.EOL, State.Error, Error);

            dfa.AddTransition(State.SqEscape, Signal.Escape, State.SqString, AddCharacter);
            dfa.AddTransition(State.SqEscape, Signal.Character, State.SqString, AddEscapeCharacter);
            dfa.AddTransition(State.SqEscape, Signal.SingleQuote, State.SqString, AddCharacter);
            dfa.AddTransition(State.SqEscape, Signal.DoubleQuote, State.SqString, AddCharacter);

            dfa.AddTransition(State.DqEscape, Signal.Escape, State.DqString, AddCharacter);
            dfa.AddTransition(State.DqEscape, Signal.Character, State.DqString, AddEscapeCharacter);
            dfa.AddTransition(State.DqEscape, Signal.SingleQuote, State.DqString, AddCharacter);
            dfa.AddTransition(State.DqEscape, Signal.DoubleQuote, State.DqString, AddCharacter);
        }

        private static int CharacterClassifier(char c)
        {
            if (c == ' ' || c == '\t')
                return Signal.Space;

            if (c == '\\')
                return Signal.Escape;

            if (c == '\'')
                return Signal.SingleQuote;

            if (c == '\"')
                return Signal.DoubleQuote;

            if (c == '\"')
                return Signal.DoubleQuote;

            if (char.IsLetter(c))
                return Signal.Character;

            if (char.IsDigit(c))
                return Signal.Character;

            throw new ArgumentException($"Unexpected character {c}");
        }

        public void Parse(string s)
        {
            mElements.Clear();
            mDFA.Reset();
            mCurrentElement.Clear();
            foreach (char c in s)
            {
                mCurrentChar = c;
                mDFA.Signal(CharacterClassifier(c));
            }
            mDFA.Signal(Signal.EOL);
        }
    }
}
