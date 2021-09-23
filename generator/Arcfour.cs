using System;

namespace generator
{
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/draft-kaukonen-cipher-arcfour-03
    /// </summary>
    public class Arcfour
    {
        private byte _i;
        private byte _j;
        private readonly byte[] _s;

        public Arcfour(byte[] seed)
        {
            if (seed.Length != 256)
            {
                throw new ArgumentOutOfRangeException(nameof(seed), "error");
            }

            var checker = new bool[256];
            foreach (var x in seed)
            {
                if (checker[x])
                {
                    throw new ArgumentOutOfRangeException(nameof(seed), "error");
                }

                checker[x] = true;
            }

            _i = 0;
            _j = 0;
            _s = new byte[256];
            Buffer.BlockCopy(seed, 0, _s, 0, 256);
        }

        public int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), "error");
            }

            var a = new byte[8];
            a[0] = GetByte();
            a[1] = GetByte();
            a[2] = GetByte();
            a[3] = GetByte();
            a[4] = GetByte();
            a[5] = GetByte();
            a[6] = GetByte();
            a[7] = GetByte();
            return (int)(BitConverter.ToUInt64(a) % (ulong)maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return Next(maxValue - minValue) + minValue;
        }

        public double NextDouble()
        {
            var a = new byte[4];
            a[0] = GetByte();
            a[1] = GetByte();
            a[2] = GetByte();
            a[3] = GetByte();
            return BitConverter.ToUInt32(a) / (uint.MaxValue + 1.0);
        }

        private byte GetByte()
        {
            ++_i;
            _j = (byte)(_j + _s[_i]);
            (_s[_i], _s[_j]) = (_s[_j], _s[_i]);
            return _s[(byte)(_s[_i] + _s[_j])];
        }
    }
}
