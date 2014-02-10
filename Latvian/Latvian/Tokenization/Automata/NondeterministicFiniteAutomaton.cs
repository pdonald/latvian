using System;
using System.Collections.Generic;
using System.Linq;

namespace Latvian.Tokenization.Automata
{
    using DFA = DeterministicFiniteAutomaton;
    using NFA = NondeterministicFiniteAutomaton;

    class NondeterministicFiniteAutomaton
    {
        public State Start { get; set; }
        public State Exit { get; set; }

        public class State
        {
            private readonly Dictionary<CharRange, HashSet<State>> transitions = new Dictionary<CharRange, HashSet<State>>();
            private readonly HashSet<State> emptyTransitions = new HashSet<State>();

            public bool IsFinal
            {
                get;
                set;
            }

            public int[] Values
            {
                get;
                set;
            }

            public IEnumerable<CharRange> Alphabet
            {
                get { return transitions.Keys; }
            }

            public IEnumerable<State> Transitions
            {
                get { return transitions.Values.SelectMany(t => t).Distinct(); }
            }

            public HashSet<State> EmptyTransitions
            {
                get { return emptyTransitions; }
            }

            public IEnumerable<State> this[CharRange input]
            {
                get
                {
                    foreach (CharRange range in transitions.Keys.OrderBy(r => r))
                        if (range.Contains(input))
                            return transitions[range];
                    return null;
                }
            }

            public void AddTransition(CharRange input, State to)
            {
                if (!transitions.ContainsKey(input))
                    transitions[input] = new HashSet<State>();

                transitions[input].Add(to);
            }

            public void AddEmptyTransition(State to)
            {
                emptyTransitions.Add(to);
            }

            public override string ToString()
            {
                return (IsFinal ? "!" : ".") + (Values != null ? " [" + string.Join(", ", Values) + "]" : "");
            }
        }

        public IEnumerable<State> States
        {
            get
            {
                HashSet<State> states = new HashSet<State>();

                Queue<State> queue = new Queue<State>();

                if (Start != null)
                    queue.Enqueue(Start);

                while (queue.Count > 0)
                {
                    State state = queue.Dequeue();

                    if (states.Add(state))
                    {
                        foreach (State toState in state.Transitions.Concat(state.EmptyTransitions))
                            queue.Enqueue(toState);
                    }
                }

                return states;
            }
        }

        #region Construction
        public static NFA Empty()
        {
            NFA nfa = new NFA();
            nfa.Start = new State();
            nfa.Exit = new State() { IsFinal = true };
            nfa.Start.AddEmptyTransition(nfa.Exit);
            return nfa;
        }

        public static NFA Char(CharRange c)
        {
            NFA nfa = new NFA();
            nfa.Start = new State();
            nfa.Exit = new State() { IsFinal = true };
            nfa.Start.AddTransition(c, nfa.Exit);
            return nfa;
        }

        public static NFA Concat(NFA a, NFA b)
        {
            NFA nfa = new NFA();
            nfa.Start = a.Start;
            nfa.Exit = b.Exit;
            a.Exit.AddEmptyTransition(b.Start);
            a.Exit.IsFinal = false;
            b.Exit.IsFinal = true;
            return nfa;
        }

        public static NFA Sequence(params NFA[] nfas)
        {
            NFA nfa = Empty();
            for (int i = 0; i < nfas.Length; i++)
                nfa = Concat(nfa, nfas[i]);
            return nfa;
        }

        public static NFA Repeat(NFA nfa, int n)
        {
            throw new NotImplementedException(); // todo: clone nfa & implement this

            /* NFA result = Empty();
            for (int i = 0; i < n; i++)
                result = Concat(result, nfa); // need to clone nfa because it's changed by Concat
            return result; */
        }

        public static NFA Or(NFA a, NFA b)
        {
            NFA nfa = new NFA();
            nfa.Start = new State();
            nfa.Exit = new State() { IsFinal = true };
            a.Exit.IsFinal = false;
            b.Exit.IsFinal = false;
            nfa.Start.AddEmptyTransition(a.Start);
            nfa.Start.AddEmptyTransition(b.Start);
            a.Exit.AddEmptyTransition(nfa.Exit);
            b.Exit.AddEmptyTransition(nfa.Exit);
            return nfa;
        }

        public static NFA Or(params NFA[] nfas)
        {
            NFA nfa = nfas[0];
            for (int i = 1; i < nfas.Length; i++)
                nfa = Or(nfa, nfas[i]);
            return nfa;
        }

        public static NFA Star(NFA nfa)
        {
            nfa.Start.AddEmptyTransition(nfa.Exit);
            nfa.Exit.AddEmptyTransition(nfa.Start);
            return nfa;
        }

        public static NFA Plus(NFA nfa)
        {
            nfa.Exit.AddEmptyTransition(nfa.Start);
            return nfa;
        }
        #endregion

        #region NFA to DFA
        // http://web.cecs.pdx.edu/~harry/compilers/slides/LexicalPart3.pdf
        public DFA ToDfa()
        {
            // each DFA states corresponds to several NFA states
            Dictionary<DFA.State, HashSet<NFA.State>> map = new Dictionary<DFA.State, HashSet<NFA.State>>();

            // create a start state for DFA
            DFA.State start = new DFA.State();
            DFA dfa = new DFA(start);
            map[start] = EmptyClosure(new[] { Start });

            Stack<DFA.State> unmarked = new Stack<DFA.State>();
            unmarked.Push(start);

            while (unmarked.Count > 0)
            {
                DFA.State state = unmarked.Pop();

                // todo: split unique ranges
                IEnumerable<CharRange> alphabet = CharRange.Split(map[state].SelectMany(s => s.Alphabet).Distinct().ToArray()).Distinct();

                foreach (CharRange c in alphabet)
                {
                    IEnumerable<State> reachableStates = map[state].Where(s => s[c] != null).SelectMany(s => s[c]).ToArray();
                    HashSet<State> move = EmptyClosure(reachableStates);

                    if (move.Count > 0)
                    {
                        DFA.State newState = map.Where(kp => kp.Value.SetEquals(move))
                                                .Select(kp => kp.Key)
                                                .FirstOrDefault();

                        if (newState == null)
                        {
                            newState = new DFA.State();
                            unmarked.Push(newState);
                            map[newState] = move;
                        }

                        state.AddTransition(c, newState);
                    }
                }
            }

            // if any NFA state is a final state, that DFA state is also a final state
            foreach (DFA.State state in map.Keys)
            {
                if (map[state].Any(s => s.IsFinal))
                {
                    state.IsFinal = true;
                }

                state.Values = map[state].Where(s => s.Values != null).SelectMany(s => s.Values).Distinct().OrderBy(v => v).ToArray();
                
                state.Minimize();
            }

            return dfa;
        }

        private HashSet<State> EmptyClosure(IEnumerable<State> states)
        {
            HashSet<State> result = new HashSet<State>(states);
            Stack<State> next = new Stack<State>(result);

            while (next.Count > 0)
            {
                State state = next.Pop();

                foreach (State edge in state.EmptyTransitions)
                {
                    if (result.Add(edge))
                    {
                        next.Push(edge);
                    }
                }
            }

            return result;
        }
        #endregion

        #region Graph
        #if DEBUG
        public Graphviz ToGraph()
        {
            Graphviz graph = new Graphviz();

            IEnumerable<State> states = States;
            Dictionary<State, string> labels = new Dictionary<State, string>();

            foreach (State state in states)
            {
                labels[state] = (labels.Count + 1).ToString();
                graph.AddShape(labels[state], state.IsFinal ? "doublecircle" : "circle");
            }

            if (Start != null)
            {
                graph.AddShape("", "none");
                graph.AddTransition("", labels[Start]);
            }

            foreach (State fromState in states)
                foreach (CharRange input in fromState.Alphabet)
                    foreach (State toState in fromState[input])
                        graph.AddTransition(labels[fromState], labels[toState], input.ToString());

            foreach (State fromState in states)
                foreach (State toState in fromState.EmptyTransitions)
                    graph.AddTransition(labels[fromState], labels[toState], "ε");

            return graph;
        }
        #endif
        #endregion
    }
}
