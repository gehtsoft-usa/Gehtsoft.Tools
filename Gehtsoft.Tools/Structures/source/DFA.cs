using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Structures
{
    public class DFA<TSignal, TState>
    {
        public TState InitialState { get; set; }

        public class Transition
        {
            public TState From { get; set; }
            public TSignal Signal { get; set; }
            public TState To { get; set; }
            public Action Action { get; set; }
        }

        private Dictionary<TState, Dictionary<TSignal, Transition>> mTransitions = new Dictionary<TState, Dictionary<TSignal, Transition>>();

        public void AddTransition(Transition transition)
        {
            if (!mTransitions.TryGetValue(transition.From, out Dictionary<TSignal, Transition> state))
            {
                state = new Dictionary<TSignal, Transition>();
                mTransitions[transition.From] = state;
            }

            if (state.TryGetValue(transition.Signal, out Transition temporary))
            {
                throw new ArgumentException("Transition already exists");
            }

            state[transition.Signal] = transition;
        }

        public TState CurrentState { get; set; }

        public void Reset()
        {
            CurrentState = InitialState;
        }

        public void Signal(TSignal signal)
        {
            if (!mTransitions.TryGetValue(CurrentState, out Dictionary<TSignal, Transition> state))
                throw new InvalidOperationException("The current state has no transitions set");

            if (!state.TryGetValue(signal, out Transition transition))
                throw new InvalidOperationException("The current state has no transition for the signal specified");

            transition.Action?.Invoke();

            CurrentState = transition.To;
        }
    }
}
