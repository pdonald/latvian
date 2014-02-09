using System.Collections.Generic;
using System.Linq;

namespace Latvian.PartOfSpeechTagging
{
    using Perceptron;
    using Perceptron.FeatureTemplates;

    public class LatvianTagger : PerceptronTagger
    {
        public LatvianTagger()
            : base()
        {
            Reverse = true;
            Average = true;
            Iterations = 8;
            WeightThreshold = 0.1;

            FeatureTemplates.Add(new Current(new TokenText()));
            FeatureTemplates.Add(new Next(1, new TokenText()));
            FeatureTemplates.Add(new Prev(1, new TokenText()));
            FeatureTemplates.Add(new Multiple(new Prev(1, new TokenText()), new Next(1, new TokenText())));
            FeatureTemplates.Add(new Prev(1, new Pos()));
            FeatureTemplates.Add(new Prev(1, new Msd()));
            FeatureTemplates.Add(new Multiple(new Prev(1, new Msd()), new Prev(2, new Msd())));

            FeatureTemplates.Add(new Multiple(new Prev(1, new TokenText()), new Prev(2, new TokenText())));
            FeatureTemplates.Add(new Multiple(new Next(1, new TokenText()), new Next(2, new TokenText())));

            for (int i = 1; i <= 4; i++)
                FeatureTemplates.Add(new Current(new TokenSuffix(i)));

            // todo: for lumii
            FeatureTemplates.Add(new PrevNoun());
            FeatureTemplates.Add(new PrevNoun2());
        }
    }
}
