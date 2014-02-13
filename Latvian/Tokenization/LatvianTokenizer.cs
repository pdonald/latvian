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

using System.Collections.Generic;
using System.Linq;

namespace Latvian.Tokenization
{
    using Tokens;

    using Sentence = IEnumerable<Token>;

    public class LatvianTokenizer : AutomatonTokenizer, ITokenizer, ISentenceTokenizer, ISentenceBreaker
    {
        public LatvianTokenizer(bool compile = true)
        {
            // todo: exceptions e.g. utt. kaut vai
            Add<WordToken>();
            Add<WhitespaceToken>();
            Add<PunctuationToken>();
            Add<RepeatingPunctuationToken>();
            Add<OrdinalNumberToken>();
            Add<CardinalNumberToken>();
            Add<DecimalNumberToken>();
            Add<FractionNumberToken>();
            Add<ThousandsNumberToken>();
            Add<MoneyToken>();
            Add<TimeToken>();
            Add<TimeSecondsToken>();
            Add<DateToken>();
            Add<IsoDateTimeToken>();
            Add<EmailToken>();
            Add<UrlToken>();
            Add<WebsiteToken>();
            Add<IPAddressToken>();
            Add<HypenedWordToken>();
            Add<InitialsToken>();
            Add<LettersWithSpacesToken>();
            Add<SymbolToken>();
            Add<CyrillicToken>();
            Add<ControlCharsToken>();
            Add<ByteOrderMarkToken>();
            Add<UnknownToken>();

            if (compile) Compile();
        }

        public LatvianTokenizer(string filename, bool compile = true)
        {
            Load(filename, compile);
        }

        public bool IncludeWhitespace { get; set; }
        public bool IncludeControlChars { get; set; }

        internal override IEnumerable<Token> Tokenize(Readers.CharReader reader)
        {
            IEnumerable<Token> tokens = base.Tokenize(reader);

            if (IncludeWhitespace && IncludeControlChars)
                return tokens;

            if (!IncludeWhitespace)
                return tokens.Where(t => !(t is WhitespaceToken));
            if (!IncludeControlChars)
                return tokens.Where(t => !(t is ControlCharsToken) && !(t is ByteOrderMarkToken));

            return tokens;
        }

        public IEnumerable<Sentence> TokenizeSentences(IEnumerable<char> text)
        {
            return BreakSentences(Tokenize(text));
        }

        public IEnumerable<Sentence> BreakSentences(IEnumerable<Token> tokens) // todo: this is not a real/serious implementation
        {
            List<Token> sentence = new List<Token>();

            foreach (Token token in tokens)
            {
                sentence.Add(token);

                if (token.Text == "." || token.Text == "?" || token.Text == "!")
                {
                    yield return sentence.ToArray();
                    sentence = new List<Token>();
                }
            }

            if (sentence.Count > 0)
                yield return sentence.ToArray();
        }

        public void Compile()
        {
            BuildAutomaton();
        }

        #region Load/Save
        public void Load(string filename, bool compile = true)
        {
            base.Load(filename);

            if (compile) Compile();
        }

        protected override void Load(System.IO.BinaryReader reader)
        {
            base.Load(reader);

            IncludeWhitespace = reader.ReadBoolean();
            IncludeControlChars = reader.ReadBoolean();
        }

        protected override void Save(System.IO.BinaryWriter writer)
        {
            base.Save(writer);

            writer.Write(IncludeWhitespace);
            writer.Write(IncludeControlChars);
        }
        #endregion
    }
}
