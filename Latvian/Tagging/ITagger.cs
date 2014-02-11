using System.Collections.Generic;

namespace Latvian.Tagging
{
    public interface ITagger
    {
        void Tag(IEnumerable<Sentence> sentences);
    }

    public interface ITrainedTagger : ITagger
    {
        void Train(IEnumerable<Sentence> sentences);
    }
}
