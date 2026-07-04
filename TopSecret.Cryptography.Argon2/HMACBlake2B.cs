namespace TopSecret.Cryptography
{
    using System;
    using System.Numerics;
    using System.Security.Cryptography;

    /// <summary>
    /// A private, internal-only Blake2b HMAC per RFC-7693, vendored from
    /// TopSecret.Cryptography.Blake2 so Argon2's own block hash
    /// (Initialize() / ModifiedBlake2.Blake2Prime()) doesn't force every
    /// consumer of this package to also pull in the separate Blake2 package —
    /// this is purely an internal implementation detail of Argon2, not part
    /// of its public surface, the way upstream Konscious.Security.Cryptography
    /// keeps Blake2b self-contained inside the same assembly as Argon2.
    /// Trimmed to only the unkeyed constructor Argon2 actually calls; the
    /// public package's keyed constructor and backend-selection test seam
    /// aren't needed here.
    /// </summary>
    internal class HMACBlake2B : HMAC
    {
        public HMACBlake2B(int hashSize)
        {
            HashName = "TopSecret.Cryptography.HMACBlake2B";

            if ((hashSize % 8) > 0)
            {
                throw new ArgumentException("Hash Size must be byte aligned", nameof(hashSize));
            }

            if (hashSize < 8 || hashSize > 512)
            {
                throw new ArgumentException("Hash Size must be between 8 and 512", nameof(hashSize));
            }

            _hashSize = hashSize;
            Key = Array.Empty<byte>();
        }

        public override int HashSize
        {
            get
            {
                return _hashSize;
            }
        }

        public override void Initialize()
        {
            _implementation = CreateImplementation();
            _implementation.Initialize(Key);
        }

        protected override void HashCore(byte[] data, int offset, int size)
        {
            if (_implementation == null)
                Initialize();

            _implementation.Update(data, offset, size);
        }

        protected override byte[] HashFinal()
        {
            return _implementation.Final();
        }

        private Blake2bBase CreateImplementation()
        {
            if (Vector.IsHardwareAccelerated)
                return new Blake2bSimd(_hashSize / 8);

            return new Blake2bNormal(_hashSize / 8);
        }

        private Blake2bBase _implementation;
        private readonly int _hashSize;
    }
}
