using System;
using System.Collections.Generic;
using System.IO;

namespace Latvian.PartOfSpeechTagging.Perceptron
{
    public class Perceptron<T>
    {
        protected Dictionary<T, Dictionary<Feature, Weight>> weights = new Dictionary<T, Dictionary<Feature, Weight>>();
        private int updateCount = 0;

        public double Score(Features features, T tag)
        {
            double score = 0;

            if (!weights.ContainsKey(tag))
                return score;

            Dictionary<Feature, Weight> tagWeights = weights[tag];

            foreach (Feature feature in features)
            {
                if (!tagWeights.ContainsKey(feature))
                    continue;

                score += tagWeights[feature].Value;
            }

            return score;
        }

        public void Update(Features features, T truth, T guess)
        {
            updateCount++;

            if (truth.Equals(guess))
                return;

            foreach (Feature feature in features)
            {
                UpdateFeature(feature, truth, 1.0);

                if (guess != null)
                    UpdateFeature(feature, guess, -1.0);
            }
        }

        private void UpdateFeature(Feature feature, T tag, double value)
        {
            if (!weights.ContainsKey(tag))
                weights[tag] = new Dictionary<Feature, Weight>();
            if (!weights[tag].ContainsKey(feature))
                weights[tag][feature] = new Weight();

            weights[tag][feature].Update(value, updateCount);
        }

        public void AverageWeights()
        {
            foreach (T tag in weights.Keys)
            {
                foreach (Feature feature in weights[tag].Keys)
                {
                    weights[tag][feature].Average(updateCount);
                }
            }

            updateCount = 0;
        }

        protected class Weight
        {
            public double Value { get; set; }
            private double Total { get; set; }
            private int Timestamp { get; set; }

            public void Update(double value, int instance)
            {
                Total += (instance - Timestamp) * Value;
                Timestamp = instance;
                Value += value;
            }

            public void Average(int instances)
            {
                Update(0, instances);

                double avg = Total / instances;

                Value = avg;
                Total = 0;
                Timestamp = 0;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class Feature : IEquatable<Feature>
        {
            private readonly string name;
            private readonly string value;
            private readonly int hashCode;

            public Feature(string value)
                : this(null, value)
            {
            }

            public Feature(string name, string value)
            {
                this.name = name;
                this.value = value;

                hashCode = 27;
                if (name != null) hashCode = (13 * hashCode) + name.GetHashCode();
                if (value != null) hashCode = (13 * hashCode) + value.GetHashCode();
            }

            public string Name { get { return name; } }
            public string Value { get { return value; } }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(Name))
                    return Value;
                return string.Format("{0} = {1}", Name, Value);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(object other)
            {
                return Equals(other as Feature);
            }

            public bool Equals(Feature other)
            {
                return other != null &&
                       other.name == name &&
                       other.value == value;
            }
        }

        public class Features : List<Feature>
        {
            public Features()
                : base()
            {
            }

            public Features(int capacity)
                : base(capacity)
            {
            }

            public bool IsNullValueAllowed
            {
                get;
                set;
            }

            public void Add(string name, string value)
            {
                if (value != null || IsNullValueAllowed)
                    Add(new Feature(name, value));
            }
        }
    }

    public class Perceptron : Perceptron<Tag>
    {
        public double WeightThreshold { get; set; }

        public void Save(Stream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(weights.Count);

                foreach (Tag tag in weights.Keys)
                {
                    writer.Write(tag.Msd); // todo: serialize other types of tags
                    writer.Write(weights[tag].Count);

                    foreach (Feature feature in weights[tag].Keys)
                    {
                        if (Math.Abs(weights[tag][feature].Value) < WeightThreshold)
                            continue;

                        writer.Write(feature.Name);
                        writer.Write(feature.Value);
                        writer.Write(weights[tag][feature].Value);
                    }
                }
            }
        }

        public void Load(Stream stream)
        {
            weights = new Dictionary<Tag, Dictionary<Feature, Weight>>();

            using (BinaryReader reader = new BinaryReader(stream))
            {
                int tagCount = reader.ReadInt32();
                for (int t = 0; t < tagCount; t++)
                {
                    Tag tag = new Tag(reader.ReadString());
                    int featureCount = reader.ReadInt32();
                    for (int f = 0; f < featureCount; f++)
                    {
                        Feature feature = new Feature(reader.ReadString(), reader.ReadString());
                        Weight weight = new Weight { Value = reader.ReadDouble() };
                        
                        if (Math.Abs(weight.Value) < WeightThreshold)
                            continue;

                        weights[tag].Add(feature, weight);
                    }
                }
            }
        }
    }
}
