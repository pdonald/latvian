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

using Latvian.Tokenization;
using Latvian.Tokenization.Readers;

using NUnit.Framework;

namespace Latvian.Tests.Tokenization
{
    [TestFixture]
    public class PositionCounterTests
    {
        [Test]
        public void Test()
        {
            string[] strings = new[] { "es", " ", "\n", "eju" };
            List<Token> tokens = new List<Token>();
            PositionCounter feeder = new PositionCounter();

            foreach (string s in strings)
            {
                Token token = new Token();
                token.Text = s;
                
                token.Position = feeder.Position;
                token.Line = feeder.Line;
                token.LinePosition = feeder.LinePosition;
                feeder.Add(s);
                token.PositionEnd = feeder.Position;
                token.LineEnd = feeder.Line;
                token.LinePositionEnd = feeder.LinePosition;

                tokens.Add(token);
            }

            Assert.AreEqual(4, tokens.Count);

            Assert.AreEqual(0, tokens[0].Position);
            Assert.AreEqual(2, tokens[0].PositionEnd);
            Assert.AreEqual(0, tokens[0].Line);
            Assert.AreEqual(0, tokens[0].LineEnd);
            Assert.AreEqual(0, tokens[0].LinePosition);
            Assert.AreEqual(2, tokens[0].LinePositionEnd);

            Assert.AreEqual(2, tokens[1].Position);
            Assert.AreEqual(3, tokens[1].PositionEnd);
            Assert.AreEqual(0, tokens[1].Line);
            Assert.AreEqual(0, tokens[1].LineEnd);
            Assert.AreEqual(2, tokens[1].LinePosition);
            Assert.AreEqual(3, tokens[1].LinePositionEnd);

            Assert.AreEqual(3, tokens[2].Position);
            Assert.AreEqual(4, tokens[2].PositionEnd);
            Assert.AreEqual(0, tokens[2].Line);
            Assert.AreEqual(1, tokens[2].LineEnd);
            Assert.AreEqual(3, tokens[2].LinePosition);
            Assert.AreEqual(0, tokens[2].LinePositionEnd);

            Assert.AreEqual(4, tokens[3].Position);
            Assert.AreEqual(7, tokens[3].PositionEnd);
            Assert.AreEqual(1, tokens[3].Line);
            Assert.AreEqual(1, tokens[3].LineEnd);
            Assert.AreEqual(0, tokens[3].LinePosition);
            Assert.AreEqual(3, tokens[3].LinePositionEnd);
        }
    }
}
