using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lion.Encrypt
{
    public class Keccak1600
    {
        /// <summary>
        /// The rate in bytes of the sponge state.
        /// </summary>
        private readonly int _rateBytes;
        /// <summary>
        /// The output length of the hash.
        /// </summary>
        private readonly int _outputLength;
        /// <summary>
        /// The state block size.
        /// </summary>
        private int _blockSize;
        /// <summary>
        /// The state.
        /// </summary>
        private ulong[] _state;

        /// <summary>
        /// The hash result.
        /// </summary>
        private byte[] _result;

        private int _hashType;

        private byte[] _extracted;


        public Keccak1600(int bits)
        {
            _rateBytes = (1600 - (bits << 1)) / 8;
            _outputLength = bits / 8;
        }

        public void Initialize(int hashType)
        {
            _hashType = hashType;

            _blockSize = default;
            _state = new ulong[25];
            _result = new byte[_outputLength];
            _extracted = new byte[_rateBytes];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Absorb(byte[] array, int start, int size)
        {
            var counter = 0;
            var offSet = 0;
            while (size > 0)
            {
                _blockSize = Math.Min(size, _rateBytes);
                for (var i = start; i < _blockSize / 8; i++)
                {
                    _state[i] ^= KeccakPermuteHelpers.AddStateBuffer(array, offSet);
                    offSet += 8;
                }

                size -= _blockSize;

                if (_blockSize != _rateBytes) continue;
                KeccakPermuteHelpers.Permute(_state);
                counter += _rateBytes;
                _blockSize = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Partial(byte[] array, int start, int size)
        {
            var mod = (size % _rateBytes) % 8;
            var finalRound = (size % _rateBytes) / 8;



            var partial = new byte[8];

            Array.Copy(array, size - mod, partial, 0, mod);
            partial[mod] = (byte)_hashType;
            _state[finalRound] ^= KeccakPermuteHelpers.AddStateBuffer(partial, 0);

            _state[(_rateBytes - 1) >> 3] ^= (1UL << 63);

            KeccakPermuteHelpers.Permute(_state);

            KeccakPermuteHelpers.Extract(_extracted, _state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] Squeeze()
        {
            var outputBytesLeft = _outputLength;

            while (outputBytesLeft > 0)
            {
                _blockSize = Math.Min(outputBytesLeft, _rateBytes);
                //Buffer.BlockCopy(_extracted, 0, _result, 0, _blockSize);
                Array.Copy(_extracted, 0, _result, 0, _blockSize);
                //_result = MarshalCopy(_extracted, _blockSize);
                outputBytesLeft -= _blockSize;

                if (outputBytesLeft <= 0) continue;
                KeccakPermuteHelpers.Permute(_state);
            }

            return _result;
        }

    }

    internal sealed class KeccakPermuteHelpers
    {

        private const int KeccakRounds = 24;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong AddStateBuffer(byte[] bs, int off)
        {
            var result = bs[off]
                | (ulong)bs[off + 1] << 8
                | (ulong)bs[off + 2] << 16
                | (ulong)bs[off + 3] << 24
                | (ulong)bs[off + 4] << 32
                | (ulong)bs[off + 5] << 40
                | (ulong)bs[off + 6] << 48
                | (ulong)bs[off + 7] << 56;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Extract(byte[] extracted, ulong[] state)
        {
            var offset = 0;
            for (var i = 0; i < extracted.Length / 8; i++)
            {
                ExtractStateBuffer(state[i], extracted, offset);
                offset += 8;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ExtractStateBuffer(ulong n, byte[] bs, int off)
        {
            bs[off] = (byte)(n);
            bs[off + 1] = (byte)(n >> 8);
            bs[off + 2] = (byte)(n >> 16);
            bs[off + 3] = (byte)(n >> 24);
            bs[off + 4] = (byte)(n >> 32);
            bs[off + 5] = (byte)(n >> 40);
            bs[off + 6] = (byte)(n >> 48);
            bs[off + 7] = (byte)(n >> 56);
        }

        internal static readonly ulong[] RoundConstants = new ulong[]
        {
            0x0000000000000001, 0x0000000000008082, 0x800000000000808A, 0x8000000080008000,
            0x000000000000808B, 0x0000000080000001, 0x8000000080008081, 0x8000000000008009,
            0x000000000000008A, 0x0000000000000088, 0x0000000080008009, 0x000000008000000A,
            0x000000008000808B, 0x800000000000008B, 0x8000000000008089, 0x8000000000008003,
            0x8000000000008002, 0x8000000000000080, 0x000000000000800A, 0x800000008000000A,
            0x8000000080008081, 0x8000000000008080, 0x0000000080000001, 0x8000000080008008
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Permute(ulong[] state)
        {
            var permuteState = new KeccakPermuteState();
            permuteState.Load(state);

            for (var round = 0; round < KeccakRounds; round++)
            {
                Theta(permuteState);
                RhoPi(permuteState);
                Chi(permuteState);
                Iota(permuteState, round);
            }

            permuteState.SetState(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Theta(KeccakPermuteState permuteState)
        {
            permuteState.C0 = permuteState.A00 ^ permuteState.A05 ^ permuteState.A10 ^ permuteState.A15 ^ permuteState.A20;
            permuteState.C1 = permuteState.A01 ^ permuteState.A06 ^ permuteState.A11 ^ permuteState.A16 ^ permuteState.A21;
            var c2 = permuteState.A02 ^ permuteState.A07 ^ permuteState.A12 ^ permuteState.A17 ^ permuteState.A22;
            var c3 = permuteState.A03 ^ permuteState.A08 ^ permuteState.A13 ^ permuteState.A18 ^ permuteState.A23;
            var c4 = permuteState.A04 ^ permuteState.A09 ^ permuteState.A14 ^ permuteState.A19 ^ permuteState.A24;

            var d0 = ShiftULongLeft(permuteState.C1, 1) ^ c4;
            var d1 = ShiftULongLeft(c2, 1) ^ permuteState.C0;
            var d2 = ShiftULongLeft(c3, 1) ^ permuteState.C1;
            var d3 = ShiftULongLeft(c4, 1) ^ c2;
            var d4 = ShiftULongLeft(permuteState.C0, 1) ^ c3;

            permuteState.A00 ^= d0;
            permuteState.A05 ^= d0;
            permuteState.A10 ^= d0;
            permuteState.A15 ^= d0;
            permuteState.A20 ^= d0;
            permuteState.A01 ^= d1;
            permuteState.A06 ^= d1;
            permuteState.A11 ^= d1;
            permuteState.A16 ^= d1;
            permuteState.A21 ^= d1;
            permuteState.A02 ^= d2;
            permuteState.A07 ^= d2;
            permuteState.A12 ^= d2;
            permuteState.A17 ^= d2;
            permuteState.A22 ^= d2;
            permuteState.A03 ^= d3;
            permuteState.A08 ^= d3;
            permuteState.A13 ^= d3;
            permuteState.A18 ^= d3;
            permuteState.A23 ^= d3;
            permuteState.A04 ^= d4;
            permuteState.A09 ^= d4;
            permuteState.A14 ^= d4;
            permuteState.A19 ^= d4;
            permuteState.A24 ^= d4;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RhoPi(KeccakPermuteState permuteState)
        {
            permuteState.C1 = ShiftULongLeft(permuteState.A01, 1);

            permuteState.A01 = ShiftULongLeft(permuteState.A06, 44);
            permuteState.A06 = ShiftULongLeft(permuteState.A09, 20);
            permuteState.A09 = ShiftULongLeft(permuteState.A22, 61);
            permuteState.A22 = ShiftULongLeft(permuteState.A14, 39);
            permuteState.A14 = ShiftULongLeft(permuteState.A20, 18);
            permuteState.A20 = ShiftULongLeft(permuteState.A02, 62);
            permuteState.A02 = ShiftULongLeft(permuteState.A12, 43);
            permuteState.A12 = ShiftULongLeft(permuteState.A13, 25);
            permuteState.A13 = ShiftULongLeft(permuteState.A19, 08);
            permuteState.A19 = ShiftULongLeft(permuteState.A23, 56);
            permuteState.A23 = ShiftULongLeft(permuteState.A15, 41);
            permuteState.A15 = ShiftULongLeft(permuteState.A04, 27);
            permuteState.A04 = ShiftULongLeft(permuteState.A24, 14);
            permuteState.A24 = ShiftULongLeft(permuteState.A21, 02);
            permuteState.A21 = ShiftULongLeft(permuteState.A08, 55);
            permuteState.A08 = ShiftULongLeft(permuteState.A16, 45);
            permuteState.A16 = ShiftULongLeft(permuteState.A05, 36);
            permuteState.A05 = ShiftULongLeft(permuteState.A03, 28);
            permuteState.A03 = ShiftULongLeft(permuteState.A18, 21);
            permuteState.A18 = ShiftULongLeft(permuteState.A17, 15);
            permuteState.A17 = ShiftULongLeft(permuteState.A11, 10);
            permuteState.A11 = ShiftULongLeft(permuteState.A07, 06);
            permuteState.A07 = ShiftULongLeft(permuteState.A10, 03);

            permuteState.A10 = permuteState.C1;

        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Chi(KeccakPermuteState permuteState)
        {
            permuteState.C0 = permuteState.A00 ^ (~permuteState.A01 & permuteState.A02);
            permuteState.C1 = permuteState.A01 ^ (~permuteState.A02 & permuteState.A03);
            permuteState.A02 ^= ~permuteState.A03 & permuteState.A04;
            permuteState.A03 ^= ~permuteState.A04 & permuteState.A00;
            permuteState.A04 ^= ~permuteState.A00 & permuteState.A01;
            permuteState.A00 = permuteState.C0;
            permuteState.A01 = permuteState.C1;

            permuteState.C0 = permuteState.A05 ^ (~permuteState.A06 & permuteState.A07);
            permuteState.C1 = permuteState.A06 ^ (~permuteState.A07 & permuteState.A08);
            permuteState.A07 ^= ~permuteState.A08 & permuteState.A09;
            permuteState.A08 ^= ~permuteState.A09 & permuteState.A05;
            permuteState.A09 ^= ~permuteState.A05 & permuteState.A06;
            permuteState.A05 = permuteState.C0;
            permuteState.A06 = permuteState.C1;

            permuteState.C0 = permuteState.A10 ^ (~permuteState.A11 & permuteState.A12);
            permuteState.C1 = permuteState.A11 ^ (~permuteState.A12 & permuteState.A13);
            permuteState.A12 ^= ~permuteState.A13 & permuteState.A14;
            permuteState.A13 ^= ~permuteState.A14 & permuteState.A10;
            permuteState.A14 ^= ~permuteState.A10 & permuteState.A11;
            permuteState.A10 = permuteState.C0;
            permuteState.A11 = permuteState.C1;

            permuteState.C0 = permuteState.A15 ^ (~permuteState.A16 & permuteState.A17);
            permuteState.C1 = permuteState.A16 ^ (~permuteState.A17 & permuteState.A18);
            permuteState.A17 ^= ~permuteState.A18 & permuteState.A19;
            permuteState.A18 ^= ~permuteState.A19 & permuteState.A15;
            permuteState.A19 ^= ~permuteState.A15 & permuteState.A16;
            permuteState.A15 = permuteState.C0;
            permuteState.A16 = permuteState.C1;

            permuteState.C0 = permuteState.A20 ^ (~permuteState.A21 & permuteState.A22);
            permuteState.C1 = permuteState.A21 ^ (~permuteState.A22 & permuteState.A23);
            permuteState.A22 ^= ~permuteState.A23 & permuteState.A24;
            permuteState.A23 ^= ~permuteState.A24 & permuteState.A20;
            permuteState.A24 ^= ~permuteState.A20 & permuteState.A21;
            permuteState.A20 = permuteState.C0;
            permuteState.A21 = permuteState.C1;

        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Iota(KeccakPermuteState permuteState, int round)
        {
            permuteState.A00 ^= RoundConstants[round];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ShiftULongLeft(ulong x, byte y) => (x << y) | (x >> (64 - y));
    }

    public class KeccakPermuteState
    {
        public ulong A00;
        public ulong A01;
        public ulong A02;
        public ulong A03;
        public ulong A04;
        public ulong A05;
        public ulong A06;
        public ulong A07;
        public ulong A08;
        public ulong A09;
        public ulong A10;
        public ulong A11;
        public ulong A12;
        public ulong A13;
        public ulong A14;
        public ulong A15;
        public ulong A16;
        public ulong A17;
        public ulong A18;
        public ulong A19;
        public ulong A20;
        public ulong A21;
        public ulong A22;
        public ulong A23;
        public ulong A24;

        public ulong C0;
        public ulong C1;

        public void Load(ulong[] state)
        {
            A00 = state[0]; A01 = state[1]; A02 = state[2]; A03 = state[3]; A04 = state[4];
            A05 = state[5]; A06 = state[6]; A07 = state[7]; A08 = state[8]; A09 = state[9];
            A10 = state[10]; A11 = state[11]; A12 = state[12]; A13 = state[13]; A14 = state[14];
            A15 = state[15]; A16 = state[16]; A17 = state[17]; A18 = state[18]; A19 = state[19];
            A20 = state[20]; A21 = state[21]; A22 = state[22]; A23 = state[23]; A24 = state[24];
        }

        public ulong[] SetState(ulong[] state)
        {
            state[0] = A00; state[1] = A01; state[2] = A02; state[3] = A03; state[4] = A04;
            state[5] = A05; state[6] = A06; state[7] = A07; state[8] = A08; state[9] = A09;
            state[10] = A10; state[11] = A11; state[12] = A12; state[13] = A13; state[14] = A14;
            state[15] = A15; state[16] = A16; state[17] = A17; state[18] = A18; state[19] = A19;
            state[20] = A20; state[21] = A21; state[22] = A22; state[23] = A23; state[24] = A24;

            return state;
        }
    }
}