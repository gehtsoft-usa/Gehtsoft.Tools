using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Gehtsoft.Tools2.Algorithm.DFA;
using System.Diagnostics;

namespace Gehtsoft.Tools2.UnitTest
{
    public class DFATest
    {
        public enum State
        {
            T1,
            T2,
            T3,
            T4,
        }

        public enum Signal
        {
            S1,
            S2,
            S3,
            S4
        }

        [Fact]
        public void Transition_Constructor_NoAction()
        {
            var t = new DFA<State, Signal>.Transition(State.T1, Signal.S1, State.T2);
            t.From.Should().Be(State.T1);
            t.Signal.Should().Be(Signal.S1);
            t.To.Should().Be(State.T2);
            t.Action.Should().BeNull();
        }

        [Fact]
        public void Transition_Constructor_Action()
        {
            Action<State, Signal, State> a = (f, s, t) => { };
            var t = new DFA<State, Signal>.Transition(State.T1, Signal.S1, State.T2, a);
            t.From.Should().Be(State.T1);
            t.Signal.Should().Be(Signal.S1);
            t.To.Should().Be(State.T2);
            t.Action.Should().BeSameAs(a);
        }

        [Fact]
        public void InitialState()
        {
            var t = new DFA<State, Signal>();
            t.CurrentState.Should().Be(default(State));

            t.InitialState = State.T2;
            t.CurrentState.Should().Be(default(State));

            t.Reset();
            t.CurrentState.Should().Be(State.T2);
        }

        [Fact]
        public void Signal_Error_AlreadyExists()
        {
            var t = new DFA<State, Signal>
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(new DFA<State, Signal>.Transition(State.T2, Signal.S1, State.T4));
            ((Action)(() => t.AddTransition(new DFA<State, Signal>.Transition(State.T2, Signal.S1, State.T4)))).Should().Throw<DFAException>();
        }

        [Fact]
        public void Signal_NoAction()
        {
            var t = new DFA<State, Signal>
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(new DFA<State, Signal>.Transition(State.T2, Signal.S1, State.T4));
            t.Signal(Signal.S1);
            t.CurrentState.Should().Be(State.T4);
        }

        [Fact]
        public void Signal_ViaParameterizedMethod()
        {
            var t = new DFA<State, Signal>
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
            var t = new DFA<State, Signal>
            {
                InitialState = State.T2
            };
            t.Reset();

            bool invoked = false;
            Action<State, Signal, State> action = (from, s, to) =>
            {
                from.Should().Be(State.T2);
                s.Should().Be(Signal.S2);
                to.Should().Be(State.T4);
                invoked = true;
            };

            t.AddTransition(new DFA<State, Signal>.Transition(State.T2, Signal.S2, State.T4, action));

            invoked.Should().Be(false);
            t.Signal(Signal.S2);
            t.CurrentState.Should().Be(State.T4);
            invoked.Should().Be(true);
        }

        [Fact]
        public void Signal_Error_NoStateTable()
        {
            var t = new DFA<State, Signal>
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(new DFA<State, Signal>.Transition(State.T3, Signal.S1, State.T4));
            ((Action)(() => t.Signal(Signal.S2))).Should().Throw<DFAException>();
        }

        [Fact]
        public void Signal_Error_NoSignal()
        {
            var t = new DFA<State, Signal>
            {
                InitialState = State.T2
            };
            t.Reset();

            t.AddTransition(new DFA<State, Signal>.Transition(State.T2, Signal.S1, State.T4));
            ((Action)(() => t.Signal(Signal.S2))).Should().Throw<DFAException>();
        }

        [Fact]
        public void IntegrationTest_StringParser()
        {
            var parser = new DFATestStringParser();
            parser.Parse("lexem1 \'sqstring\'\"dqstring\\\"\"lexem2");
            parser.Elements[0].Item1.Should().Be(DFATestStringParser.ElementType.Lexem);
            parser.Elements[0].Item2.Should().Be("lexem1");
            parser.Elements[1].Item1.Should().Be(DFATestStringParser.ElementType.String);
            parser.Elements[1].Item2.Should().Be("sqstring");
            parser.Elements[2].Item1.Should().Be(DFATestStringParser.ElementType.String);
            parser.Elements[2].Item2.Should().Be("dqstring\"");
            parser.Elements[3].Item1.Should().Be(DFATestStringParser.ElementType.Lexem);
            parser.Elements[3].Item2.Should().Be("lexem2");
        }
    }
}
