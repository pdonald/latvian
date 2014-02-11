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
        public int RandomSeed { get; set; }
        public List<Sentence> Sentences { get; private set; }

        public Results Evaluate()
        {
            Random random = new Random(RandomSeed);
            List<Sentence> sentences = Randomize ? Sentences.OrderBy(s => random.Next()).ToList() : Sentences;
            
            List<Sentence>[] folds = new List<Sentence>[Folds];
            for (int i = 0; i < Folds; i++)
                folds[i] = new List<Sentence>();

            for (int i = 0; i < sentences.Count; i++)
                folds[i % Folds].Add(sentences[i]);

            Results results = new Results();
            DateTime start = DateTime.Now;

            int progress = 0;

            Parallel.For(0, Folds, (i) =>
            {
                Results.Result result = new Results.Result();
                result.Fold = i + 1;
                result.Test = folds[i].Select(s => new Sentence(s.Select(t => new Token(t)))).ToList(); // clone
                result.Train = new List<Sentence>();

                for (int j = 0; j < Folds; j++)
                    if (j != i)
                        result.Train.AddRange(folds[j].Select(s => new Sentence(s.Select(t => new Token(t))))); // clone

                ITrainedTagger tagger = Activator.CreateInstance<T>();
                if (tagger is Tagging.Perceptron.PerceptronTagger)
                    ((Tagging.Perceptron.PerceptronTagger)tagger).IterationStarted += (it) => { lock (folds) progress++; System.Diagnostics.Debug.WriteLine(string.Format("Fold {0} iteration {1}, {2:0}% done", i, it, 100 * ((double)progress / (Folds * Folds)))); };
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

                public double CorrectPercentage
                {
                    get
                    {
                        Token[] tokens = Test.SelectMany(t => t).ToArray();
                        return 100 * ((double)tokens.Count(t => t.IsTagCorrect) / tokens.Count());
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
