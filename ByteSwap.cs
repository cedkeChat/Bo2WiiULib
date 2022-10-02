using System;

public class ByteSwap
{
	public static ushort Swap(ushort input)
	{
		return BitConverter.IsLittleEndian ? ((ushort)(((0xFF00 & input) >> 8) | ((0xFF & input) << 8))) : input;
	}

	public static uint Swap(uint input)
	{
		return BitConverter.IsLittleEndian ? (((uint)(-16777216 & (int)input) >> 24) | ((0xFF0000 & input) >> 8) | ((0xFF00 & input) << 8) | ((0xFF & input) << 24)) : input;
	}

	public static ulong Swap(ulong input)
	{
		return BitConverter.IsLittleEndian ? (((ulong)(-72057594037927936L & (long)input) >> 56) | ((0xFF000000000000 & input) >> 40) | ((0xFF0000000000 & input) >> 24) | ((0xFF00000000 & input) >> 8) | ((4278190080u & input) << 8) | ((0xFF0000 & input) << 24) | ((0xFF00 & input) << 40) | ((0xFF & input) << 56)) : input;
	}
}
