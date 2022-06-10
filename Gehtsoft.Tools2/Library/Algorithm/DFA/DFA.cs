using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools2.Algorithm.DFA
{

    /// <summary>
    /// Digital Final Automate class.
    /// </summary>
    /// <typeparam name="TSignal">The data type to define signals</typeparam>
    /// <typeparam name="TState">The data type to define states</typeparam>
    public class DFA<TState, TSignal> : IDFA<TState, TSignal>
    {
        /// <summary>
        /// The initial state of the DFA
        /// </summary>
        public TState InitialState { get; set; }

        /// <summary>
        /// The transition between states
        /// </summary>
        public class Transition
        {
            /// <summary>
            /// The original state 
            /// </summary>
            public TState From { get; set; }

            /// <summary>
            /// The signal received
            /// </summary>
            public TSignal Signal { get; set; }

            /// <summary>
            /// The destination state
            /// </summary>
            public TState To { get; set; }

            /// <summary>
            /// The optional action to be performed after transition
            /// </summary>
            public Action<TState, TSignal, TState> Action { get; set; }

            /// <summary>
            /// Default constructor
            /// </summary>
            public Transition() { }

            /// <summary>
            /// Constructor for an actionless state
            /// </summary>
            /// <param name="from"></param>
            /// <param name="signal"></param>
            /// <param name="to"></param>
            public Transition(TState from, TSignal signal, TState to)
            {
                From = from;
                Signal = signal;
                To = to;
                Action = null;
            }

            /// <summary>
            /// Construstor for a state with an action
            /// </summary>
            /// <param name="from"></param>
            /// <param name="signal"></param>
            /// <param name="to"></param>
            /// <param name="action"></param>
            public Transition(TState from, TSignal signal, TState to, Action<TState, TSignal, TState> action)
            {
                From = from;
                Signal = signal;
                To = to;
                Action = action;
            }
        }

        private readonly IEqualityComparer<TSignal> mSignalComparer;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DFA() : this(EqualityComparer<TState>.Default, EqualityComparer<TSignal>.Default)
        {
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="stateComparer"></param>
        /// <param name="signalComparer"></param>
        public DFA(IEqualityComparer<TState> stateComparer, IEqualityComparer<TSignal> signalComparer)
        {
            mTransitions = new Dictionary<TState, Dictionary<TSignal, Transition>>(stateComparer);
            mSignalComparer = signalComparer;
        }

        private readonly Dictionary<TState, Dictionary<TSignal, Transition>> mTransitions;

        /// <summary>
        /// Adds a transition
        /// </summary>
        /// <param name="transition"></param>
        /// <exception cref="ArgumentException"></exception>
        public void AddTransition(Transition transition)
        {
            if (!mTransitions.TryGetValue(transition.From, out Dictionary<TSignal, Transition> state))
            {
                state = new Dictionary<TSignal, Transition>(mSignalComparer);
                mTransitions[transition.From] = state;
            }

            if (state.ContainsKey(transition.Signal))
                throw new DFAException("Transition already exists");

            state[transition.Signal] = transition;
        }

        /// <summary>
        /// Creates and adds a transition 
        /// </summary>
        public void AddTransition(TState from, TSignal signal, TState to, Action<TState, TSignal, TState> action = null)
            => AddTransition(new Transition(from, signal, to, action));

        /// <summary>
        /// The current state of the DFA
        /// </summary>
        public TState CurrentState { get; set; }

        /// <summary>
        /// Resets DFA to its initial state
        /// </summary>
        public void Reset()
        {
            CurrentState = InitialState;
        }

        /// <summary>
        /// Send a signal to DFA
        /// </summary>
        /// <param name="signal"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Signal(TSignal signal)
        {
            if (!mTransitions.TryGetValue(CurrentState, out Dictionary<TSignal, Transition> state))
                throw new DFAException($"The current state {CurrentState} has no transitions set");

            if (!state.TryGetValue(signal, out Transition transition))
                throw new DFAException($"The current state {CurrentState} has no transition for the signal {signal} specified");

            transition.Action?.Invoke(transition.From, signal, transition.To);

            CurrentState = transition.To;
        }
    }
}
