using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

internal class tcpconn
{
	private TcpClient client;

	private NetworkStream stream;

	public string Host
	{
		get;
		private set;
	}

	public int Port
	{
		get;
		private set;
	}

	public tcpconn(string host, int port)
	{
		Host = host;
		Port = port;
		client = null;
		stream = null;
	}

	public void Connect()
	{
		try
		{
			Close();
		}
		catch (Exception)
		{
		}
		client = new TcpClient();
		client.NoDelay = true;
		IAsyncResult asyncResult = client.BeginConnect(Host, Port, null, null);
		WaitHandle asyncWaitHandle = asyncResult.AsyncWaitHandle;
		try
		{
			if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5.0), exitContext: false))
			{
				client.Close();
				throw new IOException("Connection timoeut.", new TimeoutException());
			}
			client.EndConnect(asyncResult);
		}
		finally
		{
			asyncWaitHandle.Close();
		}
		stream = client.GetStream();
		stream.ReadTimeout = 10000;
		stream.WriteTimeout = 10000;
	}

	public void Close()
	{
		try
		{
			if (client == null)
			{
				throw new IOException("Not connected.", new NullReferenceException());
			}
			client.Close();
		}
		catch (Exception)
		{
		}
		finally
		{
			client = null;
		}
	}

	public void Purge()
	{
		if (stream == null)
		{
			throw new IOException("Not connected.", new NullReferenceException());
		}
		stream.Flush();
	}

	public void Read(byte[] buffer, uint nobytes, ref uint bytes_read)
	{
		try
		{
			int num = 0;
			if (stream == null)
			{
				throw new IOException("Not connected.", new NullReferenceException());
			}
			bytes_read = 0u;
			while (nobytes != 0)
			{
				int num2 = stream.Read(buffer, num, (int)nobytes);
				if (num2 < 0)
				{
					break;
				}
				bytes_read += (uint)num2;
				num += num2;
				nobytes = (uint)((int)nobytes - num2);
			}
		}
		catch (ObjectDisposedException innerException)
		{
			throw new IOException("Connection closed.", innerException);
		}
	}

	public void Write(byte[] buffer, int nobytes, ref uint bytes_written)
	{
		try
		{
			if (stream == null)
			{
				throw new IOException("Not connected.", new NullReferenceException());
			}
			stream.Write(buffer, 0, nobytes);
			bytes_written = (uint)((nobytes >= 0) ? nobytes : 0);
			stream.Flush();
		}
		catch (ObjectDisposedException innerException)
		{
			throw new IOException("Connection closed.", innerException);
		}
	}
}
