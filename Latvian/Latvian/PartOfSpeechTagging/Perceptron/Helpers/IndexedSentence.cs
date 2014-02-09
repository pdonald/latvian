using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Latvian.PartOfSpeechTagging.Perceptron
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
