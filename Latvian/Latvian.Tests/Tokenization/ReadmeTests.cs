using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Latvian.Tokenization;
using Latvian.Tokenization.Automata;
using Latvian.Tokenization.Tokens;

using NUnit.Framework;

namespace Latvian.Tests.Tokenization
{
    using Sentence = IEnumerable<Token>;

    [TestFixture]
    public class ReadmeTests
    {
        [Test]
        public void QuickStart()
        {
            List<Token> tokens = new List<Token>();
            DateToken dateToken = null;

            Debug.WriteLine("Latvian.Tests.Tokenization.ReadmeTests.QuickStart starts");

            string text = "Sveika, pasaule! Man iet labi. Šodienas datums ir 2014-01-01";

            LatvianTokenizer tokenizer = new LatvianTokenizer();

            foreach (Token token in tokenizer.Tokenize(text))
            {
                Debug.WriteLine("Line {0}: Pos {1}: Type: {2} Token: {3}",
                    token.LineStart, token.Start, token.GetType(), token.Text);

                tokens.Add(token);

                if (token is DateToken)
                {
                    dateToken = token as DateToken;
                    Debug.WriteLine(dateToken.DateTime.ToString("dd/MM/yyyy"));
                }
            }

            Debug.WriteLine("Latvian.Tests.Tokenization.ReadmeTests.QuickStart end");

            Assert.AreEqual(12, tokens.Count);

            Assert.AreEqual("Sveika", tokens[0].Text);
            Assert.AreEqual(typeof(WordToken), tokens[0].GetType());
            Assert.AreEqual(0, tokens[0].Start);
            Assert.AreEqual(6, tokens[0].End);
            Assert.AreEqual(0, tokens[0].LineStart);
            Assert.AreEqual(0, tokens[0].LineEnd);
            Assert.AreEqual(0, tokens[0].LinePosStart);
            Assert.AreEqual(6, tokens[0].LinePosEnd);

            Assert.AreEqual(",", tokens[1].Text);
            Assert.AreEqual(typeof(PunctuationToken), tokens[1].GetType());
            Assert.AreEqual(6, tokens[1].Start);
            Assert.AreEqual(7, tokens[1].End);
            Assert.AreEqual(0, tokens[1].LineStart);
            Assert.AreEqual(0, tokens[1].LineEnd);
            Assert.AreEqual(6, tokens[1].LinePosStart);
            Assert.AreEqual(7, tokens[1].LinePosEnd);

            Assert.AreEqual("pasaule", tokens[2].Text);
            Assert.AreEqual(typeof(WordToken), tokens[2].GetType());
            Assert.AreEqual(8, tokens[2].Start);
            Assert.AreEqual(15, tokens[2].End);
            Assert.AreEqual(0, tokens[2].LineStart);
            Assert.AreEqual(0, tokens[2].LineEnd);
            Assert.AreEqual(8, tokens[2].LinePosStart);
            Assert.AreEqual(15, tokens[2].LinePosEnd);

            Assert.AreEqual("!", tokens[3].Text);
            Assert.AreEqual(typeof(PunctuationToken), tokens[3].GetType());
            Assert.AreEqual(15, tokens[3].Start);
            Assert.AreEqual(16, tokens[3].End);
            Assert.AreEqual(0, tokens[3].LineStart);
            Assert.AreEqual(0, tokens[3].LineEnd);
            Assert.AreEqual(15, tokens[3].LinePosStart);
            Assert.AreEqual(16, tokens[3].LinePosEnd);

            Assert.AreEqual("Man", tokens[4].Text);
            Assert.AreEqual(typeof(WordToken), tokens[4].GetType());
            Assert.AreEqual(17, tokens[4].Start);
            Assert.AreEqual(20, tokens[4].End);
            Assert.AreEqual(0, tokens[4].LineStart);
            Assert.AreEqual(0, tokens[4].LineEnd);
            Assert.AreEqual(17, tokens[4].LinePosStart);
            Assert.AreEqual(20, tokens[4].LinePosEnd);

            Assert.AreEqual("iet", tokens[5].Text);
            Assert.AreEqual(typeof(WordToken), tokens[5].GetType());
            Assert.AreEqual(21, tokens[5].Start);
            Assert.AreEqual(24, tokens[5].End);
            Assert.AreEqual(0, tokens[5].LineStart);
            Assert.AreEqual(0, tokens[5].LineEnd);
            Assert.AreEqual(21, tokens[5].LinePosStart);
            Assert.AreEqual(24, tokens[5].LinePosEnd);

            Assert.AreEqual("labi", tokens[6].Text);
            Assert.AreEqual(typeof(WordToken), tokens[6].GetType());
            Assert.AreEqual(25, tokens[6].Start);
            Assert.AreEqual(29, tokens[6].End);
            Assert.AreEqual(0, tokens[6].LineStart);
            Assert.AreEqual(0, tokens[6].LineEnd);
            Assert.AreEqual(25, tokens[6].LinePosStart);
            Assert.AreEqual(29, tokens[6].LinePosEnd);

            Assert.AreEqual(".", tokens[7].Text);
            Assert.AreEqual(typeof(PunctuationToken), tokens[7].GetType());
            Assert.AreEqual(29, tokens[7].Start);
            Assert.AreEqual(30, tokens[7].End);
            Assert.AreEqual(0, tokens[7].LineStart);
            Assert.AreEqual(0, tokens[7].LineEnd);
            Assert.AreEqual(29, tokens[7].LinePosStart);
            Assert.AreEqual(30, tokens[7].LinePosEnd);

            Assert.AreEqual("2014-01-01", tokens[11].Text);
            Assert.AreEqual(typeof(DateToken), tokens[11].GetType());
            Assert.AreEqual(50, tokens[11].Start);
            Assert.AreEqual(60, tokens[11].End);
            Assert.AreEqual(0, tokens[11].LineStart);
            Assert.AreEqual(0, tokens[11].LineEnd);
            Assert.AreEqual(50, tokens[11].LinePosStart);
            Assert.AreEqual(60, tokens[11].LinePosEnd);

            Assert.AreEqual(tokens[11], dateToken);
            Assert.AreEqual("01.01.2014", dateToken.DateTime.ToString("dd/MM/yyyy"));
        }

        [Test]
        public void QuickStart_TokenizeSentences()
        {
            string text = "Sveika, pasaule! Man iet labi. Šodienas datums ir 2014-01-01";

            List<Sentence> sentences = new List<Sentence>();

            LatvianTokenizer tokenizer = new LatvianTokenizer();
            
            foreach (Sentence sentence in tokenizer.TokenizeSentences(text))
            {
                List<Token> sentenceTokens = new List<Token>();

                foreach (Token token in sentence)
                {
                    sentenceTokens.Add(token);
                }

                sentences.Add(sentenceTokens);
            }

            Assert.AreEqual(3, sentences.Count());
            Assert.AreEqual(4, sentences[0].Count());
            Assert.AreEqual(4, sentences[1].Count());
            Assert.AreEqual(4, sentences[2].Count());
        }

        [Test]
        public void QuickStart_BreakSentences()
        {
            string text = "Sveika, pasaule! Man iet labi. Šodienas datums ir 2014-01-01";

            LatvianTokenizer tokenizer = new LatvianTokenizer();

            Token[] tokens = tokenizer.Tokenize(text).ToArray();
            Sentence[] sentences = tokenizer.BreakSentences(tokens).ToArray();

            Assert.AreEqual(3, sentences.Count());
            Assert.AreEqual(4, sentences[0].Count());
            Assert.AreEqual(4, sentences[1].Count());
            Assert.AreEqual(4, sentences[2].Count());
        }

        [Test]
        public void Position()
        {
            Token[] tokens = new LatvianTokenizer().Tokenize("Vārds.").ToArray();
            Assert.AreEqual(0, tokens[0].Start);
            Assert.AreEqual(5, tokens[0].End);
            Assert.AreEqual(0, tokens[0].LineStart);
            Assert.AreEqual(0, tokens[0].LineEnd);
            Assert.AreEqual(0, tokens[0].LinePosStart);
            Assert.AreEqual(5, tokens[0].LinePosEnd);

            Assert.AreEqual(".", tokens[1].Text);
            Assert.AreEqual(5, tokens[1].Start);
            Assert.AreEqual(6, tokens[1].End);
            Assert.AreEqual(0, tokens[1].LineStart);
            Assert.AreEqual(0, tokens[1].LineEnd);
            Assert.AreEqual(5, tokens[1].LinePosStart);
            Assert.AreEqual(6, tokens[1].LinePosEnd);
        }

        [Test]
        public void Text()
        {
            Token token = new LatvianTokenizer().Tokenize("vārds").First();
            Assert.AreEqual("vārds", token.Text);
            Assert.AreEqual("vārds", token.ToString());
        }

        [Test]
        public void Values()
        {
            string[] tokens = new LatvianTokenizer().Tokenize("viens divi").Select(t => t.Text).ToArray();
            Assert.AreEqual("viens", tokens[0]);
            Assert.AreEqual("divi", tokens[1]);
        }

        [Test]
        public void Distinct()
        {
            var tokens = new LatvianTokenizer().Tokenize("viens viens").Distinct();
            Assert.AreEqual(1, tokens.Count());
        }

        [Test]
        public void DateToken()
        {
            Token token = new LatvianTokenizer().Tokenize("2014-01-01").First();
            Assert.IsTrue(token is DateToken);
            Assert.AreEqual(2014, ((DateToken)token).DateTime.Year); 
        }

        [Test]
        public void OrdinalNumberToken()
        {
            Token token = new LatvianTokenizer().Tokenize("123").First();
            Assert.IsTrue(token is OrdinalNumberToken);
            Assert.AreEqual(123 * 2, ((OrdinalNumberToken)token).Value * 2);
        }

        [Test]
        public void WhitespaceExcluded()
        {
            Token[] tokens = new LatvianTokenizer().Tokenize("123 456").ToArray();
            Assert.AreEqual("123", tokens[0].Text);
            Assert.AreEqual("456", tokens[1].Text);
        }

        [Test]
        public void WhitespaceIncluded()
        {
            LatvianTokenizer tokenizer = new LatvianTokenizer() { IncludeWhitespace = true };
            Token[] tokens = tokenizer.Tokenize("123 456").ToArray();
            Assert.AreEqual("123", tokens[0].Text);
            Assert.AreEqual(" ", tokens[1].Text);
            Assert.AreEqual("456", tokens[2].Text);
        }

        [Test]
        public void Stream()
        {
            using (Stream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                    writer.Write("123 456");
                
                stream.Position = 0;

                Token[] tokens = new LatvianTokenizer().Tokenize(stream).ToArray();
                Assert.AreEqual("123", tokens[0].Text);
                Assert.AreEqual("456", tokens[1].Text);
            }
        }

        [Test]
        public void TextReader()
        {
            using (StringReader reader = new StringReader("123 456"))
            {
                Token[] tokens = new LatvianTokenizer().Tokenize(reader).ToArray();
                Assert.AreEqual("123", tokens[0].Text);
                Assert.AreEqual("456", tokens[1].Text);
            }
        }

        [Test]
        public void Initials()
        {
            Token[] tokens = new LatvianTokenizer().Tokenize("A.Bērziņš").ToArray();
            Assert.AreEqual("A.", tokens[0].Text);
            Assert.AreEqual("Bērziņš", tokens[1].Text);
        }

        [Test]
        public void InitialsRemoved()
        {
            LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
            tokenizer.Remove<InitialsToken>();
            tokenizer.Compile(); // optional

            Token[] tokens = tokenizer.Tokenize("A.Bērziņš").ToArray();
            Assert.AreEqual("A", tokens[0].Text);
            Assert.AreEqual(".", tokens[1].Text);
            Assert.AreEqual("Bērziņš", tokens[2].Text);
        }

        [Test]
        public void InitialsRemovedNotCompiled()
        {
            LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
            tokenizer.Remove<InitialsToken>();

            Token[] tokens = tokenizer.Tokenize("A.Bērziņš").ToArray();
            Assert.AreEqual("A", tokens[0].Text);
            Assert.AreEqual(".", tokens[1].Text);
            Assert.AreEqual("Bērziņš", tokens[2].Text);
        }

        [Test]
        public void CustomClass()
        {
            MyTokenizer tokenizer = new MyTokenizer();
            Token[] tokens = tokenizer.Tokenize("A.Bērziņš ").ToArray();
            Assert.AreEqual("A", tokens[0].Text);
            Assert.AreEqual(".", tokens[1].Text);
            Assert.AreEqual("Bērziņš", tokens[2].Text);
            Assert.AreEqual(" ", tokens[3].Text);
        }

        public class MyTokenizer : LatvianTokenizer
        {
            public MyTokenizer()
                : base(compile: false)
            {
                Remove<InitialsToken>();
                IncludeWhitespace = true;

                Compile();
            }
        }

        [Test]
        public void Emotions()
        {
            LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
            tokenizer.Add<EmotionToken>();

            EmotionToken[] tokens = tokenizer.Tokenize("Šodien esmu :) bet vakar biju :(").OfType<EmotionToken>().ToArray();
            Assert.AreEqual(":)", tokens[0].Text);
            Assert.AreEqual(":(", tokens[1].Text);
        }

        public class EmotionToken : Token, IHasPattern
        {
            public string Pattern
            {
                get { return "[:].[)(DP]"; } // :) :( :D :P
            }

            public bool IsHappy
            {
                get { return Text == ":)" || Text == ":D"; }
            }
        }

        [Test]
        public void LongestMatch()
        {
            Token[] tokens = new LatvianTokenizer().Tokenize("2014-01-01 2014-01-01T12:00:00").ToArray();
            Assert.AreEqual("2014-01-01", tokens[0].Text);
            Assert.AreEqual("2014-01-01T12:00:00", tokens[1].Text);
        }

        [Test]
        public void EqualMatch()
        {
            LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
            tokenizer.Clear();
            tokenizer.Add<TimeSpanToken>(); // matches 00:00:00
            tokenizer.Add<ClockToken>(); // matches 00:00:00

            Token token = tokenizer.Tokenize("00:00:00").First();
            Assert.IsTrue(token is TimeSpanToken);
        }

        public class TimeSpanToken : TimeToken
        {
        }

        public class ClockToken : TimeToken
        {
        }

        [Test]
        public void Reorder()
        {
            LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
            tokenizer.Clear();
            tokenizer.Add<TimeSpanToken>(); // matches 00:00:00
            tokenizer.Add<ClockToken>(); // matches 00:00:00
            tokenizer.Remove<ClockToken>();
            tokenizer.Insert<ClockToken>(0);
            Token token = tokenizer.Tokenize("00:00:00").First();
            Assert.IsTrue(token is ClockToken);

            tokenizer = new LatvianTokenizer(compile: false);
            tokenizer.Clear();
            tokenizer.Add<TimeSpanToken>(); // matches 00:00:00
            tokenizer.Add<ClockToken>(); // matches 00:00:00
            tokenizer.Remove(typeof(ClockToken));
            tokenizer.Insert(0, typeof(ClockToken));
            token = tokenizer.Tokenize("00:00:00").First();
            Assert.IsTrue(token is ClockToken);

            tokenizer = new LatvianTokenizer(compile: false);
            tokenizer.Clear();
            tokenizer.Add<TimeSpanToken>(); // matches 00:00:00
            tokenizer.Add<ClockToken>(); // matches 00:00:00
            tokenizer.Move<ClockToken>(0);
            token = tokenizer.Tokenize("00:00:00").First();
            Assert.IsTrue(token is ClockToken);
        }

        [Test]
        public void Regex_a()
        {
            RegularExpression regex = new RegularExpression("[a]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("a"));
            Assert.IsFalse(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("b"));
        }

        [Test]
        public void Regex_aA()
        {
            RegularExpression regex = new RegularExpression("[aA]");
            
            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("a"));
            Assert.IsTrue(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("b"));
            Assert.IsFalse(regex.IsMatch("B"));
        }

        [Test]
        public void Regex_Digit()
        {
            RegularExpression regex = new RegularExpression("[0-9]");
            for (char c = '0'; c <= '9'; c++)
                Assert.IsTrue(regex.IsMatch(c.ToString()));

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsFalse(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("b"));
            Assert.IsFalse(regex.IsMatch("B"));
        }

        [Test]
        public void Regex_DigitsZeroOrMore()
        {
            RegularExpression regex = new RegularExpression("[0-9]*");

            Assert.IsTrue(regex.IsMatch(""));

            for (char c = '0'; c <= '9'; c++)
            {
                Assert.IsTrue(regex.IsMatch(c.ToString()));
                Assert.IsTrue(regex.IsMatch(c.ToString() + c.ToString()));
            }

            Assert.IsFalse(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("b"));
            Assert.IsFalse(regex.IsMatch("B"));
        }

        [Test]
        public void Regex_DigitsOneOrMore()
        {
            RegularExpression regex = new RegularExpression("[0-9]+");

            Assert.IsFalse(regex.IsMatch(""));

            for (char c = '0'; c <= '9'; c++)
            {
                Assert.IsTrue(regex.IsMatch(c.ToString()));
                Assert.IsTrue(regex.IsMatch(c.ToString() + c.ToString()));
            }

            Assert.IsFalse(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("b"));
            Assert.IsFalse(regex.IsMatch("B"));
        }

        [Test]
        public void Regex_Concat()
        {
            RegularExpression regex = new RegularExpression("[0-9].[a-z]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("0a"));
            Assert.IsTrue(regex.IsMatch("3z"));
            Assert.IsFalse(regex.IsMatch("0aa"));
            Assert.IsFalse(regex.IsMatch("00"));
            Assert.IsFalse(regex.IsMatch("00a"));
            Assert.IsFalse(regex.IsMatch("a0"));
            Assert.IsFalse(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("b"));
            Assert.IsFalse(regex.IsMatch("B"));
        }

        [Test]
        public void Regex_Or()
        {
            RegularExpression regex = new RegularExpression("[0-9]|[a-z]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("a"));
            Assert.IsTrue(regex.IsMatch("z"));
            Assert.IsTrue(regex.IsMatch("0"));
            Assert.IsTrue(regex.IsMatch("4"));
            Assert.IsFalse(regex.IsMatch("0aa"));
            Assert.IsFalse(regex.IsMatch("00"));
            Assert.IsFalse(regex.IsMatch("00a"));
            Assert.IsFalse(regex.IsMatch("a0"));
            Assert.IsFalse(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("B"));
        }

        [Test]
        public void Regex_1Or2()
        {
            RegularExpression regex = new RegularExpression("[0].[1]|[2]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("01"));
            Assert.IsTrue(regex.IsMatch("02"));
            Assert.IsFalse(regex.IsMatch("012"));
            Assert.IsFalse(regex.IsMatch("021"));
            Assert.IsFalse(regex.IsMatch("a"));
            Assert.IsFalse(regex.IsMatch("11"));
            Assert.IsFalse(regex.IsMatch("22"));
            Assert.IsFalse(regex.IsMatch("201"));

            regex = new RegularExpression("[0].([1]|[2])");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("01"));
            Assert.IsTrue(regex.IsMatch("02"));
            Assert.IsFalse(regex.IsMatch("012"));
            Assert.IsFalse(regex.IsMatch("021"));
            Assert.IsFalse(regex.IsMatch("a"));
            Assert.IsFalse(regex.IsMatch("11"));
            Assert.IsFalse(regex.IsMatch("22"));
            Assert.IsFalse(regex.IsMatch("201"));

            regex = new RegularExpression("([0].[1])|[2]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("01"));
            Assert.IsTrue(regex.IsMatch("2"));
            Assert.IsFalse(regex.IsMatch("02"));
            Assert.IsFalse(regex.IsMatch("012"));
            Assert.IsFalse(regex.IsMatch("021"));
            Assert.IsFalse(regex.IsMatch("a"));
            Assert.IsFalse(regex.IsMatch("0"));
            Assert.IsFalse(regex.IsMatch("1"));
            Assert.IsFalse(regex.IsMatch("11"));
            Assert.IsFalse(regex.IsMatch("22"));
            Assert.IsFalse(regex.IsMatch("201"));
        }

        [Test]
        public void Regex_MultiRange()
        {
            RegularExpression regex = new RegularExpression("[a-z0-9!@#]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("a"));
            Assert.IsTrue(regex.IsMatch("d"));
            Assert.IsTrue(regex.IsMatch("z"));
            Assert.IsTrue(regex.IsMatch("0"));
            Assert.IsTrue(regex.IsMatch("4"));
            Assert.IsTrue(regex.IsMatch("9"));
            Assert.IsTrue(regex.IsMatch("!"));
            Assert.IsTrue(regex.IsMatch("@"));
            Assert.IsTrue(regex.IsMatch("#"));
            Assert.IsFalse(regex.IsMatch("A"));
            Assert.IsFalse(regex.IsMatch("C"));
            Assert.IsFalse(regex.IsMatch("0aa"));
            Assert.IsFalse(regex.IsMatch("00"));
            Assert.IsFalse(regex.IsMatch("00a"));
            Assert.IsFalse(regex.IsMatch("a0"));
        }

        [Test]
        public void Regex_Dash()
        {
            RegularExpression regex = new RegularExpression("[-02]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("0"));
            Assert.IsTrue(regex.IsMatch("2"));
            Assert.IsTrue(regex.IsMatch("-"));
            Assert.IsFalse(regex.IsMatch("1"));

            regex = new RegularExpression("[02-]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("0"));
            Assert.IsTrue(regex.IsMatch("2"));
            Assert.IsTrue(regex.IsMatch("-"));
            Assert.IsFalse(regex.IsMatch("1"));

            regex = new RegularExpression(@"[0\-2]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("0"));
            Assert.IsTrue(regex.IsMatch("2"));
            Assert.IsTrue(regex.IsMatch("-"));
            Assert.IsFalse(regex.IsMatch("1"));

            regex = new RegularExpression("[0\\-2]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("0"));
            Assert.IsTrue(regex.IsMatch("2"));
            Assert.IsTrue(regex.IsMatch("-"));
            Assert.IsFalse(regex.IsMatch("1"));

            regex = new RegularExpression("[0-2]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("0"));
            Assert.IsTrue(regex.IsMatch("1"));
            Assert.IsTrue(regex.IsMatch("2"));
            Assert.IsFalse(regex.IsMatch("-"));
        }

        [Test]
        public void Regex_Unicode()
        {
            RegularExpression regex = new RegularExpression("[\u0000-\uffff]");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsTrue(regex.IsMatch("\0"));
            Assert.IsTrue(regex.IsMatch("\u1234"));
            Assert.IsTrue(regex.IsMatch("\uffff"));
        }

        [Test]
        public void Regex_TwoOrMore()
        {
            RegularExpression regex = new RegularExpression("[a].[a]+");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsFalse(regex.IsMatch("a"));
            Assert.IsTrue(regex.IsMatch("aa"));
            Assert.IsTrue(regex.IsMatch("aaa"));
            Assert.IsTrue(regex.IsMatch("aaaa"));
            
            regex = new RegularExpression("[a].[a].[a]*");

            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsFalse(regex.IsMatch("a"));
            Assert.IsTrue(regex.IsMatch("aa"));
            Assert.IsTrue(regex.IsMatch("aaa"));
            Assert.IsTrue(regex.IsMatch("aaaa"));
        }

        [Test]
        public void LoadSave()
        {
            string filename = Path.GetTempFileName();

            LatvianTokenizer tokenizer = new LatvianTokenizer();
            tokenizer.Save(filename);

            tokenizer = new LatvianTokenizer(filename);

            Token[] tokens = tokenizer.Tokenize("123 456").ToArray();
            Assert.AreEqual("123", tokens[0].Text);
            Assert.AreEqual("456", tokens[1].Text);
        }
    }
}