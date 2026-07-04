namespace TopSecret.Cryptography.Test
{
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class LittleEndianActiveStreamTests
    {
        [Fact]
        public void EndsWhereLastByteAvailable()
        {
            LittleEndianActiveStream stream = new LittleEndianActiveStream();

            stream.Expose((ushort)0x4501);
            byte[] buffer = new byte[4];

            Assert.Equal(2, stream.Read(buffer, 0, 4));
            Assert.Equal(0x01, buffer[0]);
            Assert.Equal(0x45, buffer[1]);
        }

        [Fact]
        public void CrossesBoundariesAsExpected()
        {
            LittleEndianActiveStream stream = new LittleEndianActiveStream();

            stream.Expose((uint)0x01010101);
            stream.Expose((uint)0x30303030);
            stream.Expose((uint)0x0a0a0a0a);
            stream.Expose(new byte[] { 0x45, 0x61 });

            byte[] buffer = new byte[5];
            Assert.Equal(5, stream.Read(buffer, 0, 5));
            Assert.Equal(new byte[] { 0x1, 0x1, 0x1, 0x1, 0x30 }, buffer);

            Assert.Equal(5, stream.Read(buffer, 0, 5));
            Assert.Equal(new byte[] { 0x30, 0x30, 0x30, 0x0a, 0x0a }, buffer);

            Assert.Equal(4, stream.Read(buffer, 0, 5));
            Assert.Equal(new byte[] { 0x0a, 0x0a, 0x45, 0x61 }, buffer.Take(4));
        }

        [Fact]
        public void AllocationIsManagedAppropriately()
        {
            LittleEndianActiveStream stream = new LittleEndianActiveStream();

            stream.Expose((uint)0x01010101);
            stream.Expose((uint)0x30303030);
            stream.Expose(new byte[] { 0x45, 0x61, 0xac, 0x4c, 0xf0, 0x00, 0x0b });

            byte[] buffer = new byte[5];
            Assert.Equal(5, stream.Read(buffer, 0, 5));
            Assert.Equal(new byte[] { 0x1, 0x1, 0x1, 0x1, 0x30 }, buffer);

            Assert.Equal(5, stream.Read(buffer, 0, 5));
            Assert.Equal(new byte[] { 0x30, 0x30, 0x30, 0x45, 0x61 }, buffer);

            Assert.Equal(5, stream.Read(buffer, 0, 5));
            Assert.Equal(new byte[] { 0xac, 0x4c, 0xf0, 0x00, 0x0b }, buffer);
        }

        /// <summary>
        /// ReserveBuffer used to grow _buffer via Array.Resize, which
        /// abandons the old (smaller) buffer without clearing it — if it held
        /// sensitive bytes from an earlier Expose() call (e.g. Initialize()
        /// reuses one stream across password, salt, secret, and associated
        /// data in turn), they'd sit unwiped in that discarded array. This
        /// grabs the buffer reference via reflection specifically to check
        /// the abandoned array itself, not just the stream's current state.
        /// </summary>
        [Fact]
        public void ResizingReservedBufferWipesTheAbandonedBuffer()
        {
            var stream = new LittleEndianActiveStream();
            var bufferField = typeof(LittleEndianActiveStream).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);

            stream.Expose(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });
            Assert.Equal(4, stream.Read(new byte[4], 0, 4));

            var firstBuffer = (byte[])bufferField.GetValue(stream);
            Assert.Contains(firstBuffer, b => b != 0);

            // Exposing something bigger forces ReserveBuffer to grow _buffer.
            stream.Expose(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 });
            Assert.Equal(8, stream.Read(new byte[8], 0, 8));

            Assert.All(firstBuffer, b => Assert.Equal(0, b));
        }
    }
}