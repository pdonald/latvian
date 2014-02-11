using System.Collections.Generic;
using System.Linq;

namespace Latvian.Tokenization
{
    using Automata;
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

        public new IEnumerable<Token> Tokenize(IEnumerable<char> text)
        {
            IEnumerable<Token> tokens = base.Tokenize(text);

            if (IncludeWhitespace && IncludeControlChars)
                return tokens;

            if (!IncludeWhitespace)
                return tokens.Where(t => !(t is WhitespaceToken));
            if (!IncludeControlChars)
                return tokens.Where(t => !(t is ControlCharsToken));

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
