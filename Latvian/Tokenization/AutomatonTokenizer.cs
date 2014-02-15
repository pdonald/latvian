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

    public class AutomatonTokenizer : ITokenizer, IList<Type>
    {
        private List<Type> tokenTypes = new List<Type>();
        private Dictionary<int, Func<Token>> tokenCreators = new Dictionary<int, Func<Token>>();
        private DFA dfa;
        private bool compiled = false;

        #region Load/Save
        public void Load(string filename)
        {
            tokenTypes = new List<Type>();

            //using (Stream stream = new GZipStream(new FileStream(filename, FileMode.Open, FileAccess.Read), CompressionMode.Decompress))
            using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(stream))
                    Load(reader);
        }

        public void Save(string filename)
        {
            Compile();

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
            writer.Write(tokenTypes.Count);

            foreach (Type type in tokenTypes)
                writer.Write(type.FullName);

            dfa.Save(writer.BaseStream);
        }
        #endregion

        #region Compile
        public void Compile()
        {
            if (compiled)
                return;

            tokenCreators = new Dictionary<int, Func<Token>>();
            for (int i = 0; i < tokenTypes.Count; i++)
                if (tokenTypes[i] != null)
                    tokenCreators[i] = CreatorActivator(tokenTypes[i]);
            
            NFA nfa = NFA.Empty();

            for (int i = 0; i < tokenTypes.Count; i++)
            {
                if (tokenTypes[i] == null)
                    continue;

                IHasPattern token = (IHasPattern)tokenCreators[i]();
                string pattern = token.Pattern;
                if (pattern != null)
                {
                    RegularExpression regex = new RegularExpression(pattern);
                    NFA automaton = regex.ToNfa();
                    automaton.Exit.Values = new[] { i };
                    nfa = NFA.Or(nfa, automaton);
                }
            }

            dfa = nfa.ToDfa();
            dfa.Minimize();

            compiled = true;
        }

        private static Func<Token> CreatorActivator(Type type)
        {
            ConstructorInfo constructor = type.GetConstructors().First();
            NewExpression newExpression = Expression.New(constructor);
            LambdaExpression lambda = Expression.Lambda(typeof(Func<Token>), newExpression);
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
            using (CharReader reader = new TextReaderCharReader(textReader))
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
            Compile();

            if (dfa.Start == null)
                yield break;

            PositionCounter start = new PositionCounter();
            PositionCounter end = null;
            int[] tokenTypeIDs = null;

            DFA.State current = dfa.Start;

            while (!reader.IsEnd)
            {
                char c = reader.Read();
                current = current[c];

                // reached the end, nowhere else to go
                // return the match until the prev final state
                // and go back to the next char right after the prev final state
                if (current == null)
                {
                    yield return CreateToken(reader, start, end, tokenTypeIDs);
                    
                    reader.MoveBack(end.Position);
                    reader.Release();
                    
                    start = reader.PositionCounter;
                    end = null;
                    tokenTypeIDs = null;

                    current = dfa.Start;
                    continue;
                }

                // remember this position in case we need to come back
                if (current.IsFinal)
                {
                    end = reader.PositionCounter;
                    tokenTypeIDs = current.Values;
                }
            }

            if (end != null)
            {
                yield return CreateToken(reader, start, end, tokenTypeIDs);
                
                if (end.Position != reader.Position)
                {
                    yield return CreateToken(reader, end, reader.PositionCounter, null);
                }
            }
            else
            {
                yield return CreateToken(reader, start, reader.PositionCounter, tokenTypeIDs);
            }
        }

        private Token CreateToken(CharReader reader, PositionCounter start, PositionCounter end, int[] tokenTypeIDs)
        {
            Token token = tokenTypeIDs != null && tokenTypeIDs.Length > 0 ? tokenCreators[tokenTypeIDs[0]]() : new Token();
            token.Position = start.Position;
            token.PositionEnd = end.Position;
            token.Line = start.Line;
            token.LineEnd = end.Line;
            token.LinePosition = start.LinePosition;
            token.LinePositionEnd = end.LinePosition;
            token.Text = reader.Substring(start.Position, end.Position);
            return token;
        }
        #endregion

        #region IList<Type>
        public Type this[int index]
        {
            get { return tokenTypes[index]; }
            set { tokenTypes[index] = value; }
        }

        public void Add<T>() where T : Token, IHasPattern, new()
        {
            Add(typeof(T));
        }

        public void Add(Type patternType)
        {
            tokenTypes.Add(patternType);
            compiled = false;
        }

        public void Insert<T>(int index) where T : Token, IHasPattern, new()
        {
            Insert(index, typeof(T));
        }

        public void Insert(int index, Type patternType)
        {
            tokenTypes.Insert(index, patternType);
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
            if (tokenTypes.Remove(patternType))
            {
                compiled = false;
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            tokenTypes.RemoveAt(index);
            compiled = false;
        }

        public void Clear()
        {
            if (tokenTypes.Count > 0)
            {
                tokenTypes.Clear();
                compiled = false;
            }
        }

        public bool Contains<T>() where T : Token, IHasPattern, new()
        {
            return Contains(typeof(T));
        }

        public bool Contains(Type patternType)
        {
            return tokenTypes.Contains(patternType);
        }

        public int IndexOf<T>() where T : Token, IHasPattern, new()
        {
            return IndexOf(typeof(T));
        }

        public int IndexOf(Type patternType)
        {
            return tokenTypes.IndexOf(patternType);
        }

        public void CopyTo(Type[] array, int arrayIndex)
        {
            tokenTypes.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return tokenTypes.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<Type> GetEnumerator()
        {
            return tokenTypes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
