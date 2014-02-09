using System;
using System.Collections.Generic;
using System.Globalization;

namespace Latvian.Tokenization
{
    public class Token : IEquatable<Token>
    {
        public string Text { get; set; }

        public int Start { get; set; }
        public int End { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public int LinePosStart { get; set; }
        public int LinePosEnd { get; set; }
        public int Length { get { return Text.Length; } }

        public override string ToString()
        {
            return Text;
        }

        public override int GetHashCode()
        {
            return Text != null ? Text.GetHashCode() : 0;
        }

        public override bool Equals(object other)
        {
            return Equals(other as Token);
        }

        public bool Equals(Token other)
        {
            return other != null && other.Text == Text;
        }
    }

    namespace Tokens
    {
        public interface IHasPattern
        {
            string Pattern { get; }
        }

        public abstract class PatternToken : Token, IHasPattern
        {
            protected const string Letter = "a-zA-ZĀ-ž";
            protected const string UppercaseLetter = "A-Z";
            protected const string Digit = "0-9";
            protected const string Whitespace = " \t\r\n";
            protected const string Punctuation = ".,!?:;";
            protected const string Symbol = "\\-~'\"&^#$@*()_";

            public abstract string Pattern { get; }
        }

        public class WordToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[" + Letter + "]+"; }
            }
        }

        public class WhitespaceToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[" + Whitespace + "]+"; }
            }

            public WhitespaceType Type
            {
                get
                {
                    if (Text.Length == 0)
                        throw new ArgumentException();

                    WhitespaceType? prevType = null;

                    foreach (char c in Text)
                    {
                        WhitespaceType type = GetType(c);

                        if (prevType != null && type != prevType)
                            return WhitespaceType.Mixed;

                        prevType = type;
                    }

                    return prevType.Value;
                }
            }

            private WhitespaceType GetType(char c)
            {
                switch (c)
                {
                    case '\t': return WhitespaceType.Tab;
                    case ' ':  return WhitespaceType.Space;
                    case '\n': return WhitespaceType.NewLine;
                    case '\r': return WhitespaceType.NewLine;
                    //case '\u000c': return WhitespaceType.PageBreak;
                    default:   return WhitespaceType.Other;
                }
            }

            public enum WhitespaceType
            {
                Space,
                Tab,
                NewLine,
                //PageBreak,
                Other,
                Mixed
            }
        }

        public class TimeToken : PatternToken
        {
            public override string Pattern
            {
                get { return "(([01].[0-9])|([2].[0-3])).[:].[0-5].[0-9]"; }
            }

            public int Hours
            {
                get { return int.Parse(Text.Split(':')[0]); }
            }

            public int Minutes
            {
                get { return int.Parse(Text.Split(':')[1]); }
            }
        }

        public class TimeSecondsToken : TimeToken
        {
            public const string TimePattern = "(([01].[0-9])|([2].[0-3])).[:].[0-5].[0-9].[:].[0-5].[0-9]";

            public override string Pattern
            {
                get { return TimePattern; }
            }

            public int Seconds
            {
                get { return int.Parse(Text.Split(':')[2]); }
            }
        }

        public class DateToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[0-9].[0-9].[0-9].[0-9].[-.].[0-9].[0-9].[-.].[0-9].[0-9]"; }
            }

            public virtual DateTime DateTime
            {
                get { return DateTime.Parse(Text.Replace(".", "-"), CultureInfo.InvariantCulture); }
            }
        }

        public class IsoDateTimeToken : DateToken
        {
            public override string Pattern
            {
                get { return "[0-9].[0-9].[0-9].[0-9].[-].[0-9].[0-9].[-].[0-9].[0-9].[T]." + TimeSecondsToken.TimePattern; }
            }

            public override DateTime DateTime
            {
                get { return DateTime.Parse(Text, CultureInfo.InvariantCulture); }
            }
        }

        public class PunctuationToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[" + Punctuation + "]"; }
            }
        }

        public class RepeatingPunctuationToken : PunctuationToken
        {
            public override string Pattern
            {
                get { return base.Pattern + "." + base.Pattern + "+"; }
            }
        }

        public class IPAddressToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[0-9]+.[.].[0-9]+.[.].[0-9]+.[.].[0-9]+"; }
            }

            public System.Net.IPAddress IPAddress
            {
                get { return System.Net.IPAddress.Parse(Text); }
            }
        }

        public class EmailToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[a-z0-9.,=_\\-+]+.[@].[a-z0-9]+.[.].[a-z0-9]+"; }
            }

            public string User
            {
                get { return Text.Split('@')[0]; }
            }

            public string Domain
            {
                get { return Text.Split('@')[1]; }
            }
        }

        public abstract class NumberToken : PatternToken
        {
        }

        public class OrdinalNumberToken : NumberToken
        {
            public override string Pattern
            {
                get { return "[0-9]+"; }
            }

            public int Value
            {
                get { return int.Parse(Text, CultureInfo.InvariantCulture); }
            }
        }

        public class CardinalNumberToken : NumberToken
        {
            public override string Pattern
            {
                get { return "[0-9]+.[.]"; }
            }

            public int Value
            {
                get { return int.Parse(Text.Substring(0, Text.Length - 1), CultureInfo.InvariantCulture); }
            }
        }

        public class DecimalNumberToken : NumberToken
        {
            public override string Pattern
            {
                get { return "[0-9]+.[.,].[0-9]+"; }
            }

            public double Value
            {
                get { return double.Parse(Text.Replace(",", "."), CultureInfo.InvariantCulture); }
            }
        }

        public class FractionNumberToken : NumberToken
        {
            public override string Pattern
            {
                get { return "[0-9]+.[/].[0-9]+"; }
            }

            public int Numerator
            {
                get { return int.Parse(Text.Split('/')[0], CultureInfo.InvariantCulture); }
            }

            public int Denominator
            {
                get { return int.Parse(Text.Split('/')[1], CultureInfo.InvariantCulture); }
            }

            public double Value
            {
                get { return (double)Numerator / Denominator; }
            }
        }

        public class ThousandsNumberToken : NumberToken
        {
            public override string Pattern
            {
                get { return "([0-9]+.['].[0-9]+)+"; }
            }

            public int Value
            {
                get { return int.Parse(Text.Replace("'", ""), CultureInfo.InvariantCulture); }
            }
        }

        public class MoneyToken : PatternToken
        {
            public override string Pattern
            {
                get { return "([0-9]+|([0-9]+.[.].[0-9]+)+).[,.].[-]"; }
            }

            public decimal Value
            {
                get
                {
                    string value = Text.Replace(",", ".").Replace(".-", "").Replace("-", "");
                    return decimal.Parse(value, CultureInfo.InvariantCulture);
                }
            }
        }

        public class UrlToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[a-zA-Z]+.[:].[//].[a-zA-Z0-9.,=\\-?!&+/\\()]+"; }
            }

            public Uri Uri
            {
                get { return new Uri(Text, UriKind.Absolute); }
            }
        }

        public class WebsiteToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[w].[w].[w].[.].[a-zA-Z0-9\\-]+.[.].[a-z]+"; }
            }

            public Uri Uri
            {
                get { return new Uri("http://" + Text, UriKind.Absolute); }
            }
        }

        public class SymbolToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[" + PatternToken.Symbol + "]"; }
            }

            public new char Symbol
            {
                get { return Text[0]; }
            }
        }

        public class HypenedWordToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[" + Letter + "].[-–—‒―].[" + Letter + "]+"; }
            }
        }

        public class InitialsToken : PatternToken
        {
            public override string Pattern
            {
                get { return "([" + UppercaseLetter + "].[.])|([D].[zž].[.])"; }
            }
        }

        public class LettersWithSpacesToken : PatternToken
        {
            public override string Pattern
            {
                get { return "([" + Letter + "].[ ])+.[" + Letter + "]"; }
            }

            public string TextWithoutSpaces
            {
                get { return Text.Replace(" ", ""); }
            }
        }

        public class CyrillicToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[\u0400-\u04ff\u0500-\u052f\u2de0-\u2dff\ua640-\ua69f\u1d2b-\u1d78]+"; }
            }
        }

        public class ControlCharsToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[\u0000-\u0009]|[\u000b-\u000c]|[\u000e-\u001f]|[\u007f]|([\u00c2].[\u0080-\u00a0])|([\u00c2].[\u00ad])"; }
            }
        }

        public class ByteOrderMarkToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[\ufeff]|[\ufffe]|([\u00ef].[\u00bb].[\u00bf])"; }
            }
        }

        public class UnknownToken : PatternToken
        {
            public override string Pattern
            {
                get { return "[\u0000-\uffff]"; }
            }
        }
    }
}
