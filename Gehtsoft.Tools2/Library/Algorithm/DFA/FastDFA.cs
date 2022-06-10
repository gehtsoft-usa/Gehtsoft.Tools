using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gehtsoft.Tools2.Algorithm.DFA
{
    /// <summary>
    /// An implementation of a fast DFA.
    /// 
    /// All signals and states are defined by integer numbers, starting with 0. 
    /// </summary>
    public class FastDFA : IDFA<int, int>
    {
        /// <summary>
        /// Maximum signal id
        /// </summary>
        protected readonly int mMaxSignal;

        /// <summary>
        /// Maximum state id
        /// </summary>

        protected readonly int mMaxState;

        /// <summary>
        /// Initial state id
        /// </summary>
        public int InitialState { get; set; }

        /// <summary>
        /// Transition
        /// </summary>
        public class Transition
        {
            /// <summary>
            /// The initial state id
            /// </summary>
            public int From { get; set; }

            /// <summary>
            /// The signal
            /// </summary>
            public int Signal { get; set; }

            /// <summary>
            /// The target state id
            /// </summary>
            public int To { get; set; }

            /// <summary>
            /// The action to call
            /// </summary>
            public Action<int, int, int> Action { get; set; }

            /// <summary>
            /// Default constructor
            /// </summary>
            public Transition() { }

            /// <summary>
            /// Constructor for an actionless transition
            /// </summary>
            /// <param name="from"></param>
            /// <param name="signal"></param>
            /// <param name="to"></param>
            public Transition(int from, int signal, int to)
            {
                From = from;
                Signal = signal;
                To = to;
                Action = null;
            }


            /// <summary>
            /// Constructor for a transition
            /// </summary>
            /// <param name="from"></param>
            /// <param name="signal"></param>
            /// <param name="to"></param>
            /// <param name="action"></param>
            public Transition(int from, int signal, int to, Action<int, int, int> action)
            {
                From = from;
                Signal = signal;
                To = to;
                Action = action;
            }
        }


        /// <summary>
        /// The table of all transitions for a state
        /// </summary>
        protected class TransitionTable
        {
            /// <summary>
            /// All transitions for a state
            /// </summary>
            public Transition[] Transitions { get; set; }
        }

        /// <summary>
        /// Transition tables for all states.
        /// </summary>
        private readonly TransitionTable[] mTransitions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxState">Maximum id for a state</param>
        /// <param name="maxSignal">Maximum id for a signal</param>
        public FastDFA(int maxState, int maxSignal)
        {
            mMaxState = maxState;
            mMaxSignal = maxSignal;
            mTransitions = new TransitionTable[maxState];
        }

        /// <summary>
        /// Adds a new transition
        /// </summary>
        /// <param name="from"></param>
        /// <param name="signal"></param>
        /// <param name="to"></param>
        /// <param name="action"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddTransition(int from, int signal, int to, Action<int, int, int> action = null)
        {
            if (from < 0 || from >= mMaxState)
                throw new ArgumentOutOfRangeException(nameof(from));
            if (to < 0 || to >= mMaxState)
                throw new ArgumentOutOfRangeException(nameof(to));
            if (signal < 0 || signal >= mMaxSignal)
                throw new ArgumentOutOfRangeException(nameof(to));

            TransitionTable table = mTransitions[from];

            if (table == null)
                table = mTransitions[from] = new TransitionTable() { Transitions = new Transition[mMaxSignal] };


            Transition transition = table.Transitions[signal];

            if (transition != null)
                throw new DFAException("Transition already exists");

            transition = table.Transitions[signal] = new Transition();

            transition.To = to;
            transition.Action = action;
        }

        /// <summary>
        /// The current state of DFA
        /// </summary>
        public int CurrentState { get; set; }

        /// <summary>
        /// Resets DFA to its initial state
        /// </summary>
        public void Reset()
        {
            CurrentState = InitialState;
        }

        /// <summary>
        /// Sends a signal to DFA
        /// </summary>
        /// <param name="signal"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal(int signal)
        {
            TransitionTable table = mTransitions[CurrentState];

            if (table == null)
                throw new DFAException("The current state has no transitions set");

            Transition transition = table.Transitions[signal];
            
            if (transition == null)
                throw new DFAException("The current state has no transition for the signal specified");

            transition.Action?.Invoke(CurrentState, signal, transition.To);
            
            CurrentState = transition.To;
        }
    }
}