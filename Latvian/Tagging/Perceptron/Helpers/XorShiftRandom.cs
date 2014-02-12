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

namespace Latvian.Tagging.Perceptron
{
    // http://www.jstatsoft.org/v08/i14/paper
    // http://ayende.com/blog/165185/big-data-search-sorting-randomness
    class XorShiftRandom
    {
        private const uint Y = 842502087;
        private const uint Z = 3579807591;
        private const uint W = 273326509;
        
        private uint x, y, z, w;

        public XorShiftRandom(uint seed = 1337)
        {
            x = seed;
            y = Y;
            z = Z;
            w = W;
        }

        public uint NextUInt()
        {
            uint t = x ^ (x << 11);
            x = y; y = z; z = w;
            return w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
        }
    }
}
