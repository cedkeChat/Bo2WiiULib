using DarkOps_Tool;

public class AddressRange
{
	private AddressType PDesc;

	private byte PId;

	private uint PLow;

	private uint PHigh;

	public AddressType description => PDesc;

	public byte id => PId;

	public uint low => PLow;

	public uint high => PHigh;

	public AddressRange(AddressType desc, byte id, uint low, uint high)
	{
		PId = id;
		PDesc = desc;
		PLow = low;
		PHigh = high;
	}

	public AddressRange(AddressType desc, uint low, uint high)
		: this(desc, (byte)(low >> 24), low, high)
	{
	}
}
