using System;
using System.Collections.Generic;
using System.Text;
using Gehtsoft.Tools2.Algorithm.DFA;

namespace Gehtsoft.Tools2.UnitTest
{
    public class DFATestStringParser
    {
        public enum State
        {
            None,
            Lexem,
            SqString,
            DqString,
            SqEscape,
            DqEscape,
            Error,
        }

        public enum Signal
        {
            Character,
            Space,
            SingleQuote,
            DoubleQuote,
            Escape,
            EOL,
        }

        public enum ElementType
        {
            Lexem,
            String,
            Space
        }

        private readonly List<Tuple<ElementType, string>> mElements = new List<Tuple<ElementType, string>>();

        public List<Tuple<ElementType, string>> Elements => mElements;

        private readonly DFA<State, Signal> mDFA;

        public DFATestStringParser()
        {
            mDFA = new DFA<State, Signal>();
            SetupDfa(mDFA);
        }

        private char mCurrentChar;
        private readonly StringBuilder mCurrentElement = new StringBuilder();
        private ElementType mCurrentElementType = ElementType.Space;

        private void StartLexem(State from, Signal signal, State to)
        {
            mCurrentElement.Clear();
            mCurrentElement.Append(mCurrentChar);
            mCurrentElementType = ElementType.Lexem;
        }

        private void StartString(State from, Signal signal, State to)
        {
            if (mCurrentElementType != ElementType.Space)
                EndLexem(from, signal, to);
            mCurrentElement.Clear();
            mCurrentElementType = ElementType.String;
        }

        private void AddCharacter(State from, Signal signal, State to)
        {
            mCurrentElement.Append(mCurrentChar);
        }

        private void AddEscapeCharacter(State from, Signal signal, State to)
        {
            if (mCurrentChar == 't')
                mCurrentElement.Append('\t');
            else if (mCurrentChar == 'r')
                mCurrentElement.Append('\r');
            else if (mCurrentChar == 'n')
                mCurrentElement.Append('\n');
        }

        private void EndLexem(State from, Signal signal, State to)
        {
            mElements.Add(new Tuple<ElementType, string>(mCurrentElementType, mCurrentElement.ToString()));
            mCurrentElement.Clear();
            mCurrentElementType = ElementType.Space;
        }

        private void Error(State from, Signal signal, State to)
        {
            throw new InvalidOperationException($"Unexpected character {signal} in state {from}");
        }

        private void SetupDfa(IDFA<State, Signal> dfa)
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

        private static Signal CharacterClassifier(char c)
        {
            if (c == ' ' || c == '\t')
                return Signal.Space;

            if (c  == '\\')
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
