using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DarkOps_Tool;

public class TCPGecko
{
	private static readonly byte[] GCAllowedVersions = new byte[1]
	{
		130
	};

	private tcpconn PTCP;

	private const uint packetsize = 1024u;

	private const uint uplpacketsize = 1024u;

	private const byte cmd_poke08 = 1;

	private const byte cmd_poke16 = 2;

	private const byte cmd_pokemem = 3;

	private const byte cmd_readmem = 4;

	private const byte cmd_pause = 6;

	private const byte cmd_unfreeze = 7;

	private const byte cmd_breakpoint = 9;

	private const byte cmd_writekern = 11;

	private const byte cmd_readkern = 12;

	private const byte cmd_breakpointx = 16;

	private const byte cmd_sendregs = 47;

	private const byte cmd_getregs = 48;

	private const byte cmd_cancelbp = 56;

	private const byte cmd_sendcheats = 64;

	private const byte cmd_upload = 65;

	private const byte cmd_hook = 66;

	private const byte cmd_hookpause = 67;

	private const byte cmd_step = 68;

	private const byte cmd_status = 80;

	private const byte cmd_cheatexec = 96;

	private const byte cmd_rpc = 112;

	private const byte cmd_nbreakpoint = 137;

	private const byte cmd_version = 153;

	private const byte cmd_os_version = 154;

	private const byte GCBPHit = 17;

	private const byte GCACK = 170;

	private const byte GCRETRY = 187;

	private const byte GCFAIL = 204;

	private const byte GCDONE = byte.MaxValue;

	private const byte BlockZero = 176;

	private const byte BlockNonZero = 189;

	private const byte GCWiiVer = 128;

	private const byte GCNgcVer = 129;

	private const byte GCWiiUVer = 130;

	private const byte BPExecute = 3;

	private const byte BPRead = 5;

	private const byte BPWrite = 6;

	private const byte BPReadWrite = 7;

	private bool PConnected;

	private bool PCancelDump;

	public bool connected => PConnected;

	public bool CancelDump
	{
		get
		{
			return PCancelDump;
		}
		set
		{
			PCancelDump = value;
		}
	}

	public string Host
	{
		get
		{
			return PTCP.Host;
		}
		set
		{
			if (!PConnected)
			{
				PTCP = new tcpconn(value, PTCP.Port);
			}
		}
	}

	private event GeckoProgress PChunkUpdate;

	public event GeckoProgress chunkUpdate
	{
		add
		{
			PChunkUpdate += value;
		}
		remove
		{
			PChunkUpdate -= value;
		}
	}

	public TCPGecko(string host, int port)
	{
		PTCP = new tcpconn(host, port);
		PConnected = false;
		this.PChunkUpdate = null;
	}

	~TCPGecko()
	{
		if (PConnected)
		{
			Disconnect();
		}
	}

	protected bool InitGecko()
	{
		return true;
	}

	public bool Connect()
	{
		if (PConnected)
		{
			Disconnect();
		}
		PConnected = false;
		try
		{
			PTCP.Connect();
		}
		catch (IOException)
		{
			Disconnect();
			throw new ETCPGeckoException(ETCPErrorCode.noTCPGeckoFound);
		}
		if (!InitGecko())
		{
			return false;
		}
		Thread.Sleep(150);
		PConnected = true;
		return true;
	}

	public void Disconnect()
	{
		PConnected = false;
		PTCP.Close();
	}

	protected FTDICommand GeckoRead(byte[] recbyte, uint nobytes)
	{
		uint bytes_read = 0u;
		try
		{
			PTCP.Read(recbyte, nobytes, ref bytes_read);
		}
		catch (IOException)
		{
			Disconnect();
			return FTDICommand.CMD_FatalError;
		}
		return (bytes_read == nobytes) ? FTDICommand.CMD_OK : FTDICommand.CMD_ResultError;
	}

	internal void poke08(string v1, int v2)
	{
		throw new NotImplementedException();
	}

	protected FTDICommand GeckoWrite(byte[] sendbyte, int nobytes)
	{
		uint bytes_written = 0u;
		try
		{
			PTCP.Write(sendbyte, nobytes, ref bytes_written);
		}
		catch (IOException)
		{
			Disconnect();
			return FTDICommand.CMD_FatalError;
		}
		return (bytes_written == nobytes) ? FTDICommand.CMD_OK : FTDICommand.CMD_ResultError;
	}

	protected void SendUpdate(uint address, uint currentchunk, uint allchunks, uint transferred, uint length, bool okay, bool dump)
	{
		if (this.PChunkUpdate != null)
		{
			this.PChunkUpdate(address, currentchunk, allchunks, transferred, length, okay, dump);
		}
	}

	public void Dump(Dump dump)
	{
		Dump(dump.StartAddress, dump.EndAddress, dump);
	}

	public void Dump(uint startdump, uint enddump, Stream saveStream)
	{
		Stream[] saveStream2 = new Stream[1]
		{
			saveStream
		};
		Dump(startdump, enddump, saveStream2);
	}

	public void Dump(uint startdump, uint enddump, Stream[] saveStream)
	{
		InitGecko();
		if (ValidMemory.rangeCheckId(startdump) != ValidMemory.rangeCheckId(enddump))
		{
			enddump = ValidMemory.ValidAreas[ValidMemory.rangeCheckId(startdump)].high;
		}
		if (!ValidMemory.validAddress(startdump))
		{
			return;
		}
		uint num = enddump - startdump;
		uint num2 = num / 1024u;
		uint num3 = num % 1024u;
		uint num4 = num2;
		if (num3 != 0)
		{
			num4++;
		}
		ulong value = ByteSwap.Swap(((ulong)startdump << 32) + enddump);
		if (GeckoWrite(BitConverter.GetBytes((short)4), 1) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value), 8) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		uint num5 = 0u;
		byte b = 0;
		bool flag = false;
		CancelDump = false;
		byte[] array = new byte[1024];
		while (num5 < num2 && !flag)
		{
			SendUpdate(startdump + num5 * 1024, num5, num4, num5 * 1024, num, b == 0, dump: true);
			byte[] array2 = new byte[1];
			if (GeckoRead(array2, 1u) != FTDICommand.CMD_OK)
			{
				int num6 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			}
			if (array2[0] == 176)
			{
				for (int i = 0; i < 1024; i++)
				{
					array[i] = 0;
				}
			}
			else
			{
				switch (GeckoRead(array, 1024u))
				{
				case FTDICommand.CMD_ResultError:
					b = (byte)(b + 1);
					if (b >= 3)
					{
						int num8 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
						throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
					}
					continue;
				case FTDICommand.CMD_FatalError:
				{
					int num7 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
				}
				}
			}
			foreach (Stream stream in saveStream)
			{
				stream.Write(array, 0, 1024);
			}
			b = 0;
			num5++;
			if (CancelDump)
			{
				int num9 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				flag = true;
			}
		}
		while (!flag && num3 != 0)
		{
			SendUpdate(startdump + num5 * 1024, num5, num4, num5 * 1024, num, b == 0, dump: true);
			byte[] array3 = new byte[1];
			if (GeckoRead(array3, 1u) != FTDICommand.CMD_OK)
			{
				int num10 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			}
			if (array3[0] == 176)
			{
				for (int k = 0; k < num3; k++)
				{
					array[k] = 0;
				}
			}
			else
			{
				switch (GeckoRead(array, num3))
				{
				case FTDICommand.CMD_ResultError:
					b = (byte)(b + 1);
					if (b >= 3)
					{
						int num12 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
						throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
					}
					continue;
				case FTDICommand.CMD_FatalError:
				{
					int num11 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
				}
				}
			}
			foreach (Stream stream2 in saveStream)
			{
				stream2.Write(array, 0, (int)num3);
			}
			b = 0;
			flag = true;
		}
		SendUpdate(enddump, num4, num4, num, num, okay: true, dump: true);
	}

	public void Dump(uint startdump, uint enddump, Dump memdump)
	{
		InitGecko();
		uint num = enddump - startdump;
		uint num2 = num / 1024u;
		uint num3 = num % 1024u;
		uint num4 = num2;
		if (num3 != 0)
		{
			num4++;
		}
		ulong value = ByteSwap.Swap(((ulong)startdump << 32) + enddump);
		if (GeckoWrite(BitConverter.GetBytes((short)4), 1) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value), 8) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		uint num5 = 0u;
		byte b = 0;
		bool flag = false;
		CancelDump = false;
		byte[] array = new byte[1024];
		while (num5 < num2 && !flag)
		{
			SendUpdate(startdump + num5 * 1024, num5, num4, num5 * 1024, num, b == 0, dump: true);
			byte[] array2 = new byte[1];
			if (GeckoRead(array2, 1u) != FTDICommand.CMD_OK)
			{
				int num6 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			}
			if (array2[0] == 176)
			{
				for (int i = 0; i < 1024; i++)
				{
					array[i] = 0;
				}
			}
			else
			{
				switch (GeckoRead(array, 1024u))
				{
				case FTDICommand.CMD_ResultError:
					b = (byte)(b + 1);
					if (b >= 3)
					{
						int num8 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
						throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
					}
					continue;
				case FTDICommand.CMD_FatalError:
				{
					int num7 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
				}
				}
			}
			Buffer.BlockCopy(array, 0, memdump.mem, (int)(num5 * 1024 + (startdump - memdump.StartAddress)), 1024);
			memdump.ReadCompletedAddress = (num5 + 1) * 1024 + startdump;
			b = 0;
			num5++;
			if (CancelDump)
			{
				int num9 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				flag = true;
			}
		}
		while (!flag && num3 != 0)
		{
			SendUpdate(startdump + num5 * 1024, num5, num4, num5 * 1024, num, b == 0, dump: true);
			byte[] array3 = new byte[1];
			if (GeckoRead(array3, 1u) != FTDICommand.CMD_OK)
			{
				int num10 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			}
			if (array3[0] == 176)
			{
				for (int j = 0; j < num3; j++)
				{
					array[j] = 0;
				}
			}
			else
			{
				switch (GeckoRead(array, num3))
				{
				case FTDICommand.CMD_ResultError:
					b = (byte)(b + 1);
					if (b >= 3)
					{
						int num12 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
						throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
					}
					continue;
				case FTDICommand.CMD_FatalError:
				{
					int num11 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
				}
				}
			}
			Buffer.BlockCopy(array, 0, memdump.mem, (int)(num5 * 1024 + (startdump - memdump.StartAddress)), (int)num3);
			b = 0;
			flag = true;
		}
		SendUpdate(enddump, num4, num4, num, num, okay: true, dump: true);
	}

	public void Upload(uint startupload, uint endupload, Stream sendStream)
	{
		InitGecko();
		uint num = endupload - startupload;
		uint num2 = num / 1024u;
		uint num3 = num % 1024u;
		uint num4 = num2;
		if (num3 != 0)
		{
			num4++;
		}
		ulong value = ByteSwap.Swap(((ulong)startupload << 32) + endupload);
		if (GeckoWrite(BitConverter.GetBytes((short)65), 1) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value), 8) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		uint num5 = 0u;
		byte b = 0;
		while (num5 < num2)
		{
			SendUpdate(startupload + num5 * 1024, num5, num4, num5 * 1024, num, b == 0, dump: false);
			byte[] array = new byte[1024];
			sendStream.Read(array, 0, 1024);
			switch (GeckoWrite(array, 1024))
			{
			case FTDICommand.CMD_ResultError:
				b = (byte)(b + 1);
				if (b >= 3)
				{
					Disconnect();
					throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
				}
				sendStream.Seek(-1024L, SeekOrigin.Current);
				break;
			case FTDICommand.CMD_FatalError:
				Disconnect();
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			default:
				b = 0;
				num5++;
				break;
			}
		}
		while (num3 != 0)
		{
			SendUpdate(startupload + num5 * 1024, num5, num4, num5 * 1024, num, b == 0, dump: false);
			byte[] array2 = new byte[num3];
			sendStream.Read(array2, 0, (int)num3);
			switch (GeckoWrite(array2, (int)num3))
			{
			case FTDICommand.CMD_ResultError:
				b = (byte)(b + 1);
				if (b >= 3)
				{
					Disconnect();
					throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
				}
				sendStream.Seek(-1 * (int)num3, SeekOrigin.Current);
				break;
			case FTDICommand.CMD_FatalError:
				Disconnect();
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			default:
				b = 0;
				num3 = 0u;
				break;
			}
		}
		SendUpdate(endupload, num4, num4, num, num, okay: true, dump: false);
	}

	internal void Upload(int v1, string v2)
	{
		throw new NotImplementedException();
	}

	public bool Reconnect()
	{
		Disconnect();
		try
		{
			return Connect();
		}
		catch
		{
			return false;
		}
	}

	public FTDICommand RawCommand(byte id)
	{
		return GeckoWrite(BitConverter.GetBytes(id), 1);
	}

	public void Pause()
	{
		if (RawCommand(6) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public void SafePause()
	{
		bool flag = status() == WiiStatus.Running;
		while (flag)
		{
			Pause();
			Thread.Sleep(100);
			flag = (status() == WiiStatus.Running);
		}
	}

	public void Resume()
	{
		if (RawCommand(7) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public void sendfail()
	{
		int num = (int)RawCommand(204);
	}

	public void poke(uint address, uint value)
	{
		address = (uint)((int)address & -4);
		ulong value2 = ByteSwap.Swap(((ulong)address << 32) | value);
		if (RawCommand(3) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value2), 8) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public void poke32(uint address, uint value)
	{
		poke(address, value);
	}

	public void poke16(uint address, ushort value)
	{
		address = (uint)((int)address & -2);
		ulong value2 = ByteSwap.Swap(((ulong)address << 32) | value);
		if (RawCommand(2) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value2), 8) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public void poke08(uint address, byte value)
	{
		ulong value2 = ByteSwap.Swap(((ulong)address << 32) | value);
		if (RawCommand(1) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value2), 8) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public void poke_kern(uint address, uint value)
	{
		ulong value2 = ByteSwap.Swap(((ulong)address << 32) | value);
		if (RawCommand(11) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value2), 8) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public uint peek_kern(uint address)
	{
		address = ByteSwap.Swap(address);
		if (RawCommand(12) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(address), 4) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		byte[] array = new byte[4];
		if (GeckoRead(array, 4u) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		return ByteSwap.Swap(BitConverter.ToUInt32(array, 0));
	}

	public WiiStatus status()
	{
		Thread.Sleep(100);
		if (!InitGecko())
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDIResetError);
		}
		if (RawCommand(80) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		byte[] array = new byte[1];
		if (GeckoRead(array, 1u) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
		}
		switch (array[0])
		{
		case 0:
			return WiiStatus.Running;
		case 1:
			return WiiStatus.Paused;
		case 2:
			return WiiStatus.Breakpoint;
		case 3:
			return WiiStatus.Loader;
		default:
			return WiiStatus.Unknown;
		}
	}

	public void Step()
	{
		if (!InitGecko())
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDIResetError);
		}
		if (RawCommand(68) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	protected void Breakpoint(uint address, byte bptype, bool exact)
	{
		InitGecko();
		uint num = (address & 0xFFFFFF8) | bptype;
		bool flag = false;
		if (exact)
		{
			flag = (VersionRequest() != 129);
		}
		if (!flag)
		{
			if (RawCommand(9) != FTDICommand.CMD_OK)
			{
				throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
			}
			if (GeckoWrite(BitConverter.GetBytes(ByteSwap.Swap(num)), 4) != FTDICommand.CMD_OK)
			{
				throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
			}
		}
		else
		{
			if (RawCommand(137) != FTDICommand.CMD_OK)
			{
				throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
			}
			if (GeckoWrite(BitConverter.GetBytes(ByteSwap.Swap(((ulong)num << 32) | address)), 8) != FTDICommand.CMD_OK)
			{
				throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
			}
		}
	}

	public void BreakpointR(uint address, bool exact)
	{
		Breakpoint(address, 5, exact);
	}

	public void BreakpointR(uint address)
	{
		Breakpoint(address, 5, exact: true);
	}

	public void BreakpointW(uint address, bool exact)
	{
		Breakpoint(address, 6, exact);
	}

	public void BreakpointW(uint address)
	{
		Breakpoint(address, 6, exact: true);
	}

	public void BreakpointRW(uint address, bool exact)
	{
		Breakpoint(address, 7, exact);
	}

	public void BreakpointRW(uint address)
	{
		Breakpoint(address, 7, exact: true);
	}

	public void BreakpointX(uint address)
	{
		InitGecko();
		uint value = ByteSwap.Swap((uint)(((int)address & -4) | 3));
		if (RawCommand(16) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(BitConverter.GetBytes(value), 4) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public bool BreakpointHit()
	{
		byte[] array = new byte[1];
		return GeckoRead(array, 1u) == FTDICommand.CMD_OK && array[0] == 17;
	}

	public void CancelBreakpoint()
	{
		if (RawCommand(56) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	protected bool AllowedVersion(byte version)
	{
		for (int i = 0; i < GCAllowedVersions.Length; i++)
		{
			if (GCAllowedVersions[i] == version)
			{
				return true;
			}
		}
		return false;
	}

	public byte VersionRequest()
	{
		InitGecko();
		if (RawCommand(153) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		byte b = 0;
		byte result = 0;
		byte[] array = new byte[1];
		do
		{
			if (GeckoRead(array, 1u) != FTDICommand.CMD_OK || !AllowedVersion(array[0]))
			{
				b = (byte)(b + 1);
				continue;
			}
			result = array[0];
			break;
		}
		while (b < 3);
		return result;
	}

	public uint OsVersionRequest()
	{
		if (RawCommand(154) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		byte[] array = new byte[4];
		if (GeckoRead(array, 4u) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		return ByteSwap.Swap(BitConverter.ToUInt32(array, 0));
	}

	public uint peek(uint address)
	{
		if (!ValidMemory.validAddress(address))
		{
			return 0u;
		}
		uint num = (uint)((int)address & -4);
		MemoryStream memoryStream = new MemoryStream();
		GeckoProgress pChunkUpdate = this.PChunkUpdate;
		this.PChunkUpdate = null;
		try
		{
			Dump(num, num + 4, memoryStream);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			byte[] array = new byte[4];
			memoryStream.Read(array, 0, 4);
			return ByteSwap.Swap(BitConverter.ToUInt32(array, 0));
		}
		finally
		{
			this.PChunkUpdate = pChunkUpdate;
			memoryStream.Close();
		}
	}

	public void GetRegisters(Stream stream, uint contextAddress)
	{
		uint num = 432u;
		MemoryStream memoryStream = new MemoryStream();
		Dump(contextAddress + 8, contextAddress + 8 + num, memoryStream);
		byte[] buffer = memoryStream.ToArray();
		stream.Write(buffer, 128, 4);
		stream.Write(buffer, 140, 4);
		stream.Write(buffer, 136, 4);
		stream.Write(new byte[8], 0, 8);
		stream.Write(buffer, 144, 8);
		stream.Write(buffer, 0, 128);
		stream.Write(buffer, 132, 4);
		stream.Write(buffer, 176, 256);
	}

	public void SendRegisters(Stream sendStream, uint contextAddress)
	{
		MemoryStream memoryStream = new MemoryStream();
		byte[] array = new byte[160];
		sendStream.Seek(0L, SeekOrigin.Begin);
		sendStream.Read(array, 0, array.Length);
		memoryStream.Write(array, 28, 128);
		memoryStream.Write(array, 0, 4);
		memoryStream.Write(array, 156, 4);
		memoryStream.Write(array, 8, 4);
		memoryStream.Write(array, 4, 4);
		memoryStream.Write(array, 20, 8);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		Upload(contextAddress + 8, contextAddress + 8 + 152, memoryStream);
	}

	private ulong readInt64(Stream inputstream)
	{
		byte[] array = new byte[8];
		inputstream.Read(array, 0, 8);
		return ByteSwap.Swap(BitConverter.ToUInt64(array, 0));
	}

	private void writeInt64(Stream outputstream, ulong value)
	{
		byte[] bytes = BitConverter.GetBytes(ByteSwap.Swap(value));
		outputstream.Write(bytes, 0, 8);
	}

	private void insertInto(Stream insertStream, ulong value)
	{
		MemoryStream memoryStream = new MemoryStream();
		writeInt64(memoryStream, value);
		insertStream.Seek(0L, SeekOrigin.Begin);
		byte[] buffer = new byte[insertStream.Length];
		insertStream.Read(buffer, 0, (int)insertStream.Length);
		memoryStream.Write(buffer, 0, (int)insertStream.Length);
		insertStream.Seek(0L, SeekOrigin.Begin);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		byte[] buffer2 = new byte[memoryStream.Length];
		memoryStream.Read(buffer2, 0, (int)memoryStream.Length);
		insertStream.Write(buffer2, 0, (int)memoryStream.Length);
		memoryStream.Close();
	}

	public void sendCheats(Stream inputStream)
	{
		MemoryStream memoryStream = new MemoryStream();
		byte[] buffer = new byte[inputStream.Length];
		inputStream.Seek(0L, SeekOrigin.Begin);
		inputStream.Read(buffer, 0, (int)inputStream.Length);
		memoryStream.Write(buffer, 0, (int)inputStream.Length);
		if ((uint)memoryStream.Length % 8u != 0)
		{
			memoryStream.Close();
			throw new ETCPGeckoException(ETCPErrorCode.CheatStreamSizeInvalid);
		}
		InitGecko();
		memoryStream.Seek(-8L, SeekOrigin.End);
		ulong num = (ulong)((long)readInt64(memoryStream) & -144115188075855872L);
		if (num != 17293822569102704640uL && num != 18302628885633695744uL)
		{
			memoryStream.Seek(0L, SeekOrigin.End);
			writeInt64(memoryStream, 17293822569102704640uL);
		}
		memoryStream.Seek(0L, SeekOrigin.Begin);
		if (readInt64(memoryStream) != 58758854884770014L)
		{
			insertInto(memoryStream, 58758854884770014uL);
		}
		memoryStream.Seek(0L, SeekOrigin.Begin);
		uint num2 = (uint)memoryStream.Length;
		if (GeckoWrite(BitConverter.GetBytes((short)64), 1) != FTDICommand.CMD_OK)
		{
			memoryStream.Close();
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		uint num3 = num2 / 1024u;
		uint num4 = num2 % 1024u;
		uint num5 = num3;
		if (num4 != 0)
		{
			num5++;
		}
		byte b = 0;
		while (b < 10)
		{
			byte[] array = new byte[1];
			if (GeckoRead(array, 1u) != FTDICommand.CMD_OK)
			{
				memoryStream.Close();
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			}
			if (array[0] != 170)
			{
				if (b == 9)
				{
					memoryStream.Close();
					throw new ETCPGeckoException(ETCPErrorCode.FTDIInvalidReply);
				}
				continue;
			}
			break;
		}
		if (GeckoWrite(BitConverter.GetBytes(ByteSwap.Swap(num2)), 4) != FTDICommand.CMD_OK)
		{
			memoryStream.Close();
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		uint num6 = 0u;
		byte b2 = 0;
		while (num6 < num3)
		{
			SendUpdate(13680862u, num6, num5, num6 * 1024, num2, b2 == 0, dump: false);
			byte[] array2 = new byte[1024];
			memoryStream.Read(array2, 0, 1024);
			switch (GeckoWrite(array2, 1024))
			{
			case FTDICommand.CMD_ResultError:
			{
				b2 = (byte)(b2 + 1);
				if (b2 >= 3)
				{
					int num7 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					memoryStream.Close();
					throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
				}
				memoryStream.Seek(-1024L, SeekOrigin.Current);
				int num8 = (int)GeckoWrite(BitConverter.GetBytes((short)187), 1);
				continue;
			}
			case FTDICommand.CMD_FatalError:
			{
				int num9 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				memoryStream.Close();
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			}
			}
			byte[] array3 = new byte[1];
			FTDICommand fTDICommand = GeckoRead(array3, 1u);
			if (fTDICommand == FTDICommand.CMD_ResultError || array3[0] != 170)
			{
				b2 = (byte)(b2 + 1);
				if (b2 >= 3)
				{
					int num10 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					memoryStream.Close();
					throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
				}
				memoryStream.Seek(-1024L, SeekOrigin.Current);
				int num11 = (int)GeckoWrite(BitConverter.GetBytes((short)187), 1);
			}
			else
			{
				if (fTDICommand == FTDICommand.CMD_FatalError)
				{
					int num12 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					memoryStream.Close();
					throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
				}
				b2 = 0;
				num6++;
			}
		}
		while (num4 != 0)
		{
			SendUpdate(13680862u, num6, num5, num6 * 1024, num2, b2 == 0, dump: false);
			byte[] array4 = new byte[num4];
			memoryStream.Read(array4, 0, (int)num4);
			switch (GeckoWrite(array4, (int)num4))
			{
			case FTDICommand.CMD_ResultError:
			{
				b2 = (byte)(b2 + 1);
				if (b2 >= 3)
				{
					int num14 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
					memoryStream.Close();
					throw new ETCPGeckoException(ETCPErrorCode.TooManyRetries);
				}
				memoryStream.Seek(-1 * (int)num4, SeekOrigin.Current);
				int num15 = (int)GeckoWrite(BitConverter.GetBytes((short)187), 1);
				break;
			}
			case FTDICommand.CMD_FatalError:
			{
				int num13 = (int)GeckoWrite(BitConverter.GetBytes((short)204), 1);
				memoryStream.Close();
				throw new ETCPGeckoException(ETCPErrorCode.FTDIReadDataError);
			}
			default:
				b2 = 0;
				num4 = 0u;
				break;
			}
		}
		SendUpdate(13680862u, num5, num5, num2, num2, okay: true, dump: false);
		memoryStream.Close();
	}

	public void ExecuteCheats()
	{
		if (RawCommand(96) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public void Hook(bool pause, WiiLanguage language, WiiPatches patches, WiiHookType hookType)
	{
		InitGecko();
		if (RawCommand((byte)(((!pause) ? 66 : 67) + (byte)hookType)) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (RawCommand((byte)((language == WiiLanguage.NoOverride) ? 205 : ((byte)(language - 1)))) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (RawCommand((byte)patches) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
	}

	public void Hook()
	{
		Hook(pause: false, WiiLanguage.NoOverride, WiiPatches.NoPatches, WiiHookType.VI);
	}

	private static byte ConvertSafely(double floatValue)
	{
		return (byte)Math.Round(Math.Max(0.0, Math.Min(floatValue, 255.0)));
	}

	private static Bitmap ProcessImage(uint width, uint height, Stream analyze)
	{
		Bitmap bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, (int)width, (int)height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
		int num = bitmapData.Stride * bitmapData.Height;
		byte[] array = new byte[num];
		Marshal.Copy(bitmapData.Scan0, array, 0, num);
		byte[] array2 = new byte[width * height * 2];
		int num2 = 0;
		int num3 = 0;
		analyze.Read(array2, 0, (int)(width * height * 2));
		for (int i = 0; i < width * height; i++)
		{
			int num4 = i * 2;
			int num5;
			if (i % 2 == 0)
			{
				num5 = array2[num4];
				num2 = array2[num4 + 1];
				num3 = array2[num4 + 3];
			}
			else
			{
				num5 = array2[num4];
			}
			int num6 = i * 3;
			array[num6] = ConvertSafely(1.164 * (double)(num5 - 16) + 2.017 * (double)(num2 - 128));
			array[num6 + 1] = ConvertSafely(1.164 * (double)(num5 - 16) - 0.392 * (double)(num2 - 128) - 0.813 * (double)(num3 - 128));
			array[num6 + 2] = ConvertSafely(1.164 * (double)(num5 - 16) + 1.596 * (double)(num3 - 128));
		}
		Marshal.Copy(array, 0, bitmapData.Scan0, array.Length);
		bitmap.UnlockBits(bitmapData);
		return bitmap;
	}

	public Image Screenshot()
	{
		MemoryStream memoryStream = new MemoryStream();
		Dump(3422560256u, 3422560384u, memoryStream);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		byte[] array = new byte[128];
		memoryStream.Read(array, 0, 128);
		memoryStream.Close();
		uint num = (uint)(array[73] << 3);
		uint num2 = (uint)(((array[0] << 5) | (array[1] >> 3)) & 0x7FE);
		uint num3 = (uint)((array[29] << 16) | (array[30] << 8) | array[31]);
		if ((array[28] & 0x10) == 16)
		{
			num3 <<= 5;
		}
		uint num4 = (uint)((int)num3 + int.MinValue - ((array[28] & 0xF) << 3));
		MemoryStream memoryStream2 = new MemoryStream();
		Dump(num4, num4 + num2 * num * 2, memoryStream2);
		memoryStream2.Seek(0L, SeekOrigin.Begin);
		if (num2 > 600)
		{
			num2 /= 2u;
			num *= 2;
		}
		Bitmap result = ProcessImage(num, num2, memoryStream2);
		memoryStream2.Close();
		return result;
	}

	public uint rpc(uint address, params uint[] args)
	{
		return (uint)(rpc64(address, args) >> 32);
	}

	public ulong rpc64(uint address, params uint[] args)
	{
		byte[] array = new byte[36];
		address = ByteSwap.Swap(address);
		BitConverter.GetBytes(address).CopyTo(array, 0);
		for (int i = 0; i < 8; i++)
		{
			if (i < args.Length)
			{
				BitConverter.GetBytes(ByteSwap.Swap(args[i])).CopyTo(array, 4 + i * 4);
			}
			else
			{
				BitConverter.GetBytes(4274704570u).CopyTo(array, 4 + i * 4);
			}
		}
		if (RawCommand(112) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoWrite(array, array.Length) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		if (GeckoRead(array, 8u) != FTDICommand.CMD_OK)
		{
			throw new ETCPGeckoException(ETCPErrorCode.FTDICommandSendError);
		}
		return ByteSwap.Swap(BitConverter.ToUInt64(array, 0));
	}
}
