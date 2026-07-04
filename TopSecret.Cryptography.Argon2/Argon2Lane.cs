namespace TopSecret.Cryptography
{
    using System;
    using System.Runtime.InteropServices;

    internal class Argon2Lane
    {
        public Argon2Lane(int blockCount)
        {
            _memory = new Memory<ulong>(new ulong[128 * blockCount]);
            BlockCount = blockCount;
        }

        public Memory<ulong> this[int index]
        {
            get
            {
                if (index < 0 || index > BlockCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _memory.Slice(128*index, 128);
            }
        }

        public int BlockCount { get; }

        /// <summary>
        /// Zeroes this lane's entire working memory. Every block here holds
        /// memory-hard KDF state derived from the password, salt, and secret
        /// for the whole hash — nothing else clears it once Finalize() is
        /// done extracting the tag, so without this it sits on the managed
        /// heap, unwiped, until the GC eventually reclaims it.
        /// </summary>
        public void Wipe()
        {
            var bytes = MemoryMarshal.Cast<ulong, byte>(_memory.Span);
#if NET5_0_OR_GREATER
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
#else
            bytes.Clear();
#endif
        }

        private readonly Memory<ulong> _memory;
    }
}