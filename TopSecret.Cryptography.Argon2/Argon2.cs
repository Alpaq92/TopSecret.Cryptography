using System.Diagnostics.CodeAnalysis;

namespace TopSecret.Cryptography
{
    using System;
    using System.Threading.Tasks;
    using System.Security.Cryptography;

    /// <summary>
    /// An implementation of Argon2 https://github.com/P-H-C/phc-winner-argon2
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1819")]  
    public abstract class Argon2 : DeriveBytes
    {
        /// <summary>
        /// Create an Argon2 for encrypting the given password
        /// </summary>
        /// <param name="password"></param>
        public Argon2(byte[] password)
        {
            if (password == null || password.Length == 0)
                throw new ArgumentException("Argon2 needs a password set", nameof(password));

            _password = password;
        }

        /// <summary>
        /// Implementation of Reset
        /// </summary>
        public override void Reset()
        {
        }

        /// <summary>
        /// Implementation of GetBytes
        /// </summary>
        /// <remarks>
        /// Blocks until the hash completes. At <see cref="DegreeOfParallelism"/> = 1
        /// (the only value a single-threaded runtime such as browser WASM can
        /// run) this now completes there too, since the hash never touches the
        /// thread pool in that case. At <see cref="DegreeOfParallelism"/> &gt; 1
        /// on a detected single-threaded host, this throws
        /// <see cref="PlatformNotSupportedException"/> up front instead of
        /// blocking. See the README's "Browser / WebAssembly" section for the
        /// full story.
        /// </remarks>
        public override byte[] GetBytes(int bc)
        {
            return GetBytesAsync(bc).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Implementation of GetBytes
        /// </summary>
        public Task<byte[]> GetBytesAsync(int bc)
        {
            ValidateParameters(bc);
            return GetBytesAsyncImpl(bc);
        }

        /// <summary>
        /// The password hashing salt
        /// </summary>
        public byte[] Salt { get; set; }

        /// <summary>
        /// An optional secret to use while hashing the Password
        /// </summary>
        public byte[] KnownSecret { get; set; }

        /// <summary>
        /// Any extra associated data to use while hashing the password
        /// </summary>
        public byte[] AssociatedData { get; set; }

        /// <summary>
        /// The number of iterations to apply to the password hash
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// The number of 1kB memory blocks to use while proessing the hash
        /// </summary>
        public int MemorySize { get; set; }

        /// <summary>
        /// The number of lanes to use while processing the hash
        /// </summary>
        public int DegreeOfParallelism { get; set; }

        internal abstract Argon2Core BuildCore(int bc);

        private void ValidateParameters(int bc)
        {
            if (bc < 1)
                throw new ArgumentOutOfRangeException(nameof(bc), "Argon2 must generate at least 1 byte");

            if (bc > 1024)
                throw new NotSupportedException("Current implementation of Argon2 only supports generating up to 1024 bytes");

            if (Iterations < 1)
                throw new InvalidOperationException("Cannot perform an Argon2 Hash with out at least 1 iteration");

            if (MemorySize < 4)
                throw new InvalidOperationException("Argon2 requires a minimum of 4kB of memory (MemorySize >= 4)");

            if (DegreeOfParallelism < 1)
                throw new InvalidOperationException("Argon2 requires at least 1 thread (DegreeOfParallelism)");

            if (DegreeOfParallelism > 1 && IsSingleThreadedHost())
                throw new PlatformNotSupportedException(
                    $"{nameof(DegreeOfParallelism)} = {DegreeOfParallelism} requires a host that can run more than " +
                    "one thread. This host (browser WASM, or a process pinned to a single core) can only run " +
                    $"Argon2 with {nameof(DegreeOfParallelism)} = 1 — set it to 1 before hashing here.");
        }

        /// <summary>
        /// True on a runtime that cannot service more than one lane's worth of
        /// concurrent work — browser WASM (always single-threaded today) or a
        /// host pinned to a single logical core. Checked so a <see cref="DegreeOfParallelism"/>
        /// misconfiguration fails fast with an actionable message here, rather
        /// than surfacing later as the runtime's own opaque failure to block a
        /// thread that queued work depends on to make progress.
        /// </summary>
        private static bool IsSingleThreadedHost()
        {
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsBrowser())
                return true;
#endif
            return Environment.ProcessorCount == 1;
        }

        private Task<byte[]> GetBytesAsyncImpl(int bc)
        {
            var n = BuildCore(bc);
            n.Salt = Salt;
            n.Secret = KnownSecret;
            n.AssociatedData = AssociatedData;
            n.Iterations = Iterations;
            n.MemorySize = MemorySize;
            n.DegreeOfParallelism = DegreeOfParallelism;

            return n.Hash(_password);
        }

        private byte[] _password;
    }
}
