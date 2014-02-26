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
    public class CrossValidation<T> where T : ITrainedTagger
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

        public Results Evaluate()
        {
            Helpers.XorShiftRandom random = new Helpers.XorShiftRandom(RandomSeed);
            List<Sentence> sentences = Randomize ? Sentences.OrderBy(s => random.NextUInt()).ToList() : Sentences;
            
            List<Sentence>[] folds = new List<Sentence>[Folds];
            for (int i = 0; i < Folds; i++)
                folds[i] = new List<Sentence>();

            for (int i = 0; i < sentences.Count; i++)
                folds[i % Folds].Add(sentences[i]);

            Results results = new Results();
            DateTime start = DateTime.Now;

            int iterationsStarted = 0;

            Parallel.For(0, Folds, (i) =>
            {
                Results.Result result = new Results.Result();
                result.Fold = i + 1;
                result.Test = folds[i].Select(sentence => sentence.Clone()).ToList();
                result.Train = new List<Sentence>();

                for (int j = 0; j < Folds; j++)
                    if (j != i)
                        result.Train.AddRange(folds[j].Select(sentence => sentence.Clone()));

                ITrainedTagger tagger = Activator.CreateInstance<T>();
                
                if (tagger is Tagging.Perceptron.PerceptronTagger)
                {
                    ((Tagging.Perceptron.PerceptronTagger)tagger).IterationStarted += (it) =>
                    {
                        lock (folds) iterationsStarted++;
                        System.Diagnostics.Debug.WriteLine(string.Format("Fold {0} iteration {1}, {2:0}% done", i, it, 100 * ((double)iterationsStarted / (Folds * Folds))));
                    };
                }

                tagger.Train(result.Train);
                tagger.Tag(result.Test);

                results.Add(result);
            });

            results.Duration = DateTime.Now - start;

            return results;
        }

        public class Results : List<Results.Result>
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
                get { return new ConfidenceInterval(Mean, MarginOfError(2.58)); }
            }

            public ConfidenceInterval ConfidenceIntervalAt95
            {
                get { return new ConfidenceInterval(Mean, MarginOfError(1.96)); }
            }

            public ConfidenceInterval ConfidenceIntervalAt90
            {
                get { return new ConfidenceInterval(Mean, MarginOfError(1.645)); }
            }

            public double MarginOfError(double z)
            {
                return z * StandardDeviation / Math.Sqrt(this.Count);
            }

            public class ConfidenceInterval
            {
                public ConfidenceInterval(double mean, double marginOfError)
                {
                    Lower = mean - marginOfError;
                    Upper = mean + marginOfError;
                }

                public double Lower { get; private set; }
                public double Upper { get; private set; }

                public override string ToString()
                {
                    return string.Format("{0} - {1}", Lower, Upper);
                }
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
    }
}
