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
    abstract class CharReader : IDisposable
    {
        int position = 0;
        int start = 0;
        int line = 0;
        int linePos = 0;
        int lineBeforeStart = 0;
        int linePosBeforeStart = 0;

        public int Position { get { return position; } }
        public int Line { get { return line; } }
        public int LinePosition { get { return linePos; } }
        
        public abstract bool IsEnd { get; }
        public abstract char ReadNext();
        public abstract char this[int position] { get; }
        public abstract string Substring(int start, int end);

        public virtual char Read()
        {
            char c = ReadNext();
            position++;
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
            this.position = position;
            line = lineBeforeStart;
            linePos = linePosBeforeStart;

            for (int i = start; i < position; i++)
            {
                linePos++;
                if (this[i] == '\n')
                {
                    line++;
                    linePos = 0;
                }
            }
        }

        public virtual void Release()
        {
            start = position;
            lineBeforeStart = line;
            linePosBeforeStart = linePos;
        }

        public virtual void Reset()
        {
            position = 0;
            start = 0;
            line = 0;
            linePos = 0;
            lineBeforeStart = 0;
            linePosBeforeStart = 0;
        }

        public virtual void Dispose()
        {
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

        public override char ReadNext()
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
        private const int MinReleaseBufferSize = 16 * 1024;

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

        public override char ReadNext()
        {
            if ((buffer.Length > 0 && bufferPosition < buffer.Length) || !IsEnd)
            {
                char c = buffer[bufferPosition];
                bufferPosition++;
                return c;
            }

            throw new EndOfStreamException();
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

    class TextCharReader : BufferingCharReader
    {
        TextReader textReader;

        public TextCharReader(TextReader textReader)
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

        public override void Dispose()
        {
            textReader.Dispose();
            textReader = null;
            base.Dispose();
        }

        public override void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
