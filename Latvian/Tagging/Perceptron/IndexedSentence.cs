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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Latvian.Tagging.Perceptron
{
    public class IndexedSentence : IEnumerable<Token>
    {
        private readonly List<Token> tokens;
        private readonly Dictionary<Token, int> indexes;

        public IndexedSentence(IEnumerable<Token> tokens)
        {
            this.tokens = tokens.ToList();
            this.indexes = new Dictionary<Token, int>();

            for (int i = 0; i < this.tokens.Count; i++)
                this.indexes[this.tokens[i]] = i;
        }

        public Token this[int index]
        {
            get { return tokens[index]; }
        }

        public int this[Token token]
        {
            get { return indexes[token]; }
        }

        public int Count
        {
            get { return tokens.Count; }
        }

        public IEnumerator<Token> GetEnumerator()
        {
            return tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
