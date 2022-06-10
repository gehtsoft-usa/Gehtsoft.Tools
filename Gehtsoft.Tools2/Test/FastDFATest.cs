using System;
using Xunit;
using FluentAssertions;
using Gehtsoft.Tools2.Algorithm.DFA;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Gehtsoft.Tools2.UnitTest
{
    public class FastDFATest
    {
        private readonly ITestOutputHelper mTestOutputHelper;

        public FastDFATest(ITestOutputHelper testOutputHelper)
        {
            mTestOutputHelper = testOutputHelper;
        }


        public static class State
        {
            public const int T1 = 1;
            public const int T2 = 2;
            public const int T3 = 3;
            public const int T4 = 4;

            public const int MAX = 5;
        }

        public static class Signal
        {
            public const int S1 = 1;
            public const int S2 = 2;
            public const int S3 = 3;
            public const int S4 = 4;

            public const int MAX = 5;
        }

        [Fact]
        public void InitialState()
        {
            var t = new FastDFA(State.MAX, Signal.MAX);
            t.CurrentState.Should().Be(0);

            t.InitialState = State.T2;
            t.CurrentState.Should().Be(0);

            t.Reset();
            t.CurrentState.Should().Be(State.T2);
        }

        [Fact]
        public void Signal_Error_AlreadyExists()
        {
            var t = new FastDFA(State.MAX, Signal.MAX)
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(State.T2, Signal.S1, State.T4);
            ((Action)(() => t.AddTransition(State.T2, Signal.S1, State.T4))).Should().Throw<DFAException>();
        }

        [Fact]
        public void Signal_NoAction()
        {
            var t = new FastDFA(State.MAX, Signal.MAX)
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(State.T2, Signal.S1, State.T4);
            t.Signal(Signal.S1);
            t.CurrentState.Should().Be(State.T4);
        }

       
        [Fact]
        public void Signal_Action()
        {
            var t = new FastDFA(State.MAX, Signal.MAX)
            {
                InitialState = State.T2
            };
            t.Reset();

            bool invoked = false;
            Action<int, int, int> action = (from, s, to) =>
            {
                from.Should().Be(State.T2);
                s.Should().Be(Signal.S2);
                to.Should().Be(State.T4);
                invoked = true;
            };

            t.AddTransition(State.T2, Signal.S2, State.T4, action);

            invoked.Should().Be(false);
            t.Signal(Signal.S2);
            t.CurrentState.Should().Be(State.T4);
            invoked.Should().Be(true);
        }

        [Fact]
        public void Signal_Error_NoStateTable()
        {
            var t = new FastDFA(State.MAX, Signal.MAX)
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(State.T3, Signal.S1, State.T4);
            ((Action)(() => t.Signal(Signal.S2))).Should().Throw<DFAException>();
        }

        [Fact]
        public void Signal_Error_NoSignal()
        {
            var t = new FastDFA(State.MAX, Signal.MAX)
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(State.T2, Signal.S1, State.T4);
            ((Action)(() => t.Signal(Signal.S2))).Should().Throw<DFAException>();
        }

        [Fact]
        public void IntegrationTest_StringParser()
        {
            var parser = new FastDFATestStringParser();
            parser.Parse("lexem1 \'sqstring\'\"dqstring\\\"\"lexem2");
            parser.Elements[0].Item1.Should().Be(FastDFATestStringParser.ElementType.Lexem);
            parser.Elements[0].Item2.Should().Be("lexem1");
            parser.Elements[1].Item1.Should().Be(FastDFATestStringParser.ElementType.String);
            parser.Elements[1].Item2.Should().Be("sqstring");
            parser.Elements[2].Item1.Should().Be(FastDFATestStringParser.ElementType.String);
            parser.Elements[2].Item2.Should().Be("dqstring\"");
            parser.Elements[3].Item1.Should().Be(FastDFATestStringParser.ElementType.Lexem);
            parser.Elements[3].Item2.Should().Be("lexem2");
        }

        [Fact]
        public void Performance()
        {
            Stopwatch sw = new Stopwatch();
            TimeSpan dfaTime, fastDfaTime;

            var dfa = new DFA<int, int>();
            for (int i = 0; i < 32; i++)
                for (int j = 0; j < 32; j++)
                    dfa.AddTransition(i, j, j);

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 50000; i++)
                dfa.Signal(i % 32);
            sw.Stop();
            dfaTime = sw.Elapsed;

            var fdfa = new FastDFA(32, 32);
            for (int i = 0; i < 32; i++)
                for (int j = 0; j < 32; j++)
                    fdfa.AddTransition(i, j, j);

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 50000; i++)
                fdfa.Signal(i % 32);
            sw.Stop();
            fastDfaTime = sw.Elapsed;

            mTestOutputHelper.WriteLine($"DFA Perofrmance {dfaTime} vs {fastDfaTime}"); 
            fastDfaTime.Should().BeLessThan(dfaTime);
        }
    }
}
