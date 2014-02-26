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

namespace Latvian.Tagging
{
    public class Tag : IEquatable<Tag>
    {
        private readonly string msd;
        private readonly string lemma;
        private int? hashCode;

        public Tag(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            string[] values = value.Split('/');

            if (values.Length == 0)
                throw new ArgumentException("Value cannot be empty", "value");
            if (values.Length > 2)
                throw new ArgumentException("Value may contain at most one forward slash that separates msd from lemma", "value");
            if (values[0].Length == 0)
                throw new ArgumentException("Morphosyntactic descriptor cannot be empty", "value");

            this.msd = values[0];
            this.lemma = values.Length == 2 ? values[1] : null;
        }

        public Tag(string msd, string lemma)
        {
            if (msd == null)
                throw new ArgumentNullException("msd");
            if (msd.Length == 0)
                throw new ArgumentException("Morphosyntactic descriptor cannot be empty", "msd");

            this.msd = msd;
            this.lemma = lemma;
        }

        public string Value
        {
            get { return Msd + (Lemma != null ? "/" + Lemma : ""); }
        }

        public string Msd
        {
            get { return msd; }
        }

        public string Lemma
        {
            get { return lemma; }
        }

        public char PartOfSpeech
        {
            get { return char.ToLower(Msd[0]); }
        }

        public override string ToString()
        {
            return Value;
        }

        public override int GetHashCode()
        {
            if (hashCode == null)
                hashCode = Helpers.HashCodeGenerator.Create(msd, lemma);
            return hashCode.Value;
        }

        public override bool Equals(object other)
        {
            return Equals(other as Tag);
        }

        public bool Equals(Tag other)
        {
            return other != null && other.msd == msd && other.lemma == lemma;
        }
    }
}
