using System.Security.Cryptography;

namespace Lidgren.Network;

public class NetAESEncryption : NetCryptoProviderBase
{
	static NetAESEncryption()
	{
	}

	public NetAESEncryption(NetPeer peer)
		: base(peer, new AesCryptoServiceProvider
		{
			Mode = CipherMode.CBC,
			Padding = PaddingMode.PKCS7
		})
	{
	}

	public NetAESEncryption(NetPeer peer, string key)
		: this(peer)
	{
		SetKey(key);
	}

	public NetAESEncryption(NetPeer peer, byte[] data)
		: this(peer, data, 0, data.Length)
	{
	}

	public NetAESEncryption(NetPeer peer, byte[] data, int offset, int count)
		: this(peer)
	{
		SetKey(data, offset, count);
	}
}
