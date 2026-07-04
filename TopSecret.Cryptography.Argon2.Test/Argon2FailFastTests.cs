namespace TopSecret.Cryptography
{
    using System;
    using System.Text;
    using Xunit;

    /// <summary>
    /// DegreeOfParallelism = 1 is the only value a single-threaded host (browser
    /// WASM, or any process pinned to one logical core) can actually run — see
    /// Argon2Core.Hash's lane-0-inline / Task.Run(1..N-1) split.
    /// </summary>
    public class Argon2FailFastTests
    {
        [Fact]
        public void DegreeOfParallelismOfOne_NeverThrowsRegardlessOfHost()
        {
            using var argon = new Argon2id(Encoding.UTF8.GetBytes("password"))
            {
                Salt = Encoding.UTF8.GetBytes("0123456789abcdef"),
                Iterations = 1,
                MemorySize = 4,
                DegreeOfParallelism = 1,
            };

            var ex = Record.Exception(() => argon.GetBytes(4));
            Assert.Null(ex);
        }

        /// <summary>
        /// Exercises whichever branch of IsSingleThreadedHost this actual test
        /// host qualifies for — asserted either way, rather than silently
        /// skipping the branch the host doesn't happen to match today.
        /// </summary>
        [Fact]
        public void DegreeOfParallelismAboveOne_ThrowsOnlyOnSingleThreadedHost()
        {
            using var argon = new Argon2id(Encoding.UTF8.GetBytes("password"))
            {
                Salt = Encoding.UTF8.GetBytes("0123456789abcdef"),
                Iterations = 1,
                MemorySize = 32,
                DegreeOfParallelism = 2,
            };

            var ex = Record.Exception(() => argon.GetBytes(4));

            if (Environment.ProcessorCount == 1)
            {
                Assert.IsType<PlatformNotSupportedException>(ex);
            }
            else
            {
                Assert.Null(ex);
            }
        }
    }
}
