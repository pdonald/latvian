using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Latvian.Tokenization.Automata
{
    using Tokens;

    using DFA = DeterministicFiniteAutomaton;
    using NFA = NondeterministicFiniteAutomaton;

    // todo: support string patterns
    public class AutomatonTokenizer : ITokenizer, IList<Type>
    {
        private List<Type> patterns = new List<Type>();
        private Dictionary<int, Func<Token>> activators = new Dictionary<int, Func<Token>>();
        private DFA dfa;
        private bool compiled = false;

        static Func<Token> GetActivator(Type type)
        {
            ConstructorInfo ctor = type.GetConstructors().First();
            NewExpression newExp = Expression.New(ctor);
            LambdaExpression lambda = Expression.Lambda(typeof(Func<Token>), newExp);
            Func<Token> compiled = (Func<Token>)lambda.Compile();
            return compiled;
        }

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
                NFA n = new RegularExpression(token.Pattern).ToNfa();
                n.Exit.Values = new[] { i };
                nfa = NFA.Or(nfa, n);
            }

            dfa = nfa.ToDfa();
            dfa.Minimize();

            compiled = true;
        }

        public IEnumerable<Token> Tokenize(IEnumerable<char> text)
        {
            BuildAutomaton();

            if (dfa.Start == null)
                yield break;

            DFA.State current = dfa.Start;

            TextReader reader = text is string ? (TextReader)new StringReader(text as string) : (TextReader)new StreamingReader(text);
            //TextReader reader = new StreamingReader(text);

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

        #region IList<Type>
        public Type this[int index]
        {
            get { return patterns[index]; }
            set { patterns[index] = value; }
        }

        public void Add<T>()
        {
            Add(typeof(T));
        }

        public void Add(Type patternType)
        {
            patterns.Add(patternType);
            compiled = false;
        }

        public void Insert<T>(int index)
        {
            Insert(index, typeof(T));
        }

        public void Insert(int index, Type patternType)
        {
            patterns.Insert(index, patternType);
            compiled = false;
        }

        public void Move<T>(int index)
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

        public bool Remove<T>()
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

        public bool Contains<T>()
        {
            return Contains(typeof(T));
        }

        public bool Contains(Type patternType)
        {
            return patterns.Contains(patternType);
        }

        public int IndexOf<T>()
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

        #region Readers
        private abstract class TextReader : IDisposable
        {
            int start = 0;
            int line = 0;
            int linePos = 0;
            int lineBeforeStart = 0;
            int linePosBeforeStart = 0;

            public int Line { get { return line; } }
            public int LinePosition { get { return linePos; } }

            public abstract int Position { get; }
            public abstract bool IsEnd { get; }

            public abstract char Read2();
            
            public virtual char Read()
            {
                char c = Read2();
                linePos++;
                if (c == '\n')
                {
                    line++;
                    linePos = 0;
                }
                return c;
            }

            public virtual void MoveBack(int position)
            {
                line = lineBeforeStart;
                linePos = linePosBeforeStart;

                for (int i = start; i < position; i++)
                {
                    linePos++;
                    if (At(i) == '\n')
                    {
                        line++;
                        linePos = 0;
                    }
                }
            }

            public abstract char At(int position);
            public abstract string Substring(int start, int end);
            
            public virtual void Reset()
            {
                start = 0;
                line = 0;
                linePos = 0;
                lineBeforeStart = 0;
                linePosBeforeStart = 0;
            }
            
            public virtual void Release()
            {
                start = Position;
                lineBeforeStart = line;
                linePosBeforeStart = linePos;
            }

            public virtual void Dispose()
            {
            }
        }

        private class StreamingReader : TextReader
        {
            IEnumerator<char> source;
            int sourcePosition = 0;
            bool sourceEnd = false;
            //int sourceReleasePosition = 0;
            StringBuilder buffer = new StringBuilder();
            int bufferPosition = 0;
            int bufferStartsAtThisSourcePosition = 0;
            int minReleaseBufferSize;

            public StreamingReader(IEnumerable<char> source, int minReleaseBufferSize = 16 * 1024)
            {
                this.source = source.GetEnumerator();
                this.minReleaseBufferSize = minReleaseBufferSize;
            }

            public override int Position
            {
                get { return bufferStartsAtThisSourcePosition + bufferPosition; }
            }

            public override bool IsEnd
            {
                get
                {
                    if (sourceEnd)
                        return true;

                    if (buffer.Length > 0 && bufferPosition < buffer.Length)
                        return false;

                    if (source.MoveNext())
                    {
                        buffer.Append(source.Current);
                        sourcePosition++;
                        return false;
                    }

                    sourceEnd = true;
                    return true;
                }
            }

            public override char Read2()
            {
                if ((buffer.Length > 0 && bufferPosition < buffer.Length) || !IsEnd)
                {
                    char c = buffer[bufferPosition];
                    bufferPosition++;
                    return c;
                }

                throw new EndOfStreamException();
            }

            public override void MoveBack(int position)
            {
                bufferPosition = position - bufferStartsAtThisSourcePosition;
                base.MoveBack(position);
            }

            public override char At(int position)
            {
                return buffer[position - bufferStartsAtThisSourcePosition];
            }

            public override string Substring(int start, int end)
            {
                int bufferStart = start - bufferStartsAtThisSourcePosition;
                int bufferEnd = end - bufferStartsAtThisSourcePosition;

                return buffer.ToString(bufferStart, bufferEnd - bufferStart);
            }

            public override void Release()
            {
                base.Release();
                //sourceReleasePosition = Position;
                
                if (bufferPosition >= minReleaseBufferSize)
                {
                    buffer.Remove(0, bufferPosition);
                    bufferStartsAtThisSourcePosition += bufferPosition;
                    bufferPosition = 0;
                }
            }

            public override void Reset()
            {
                base.Reset();

                source.Reset();
                sourcePosition = 0;
                sourceEnd = false;
                buffer = new StringBuilder();
                bufferPosition = 0;
                bufferStartsAtThisSourcePosition = 0;
            }

            public override void Dispose()
            {
                base.Dispose();

                source.Dispose();
                source = null;
                buffer = null;
            }
        }

        private class StringReader : TextReader
        {
            string s;
            int position;
            int length;

            public StringReader(string s)
            {
                this.s = s;
                this.position = 0;
                this.length = s.Length;
            }

            public override int Position
            {
                get { return position; }
            }

            public override bool IsEnd
            {
                get { return position >= length; }
            }

            public override char Read2()
            {
                char c = s[position];
                position++;
                return c;
            }

            public override char At(int position)
            {
                return s[position];
            }

            public override string Substring(int start, int end)
            {
                return s.Substring(start, end - start);
            }

            public override void MoveBack(int position)
            {
                this.position = position;
                base.MoveBack(position);
            }

            public override void Reset()
            {
                position = 0;
                base.Reset();
            }
        }
        #endregion
    }
}
