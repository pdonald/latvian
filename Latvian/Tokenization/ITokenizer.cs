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

namespace Latvian.Tokenization
{
    using Sentence = IEnumerable<Token>;

    public interface ITokenizer
    {
        IEnumerable<Token> Tokenize(IEnumerable<char> text);
    }

    public interface ISentenceTokenizer
    {
        IEnumerable<Sentence> TokenizeSentences(IEnumerable<char> text);
    }

    public interface ISentenceBreaker
    {
        IEnumerable<Sentence> BreakSentences(IEnumerable<Token> tokens);
    }

    public static class ITokenizerExtensions
    {
        public static IEnumerable<Token> Tokenize(this ITokenizer tokenizer, Stream stream)
        {
            if (tokenizer == null)
                throw new ArgumentNullException("tokenizer");
            if (stream == null)
                throw new ArgumentNullException("stream");

            using (TextReader reader = new StreamReader(stream))
                foreach (Token token in tokenizer.Tokenize(reader.ReadAll()))
                    yield return token;
        }

        public static IEnumerable<Token> Tokenize(this ITokenizer tokenizer, TextReader textReader)
        {
            if (tokenizer == null)
                throw new ArgumentNullException("tokenizer");
            if (textReader == null)
                throw new ArgumentNullException("textReader");

            return tokenizer.Tokenize(textReader.ReadAll());
        }

        public static IEnumerable<Sentence> TokenizeSentences(this ISentenceTokenizer tokenizer, Stream stream)
        {
            if (tokenizer == null)
                throw new ArgumentNullException("tokenizer");
            if (stream == null)
                throw new ArgumentNullException("stream");

            using (TextReader reader = new StreamReader(stream))
                foreach (Sentence sentence in tokenizer.TokenizeSentences(reader.ReadAll()))
                    yield return sentence;
        }

        public static IEnumerable<Sentence> TokenizeSentences(this ISentenceTokenizer tokenizer, TextReader textReader)
        {
            if (tokenizer == null)
                throw new ArgumentNullException("tokenizer");
            if (textReader == null)
                throw new ArgumentNullException("textReader");

            return tokenizer.TokenizeSentences(textReader.ReadAll());
        }

        internal static IEnumerable<char> ReadAll(this TextReader textReader)
        {
            if (textReader == null)
                throw new ArgumentNullException("textReader");

            for (int c = textReader.Read(); c != -1; c = textReader.Read())
                yield return (char)c;
        }
    }
}
