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
using System.Threading.Tasks;

namespace Latvian.Tagging.Evaluation
{
    public class CrossValidation<T> where T : ITrainableTagger
    {
        public CrossValidation()
        {
            Sentences = new List<Sentence>();
            Folds = 10;
            Randomize = true;
            RandomSeed = 1;
        }

        public int Folds { get; set; }
        public bool Randomize { get; set; }
        public uint RandomSeed { get; set; }
        public List<Sentence> Sentences { get; private set; }

        public event Action<double> Progress;

        public CrossValidationResults Evaluate()
        {
            Helpers.XorShiftRandom random = new Helpers.XorShiftRandom(RandomSeed);
            List<Sentence> sentences = Randomize ? Sentences.OrderBy(s => random.NextUInt()).ToList() : Sentences;
            
            List<Sentence>[] folds = new List<Sentence>[Folds];
            for (int i = 0; i < Folds; i++)
                folds[i] = new List<Sentence>();

            for (int i = 0; i < sentences.Count; i++)
                folds[i % Folds].Add(sentences[i]);

            CrossValidationResults results = new CrossValidationResults();
            DateTime start = DateTime.Now;

            int iterationsCount = 0;

            Parallel.For(0, Folds, (i) =>
            {
                CrossValidationResults.Result result = new CrossValidationResults.Result();
                result.Fold = i + 1;
                result.Test = folds[i].Select(sentence => sentence.Clone()).ToList();
                result.Train = new List<Sentence>();

                for (int j = 0; j < Folds; j++)
                    if (j != i)
                        result.Train.AddRange(folds[j].Select(sentence => sentence.Clone()));

                ITrainableTagger tagger = Activator.CreateInstance<T>();
                
                if (tagger is Tagging.Perceptron.PerceptronTagger)
                {
                    Tagging.Perceptron.PerceptronTagger ptagger = tagger as Tagging.Perceptron.PerceptronTagger;
                    ptagger.IterationFinished += (it) =>
                    {
                        lock (folds) iterationsCount++;
                        if (Progress != null) 
                            Progress(((double)iterationsCount / (ptagger.Iterations * Folds)));
                    };
                }

                tagger.Train(result.Train);
                tagger.Tag(result.Test);

                results.Add(result);
            });

            results.Duration = DateTime.Now - start;

            return results;
        }
    }

    public class CrossValidationResults : List<CrossValidationResults.Result>
    {
        public TimeSpan Duration
        {
            get;
            set;
        }

        public double Mean
        {
            get { return this.Sum(r => r.CorrectPercentage) / this.Count; }
        }

        public double MeanMsd
        {
            get { return this.Sum(r => r.CorrectMsd) / this.Count; }
        }

        public double MeanLemma
        {
            get { return this.Sum(r => r.CorrectLemma) / this.Count; }
        }

        public double StandardDeviation
        {
            get
            {
                double mean = Mean;
                return Math.Sqrt(this.Sum(r => Math.Pow(r.CorrectPercentage - mean, 2)) / (this.Count - 1));
            }
        }

        public ConfidenceInterval ConfidenceIntervalAt99
        {
            get { return new ConfidenceInterval(0.99, Mean, StandardDeviation, Count); }
        }

        public ConfidenceInterval ConfidenceIntervalAt95
        {
            get { return new ConfidenceInterval(0.95, Mean, StandardDeviation, Count); }
        }

        public ConfidenceInterval ConfidenceIntervalAt90
        {
            get { return new ConfidenceInterval(0.90, Mean, StandardDeviation, Count); }
        }

        public class Result
        {
            public int Fold { get; set; }
            public List<Sentence> Train { get; set; }
            public List<Sentence> Test { get; set; }

            public IEnumerable<Token> WrongTags { get { return Test.SelectMany(t => t).Where(t => !t.IsTagCorrect); } }
            public IEnumerable<Token> WrongMsds { get { return Test.SelectMany(t => t).Where(t => !t.IsMsdCorrect); } }
            public IEnumerable<Token> WrongLemmas { get { return Test.SelectMany(t => t).Where(t => !t.IsLemmaCorrect); } }

            public double CorrectPercentage
            {
                get
                {
                    Token[] tokens = Test.SelectMany(t => t).ToArray();
                    return 100 * ((double)tokens.Count(t => t.IsCorrect) / tokens.Count());
                }
            }

            public double CorrectMsd
            {
                get
                {
                    Token[] tokens = Test.SelectMany(t => t).ToArray();
                    return 100 * ((double)tokens.Count(t => t.IsMsdCorrect) / tokens.Count());
                }
            }

            public double CorrectLemma
            {
                get
                {
                    Token[] tokens = Test.SelectMany(t => t).ToArray();
                    return 100 * ((double)tokens.Count(t => t.IsLemmaCorrect) / tokens.Count());
                }
            }

            public double WrongPercentage
            {
                get { return 100 - CorrectPercentage; }
            }

            public override string ToString()
            {
                return "#" + Fold + " - " + CorrectPercentage;
            }
        }
    }

    public struct ConfidenceInterval
    {
        public readonly double Percentage;
        public readonly double Lower;
        public readonly double Upper;

        public ConfidenceInterval(double p, double mean, double marginOfError)
        {
            Percentage = p;
            Lower = mean - marginOfError;
            Upper = mean + marginOfError;
        }

        public ConfidenceInterval(double p, double mean, double stddev, double count)
            : this(p, mean, MarginOfError(Z(p), stddev, count))
        {
        }

        public ConfidenceInterval(double p, double mean, double stddev, double count, double z)
            : this(p, mean, MarginOfError(z, stddev, count))
        {
        }

        public ConfidenceInterval(double p, IEnumerable<double> percentages)
        {
            double sum = percentages.Sum();
            double count = percentages.Count();
            double mean = sum / count;
            double stddev = Math.Sqrt(percentages.Sum(r => Math.Pow(r - mean, 2)) / (count - 1));;
            double marginOfError = MarginOfError(Z(p), stddev, count);
            Percentage = p;
            Lower = mean - marginOfError;
            Upper = mean + marginOfError;
        }

        public static double Z(double p)
        {
            if (p == 0.99) return 2.58;
            if (p == 0.95) return 1.96;
            if (p == 0.90) return 1.645;

            throw new NotImplementedException();
        }

        public static double MarginOfError(double z, double stddev, double count)
        {
            return z * stddev / Math.Sqrt(count);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Lower, Upper);
        }
    }
}
