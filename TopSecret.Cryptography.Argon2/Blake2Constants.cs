namespace TopSecret.Cryptography
{
    // Vendored from TopSecret.Cryptography.Blake2 so this package doesn't need
    // a cross-package dependency just for the HMAC-Blake2b-512 block hash Argon2
    // itself needs (Initialize() / ModifiedBlake2.Blake2Prime()). Kept internal
    // and self-contained — see HMACBlake2B.cs for the full rationale.
    internal static class Blake2Constants
    {
        public static readonly ulong[] IV = {
            0x6a09e667f3bcc908UL,
            0xbb67ae8584caa73bUL,
            0x3c6ef372fe94f82bUL,
            0xa54ff53a5f1d36f1UL,
            0x510e527fade682d1UL,
            0x9b05688c2b3e6c1fUL,
            0x1f83d9abfb41bd6bUL,
            0x5be0cd19137e2179UL
        };

        public static readonly int[][] Sigma = new int[][] {
            new int[] {  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 },
            new int[] { 14, 10,  4,  8,  9, 15, 13,  6,  1, 12,  0,  2, 11,  7,  5,  3 },
            new int[] { 11,  8, 12,  0,  5,  2, 15, 13, 10, 14,  3,  6,  7,  1,  9,  4 },
            new int[] {  7,  9,  3,  1, 13, 12, 11, 14,  2,  6,  5, 10,  4,  0, 15,  8 },
            new int[] {  9,  0,  5,  7,  2,  4, 10, 15, 14,  1, 11, 12,  6,  8,  3, 13 },
            new int[] {  2, 12,  6, 10,  0, 11,  8,  3,  4, 13,  7,  5, 15, 14,  1,  9 },
            new int[] { 12,  5,  1, 15, 14, 13,  4, 10,  0,  7,  6,  3,  9,  2,  8, 11 },
            new int[] { 13, 11,  7, 14, 12,  1,  3,  9,  5,  0, 15,  4,  8,  6,  2, 10 },
            new int[] {  6, 15, 14,  9, 11,  3,  0,  8, 12,  2, 13,  7,  1,  4, 10,  5 },
            new int[] { 10,  2,  8,  4,  7,  6,  1,  5, 15, 11,  9, 14,  3, 12, 13,  0 },
            new int[] {  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 },
            new int[] { 14, 10,  4,  8,  9, 15, 13,  6,  1, 12,  0,  2, 11,  7,  5,  3 },
        };
    }
}
