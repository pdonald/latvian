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
using System.Text;

namespace Latvian.Tokenization.Automata
{
    using DFA = DeterministicFiniteAutomaton;
    using NFA = NondeterministicFiniteAutomaton;

    class RegularExpression
    {
        private readonly string pattern;

        public RegularExpression(string pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException("pattern");

            this.pattern = pattern;
        }

        public bool IsMatch(string input)
        {
            NFA nfa = ToNfa();
            DFA dfa = nfa.ToDfa();
            return dfa.IsMatch(input);
        }

        public NFA ToNfa()
        {
            Expression tree = Parse(pattern);
            NFA nfa = Construct(tree);
            return nfa;
        }

        private Expression Parse(string pattern)
        {
            using (StringReader reader = new StringReader(pattern))
                return new SingleExpression(reader);
        }

        private NFA Construct(Expression expression)
        {
            return expression.Visit();
        }

        private abstract class Expression
        {
            public abstract NFA Visit();
        }

        private abstract class UnaryExpression : Expression
        {
            public Expression Expression { get; set; }

            public override NFA Visit()
            {
                return Visit(Expression.Visit());
            }

            public virtual NFA Visit(NFA nfa)
            {
                return nfa;
            }
        }

        private abstract class BinaryExpression : Expression
        {
            public Expression Left { get; set; }
            public Expression Right { get; set; }
        }

        // todo: support implicit concatenation
        private class SingleExpression : UnaryExpression
        {
            public SingleExpression(TextReader reader)
            {
                while (reader.Peek() != -1)
                {
                    char c = (char)reader.Peek();

                    if (c == '(')
                    {
                        reader.Read();
                        Expression = new SingleExpression(reader);
                        if ((char)reader.Read() != ')')
                            throw new Exception("Expected )");
                    }
                    else if (c == ')')
                    {
                        return;
                    }
                    else if (c == '[')
                    {
                        reader.Read();
                        Expression = new RangeExpression(reader);
                        if ((char)reader.Read() != ']')
                            throw new Exception("Expected ]");
                    }
                    else if (c == '{')
                    {
                        reader.Read();
                        Expression = new RepeatExpression(reader) { Expression = Expression };
                        if ((char)reader.Read() != '}')
                            throw new Exception("Expected }");
                    }
                    else if (c == '|')
                    {
                        reader.Read();
                        Expression = new OrExpression { Left = Expression, Right = new SingleExpression(reader) };
                    }
                    else if (c == '.')
                    {
                        reader.Read();
                        Expression = new ConcatExpression { Left = Expression, Right = new SingleExpression(reader).Expression };
                    }
                    else if (c == '*')
                    {
                        reader.Read();
                        Expression = new StarExpression { Expression = Expression };
                    }
                    else if (c == '+')
                    {
                        reader.Read();
                        Expression = new PlusExpression { Expression = Expression };
                    }
                    else
                    {
                        throw new Exception("Unexpected " + c);
                    }
                }
            }

            public override string ToString()
            {
                return "(" + Expression + ")";
            }
        }

        // todo: support inverted ranges
        // todo: support escaped ]
        private class RangeExpression : Expression
        {
            public RangeExpression(TextReader reader)
            {
                StringBuilder chars = new StringBuilder();

                while (reader.Peek() != -1 && (char)reader.Peek() != ']')
                    chars.Append((char)reader.Read());

                if (chars.Length == 0)
                    throw new Exception("Expected at least one char in range");

                string s = chars.ToString();

                List<CharRange> ranges = new List<CharRange>();
                for (int i = 0; i < s.Length; i++)
                {
                    if (i - 1 >= 0 && s[i] == '-' && i + 1 < s.Length && s[i - 1] != '\\')
                    {
                        // [0-9]
                        ranges.Add(new CharRange(s[i - 1], s[i + 1]));
                        i++;
                    }
                    else if (i - 1 >= 0 && i + 1 == s.Length && s[i] == '-')
                    {
                        // [02-]
                        ranges.Add(s[i - 1]);
                        ranges.Add('-');
                    }
                    else
                    {
                        if (i + 1 < s.Length && s[i + 1] == '-' && s[i] != '\\')
                        {
                            // range start
                        }
                        else if (i + 1 < s.Length && s[i] == '\\' && s[i + 1] == '-')
                        {
                            // escape dash
                        }
                        else
                        {
                            ranges.Add(s[i]);
                        }
                    }
                }

                Ranges = ranges;
            }

            public IEnumerable<CharRange> Ranges
            {
                get;
                private set;
            }

            public override NFA Visit()
            {
                return NFA.Or(Ranges.Select(c => NFA.Char(c)).ToArray());
            }

            public override string ToString()
            {
                return "[" + Ranges.Select(r => r.ToString()) + "]";
            }
        }

        private class RepeatExpression : UnaryExpression
        {
            public RepeatExpression(TextReader reader)
            {
                StringBuilder buffer = new StringBuilder();

                while (reader.Peek() != -1 && (char)reader.Peek() != '}')
                    buffer.Append((char)reader.Read());

                Count = int.Parse(buffer.ToString());
            }

            public int Count
            {
                get;
                private set;
            }

            public override NFA Visit(NFA nfa)
            {
                return NFA.Repeat(nfa, Count);
            }

            public override string ToString()
            {
                return Expression + "{" + Count + "}";
            }
        }

        private class OrExpression : BinaryExpression
        {
            public override NFA Visit()
            {
                return NFA.Or(Left.Visit(), Right.Visit());
            }

            public override string ToString()
            {
                return Left + "|" + Right;
            }
        }

        private class ConcatExpression : BinaryExpression
        {
            public override NFA Visit()
            {
                return NFA.Concat(Left.Visit(), Right.Visit());
            }

            public override string ToString()
            {
                return Left + "" + Right;
            }
        }

        private class StarExpression : UnaryExpression
        {
            public override NFA Visit(NFA nfa)
            {
                return NFA.Star(nfa);
            }

            public override string ToString()
            {
                return Expression + "*";
            }
        }

        private class PlusExpression : UnaryExpression
        {
            public override NFA Visit(NFA nfa)
            {
                return NFA.Plus(nfa);
            }

            public override string ToString()
            {
                return Expression + "+";
            }
        }
    }
}
