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
using System.Linq;

namespace Latvian.Tagging
{
    public class Token
    {
        private int hashCode;

        public Token(string token, IEnumerable<Tag> possibleTags)
        {
            if (token == null)
                throw new ArgumentNullException("token");
            if (possibleTags == null)
                throw new ArgumentNullException("possibleTags");
            if (possibleTags.Count() == 0)
                throw new ArgumentException("possibleTags must contain at least one tag");

            Text = token.ToLower();
            TextTrueCase = token;
            PossibleTags = possibleTags.Distinct().OrderBy(t => t.Msd).ToArray();
        }

        public Token(string token, IEnumerable<Tag> possibleTags, Tag correctTag, Sentence sentence)
            : this(token, possibleTags)
        {
            if (correctTag != null && !possibleTags.Any(t => t.Equals(correctTag)))
                throw new ArgumentOutOfRangeException("possibleTags must contain correctTag");

            CorrectTag = correctTag;
            Sentence = sentence;
        }

        public Token(Token token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            Text = token.Text;
            TextTrueCase = token.TextTrueCase;
            CorrectTag = token.CorrectTag;
            PossibleTags = token.PossibleTags;
            PredictedTag = token.PredictedTag;
            Sentence = token.Sentence;
        }

        public string Text { get; set; }
        public string TextTrueCase { get; set; }
        public Tag CorrectTag { get; set; }
        public Tag[] PossibleTags { get; set; }
        public Tag PredictedTag { get; set; }
        public Sentence Sentence { get; set; }

        public bool IsPosCorrect
        {
            get { return PredictedTag != null && CorrectTag != null && PredictedTag.PartOfSpeech == CorrectTag.PartOfSpeech; }
        }

        public bool IsTagCorrect
        {
            get { return PredictedTag != null && CorrectTag != null && PredictedTag.Equals(CorrectTag); }
        }

        public override string ToString()
        {
            if (PossibleTags != null)
            {
                return string.Format("{0} [{1}: {2}]", TextTrueCase, CorrectTag,
                    string.Join(", ", PossibleTags.Select(t => t.ToString())));
            }
            else
            {
                return string.Format("{0} [{1}]", TextTrueCase, CorrectTag);
            }
        }

        public override int GetHashCode()
        {
            if (hashCode == 0)
            {
                hashCode = 27;
                hashCode = (13 * hashCode) + TextTrueCase.GetHashCode();
                foreach (Tag tag in PossibleTags)
                    hashCode = (13 * hashCode) + tag.GetHashCode();
            }

            return hashCode;
        }

        public override bool Equals(object other)
        {
            return other != null && object.ReferenceEquals(this, other);
        }
    }

    public class Sentence : List<Token>
    {
        public Sentence()
        {
        }

        public Sentence(IEnumerable<Token> tokens)
            : base(tokens)
        {
        }
    }
}
