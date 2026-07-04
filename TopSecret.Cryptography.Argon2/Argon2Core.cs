namespace TopSecret.Cryptography
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;



    internal abstract class Argon2Core
    {
        public Argon2Core(int hashSize)
        {
            _tagLine = hashSize;
        }

        public int DegreeOfParallelism { get; set; }

        public int MemorySize { get; set; }

        public int Iterations { get; set; }

        public abstract int Type { get; }

        public byte[] AssociatedData { get; set; }

        public byte[] Salt { get; set; }

        public byte[] Secret { get; set; }

        // private stuff starts here
        internal async Task<byte[]> Hash(byte[] password)
        {
            var lanes = await InitializeLanes(password).ConfigureAwait(false);

            var start = 2;
            for (var i = 0; i < Iterations; ++i)
            {
                for (var s = 0; s < 4; s++)
                {
                    void ProcessLane(int l)
                    {
                        var lane = lanes[l];
                        var segmentLength = lane.BlockCount / 4;
                        var curOffset = s * segmentLength + start;

                        var prevLane = l;
                        var prevOffset = curOffset - 1;
                        if (curOffset == 0)
                        {
                            prevOffset = lane.BlockCount - 1;
                        }

                        var state = GenerateState(lanes, segmentLength, i, l, s);
                        for (var c = start; c < segmentLength; ++c, curOffset++)
                        {
                            var pseudoRand = state.PseudoRand(c, prevLane, prevOffset);
                            var refLane = (uint)(pseudoRand >> 32) % lanes.Length;

                            if (i == 0 && s == 0)
                            {
                                refLane = l;
                            }

                            var refIndex = IndexAlpha(l == refLane, (uint)pseudoRand, lane.BlockCount, segmentLength, i, s, c);
                            var refBlock = lanes[refLane][refIndex].Span;
                            var curBlock = lane[curOffset].Span;

                            Compress(curBlock, refBlock, lanes[prevLane][prevOffset].Span);
                            prevOffset = curOffset;
                        }
                    }

                    // Lane 0 always runs inline, on the calling thread. Lanes
                    // 1..N-1 (if any) are dispatched via Task.Run — .ToArray()
                    // forces that dispatch to happen NOW, before ProcessLane(0)
                    // runs, so on a multi-core host they genuinely run
                    // concurrently with lane 0 rather than starting only after
                    // it finishes (Enumerable.Select is lazy; without eager
                    // materialization here, Task.WhenAll wouldn't enumerate —
                    // and therefore wouldn't queue — the Task.Run calls until
                    // after the inline call below already returned).
                    //
                    // At DegreeOfParallelism == 1 (the only value used on
                    // browser WASM), Enumerable.Range(1, 0) is empty: no
                    // Task.Run ever happens, and awaiting Task.WhenAll of an
                    // empty/already-complete set resolves synchronously
                    // without suspending — so this loop iteration, and by
                    // extension the whole method, never touches the thread
                    // pool and never needs a second thread to make progress.
                    //
                    // Cross-lane reads (IndexAlpha's sameLane=false branch)
                    // only ever address blocks from an already-completed
                    // pass/slice, never the slice currently being written —
                    // so lane execution order within a slice (inline+
                    // concurrent, inline+sequential, or original all-Task.Run)
                    // cannot change the output, only wall-clock time.
                    var rest = Enumerable.Range(1, lanes.Length - 1).Select(l => Task.Run(() => ProcessLane(l))).ToArray();
                    try
                    {
                        ProcessLane(0);
                    }
                    catch
                    {
                        // Lanes 1..N-1 are already dispatched. Observe them
                        // before propagating lane 0's failure, so they can't
                        // outlive this method as orphaned background work
                        // still mutating shared lane memory after the caller
                        // has already seen this call fault.
                        await SwallowAsync(rest).ConfigureAwait(false);
                        throw;
                    }

                    await Task.WhenAll(rest).ConfigureAwait(false);

                    start = 0;
                }
            }

            return Finalize(lanes);
        }

        private static void XorLanes(Argon2Lane[] lanes)
        {
            var data = lanes[0][lanes[0].BlockCount - 1].Span;

            foreach (var lane in lanes.Skip(1))
            {
                var block = lane[lane.BlockCount - 1].Span;

                for (var b = 0; b < 128; ++b)
                {
                    if (!BitConverter.IsLittleEndian)
                    {
                        block[b] = (block[b] >> 56) ^
                            ((block[b] >> 40) & 0xff00UL) ^
                            ((block[b] >> 24) & 0xff0000UL) ^
                            ((block[b] >> 8) & 0xff000000UL) ^
                            ((block[b] << 8) & 0xff00000000UL) ^
                            ((block[b] << 24) & 0xff0000000000UL) ^
                            ((block[b] << 40) & 0xff000000000000UL) ^
                            ((block[b] << 56) & 0xff00000000000000UL);
                    }

                    data[b] ^= block[b];
                }
            }
        }

        private byte[] Finalize(Argon2Lane[] lanes)
        {
            XorLanes(lanes);

            var ds = new LittleEndianActiveStream();
            ds.Expose(lanes[0][lanes[0].BlockCount - 1]);

            ModifiedBlake2.Blake2Prime(lanes[0][1], ds, _tagLine);
            var result = new byte[_tagLine];
            var tmp = MemoryMarshal.Cast<ulong, byte>(lanes[0][1].Span).Slice(0,result.Length);
            tmp.CopyTo(result);

            foreach (var lane in lanes)
            {
                lane.Wipe();
            }

            AfterWipeForTesting?.Invoke(lanes);

            return result;
        }

        /// <summary>
        /// Test-only hook, invoked with the (now-wiped) lanes immediately
        /// after Finalize() zeroes them — lets a test verify the wipe
        /// actually reached the real buffers a genuine Hash() call used,
        /// not just Argon2Lane.Wipe() exercised in isolation. Always null
        /// in production.
        /// </summary>
        internal Action<Argon2Lane[]> AfterWipeForTesting;

        internal unsafe static void Compress(Span<ulong> dest, Span<ulong> refb, Span<ulong> prev)
        {
            var tmpblock = stackalloc ulong[dest.Length];
            for (var n = 0; n < 128; ++n)
            {
                tmpblock[n] = refb[n] ^ prev[n];
                dest[n] ^= tmpblock[n];
            }

            for (var i = 0; i < 8; ++i)
                ModifiedBlake2.DoRoundColumns(tmpblock, i);
            for (var i = 0; i < 8; ++i)
                ModifiedBlake2.DoRoundRows(tmpblock, i);

            for (var n = 0; n < 128; ++n)
                dest[n] ^= tmpblock[n];
        }

        internal abstract IArgon2PseudoRands GenerateState(Argon2Lane[] lanes, int segmentLength, int pass, int lane, int slice);

        internal async Task<Argon2Lane[]> InitializeLanes(byte[] password)
        {
            var blockHash = Initialize(password);

            var lanes = new Argon2Lane[DegreeOfParallelism];

            // adjust memory size if needed so that each segment has
            // an even size
            var segmentLength = MemorySize / (lanes.Length * 4);
            MemorySize = segmentLength * 4 * lanes.Length;
            var blocksPerLane = MemorySize / lanes.Length;

            if (blocksPerLane < 4)
            {
                throw new InvalidOperationException($"Memory should be enough to provide at least 4 blocks per {nameof(DegreeOfParallelism)}");
            }

            // Pre-allocate every lane's storage synchronously, before any
            // initialization work starts — matching the original's guarantee
            // that the `lanes` array is fully populated before any of its
            // elements are touched concurrently.
            for (var i = 0; i < lanes.Length; ++i)
            {
                lanes[i] = new Argon2Lane(blocksPerLane);
            }

            void InitLane(int l)
            {
                var stream0 = new LittleEndianActiveStream();
                stream0.Expose(blockHash);
                stream0.Expose(0);
                stream0.Expose(l);
                ModifiedBlake2.Blake2Prime(lanes[l][0], stream0);

                var stream1 = new LittleEndianActiveStream();
                stream1.Expose(blockHash);
                stream1.Expose(1);
                stream1.Expose(l);
                ModifiedBlake2.Blake2Prime(lanes[l][1], stream1);
            }

            // Same pattern as Hash(): lane 0 runs inline, lanes 1..N-1 (if
            // any) are dispatched via Task.Run with the dispatch forced eager
            // via .ToArray() so they genuinely overlap with the inline lane
            // on a multi-core host. At DegreeOfParallelism == 1 this is an
            // empty array — no Task.Run, no thread-pool touch.
            var rest = Enumerable.Range(1, lanes.Length - 1).Select(l => Task.Run(() => InitLane(l))).ToArray();
            try
            {
                InitLane(0);
            }
            catch
            {
                // See the matching catch in Hash(): don't leave lanes 1..N-1
                // as orphaned background work after this method has faulted.
                await SwallowAsync(rest).ConfigureAwait(false);
                throw;
            }

            await Task.WhenAll(rest).ConfigureAwait(false);

            Array.Clear(blockHash, 0, blockHash.Length);
            return lanes;
        }

        private static async Task SwallowAsync(Task[] tasks)
        {
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                // The lane-0 failure being propagated by the caller's own
                // catch block already identifies the problem; this only
                // exists to ensure lanes 1..N-1 are awaited (so they can't
                // outlive the call), not to report their failures too.
            }
        }

        internal byte[] Initialize(byte[] password)
        {
            // initialize the lanes
            var blake2 = new HMACBlake2B(512);
            var dataStream = new LittleEndianActiveStream();

            dataStream.Expose(DegreeOfParallelism);
            dataStream.Expose(_tagLine);
            dataStream.Expose(MemorySize);
            dataStream.Expose(Iterations);
            dataStream.Expose((uint)0x13);
            dataStream.Expose((uint)Type);
            dataStream.Expose(password.Length);
            dataStream.Expose(password);
            dataStream.Expose(Salt?.Length ?? 0);
            dataStream.Expose(Salt);
            dataStream.Expose(Secret?.Length ?? 0);
            dataStream.Expose(Secret);
            dataStream.Expose(AssociatedData?.Length ?? 0);
            dataStream.Expose(AssociatedData);

            blake2.Initialize();
            var blockhash = blake2.ComputeHash(dataStream);

            dataStream.ClearBuffer();
            return blockhash;
        }

        private static int IndexAlpha(bool sameLane, uint pseudoRand, int laneLength, int segmentLength, int pass, int slice, int index)
        {
            uint refAreaSize;
            if (pass == 0)
            {
                if (slice == 0)
                    refAreaSize = (uint)index - 1;
                else if (sameLane)
                    refAreaSize = (uint)(slice * segmentLength) + (uint)index - 1;
                else
                    refAreaSize = (uint)(slice * segmentLength) - ((index == 0) ? 1U : 0);
            }
            else if (sameLane)
                refAreaSize = (uint)laneLength - (uint)segmentLength + (uint)index - 1;
            else
                refAreaSize = (uint)laneLength - (uint)segmentLength - ((index == 0) ? 1U : 0);

            ulong relativePos = pseudoRand;
            relativePos = relativePos * relativePos >> 32;
            relativePos = refAreaSize - 1 - (refAreaSize * relativePos >> 32);

            uint startPos = 0;
            if (pass != 0)
                startPos = (slice == 3) ? 0 : ((uint)slice + 1U) * (uint)segmentLength;

            return (int)(((ulong)startPos + relativePos) % (ulong)laneLength);
        }

#if DEBUG
        private static void DebugWrite(Span<ulong> data)
        {
            int offset = 0;
            while (offset < data.Length)
            {
                for (int i = 0; i < 8; i++, offset++)
                {
                    if (offset == data.Length)
                        break;

                    Console.Write("0x{0:x16} ", data[offset]);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine();
        }
#endif

        private readonly int _tagLine;
    }
}