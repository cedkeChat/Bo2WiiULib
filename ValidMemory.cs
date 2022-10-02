using DarkOps_Tool;

public static class ValidMemory
{
	public static bool addressDebug = false;

	public static readonly AddressRange[] ValidAreas = new AddressRange[10]
	{
		new AddressRange(AddressType.Ex, 16777216u, 25165824u),
		new AddressRange(AddressType.Ex, 3808428032u, 268435456u),
		new AddressRange(AddressType.Rw, 268435456u, 1342177280u),
		new AddressRange(AddressType.Ro, 3758096384u, 3825205248u),
		new AddressRange(AddressType.Ro, 3892314112u, 3925868544u),
		new AddressRange(AddressType.Ro, 4093640704u, 4127195136u),
		new AddressRange(AddressType.Ro, 4127195136u, 4135583744u),
		new AddressRange(AddressType.Ro, 4160749568u, 4211081216u),
		new AddressRange(AddressType.Ro, 4211081216u, 4219469824u),
		new AddressRange(AddressType.Rw, 4294836224u, uint.MaxValue)
	};

	public static AddressType rangeCheck(uint address)
	{
		int num = rangeCheckId(address);
		return (num == -1) ? AddressType.Unknown : ValidAreas[num].description;
	}

	public static int rangeCheckId(uint address)
	{
		for (int i = 0; i < ValidAreas.Length; i++)
		{
			AddressRange addressRange = ValidAreas[i];
			if (address >= addressRange.low && address < addressRange.high)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool validAddress(uint address, bool debug)
	{
		return debug || rangeCheckId(address) >= 0;
	}

	public static bool validAddress(uint address)
	{
		return validAddress(address, addressDebug);
	}

	public static bool validRange(uint low, uint high, bool debug)
	{
		return debug || rangeCheckId(low) == rangeCheckId(high - 1);
	}

	public static bool validRange(uint low, uint high)
	{
		return validRange(low, high, addressDebug);
	}

	public static void setDataUpper(TCPGecko upper)
	{
		uint num = upper.OsVersionRequest();
		uint num2 = num;
		if (num2 == 400 || num2 == 410)
		{
			uint num3 = upper.peek_kern(4293419420u);
			uint num4 = upper.peek_kern(num3 + 4);
			uint num5 = upper.peek_kern(num4 + 20);
			uint num6 = upper.peek_kern(num5);
			uint num7 = upper.peek_kern(num5 + 4);
			uint num8 = upper.peek_kern(num5 + 16);
			uint num9 = upper.peek_kern(num5 + 4 + 16);
			uint num10 = upper.peek_kern(num5 + 32);
			uint num11 = upper.peek_kern(num5 + 4 + 32);
			ValidAreas[0] = new AddressRange(AddressType.Ex, num6, num6 + num7);
			ValidAreas[1] = new AddressRange(AddressType.Ex, num8, num8 + num9);
			ValidAreas[2] = new AddressRange(AddressType.Rw, num10, num10 + num11);
		}
	}
}
