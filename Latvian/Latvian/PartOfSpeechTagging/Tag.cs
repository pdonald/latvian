using System;

namespace Latvian.PartOfSpeechTagging
{
    public class Tag : IEquatable<Tag>
    {
        protected readonly int hashCode;

        public Tag(string msd)
        {
            if (msd == null)
                throw new ArgumentNullException("msd");

            Msd = msd;
            hashCode = Msd.GetHashCode();
        }

        public string Msd
        {
            get;
            private set;
        }

        public char PartOfSpeech
        {
            get { return char.ToLower(Msd[0]); }
        }

        public override string ToString()
        {
            return Msd;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object other)
        {
            return Equals(other as Tag);
        }

        public bool Equals(Tag other)
        {
            return other != null && other.Msd == Msd;
        }
    }

    public class TagWithLemma : Tag, IEquatable<TagWithLemma>
    {
        protected readonly int hashCodeWithLemma;

        public TagWithLemma(string msd, string lemma)
            : base(msd)
        {
            if (lemma == null)
                throw new ArgumentNullException("lemma");

            Lemma = lemma;

            hashCodeWithLemma = 27;
            hashCodeWithLemma = (13 * hashCodeWithLemma) + base.hashCode;
            hashCodeWithLemma = (13 * hashCodeWithLemma) + Lemma.GetHashCode();
        }

        public string Lemma
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Msd, Lemma);
        }

        public override int GetHashCode()
        {
            return hashCodeWithLemma;
        }

        public override bool Equals(object other)
        {
            return Equals(other as TagWithLemma);
        }

        public bool Equals(TagWithLemma other)
        {
            return base.Equals(other as Tag) && other.Lemma == Lemma;
        }
    }

    public class TagWithInformationalLemma : TagWithLemma, IEquatable<TagWithInformationalLemma>
    {
        public TagWithInformationalLemma(string msd, string lemma)
            : base(msd, lemma)
        {
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object other)
        {
            return Equals(other as TagWithInformationalLemma);
        }

        public bool Equals(TagWithInformationalLemma other)
        {
            return base.Equals(other as Tag);
        }
    }
}
