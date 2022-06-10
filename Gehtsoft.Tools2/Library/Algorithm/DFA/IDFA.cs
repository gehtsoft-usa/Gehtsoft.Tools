using System;

namespace Gehtsoft.Tools2.Algorithm.DFA
{
    /// <summary>
    /// Digital Final Automate interface.
    /// </summary>
    /// <typeparam name="TState">The data type to define states</typeparam>
    /// <typeparam name="TSignal">The data type to define signals</typeparam>

    public interface IDFA<TState, TSignal>
    {
        /// <summary>
        /// The initial state of the DFA
        /// </summary>
        TState InitialState { get; set; }

        /// <summary>
        /// Creates and adds a transition 
        /// </summary>
        void AddTransition(TState from, TSignal signal, TState to, Action<TState, TSignal, TState> action = null);

        /// <summary>
        /// The current state of the DFA
        /// </summary>
        TState CurrentState { get; set; }

        /// <summary>
        /// Resets DFA to its initial state
        /// </summary>
        void Reset();

        /// <summary>
        /// Send a signal to DFA
        /// </summary>
        /// <param name="signal"></param>
        /// <exception cref="InvalidOperationException"></exception>
        void Signal(TSignal signal);
    }
}
