namespace Latvian.Tagging.Perceptron
{
    // http://www.jstatsoft.org/v08/i14/paper
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
