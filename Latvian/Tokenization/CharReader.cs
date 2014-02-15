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
using System.IO;
using System.Text;

namespace Latvian.Tokenization.Readers
{
    public class PositionCounter
    {
        public PositionCounter()
        {
        }

        public PositionCounter(PositionCounter other)
        {
            Position = other.Position;
            Line = other.Line;
            LinePosition = other.LinePosition;
        }

        public int Position { get; private set; }
        public int Line { get; private set; }
        public int LinePosition { get; private set; }

        public void Add(char c)
        {
            Position++;
            LinePosition++;

            if (c == '\n')
            {
                Line++;
                LinePosition = 0;
            }
        }

        public void Add(string s)
        {
            foreach (char c in s)
            {
                Add(c);
            }
        }
    }

    abstract class CharReader : IDisposable
    {
        PositionCounter counter = new PositionCounter();
        PositionCounter before = new PositionCounter();

        public PositionCounter PositionCounter { get { return new PositionCounter(counter); } }
        public int Position { get { return counter.Position; } }
        public int Line { get { return counter.Line; } }
        public int LinePosition { get { return counter.LinePosition; } }
        
        public abstract bool IsEnd { get; }
        public abstract char this[int position] { get; }
        public abstract char Peek();
        public abstract string Substring(int start, int end);

        public virtual char Read()
        {
            char c = Peek();
            counter.Add(c);
            return c;
        }

        public virtual void MoveBack(int position)
        {
            counter = new PositionCounter(before);

            while (counter.Position < position)
            {
                counter.Add(this[counter.Position]);
            }
        }

        public virtual void Release()
        {
            before = new PositionCounter(counter);
        }

        public virtual void Reset()
        {
            counter = new PositionCounter();
            before = new PositionCounter();
        }

        public virtual void Dispose()
        {
            counter = null;
            before = null;
        }
    }

    class StringCharReader : CharReader
    {
        string s;

        public StringCharReader(string s)
        {
            this.s = s;
        }

        public override bool IsEnd
        {
            get { return Position >= s.Length; }
        }

        public override char this[int position]
        {
            get { return s[position]; }
        }

        public override char Peek()
        {
            return s[Position];
        }

        public override string Substring(int start, int end)
        {
            return s.Substring(start, end - start);
        }

        public override void Dispose()
        {
            s = null;
            base.Dispose();
        }
    }

    abstract class BufferingCharReader : CharReader
    {
        const int MinReleaseBufferSize = 16 * 1024;

        int sourcePosition = 0;
        bool sourceEnd = false;
        int sourceReleasePosition = 0;

        StringBuilder buffer = new StringBuilder();
        int bufferPosition = 0;
        int bufferStartsAtThisSourcePosition = 0;
        
        protected abstract char? ReadNextFromSource();

        public override bool IsEnd
        {
            get
            {
                if (sourceEnd)
                    return true;

                if (buffer.Length > 0 && bufferPosition < buffer.Length)
                    return false;

                char? next = ReadNextFromSource();
                if (next != null)
                {
                    buffer.Append(next.Value);
                    sourcePosition++;
                    return false;
                }

                sourceEnd = true;
                return true;
            }
        }

        public override char Peek()
        {
            if ((buffer.Length > 0 && bufferPosition < buffer.Length) || !IsEnd)
            {
                return buffer[bufferPosition];
            }

            throw new EndOfStreamException();
        }

        public override char Read()
        {
            char c = base.Read();
            bufferPosition++;
            return c;
        }

        public override char this[int position]
        {
            get { return buffer[position - bufferStartsAtThisSourcePosition]; }
        }

        public override string Substring(int start, int end)
        {
            int bufferStart = start - bufferStartsAtThisSourcePosition;
            int bufferEnd = end - bufferStartsAtThisSourcePosition;

            return buffer.ToString(bufferStart, bufferEnd - bufferStart);
        }

        public override void MoveBack(int position)
        {
            bufferPosition = position - bufferStartsAtThisSourcePosition;
            base.MoveBack(position);
        }

        public override void Release()
        {
            sourceReleasePosition = Position;

            if (bufferPosition >= MinReleaseBufferSize)
            {
                buffer.Remove(0, bufferPosition);
                bufferStartsAtThisSourcePosition += bufferPosition;
                bufferPosition = 0;
            }

            base.Release();
        }

        public override void Reset()
        {
            sourcePosition = 0;
            sourceEnd = false;
            sourceReleasePosition = 0;
            buffer = new StringBuilder();
            bufferPosition = 0;
            bufferStartsAtThisSourcePosition = 0;
            base.Reset();
        }

        public override void Dispose()
        {
            buffer = null;
            base.Dispose();
        }
    }

    class EnumeratorCharReader : BufferingCharReader
    {
        IEnumerator<char> source;

        public EnumeratorCharReader(IEnumerable<char> source)
        {
            this.source = source.GetEnumerator();
        }

        protected override char? ReadNextFromSource()
        {
            if (source.MoveNext())
            {
                return source.Current;
            }

            return null;
        }

        public override void Reset()
        {
            source.Reset();
            base.Reset();
        }

        public override void Dispose()
        {
            source.Dispose();
            source = null;
            base.Dispose();
        }
    }

    class TextReaderCharReader : BufferingCharReader
    {
        TextReader textReader;

        public TextReaderCharReader(TextReader textReader)
        {
            this.textReader = textReader;
        }

        protected override char? ReadNextFromSource()
        {
            int c = textReader.Read();
            
            if (c != -1)
                return (char)c;
            
            return null;
        }

        public override void Reset()
        {
            throw new NotSupportedException();
        }

        public override void Dispose()
        {
            textReader.Dispose();
            textReader = null;
            base.Dispose();
        }
    }
}
