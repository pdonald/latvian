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
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Latvian.Tagging.Perceptron
{
    using Normalizer = Func<Token, Token>;
    using Unnormalizer = Action<Token>; // todo: rename

    public class PerceptronTagger : ITrainedTagger
    {
        private Perceptron perceptronMsd;
        private Perceptron<string> perceptronLemma;
        private Helpers.XorShiftRandom random;
        private List<FeatureTemplate> featureTemplatesTag;
        private List<FeatureTemplate> featureTemplatesLemma;
        private List<Normalizer> normalizers;
        private List<Unnormalizer> unnormalizers;

        public PerceptronTagger()
        {
            random = new Helpers.XorShiftRandom();
            featureTemplatesTag = new List<FeatureTemplate>();
            featureTemplatesLemma = new List<FeatureTemplate>();
            normalizers = new List<Normalizer>(); // todo: fix new possible clone in Tag()
            unnormalizers = new List<Unnormalizer>();
            
            Iterations = 1;
            Reverse = false;
            Average = false;
            WeightThreshold = 0;
        }

        #region Properties
        public List<FeatureTemplate> FeatureTemplates
        {
            get { return featureTemplatesTag; }
        }

        public List<FeatureTemplate> LemmaFeatureTemplates
        {
            get { return featureTemplatesLemma; }
        }

        public List<Normalizer> Normalizers
        {
            get { return normalizers; }
        }

        public List<Unnormalizer> Unnormalizers
        {
            get { return unnormalizers; }
        }

        public int Iterations
        {
            get;
            set;
        }

        public bool Reverse
        {
            get;
            set;
        }

        public bool Average
        {
            get;
            set;
        }

        public double WeightThreshold
        {
            get;
            set;
        }

        public event Action<int> IterationStarted;
        public event Action<int> IterationFinished;
        #endregion

        #region Load/Save
        public void Save(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (perceptronMsd == null)
                throw new InvalidOperationException("Can't save a model that has not been loaded or trained.");

            // todo: load/save feature templates + other tag classes + other settings + versioning
            using (Stream stream = new GZipStream(new FileStream(filename, FileMode.Create, FileAccess.Write), CompressionLevel.Optimal))
            {
                perceptronMsd.Save(stream);
                // todo: lemma perceptron
            }
        }

        public void Load(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");

            using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                Load(stream);
        }

        protected void Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            perceptronMsd = new Perceptron();

            using (Stream decompressed = new GZipStream(stream, CompressionMode.Decompress))
                perceptronMsd.Load(decompressed);
        }
        #endregion

        private Features GetFeatures(List<FeatureTemplate> list, Token token, IndexedSentence sentence, bool? local = null)
        {
            Features features = new Features(list.Count);

            foreach (FeatureTemplate template in list)
            {
                if (local == null || local == template.IsLocal)
                {
                    features.Add(template.Name, template.GetValue(token, sentence));
                }
            }

            return features;
        }

        private Token Normalize(Token token)
        {
            if (normalizers.Count > 0)
            {
                foreach (Normalizer normalizer in Normalizers)
                    token = normalizer(token);
            }

            return token;
        }

        private void Unnormalize(Token token)
        {
            if (unnormalizers.Count > 0)
            {
                foreach (Unnormalizer unnormalizer in unnormalizers)
                    unnormalizer(token);
            }
        }

        public void Train(IEnumerable<Sentence> sentences)
        {
            perceptronMsd = new Perceptron();
            perceptronLemma = new Perceptron<string>();

            List<IndexedSentence> normalizedSentences =
                sentences.Select(s => new IndexedSentence((Reverse ? (s as IEnumerable<Token>).Reverse() : s).Select(t => Normalize(t)))).ToList();

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                if (IterationStarted != null)
                    IterationStarted(iteration + 1);

                foreach (IndexedSentence sentence in normalizedSentences.OrderBy(s => random.NextUInt()))
                {
                    foreach (Token token in sentence)
                    {
                        if (token.PossibleTags.Length == 1)
                        {
                            token.PredictedTag = token.PossibleTags[0];
                            continue;
                        }

                        double? bestMsdScore = null;
                        Tag bestMsd = null;
                        Features bestMsdFeatures = null;
                        
                        Features localFeaturesTag = GetFeatures(featureTemplatesTag, token, sentence, true);

                        foreach (Tag tag in token.PossibleTags)
                        {
                            Tag tagMsd = new Tag(tag.Msd);
                            token.PredictedTag = tagMsd;

                            Features featuresTag = GetFeatures(featureTemplatesTag, token, sentence, false);
                            featuresTag.AddRange(localFeaturesTag);
                            
                            double score = perceptronMsd.Score(featuresTag, tagMsd);
                            if (bestMsdScore == null || score > bestMsdScore.Value)
                            {
                                bestMsdScore = score;
                                bestMsd = tagMsd;
                                bestMsdFeatures = featuresTag;
                            }
                        }

                        perceptronMsd.Update(bestMsdFeatures, new Tag(token.CorrectTag.Msd), bestMsd);

                        if (token.CorrectTag.Lemma != null)
                        {
                            double? bestLemmaScore = null;
                            string bestLemma = null;
                            Features bestLemmaFeatures = null;

                            token.PredictedTag = new Tag(token.CorrectTag.Msd);
                            Features localFeaturesLemma = GetFeatures(featureTemplatesLemma, token, sentence, true);

                            foreach (Tag tag in token.PossibleTags)
                            {
                                if (tag.Msd != bestMsd.Msd || tag.Lemma == null)
                                    continue;
                                token.PredictedTag = new Tag(token.CorrectTag.Msd, tag.Lemma);
                                Features featuresLemma = GetFeatures(featureTemplatesLemma, token, sentence, false);
                                featuresLemma.AddRange(localFeaturesLemma);
                                double scoreLemma = perceptronLemma.Score(featuresLemma, tag.Lemma);
                                if (bestLemmaScore == null || scoreLemma > bestLemmaScore.Value)
                                {
                                    bestLemmaScore = scoreLemma;
                                    bestLemma = tag.Lemma;
                                    bestLemmaFeatures = featuresLemma;
                                }
                            }

                            perceptronLemma.Update(bestLemmaFeatures, token.CorrectTag.Lemma, bestLemma);
                        }

                        token.PredictedTag = token.CorrectTag;
                    }
                }

                if (IterationFinished != null)
                    IterationFinished(iteration + 1);
            }

            if (Average)
            {
                perceptronMsd.AverageWeights();
                perceptronLemma.AverageWeights();
            }

            perceptronMsd.RemoveInsignificantWeights(WeightThreshold);
            perceptronLemma.RemoveInsignificantWeights(WeightThreshold);
        }

        public void Tag(IEnumerable<Sentence> sentences)
        {
            if (perceptronMsd == null)
                throw new InvalidOperationException("Model not loaded.");

            Parallel.ForEach(sentences, (sentence, index) =>
            {
                IndexedSentence normalizedSentence =
                    new IndexedSentence((Reverse ? (sentence as IEnumerable<Token>).Reverse() : sentence).Select(t => Normalize(t)));

                foreach (Token token in normalizedSentence)
                {
                    if (token.PossibleTags.Length == 1)
                    {
                        token.PredictedTag = token.PossibleTags[0];
                    }
                    else
                    {
                        double? maxScoreMsd = null;
                        Tag bestMsd = null;

                        Features localFeaturesTag = GetFeatures(featureTemplatesTag, token, normalizedSentence, true);

                        foreach (Tag tag in token.PossibleTags)
                        {
                            Tag tagMsd = new Tag(tag.Msd);
                            token.PredictedTag = tagMsd;

                            Features featuresTag = GetFeatures(featureTemplatesTag, token, normalizedSentence, false);
                            featuresTag.AddRange(localFeaturesTag);
                            
                            double scoreMsd = perceptronMsd.Score(featuresTag, tagMsd);
                            if (maxScoreMsd == null || scoreMsd > maxScoreMsd.Value)
                            {
                                maxScoreMsd = scoreMsd;
                                bestMsd = tagMsd;
                            }
                        }

                        double? maxScoreLemma = null;
                        string bestLemma = null;

                        token.PredictedTag = bestMsd;
                        Features localFeaturesLemma = GetFeatures(featureTemplatesLemma, token, normalizedSentence, true);
                        
                        foreach (Tag tag in token.PossibleTags)
                        {
                            if (tag.Msd != bestMsd.Msd || tag.Lemma == null)
                                continue;

                            token.PredictedTag = new Tag(token.CorrectTag.Msd, tag.Lemma);
                            Features featuresLemma = GetFeatures(featureTemplatesLemma, token, normalizedSentence, false);
                            featuresLemma.AddRange(localFeaturesLemma);
                            
                            double scoreLemma = perceptronLemma.Score(featuresLemma, tag.Lemma);
                            if (maxScoreLemma == null || scoreLemma > maxScoreLemma.Value)
                            {
                                maxScoreLemma = scoreLemma;
                                bestLemma = tag.Lemma;
                            }
                        }

                        token.PredictedTag = new Tag(bestMsd.Msd, bestLemma);
                    }

                    int pos = normalizedSentence[token];
                    if (Reverse) pos = normalizedSentence.Count - pos - 1;
                    Token original = sentence[pos];
                    original.PredictedTag = token.PredictedTag;
                    Unnormalize(original);
                }
            });
        }
    }
}
