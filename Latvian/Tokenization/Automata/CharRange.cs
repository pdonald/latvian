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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Latvian.Tokenization.Automata
{
    struct CharRange : IEquatable<CharRange>, IComparable<CharRange>, IEnumerable<char>
    {
        public readonly static CharRange Empty = new CharRange();

        private readonly char from;
        private readonly char to;

        public CharRange(char c)
            : this(c, c)
        {
        }

        public CharRange(char from, char to)
        {
            if (from > to)
                throw new ArgumentException("The lower bound must be lower than the upper bound.");

            this.from = from;
            this.to = to;
        }

        public char From { get { return from; } }
        public char To { get { return to; } }

        public bool Contains(CharRange range)
        {
            return range.From >= From && range.to <= To;
        }

        public bool Overlaps(CharRange range)
        {
            return !(range.From > To || range.To < From);
        }

        public static CharRange[] Split(IEnumerable<CharRange> ranges)
        {
            // big todo: optimize

            List<CharRange> results = ranges.ToList();

            while (results.Any(r => results.Where(rr => rr != r).Any(rr => rr.Overlaps(r))))
            {
                var overlaping1 = results.Where(r => results.Where(rr => rr != r).Any(rr => rr.Overlaps(r))).First();
                var overlaping2 = results.Where(r => r.Overlaps(overlaping1) && r != overlaping1).First();

                results.Remove(overlaping1);
                results.Remove(overlaping2);
                
                var x = overlaping1.Split(overlaping2);

                results.AddRange(x);
            }

            return results.ToArray();
        }

        public CharRange[] Split(CharRange range)
        {
            // todo: make it prettier

            if (Equals(range))
                return new[] { this };
            if (!Overlaps(range))
                return new[] { this, range };

            if ((from == to || range.from == range.to) && (from == range.from || to == range.to))
            {
                CharRange single = from == to ? this : range;
                CharRange other = from == to ? range : this;
                if (single.from == other.from) // [a] == [a..z]
                    return new[] { single, new CharRange((char)(single.from + 1), other.to) };
                else // [z] == [a..z]
                    return new[] { single, new CharRange(other.from, (char)(single.to - 1)) };
            }

            if (this.from == range.from)
            {
                // a..z + a..c => a..c + d..z
                // a..c + a..z => a..c + d..z
                char a = this.to < range.to ? this.to : range.to;
                char b = this.to < range.to ? range.to : this.to;
                return new[] { new CharRange(this.from, a), new CharRange((char)(a + 1), b) };
            }
            else if (this.to == range.to)
            {
                // a..z + c..z => a..b + c..z
                // c..z + a..z => a..b + c..z
                char a = this.from < range.from ? this.from : range.from;
                char b = this.from < range.from ? range.from : this.from;
                return new[] { new CharRange(a, (char)(b - 1)), new CharRange(b, this.to) };
            }
            else
            {
                CharRange a = this.from < range.from ? this : range;
                CharRange b = this.from < range.from ? range : this;

                // a..z + c..f => a..b + c..f + g..z
                // c..f + a..z => a..b + c..f + g..z
                return new[] {
                    b,
                    new CharRange(a.from, (char)(b.from - 1)),
                    new CharRange((char)(b.to + 1), a.to),
                };
            }
        }

        public int CompareTo(CharRange other)
        {
            int c = From.CompareTo(other.From);
            if (c == 0) return To.CompareTo(other.To);
            return c;
        }

        public override bool Equals(object other)
        {
            return other is CharRange && Equals((CharRange)other);
        }

        public bool Equals(CharRange other)
        {
            return other.From == From && other.To == To;
        }

        public override int GetHashCode()
        {
            // todo: does it produce uniformly distributed hashcodes?
            return From.GetHashCode() ^ To.GetHashCode();
        }

        public override string ToString()
        {
            if (From == To)
                return From.ToString();
            return From + ".." + To;
        }

        public IEnumerator<char> GetEnumerator()
        {
            return Enumerable.Range(From, To - From + 1).Select(i => (char)i).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator CharRange(char c)
        {
            return new CharRange(c);
        }

        public static bool operator ==(CharRange x, CharRange y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(CharRange x, CharRange y)
        {
            return !x.Equals(y);
        }
    }
}
