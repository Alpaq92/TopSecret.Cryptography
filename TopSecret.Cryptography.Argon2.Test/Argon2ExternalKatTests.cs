namespace TopSecret.Cryptography
{
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Known-answer tests against INDEPENDENT external references, not this
    /// library's own historical output. These exist specifically to catch a
    /// class of bug that self-referential vectors cannot: a change to lane
    /// dispatch/execution order (e.g. running lane 0 inline instead of via
    /// Task.Run) that silently reorders Argon2's memory-hard block
    /// processing and produces a different-but-plausible-looking hash.
    /// </summary>
    public class Argon2ExternalKatTests
    {
        /// <summary>
        /// RFC 9106 Section 5.3 official Argon2id test vector — the
        /// standardized reference implementation's own worked example.
        /// Every byte here was re-read directly from the RFC's raw text
        /// (not a summary or a secondary source) to avoid transcription
        /// error in a value whose entire point is being an external answer
        /// key. Uses DegreeOfParallelism = 4 ("Parallelism: 4 lanes" in the
        /// RFC), so this exercises the inline-lane-0 + Task.Run(1..3) path,
        /// not just the DegreeOfParallelism = 1 case.
        /// </summary>
        [Fact]
        public async Task Rfc9106_Argon2id_OfficialTestVector()
        {
            var password = Enumerable.Repeat((byte)0x01, 32).ToArray();
            var salt = Enumerable.Repeat((byte)0x02, 16).ToArray();
            var secret = Enumerable.Repeat((byte)0x03, 8).ToArray();
            var associatedData = Enumerable.Repeat((byte)0x04, 12).ToArray();

            var expectedTag = new byte[]
            {
                0x0d, 0x64, 0x0d, 0xf5, 0x8d, 0x78, 0x76, 0x6c,
                0x08, 0xc0, 0x37, 0xa3, 0x4a, 0x8b, 0x53, 0xc9,
                0xd0, 0x1e, 0xf0, 0x45, 0x2d, 0x75, 0xb6, 0x5e,
                0xb5, 0x25, 0x20, 0xe9, 0x6b, 0x01, 0xe6, 0x59,
            };

            using var argon = new Argon2id(password)
            {
                Salt = salt,
                KnownSecret = secret,
                AssociatedData = associatedData,
                MemorySize = 32,
                Iterations = 3,
                DegreeOfParallelism = 4,
            };

            var actualTag = await argon.GetBytesAsync(32);
            Assert.Equal(expectedTag, actualTag);
        }

        /// <summary>
        /// Cross-checked against argon2-cffi (Python), which wraps the
        /// official phc-winner-argon2 C reference implementation — an
        /// independent implementation, not this fork's own prior output.
        /// Verified via PythonHarness/main.py's PasswordHasher.verify()
        /// against each PHC-formatted hash this method asserts on.
        ///
        /// DegreeOfParallelism = 1 specifically: the case this fork's WASM
        /// fix targets (the only value TopSecret.ProtectedString uses, and
        /// the one where Hash()/InitializeLanes() must never touch
        /// Task.Run). Three different memory/iteration combinations, to
        /// catch a bug that only shows up at specific block counts or
        /// segment lengths.
        /// </summary>
        [Theory]
        [InlineData(65536, 3, 32, "correct horse battery staple",
            "94C86F541ABDB3D9AABFEA59AA03963549483E9C0B1A79336E76B54CEE6C917E")]
        [InlineData(65536, 1, 32, "p1-minimal-iterations",
            "7181A48DBC0B0D19FED62A19734A04DBCB75234B09BCAE446BACC8E98FE7A3AD")]
        [InlineData(4096, 3, 32, "p1-small-memory",
            "208C2E3421AFFA9B9E3A9376C20E87BFE6C24702BA4B9D124385620414D4F38E")]
        public async Task ExternallyVerified_Argon2id_DegreeOfParallelism1(
            int memoryKb, int iterations, int hashLength, string password, string expectedHashHex)
        {
            var salt = Encoding.UTF8.GetBytes("0123456789abcdef");

            using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                Iterations = iterations,
                MemorySize = memoryKb,
                DegreeOfParallelism = 1,
            };

            var actual = await argon.GetBytesAsync(hashLength);
            Assert.Equal(expectedHashHex, System.Convert.ToHexString(actual));
        }

        /// <summary>
        /// Same external cross-check (argon2-cffi / phc-winner-argon2), at
        /// DegreeOfParallelism values that exercise the "N-1 lanes via
        /// Task.Run" branch with more than one dispatched task, including
        /// an odd count (7) that isn't a power of two.
        /// </summary>
        [Theory]
        [InlineData(2, "p2-control", "4F654F0CB5DA911351084644014D46F1D7CEC2B79F6F3E55CC27A9D585A71E90")]
        [InlineData(4, "p4-control", "03CB4D71984E5B639634BAAE1562F17B2932EA06D5F21B1E0A4D9D3684C4A170")]
        [InlineData(7, "p7-odd-control", "E0809F666D41CF790EE9D0810CBADA831AEBDCED066863370AA1856D08589F85")]
        public async Task ExternallyVerified_Argon2id_MultiLane(int parallelism, string password, string expectedHashHex)
        {
            var salt = Encoding.UTF8.GetBytes("0123456789abcdef");

            using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                Iterations = 3,
                MemorySize = 65536,
                DegreeOfParallelism = parallelism,
            };

            var actual = await argon.GetBytesAsync(32);
            Assert.Equal(expectedHashHex, System.Convert.ToHexString(actual));
        }
    }
}
