﻿// Autogenerated
// ReSharper disable BuiltInTypeReferenceStyle
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace StreamingCbor
{
    public partial class CborWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(uint a)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(4);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Advance(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(uint a, byte b)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(5);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 4), b);
            Advance(5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(uint a, ushort b)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(6);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 4), b);
            Advance(6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(uint a, ushort b, byte c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(7);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 4), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 6), c);
            Advance(7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(8);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Advance(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, byte b)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(9);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Advance(9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ushort b)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(10);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Advance(10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ushort b, byte c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(11);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 10), c);
            Advance(11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, uint b)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(12);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Advance(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, uint b, byte c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(13);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 12), c);
            Advance(13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, uint b, ushort c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(14);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 12), c);
            Advance(14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, uint b, ushort c, byte d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(15);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 12), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 14), d);
            Advance(15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(16);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Advance(16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, byte c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(17);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Advance(17);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ushort c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(18);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Advance(18);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ushort c, byte d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(19);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 18), d);
            Advance(19);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, uint c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(20);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Advance(20);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, uint c, byte d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(21);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 20), d);
            Advance(21);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, uint c, ushort d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(22);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 20), d);
            Advance(22);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, uint c, ushort d, byte e)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(23);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 20), d);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 22), e);
            Advance(23);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(24);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Advance(24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, byte d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(25);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Advance(25);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, ushort d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(26);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Advance(26);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, ushort d, byte e)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(27);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 26), e);
            Advance(27);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, uint d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(28);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Advance(28);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, uint d, byte e)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(29);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 28), e);
            Advance(29);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, uint d, ushort e)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(30);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 28), e);
            Advance(30);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, uint d, ushort e, byte f)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(31);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 28), e);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 30), f);
            Advance(31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVerbatim(ulong a, ulong b, ulong c, ulong d)
        {
            Span<byte> bytes = _pipeWriter.GetSpan(32);
            ref var bStart = ref bytes[0];
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 0), a);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 8), b);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 16), c);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bStart, 24), d);
            Advance(32);
        }

    }
}