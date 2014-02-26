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

namespace Latvian.Tagging.Evaluation
{
    class Tester<T> where T : ITrainedTagger
    {
        private Helpers.XorShiftRandom random = new Helpers.XorShiftRandom();
        private List<Sentence> corpus = new List<Sentence>();

        public Tester()
        {
        }

        public Tester(IEnumerable<Sentence> sentences)
        {
            corpus = sentences.ToList();
        }

        public Tester(IEnumerable<IEnumerable<Sentence>> listOfSentences)
        {
            corpus = listOfSentences.SelectMany(s => s).ToList();
        }

        public IEnumerable<Sentence> Corpus { get { return corpus; } }
        public IEnumerable<Token> Tokens { get { return corpus.SelectMany(t => t); } }
        public IEnumerable<Tag> Tags { get { return corpus.SelectMany(t => t).SelectMany(t => t.PossibleTags).Distinct(); } }

        public CrossValidation<T>.Results CrossValidate(int folds = 10)
        {
            CrossValidation<T> xval = new CrossValidation<T>();
            xval.Folds = folds;
            xval.Sentences.AddRange(corpus);
            CrossValidation<T>.Results results = xval.Evaluate();
            return results;
        }

        public TestResult TestSplit(double trainPercentage, bool randomize = false)
        {
            if (trainPercentage > 1)
                trainPercentage /= 100;

            IEnumerable<Sentence> data = corpus;
            if (randomize)
                data = data.OrderBy(x => random.NextUInt()).ToList();

            List<Sentence> train = data.Take((int)(data.Count() * trainPercentage)).ToList();
            List<Sentence> test = data.Except(train).ToList();

            LatvianTagger tagger = new LatvianTagger();
            DateTime trainStart = DateTime.Now;
            tagger.Train(train);
            DateTime trainEnd = DateTime.Now;
            tagger.Tag(test);
            DateTime tagEnd = DateTime.Now;

            List<Token> tokens = test.SelectMany(s => s).ToList();
            double accuracy = 100.0 * tokens.Count(t => t.IsTagCorrect) / tokens.Count;

            TestResult results = new TestResult();
            results.Train = train;
            results.Test = test;
            results.TrainingDuration = trainEnd - trainStart;
            results.TaggingDuration = tagEnd - trainEnd;
            return results;
        }

        public class TestResult
        {
            public TimeSpan TrainingDuration { get; set; }
            public TimeSpan TaggingDuration { get; set; }

            public List<Sentence> Train { get; set; }
            public List<Sentence> Test { get; set; }

            public double TrainingSpeed
            {
                get { return Train.SelectMany(s => s).Count() / TrainingDuration.TotalSeconds; }
            }

            public double TaggingSpeed
            {
                get { return Test.SelectMany(s => s).Count() / TaggingDuration.TotalSeconds; }
            }

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

            public IEnumerable<Token> Incorrect
            {
                get { return Test.SelectMany(s => s).Where(t => !t.IsTagCorrect); }
            }

            public override string ToString()
            {
                return CorrectPercentage.ToString();
            }
        }
    }
}
