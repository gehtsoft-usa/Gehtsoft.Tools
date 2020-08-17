using System;
using System.Reflection;

namespace Gehtsoft.Tools.Structures
{
    public class FastDFA
    {
        protected int mMaxSignal, mMaxState;

        public int InitialState { get; set; }

        public class Transition
        {
            public int To { get; set; }
            public Action Action { get; set; }
        }

        public class TransitionTable
        {
            public Transition[] Transitions { get; set; }
        }

        private TransitionTable[] mTransitions;

        protected FastDFA()
        {

        }

        public FastDFA(int maxState, int maxSignal)
        {
            Initialize(maxState, maxSignal);
        }

        protected void Initialize(int maxState, int maxSignal)
        {
            mMaxState = maxState;
            mMaxSignal = maxSignal;
            mTransitions = new TransitionTable[maxState];

        }

        public void AddTransition(int from, int to, int signal, Action action = null)
        {
            if (from < 0 || from >= mMaxState)
                throw new ArgumentOutOfRangeException(nameof(from));
            if (to < 0 || to >= mMaxState)
                throw new ArgumentOutOfRangeException(nameof(to));
            if (signal < 0 || signal >= mMaxSignal)
                throw new ArgumentOutOfRangeException(nameof(to));

            TransitionTable table = mTransitions[from];
            
            if (table == null)
                table = mTransitions[from] = new TransitionTable() { Transitions = new Transition[mMaxSignal]};

            Transition transition = table.Transitions[signal];

            if (transition == null)
                transition = table.Transitions[signal] = new Transition();

            transition.To = to;
            transition.Action = action;
        }

        public int CurrentState { get; set; }

        public void Reset()
        {
            CurrentState = InitialState;
        }

        public void Signal(int signal)
        {
            if (signal < 0 || signal >= mMaxSignal)
                throw new ArgumentOutOfRangeException(nameof(signal));

            TransitionTable table = mTransitions[CurrentState];

            if (table == null)
                throw new InvalidOperationException("The current state has no transitions set");

            Transition transition = table.Transitions[signal];

            if (transition == null)
                throw new InvalidOperationException("The current state has no transition for the signal specified");

            transition.Action?.Invoke();
            CurrentState = transition.To;
        }
    }

    public class FastDFA<TState, TSignal> : FastDFA
    {
        public FastDFA()
        {
            Initialize(GetMaxValue<TState>() + 1, GetMaxValue<TSignal>() + 1);
        }

        private int GetMaxValue<T>()
        {
            int maxValue = 0;
            Type type = typeof(T);
            TypeInfo typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsEnum)
                throw new ArgumentException("Type is not an enumeration", nameof(T));

            FieldInfo[] fields = typeInfo.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                object ov = field.GetRawConstantValue();
                int v = (int) Convert.ChangeType(ov, typeof(int));
                if (v < 0)
                    throw new Exception("Integer value of the enumeration must not be negative");
                if (v > 8192)
                    throw new Exception("Integer value of the enumeration must not be larger than 8192");

                if (v > maxValue)
                    maxValue = v;
            }

            return maxValue;
        }

        public void AddTransition(TState from, TState to, TSignal signal, Action action = null) 
            => base.AddTransition((int)Convert.ChangeType(from, typeof(int)), (int)Convert.ChangeType(to, typeof(int)), (int)Convert.ChangeType(signal, typeof(int)), action);
                
        public new TState CurrentState { 
            get => (TState)Enum.ToObject(typeof(TState), base.CurrentState); 
            set => base.CurrentState = (int)Convert.ChangeType(value, typeof(int)); 
        }

        public new TState InitialState { 
            get => (TState)Enum.ToObject(typeof(TState), base.InitialState); 
            set => base.InitialState = (int)Convert.ChangeType(value, typeof(int)); 
        }

        public void Signal(TSignal signal) => base.Signal((int) Convert.ChangeType(signal, typeof(int)));
    }
}