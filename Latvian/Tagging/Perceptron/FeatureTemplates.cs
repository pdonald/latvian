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

namespace Latvian.Tagging.Perceptron
{
    public abstract class FeatureTemplate
    {
        public abstract string Name { get; }
        public virtual bool IsLocal { get { return false; } }
        public abstract string GetValue(Token token, IndexedSentence sentence);

        public override string ToString()
        {
            return Name;
        }
    }

    namespace FeatureTemplates
    {
        #region Features
        public class At : FeatureTemplate
        {
            private readonly string name;
            private readonly int offset;
            private readonly ITransformer transformer;

            public At(int positionOffset, ITransformer transformer)
            {
                this.offset = positionOffset;
                this.transformer = transformer;
                this.name = string.Format("{0} {1}", transformer.Name,
                    (offset == 0 ? "" : (offset > 0 ? "+" : "-") + Math.Abs(offset))).Trim();
            }

            public override string Name
            {
                get { return name; }
            }

            public override bool IsLocal
            {
                get { return offset == 0; }
            }

            public override string GetValue(Token token, IndexedSentence sentence)
            {
                int currentTokenPosition = sentence[token];
                int position = currentTokenPosition + offset;

                if (position >= 0 && position < sentence.Count)
                    return transformer.Transform(sentence[position]);

                return null;
            }
        }

        public class Current : FeatureTemplate
        {
            private readonly string name;
            private readonly ITransformer transformer;

            public Current(ITransformer transformer)
            {
                this.name = "Token " + transformer.Name;
                this.transformer = transformer;
            }

            public override bool IsLocal
            {
                get { return true; }
            }

            public override string Name
            {
                get { return name; }
            }

            public override string GetValue(Token token, IndexedSentence sentence)
            {
                return transformer.Transform(token);
            }
        }

        public class Prev : At
        {
            public Prev(ITransformer transformer)
                : this(1, transformer)
            {
            }

            public Prev(int pos, ITransformer transformer)
                : base(-Math.Abs(pos), transformer)
            {
                if (pos == 0)
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class Next : At
        {
            public Next(ITransformer transformer)
                : this(1, transformer)
            {
            }

            public Next(int pos, ITransformer transformer)
                : base(pos, transformer)
            {
                if (pos <= 0)
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class Multiple : FeatureTemplate
        {
            private const string nameDelimiter = ", ";
            private const string valueDelimiter = "/";
            private readonly string name;
            private readonly FeatureTemplate[] features;
            private readonly bool isLocal;

            public Multiple(params FeatureTemplate[] features)
                : this((IEnumerable<FeatureTemplate>)features)
            {
            }

            public Multiple(IEnumerable<FeatureTemplate> features)
            {
                this.features = features.ToArray();
                this.name = string.Join(nameDelimiter, features.Select(f => f.Name));
                this.isLocal = features.All(f => f.IsLocal);
            }

            public override string Name
            {
                get { return name; }
            }

            public override bool IsLocal
            {
                get { return isLocal; }
            }

            public override string GetValue(Token token, IndexedSentence sentence)
            {
                string[] values = new string[features.Length];

                for (int i = 0; i < features.Length; i++)
                    values[i] = features[i].GetValue(token, sentence);

                return string.Join(valueDelimiter, values);
            }
        }

        public class CustomName : FeatureTemplate
        {
            private readonly string name;
            private readonly FeatureTemplate template;

            public CustomName(string name, FeatureTemplate template)
            {
                this.name = name;
                this.template = template;
            }

            public override bool IsLocal
            {
                get { return template.IsLocal; }
            }

            public override string Name
            {
                get { return name; }
            }

            public override string GetValue(Token token, IndexedSentence sentence)
            {
                return template.GetValue(token, sentence);
            }
        }

        public class PrevNoun : FeatureTemplate
        {
            public override string Name
            {
                get { return "Prev noun"; }
            }

            public override string GetValue(Token token, IndexedSentence sentence)
            {
                int position = sentence[token];

                for (int i = position - 1; i >= 0; i--)
                {
                    if ((sentence[i].PredictedTag.PartOfSpeech == 'n' || sentence[i].PredictedTag.PartOfSpeech == 'p') && (sentence[i].PredictedTag.Msd[4] != 'g' && sentence[i].PredictedTag.Msd[4] != 'l'))
                    {
                        return sentence[i].PredictedTag.Msd;
                    }
                }

                return null;
            }
        }

        public class PrevNoun2 : FeatureTemplate
        {
            public override string Name
            {
                get { return "Prev noun2"; }
            }

            public override string GetValue(Token token, IndexedSentence sentence)
            {
                int position = sentence[token];

                for (int i = position - 1, prevPos = -1; i >= 0; i--)
                {
                    if (sentence[i].PredictedTag.PartOfSpeech == 'n')
                    {
                        prevPos = i;
                    }
                    else
                    {
                        if (prevPos != -1)
                        {
                            return sentence[prevPos].PredictedTag.Msd;
                        }
                    }
                }

                return null;
            }
        }
        #endregion

        #region Transformers
        public interface ITransformer // todo: rename
        {
            string Name { get; }
            string Transform(Token token);
        }

        public abstract class BooleanTransformer : ITransformer
        {
            public abstract string Name { get; }
            public abstract bool IsMatch(Token token);

            public string Transform(Token token)
            {
                return IsMatch(token) ? "yes" : "no";
            }
        }

        public class TokenText : ITransformer
        {
            public string Name
            {
                get { return "Token"; }
            }

            public string Transform(Token token)
            {
                return token.Text;
            }
        }

        public class Msd : ITransformer
        {
            public string Name
            {
                get { return "Msd"; }
            }

            public string Transform(Token token)
            {
                return token.PredictedTag.Msd;
            }
        }

        public class Pos : ITransformer
        {
            public string Name
            {
                get { return "Pos"; }
            }

            public string Transform(Token token)
            {
                return token.PredictedTag.PartOfSpeech.ToString();
            }
        }

        public class Lemma : ITransformer
        {
            public string Name
            {
                get { return "Lemma"; }
            }

            public string Transform(Token token)
            {
                TagWithLemma lemmaTag = token.PredictedTag as TagWithLemma;

                if (lemmaTag != null)
                    return lemmaTag.Lemma;

                return null;
            }
        }

        public class TokenPrefix : ITransformer
        {
            private readonly string name;
            private readonly int length;

            public TokenPrefix(int length)
            {
                this.name = "TokenPrefix-" + length;
                this.length = length;
            }

            public string Name
            {
                get { return name; }
            }

            public string Transform(Token token)
            {
                if (token.Text.Length >= length)
                    return token.Text.Substring(0, length); // todo: max(token.length, length)

                return null;
            }
        }

        public class TokenSuffix : ITransformer
        {
            private readonly string name;
            private readonly int length;

            public TokenSuffix(int length)
            {
                this.name = "TokenPrefix-" + length;
                this.length = length;
            }

            public string Name
            {
                get { return name; }
            }

            public string Transform(Token token)
            {
                if (token.Text.Length >= length)
                    return token.Text.Substring(token.Text.Length - length);

                return null;
            }
        }

        public class SubMsd : ITransformer
        {
            private readonly string name;
            private readonly int length;

            public SubMsd(int length)
            {
                this.name = "SubTag-" + length;
                this.length = length;
            }

            public string Name
            {
                get { return name; }
            }

            public string Transform(Token token)
            {
                if (token.PredictedTag.Msd.Length >= length)
                    return token.PredictedTag.Msd.Substring(0, length);

                return null;
            }
        }

        public class Capitalized : BooleanTransformer
        {
            public override string Name
            {
                get { return "Is Capitalized?"; }
            }

            public override bool IsMatch(Token token)
            {
                return token.TextTrueCase.Length > 0 && char.IsUpper(token.TextTrueCase[0]);
            }
        }
        #endregion
    }
}
