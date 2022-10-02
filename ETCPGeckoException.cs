using System;
using DarkOps_Tool;

public class ETCPGeckoException : Exception
{
	private ETCPErrorCode PErrorCode;

	public ETCPErrorCode ErrorCode => PErrorCode;

	public ETCPGeckoException(ETCPErrorCode code)
	{
		PErrorCode = code;
	}

	public ETCPGeckoException(ETCPErrorCode code, string message)
		: base(message)
	{
		PErrorCode = code;
	}

	public ETCPGeckoException(ETCPErrorCode code, string message, Exception inner)
		: base(message, inner)
	{
		PErrorCode = code;
	}
}
