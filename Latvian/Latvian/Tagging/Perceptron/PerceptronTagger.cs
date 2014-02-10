using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Latvian.Tagging.Perceptron
{
    using Features = Perceptron<Tag>.Features;
    using Normalizer = Func<Token, Token>;
    using Unnormalizer = Action<Token>; // todo: rename

    public class PerceptronTagger : ITrainedTagger
    {
        private Perceptron perceptron;
        private XorShiftRandom random;
        private List<FeatureTemplate> featureTemplates;
        private List<Normalizer> normalizers;
        private List<Unnormalizer> unnormalizers;

        public PerceptronTagger()
        {
            random = new XorShiftRandom();
            featureTemplates = new List<FeatureTemplate>();
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
            get { return featureTemplates; }
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
            if (perceptron == null)
                throw new InvalidOperationException("Can't save a model that has not been loaded or trained.");

            // todo: load/save feature templates + other tag classes + other settings + versioning
            using (Stream stream = new GZipStream(new FileStream(filename, FileMode.Create, FileAccess.Write), CompressionLevel.Optimal))
                perceptron.Save(stream);
        }

        public void Load(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");

            perceptron = new Perceptron();

            using (Stream stream = new GZipStream(new FileStream(filename, FileMode.Open, FileAccess.Read), CompressionMode.Decompress))
                perceptron.Load(stream);
        }
        #endregion

        private Features GetFeatures(Token token, IndexedSentence sentence, bool? local = null)
        {
            Features features = new Features(FeatureTemplates.Count);

            foreach (FeatureTemplate template in FeatureTemplates)
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
            perceptron = new Perceptron();

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
                        double? bestScore = null;
                        Tag bestPrediction = null;
                        Features bestFeatures = null;

                        // todo: cache features in the first iteration
                        Features localFeatures = GetFeatures(token, sentence, true);

                        foreach (Tag tag in token.PossibleTags)
                        {
                            token.PredictedTag = tag;

                            Features features = GetFeatures(token, sentence, false);
                            features.AddRange(localFeatures);
                            double score = perceptron.Score(features, tag);

                            if (bestScore == null || score > bestScore.Value)
                            {
                                bestScore = score;
                                bestPrediction = tag;
                                bestFeatures = features;
                            }
                        }

                        token.PredictedTag = token.CorrectTag;

                        perceptron.Update(bestFeatures, token.CorrectTag, bestPrediction);
                    }
                }

                if (IterationFinished != null)
                    IterationFinished(iteration + 1);
            }

            if (Average)
            {
                perceptron.WeightThreshold = WeightThreshold;
                perceptron.AverageWeights();
            }
        }

        public void Tag(IEnumerable<Sentence> sentences)
        {
            if (perceptron == null)
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
                        double? maxScore = null;
                        Tag bestPrediction = null;

                        Features localFeatures = GetFeatures(token, normalizedSentence, true);

                        foreach (Tag tag in token.PossibleTags)
                        {
                            token.PredictedTag = tag;

                            Features features = GetFeatures(token, normalizedSentence, false);
                            features.AddRange(localFeatures);
                            double score = perceptron.Score(features, tag);

                            if (maxScore == null || score > maxScore.Value)
                            {
                                maxScore = score;
                                bestPrediction = tag;
                            }
                        }

                        token.PredictedTag = bestPrediction;
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
