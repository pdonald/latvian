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

using System.Collections.Generic;
using System.Linq;

namespace Latvian.Tagging
{
    using Perceptron;
    using Perceptron.FeatureTemplates;

    public class LatvianTagger : PerceptronTagger
    {
        public LatvianTagger()
        {
            Reverse = true;
            Average = true;
            Iterations = 8;
            WeightThreshold = 0.1;

            FeatureTemplates.Add(new Current(new TokenText()));
            FeatureTemplates.Add(new Next(new TokenText()));
            FeatureTemplates.Add(new Prev(new TokenText()));
            FeatureTemplates.Add(new Prev(new Pos()));
            FeatureTemplates.Add(new Prev(new Msd()));
            
            FeatureTemplates.Add(new Multiple(new Prev(1, new Msd()), new Prev(2, new Msd())));
            FeatureTemplates.Add(new Multiple(new Prev(1, new TokenText()), new Next(1, new TokenText())));
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
