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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Latvian.Tokenization
{
    using Automata;
    using Readers;
    using Tokens;

    using DFA = Automata.DeterministicFiniteAutomaton;
    using NFA = Automata.NondeterministicFiniteAutomaton;

    // todo: support string patterns
    public class AutomatonTokenizer : ITokenizer, IList<Type>
    {
        private List<Type> patterns = new List<Type>();
        private Dictionary<int, Func<Token>> activators = new Dictionary<int, Func<Token>>();
        private DFA dfa;
        private bool compiled = false;

        #region Load/Save
        public void Load(string filename)
        {
            patterns = new List<Type>();

            //using (Stream stream = new GZipStream(new FileStream(filename, FileMode.Open, FileAccess.Read), CompressionMode.Decompress))
            using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(stream))
                    Load(reader);
        }

        public void Save(string filename)
        {
            BuildAutomaton();

            //using (Stream stream = new GZipStream(new FileStream(filename, FileMode.Create, FileAccess.Write), CompressionLevel.Optimal))
            using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(stream))
                    Save(writer);
        }

        protected virtual void Load(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                string typeName = reader.ReadString();
                Type type = Type.GetType(typeName);
                Add(type);
            }

            dfa = new DFA();
            dfa.Load(reader.BaseStream);
        }

        protected virtual void Save(BinaryWriter writer)
        {
            writer.Write(patterns.Count);
            foreach (Type type in patterns)
                writer.Write(type.FullName);

            dfa.Save(writer.BaseStream);
        }
        #endregion

        #region Compile
        protected void BuildAutomaton()
        {
            if (compiled)
                return;

            activators = new Dictionary<int, Func<Token>>();
            for (int i = 0; i < patterns.Count; i++)
                if (patterns[i] != null)
                    activators[i] = GetActivator(patterns[i]);
            
            NFA nfa = NFA.Empty();

            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i] == null)
                    continue;

                IHasPattern token = (IHasPattern)activators[i]();
                string pattern = token.Pattern;
                if (pattern != null)
                {
                    NFA n = new RegularExpression(pattern).ToNfa();
                    n.Exit.Values = new[] { i };
                    nfa = NFA.Or(nfa, n);
                }
            }

            dfa = nfa.ToDfa();
            dfa.Minimize();

            compiled = true;
        }

        private static Func<Token> GetActivator(Type type)
        {
            ConstructorInfo ctor = type.GetConstructors().First();
            NewExpression newExp = Expression.New(ctor);
            LambdaExpression lambda = Expression.Lambda(typeof(Func<Token>), newExp);
            Func<Token> compiled = (Func<Token>)lambda.Compile();
            return compiled;
        }
        #endregion

        #region Tokenize
        public IEnumerable<Token> Tokenize(string text)
        {
            using (CharReader reader = new StringCharReader(text))
                foreach (Token token in Tokenize(reader))
                    yield return token;
        }

        public IEnumerable<Token> Tokenize(TextReader textReader)
        {
            using (CharReader reader = new TextCharReader(textReader))
                foreach (Token token in Tokenize(reader))
                    yield return token;
        }

        public IEnumerable<Token> Tokenize(IEnumerable<char> text)
        {
            using (CharReader reader = new EnumeratorCharReader(text))
                foreach (Token token in Tokenize(reader))
                    yield return token;
        }

        internal virtual IEnumerable<Token> Tokenize(CharReader reader)
        {
            BuildAutomaton();

            if (dfa.Start == null)
                yield break;

            DFA.State current = dfa.Start;

            int start = 0;
            int end = -1;
            int lineStart = 0;
            int lineEnd = -1;
            int linePosStart = 0;
            int linePosEnd = -1;
            int[] values = null;

            Func<Token> createToken = () =>
            {
                Token token = values != null && values.Length > 0 ? activators[values[0]]() : new Token();
                token.Line = lineStart;
                token.LineEnd = lineEnd;
                token.LinePosition = linePosStart;
                token.LinePositionEnd = linePosEnd;
                token.Position = start;
                token.PositionEnd = end;
                token.Text = reader.Substring(start, end);
                return token;
            };

            while (!reader.IsEnd)
            {
                char c = reader.Read();
                current = current[c];
                
                if (current == null)
                {
                    // reached the end
                    // return the match until prev final state

                    //if (end == -1) throw new Exception("negative end");

                    yield return createToken();

                    // go back to the next char right after the prev final state
                    reader.MoveBack(end); // continue will move +1
                    reader.Release();
                    
                    start = reader.Position;
                    lineStart = reader.Line;
                    linePosStart = reader.LinePosition;
                    end = -1;
                    lineEnd = -1;
                    linePosEnd = -1;
                    values = null;
                    current = dfa.Start;

                    continue;
                }

                if (current.IsFinal)
                {
                    end = reader.Position;
                    lineEnd = reader.Line;
                    linePosEnd = reader.LinePosition;
                    values = current.Values;
                }
            }

            if (end != -1)
            {
                yield return createToken();
                
                if (end != reader.Position)
                {
                    start = end;
                    end = reader.Position;
                    // todo: line
                    yield return createToken();
                }
            }
            else if (start != -1)
            {
                throw new Exception("negative start");
            }
        }
        #endregion

        #region IList<Type>
        public Type this[int index]
        {
            get { return patterns[index]; }
            set { patterns[index] = value; }
        }

        public void Add<T>() where T : Token, IHasPattern, new()
        {
            Add(typeof(T));
        }

        public void Add(Type patternType)
        {
            patterns.Add(patternType);
            compiled = false;
        }

        public void Insert<T>(int index) where T : Token, IHasPattern, new()
        {
            Insert(index, typeof(T));
        }

        public void Insert(int index, Type patternType)
        {
            patterns.Insert(index, patternType);
            compiled = false;
        }

        public void Move<T>(int index) where T : Token, IHasPattern, new()
        {
            Move(index, typeof(T));
        }

        public void Move(int index, Type patternType)
        {
            if (index == Count)
                index--;
            Remove(patternType);
            Insert(index, patternType);
        }

        public bool Remove<T>() where T : Token, IHasPattern, new()
        {
            return Remove(typeof(T));
        }

        public bool Remove(Type patternType)
        {
            if (patterns.Remove(patternType))
            {
                compiled = false;
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            patterns.RemoveAt(index);
        }

        public void Clear()
        {
            if (patterns.Count > 0)
            {
                patterns.Clear();
                compiled = false;
            }
        }

        public bool Contains<T>() where T : Token, IHasPattern, new()
        {
            return Contains(typeof(T));
        }

        public bool Contains(Type patternType)
        {
            return patterns.Contains(patternType);
        }

        public int IndexOf<T>() where T : Token, IHasPattern, new()
        {
            return IndexOf(typeof(T));
        }

        public int IndexOf(Type patternType)
        {
            return patterns.IndexOf(patternType);
        }

        public void CopyTo(Type[] array, int arrayIndex)
        {
            patterns.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return patterns.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<Type> GetEnumerator()
        {
            return patterns.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
