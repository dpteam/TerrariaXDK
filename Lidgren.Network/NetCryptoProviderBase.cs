using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;

namespace Lidgren.Network;

public abstract class NetCryptoProviderBase : NetEncryption
{
	protected sealed class ICryptoTransformProvider
	{
		public delegate ICryptoTransform CreateTransform();

		private readonly NetCryptoProviderBase m_cryptoProvider;

		private readonly CreateTransform m_createTransform;

		private readonly ConcurrentStack<ICryptoTransform> m_transforms;

		public ICryptoTransformProvider(NetCryptoProviderBase cryptoProvider, CreateTransform createTransform)
		{
			m_cryptoProvider = cryptoProvider;
			m_createTransform = createTransform;
			m_transforms = new ConcurrentStack<ICryptoTransform>();
		}

		public bool Pop(out ICryptoTransform cryptoTransform)
		{
			if (m_cryptoProvider.ForceNewTransform || !m_transforms.TryPop(out cryptoTransform))
			{
				cryptoTransform = m_createTransform();
			}
			return true;
		}

		public void Push(ICryptoTransform cryptoTransform)
		{
			if (cryptoTransform == null)
			{
				throw new ArgumentNullException("cryptoTransform");
			}
			if (!m_cryptoProvider.ForceNewTransform && cryptoTransform.CanReuseTransform)
			{
				m_transforms.Push(cryptoTransform);
			}
			else
			{
				cryptoTransform.Dispose();
			}
		}
	}

	protected readonly SymmetricAlgorithm m_algorithm;

	protected readonly ICryptoTransformProvider m_encryptorProvider;

	protected readonly ICryptoTransformProvider m_decryptorProvider;

	public bool ForceNewTransform { get; set; }

	public NetCryptoProviderBase(NetPeer peer, SymmetricAlgorithm algo)
		: base(peer)
	{
		m_algorithm = algo;
		m_encryptorProvider = new ICryptoTransformProvider(this, m_algorithm.CreateEncryptor);
		m_decryptorProvider = new ICryptoTransformProvider(this, m_algorithm.CreateDecryptor);
		m_algorithm.GenerateKey();
		m_algorithm.GenerateIV();
	}

	public override void SetKey(byte[] data, int offset, int count)
	{
		int num = m_algorithm.Key.Length;
		byte[] array = new byte[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = data[offset + i % count];
		}
		m_algorithm.Key = array;
		num = m_algorithm.IV.Length;
		array = new byte[num];
		for (int j = 0; j < num; j++)
		{
			array[num - 1 - j] = data[offset + j % count];
		}
		m_algorithm.IV = array;
	}

	public override bool Encrypt(NetOutgoingMessage msg)
	{
		try
		{
			int lengthBits = msg.LengthBits;
			int num = (int)Math.Ceiling((double)lengthBits / (double)m_algorithm.BlockSize);
			int num2 = 4 + num * m_algorithm.BlockSize / 8;
			MemoryStream memoryStream = new MemoryStream(num2);
			memoryStream.Write(BitConverter.GetBytes(lengthBits), 0, 4);
			if (!m_encryptorProvider.Pop(out var cryptoTransform))
			{
				return false;
			}
			CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
			cryptoStream.Write(msg.m_data, 0, msg.LengthBytes);
			cryptoStream.Close();
			m_peer.Recycle(msg.m_data);
			msg.m_bitLength = num2 << 3;
			msg.m_readPosition = msg.m_bitLength;
			msg.m_data = memoryStream.GetBuffer();
			memoryStream.Close();
			return true;
		}
		catch
		{
			throw;
		}
	}

	public override bool Decrypt(NetIncomingMessage msg)
	{
		try
		{
			MemoryStream memoryStream = new MemoryStream(msg.m_data);
			byte[] array = new byte[4];
			if (4 != memoryStream.Read(array, 0, 4))
			{
				throw new Exception();
			}
			int num = BitConverter.ToInt32(array, 0);
			if (!m_decryptorProvider.Pop(out var cryptoTransform))
			{
				return false;
			}
			CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read);
			int num2 = NetUtility.BytesToHoldBits(num);
			byte[] storage = m_peer.GetStorage(num2 << 3);
			int num3;
			for (int i = 0; i < num2; i += num3)
			{
				num3 = cryptoStream.Read(storage, i, num2 - i);
				if (num3 == 0)
				{
					return false;
				}
			}
			cryptoStream.Close();
			m_peer.Recycle(msg.m_data);
			msg.m_data = storage;
			msg.m_bitLength = num;
			msg.m_readPosition = 0;
			return true;
		}
		catch
		{
			throw;
		}
	}
}
