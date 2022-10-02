using System;
using System.IO;
using DarkOps_Tool;

public class Dump
{
	public byte[] mem;

	private uint startAddress;

	private uint endAddress;

	private uint readCompletedAddress;

	private int fileNumber;

	public uint StartAddress => startAddress;

	public uint EndAddress => endAddress;

	public uint ReadCompletedAddress
	{
		get
		{
			return readCompletedAddress;
		}
		set
		{
			readCompletedAddress = value;
		}
	}

	public Dump(uint theStartAddress, uint theEndAddress)
	{
		Construct(theStartAddress, theEndAddress, 0);
	}

	public Dump(uint theStartAddress, uint theEndAddress, int theFileNumber)
	{
		Construct(theStartAddress, theEndAddress, theFileNumber);
	}

	private void Construct(uint theStartAddress, uint theEndAddress, int theFileNumber)
	{
		startAddress = theStartAddress;
		endAddress = theEndAddress;
		readCompletedAddress = theStartAddress;
		mem = new byte[endAddress - startAddress];
		fileNumber = theFileNumber;
	}

	public uint ReadAddress32(uint addressToRead)
	{
		if (addressToRead < startAddress || addressToRead > endAddress - 4)
		{
			return 0u;
		}
		byte[] array = new byte[4];
		Buffer.BlockCopy(mem, index(addressToRead), array, 0, 4);
		return ByteSwap.Swap(BitConverter.ToUInt32(array, 0));
	}

	private int index(uint addressToRead)
	{
		return (int)(addressToRead - startAddress);
	}

	public uint ReadAddress(uint addressToRead, int numBytes)
	{
		if (addressToRead < startAddress || addressToRead > endAddress - numBytes)
		{
			return 0u;
		}
		byte[] array = new byte[4];
		Buffer.BlockCopy(mem, index(addressToRead), array, 0, numBytes);
		switch (numBytes)
		{
		case 2:
			return ByteSwap.Swap(BitConverter.ToUInt16(array, 0));
		case 4:
			return ByteSwap.Swap(BitConverter.ToUInt32(array, 0));
		default:
			return array[0];
		}
	}

	public void WriteStreamToDisk()
	{
		string text = Environment.CurrentDirectory + "\\searchdumps\\";
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		WriteStreamToDisk(text + "dump" + fileNumber.ToString() + ".dmp");
	}

	public void WriteStreamToDisk(string filepath)
	{
		FileStream fileStream = new FileStream(filepath, FileMode.Create);
		fileStream.Write(mem, 0, (int)(endAddress - startAddress));
		fileStream.Close();
		fileStream.Dispose();
	}
}
