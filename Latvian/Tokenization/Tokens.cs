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
using System.Globalization;
using System.Linq;

namespace Latvian.Tokenization
{
    public class Token : IEquatable<Token>
    {
        public string Text { get; set; }

        public int Position { get; set; }
        public int PositionEnd { get; set; }
        public int Line { get; set; }
        public int LineEnd { get; set; }
        public int LinePosition { get; set; }
        public int LinePositionEnd { get; set; }
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
            protected const string UppercaseLetter = "A-ZĀČĒĢĪĶĻŅŌŖŠŪŽ";
            protected const string Digit = "0-9";
            protected const string Whitespace = " \t\r\n";
            protected const string Punctuation = ".,!?:;";
            protected const string Symbol = "\\-~&^#$@*()_";
            protected const string DoubleQuotes = "\"„”“”";
            protected const string SingleQuotes = "'";
            protected const string Quotes = DoubleQuotes + SingleQuotes;

            public abstract string Pattern { get; }

            protected static string FormatExpression(string s)
            {
                return "[" + string.Join("].[", s.Select(c => char.ToLower(c) != char.ToUpper(c) ? char.ToLower(c).ToString() + char.ToUpper(c).ToString() : c.ToString())) + "]";
            }

            protected static string JoinExpressions(IEnumerable<string> expressions)
            {
                return "(" + string.Join(")|(", expressions) + ")";
            }
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
                get { return "([0-9]+|([0-9]+.[.].[0-9]+)+).[,].[-]"; }
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

        public class NumberingToken : PatternToken
        {
            public override string Pattern
            {
                // 1.3a.
                get { return @"([0-9].[.].[0-9].[a-zA-Z].[.])"; }
            }
        }

        public class UrlToken : PatternToken
        {
            public override string Pattern
            {
                get { return @"[a-zA-Z]+.[:].[//].[//].[-a-zA-Z0-9.,_=?!:&+/\()]*.[-a-zA-Z0-9_=?:&+/\]"; }
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

        public class DomainToken : PatternToken
        {
            private static readonly string[] domains = new[] { "lv", "com", "net", "org", "co.uk" };

            public override string Pattern
            {
                get { return "[a-zA-Z0-9\\-]+.[.].(" + JoinExpressions(domains.Select(d => FormatExpression(d))) + ")+"; }
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
                get
                {
                    string initials = "([" + UppercaseLetter + "])|([D].[zž])|([K].[r])";
                    return "(" + initials + ")" + ".[.]";
                }
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

        public class LettersWithPeriodsToken : PatternToken
        {
            public override string Pattern
            {
                get { return "([" + Letter + "].[.])+.[" + Letter + "].[.]"; }
            }

            public string TextWithoutPeriods
            {
                get { return Text.Replace(".", ""); }
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

        public abstract class QuotesToken : PatternToken
        {
        }

        public class SingleQuotesToken : QuotesToken
        {
            public override string Pattern
            {
                get { return "[" + SingleQuotes + "]"; }
            }
        }

        public class DoubleQuotesToken : QuotesToken
        {
            public override string Pattern
            {
                get { return "[" + DoubleQuotes + "]"; }
            }
        }
    }
}
