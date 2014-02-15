// Copyright 2014 Pēteris Ņikiforovs
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Latvian.Tokenization.Automata
{
    class DeterministicFiniteAutomaton
    {
        public DeterministicFiniteAutomaton()
        {
        }

        public DeterministicFiniteAutomaton(State start)
        {
            Start = start;
        }

        public State Start
        {
            get;
            set;
        }

        public class State
        {
            private readonly Dictionary<CharRange, State> transitions = new Dictionary<CharRange, State>();

            // optimization: arrays are faster than dictionary
            private CharRange[] ranges;
            private State[] states;
            private CharRange firstRange;
            private int count;

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
                get { return transitions.Keys.ToArray(); }
            }

            public IEnumerable<State> Transitions
            {
                get { return transitions.Values.Distinct().ToArray(); }
            }

            public State this[char c]
            {
                // this is the hottest code
                // it gets called a gazillion times
                get
                {
                    if (count == 0)
                        return null;
                    if (count == 1)
                        return c >= firstRange.From && c <= firstRange.To ? states[0] : null;

                    int low = 0;
                    int high = count - 1;

                    while (low <= high)
                    {
                        int mid = low + (high - low) / 2;
                        CharRange range = ranges[mid];

                        if (c >= range.From && c <= range.To)
                            return states[mid];

                        if (range.From < c)
                            low = mid + 1;
                        else
                            high = mid - 1;
                    }

                    return null;
                }
            }

            public State this[CharRange input]
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
                if (transitions.Keys.Any(range => range.Overlaps(input)))
                    throw new ArgumentOutOfRangeException("Already have a transition for this input or it's overlapping", "input");

                transitions[input] = to;

                Minimize();
            }

            public void SetTransition(CharRange input, State to) // not used by NFA.ToDfa()
            {
                CharRange overlaping = transitions.Keys.SingleOrDefault(range => range.Overlaps(input));

                if (overlaping == CharRange.Empty) // [a..c -> 1] + [d..f -> 2]
                {
                    transitions[input] = to;
                }
                else // [a..z -> 1] + [c..f -> 2] = [a..b -> 1] + [c..f -> 2] + [g..z -> 1]
                {
                    State overlapingState = transitions[overlaping];
                    transitions.Remove(overlaping);

                    CharRange[] ranges = overlaping.Split(input);
                    foreach (CharRange range in ranges)
                        transitions[range] = range.Overlaps(input) ? to : overlapingState;
                }

                Minimize();
            }

            public void RemoveTransition(CharRange input)
            {
                // overlaps
                // doesn't overlap
                // throw new NotImplementedException();

                transitions.Remove(input);
                Minimize();
            }

            public void RemoveTransitions()
            {
                transitions.Clear();
                Minimize();
            }

            public void Minimize()
            {
                // todo: join ranges, maybe?

                // this makes it so much faster!!!!!
                var sorted = transitions.OrderBy(kp => kp.Key).ToArray();
                ranges = sorted.Select(kp => kp.Key).ToArray();
                states = sorted.Select(kp => kp.Value).ToArray();
                count = ranges.Length;
                if (count > 0) firstRange = ranges[0];
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
                        foreach (State toState in state.Transitions)
                            queue.Enqueue(toState);
                    }
                }

                return states;
            }
        }

        public bool IsMatch(IEnumerable<char> input)
        {
            if (Start == null)
                return false;

            State current = Start;

            foreach (char c in input)
            {
                current = current[c];

                if (current == null)
                    return false;
            }

            return current.IsFinal;
        }

        public void Minimize()
        {
            return;

            IEnumerable<State> Q = States;
            IEnumerable<State> F = States.Where(s => s.IsFinal).ToList();
            List<IEnumerable<State>> P = new List<IEnumerable<State>> { F, Q.Except(F).ToList() };
            IEnumerable<CharRange> E = Q.SelectMany(s => s.Alphabet).Distinct().ToList();

            List<IEnumerable<State>> W = new List<IEnumerable<State>> { F };

            while (W.Count > 0)
            {
                IEnumerable<State> A = W[0];
                W.Remove(A);

                foreach (CharRange c in E)
                {
                    IEnumerable<State> X = Q.Where(s => A.Contains(s[c])).ToList();

                    foreach (IEnumerable<State> Y in P.ToArray())
                    {
                        var xy = X.Intersect(Y).ToList();
                        var yx = Y.Except(X).ToList();

                        if (xy.Any() && yx.Any())
                        {
                            P.Remove(Y);
                            P.Add(xy);
                            P.Add(yx);

                            if (W.Contains(Y))
                            {
                                W.Remove(Y);
                                W.Add(xy);
                                W.Add(yx);
                            }
                            else
                            {
                                if (xy.Count() <= yx.Count())
                                {
                                    W.Add(xy);
                                }
                                else
                                {
                                    W.Add(yx);
                                }
                            }
                        }
                    }
                }
            }

            var startStates = P.Where(g => g.Contains(Start)).ToList();

            foreach (IEnumerable<State> states in P.Where(s => s.Count() > 1))
            {
                State best = states.First();
                IEnumerable<State> other = states.Skip(1).ToList();

                foreach (State state in States)
                {
                    foreach (CharRange input in state.Alphabet)
                    {
                        if (other.Contains(state[input]))
                        {
                            state.RemoveTransition(input);
                            state.AddTransition(input, best);
                        }
                    }
                }
            }
        }

        #region Load/Save
        public void Load(Stream stream)
        {
            Dictionary<int, State> states = new Dictionary<int, State>();

            using (BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                int stateCount = reader.ReadInt32();

                for (int i = 0; i < stateCount; i++)
                {
                    int id = reader.ReadInt32();
                    bool isFinal = reader.ReadBoolean();
                    int valueCount = reader.ReadInt32();
                    int[] values = new int[valueCount];
                    for (int v = 0; v < valueCount; v++)
                        values[v] = reader.ReadInt32();
                    states[id] = new State { IsFinal = isFinal, Values = values };
                }

                for (int i = 0; i < stateCount; i++)
                {
                    int transitionCount = reader.ReadInt32();

                    for (int t = 0; t < transitionCount; t++)
                    {
                        CharRange input = new CharRange(reader.ReadChar(), reader.ReadChar());
                        int fromState = reader.ReadInt32();
                        int toState = reader.ReadInt32();
                        states[fromState].AddTransition(input, states[toState]);
                    }
                }

                int startState = reader.ReadInt32();
                if (startState != -1)
                    Start = states[startState];
            }
        }

        public void Save(Stream stream)
        {
            IEnumerable<State> states = States;
            Dictionary<State, int> labels = new Dictionary<State, int>();

            using (BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(states.Count());

                foreach (State state in states)
                {
                    int id = labels[state] = labels.Count;

                    writer.Write(id);
                    writer.Write(state.IsFinal);

                    if (state.Values != null)
                    {
                        writer.Write(state.Values.Count());
                        foreach (int value in state.Values)
                            writer.Write(value);
                    }
                    else
                    {
                        writer.Write(0);
                    }
                }

                foreach (State state in states)
                {
                    writer.Write(state.Alphabet.Count());

                    foreach (CharRange input in state.Alphabet)
                    {
                        writer.Write(input.From);
                        writer.Write(input.To);
                        writer.Write(labels[state]);
                        writer.Write(labels[state[input]]);
                    }
                }

                writer.Write(Start != null ? labels[Start] : -1);
            }
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
                    graph.AddTransition(labels[fromState], labels[fromState[input]], input.ToString());

            return graph;
        }
        #endif
        #endregion
    }
}
