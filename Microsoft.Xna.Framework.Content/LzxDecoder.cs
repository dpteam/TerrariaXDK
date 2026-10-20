using System.IO;

namespace Microsoft.Xna.Framework.Content;

internal class LzxDecoder
{
	private class BitBuffer
	{
		private uint buffer;

		private byte bitsleft;

		private Stream byteStream;

		public BitBuffer(Stream stream)
		{
			byteStream = stream;
			InitBitStream();
		}

		public void InitBitStream()
		{
			buffer = 0u;
			bitsleft = 0;
		}

		public void EnsureBits(byte bits)
		{
			while (bitsleft < bits)
			{
				int num = (byte)byteStream.ReadByte();
				int num2 = (byte)byteStream.ReadByte();
				buffer |= (uint)(((num2 << 8) | num) << 16 - bitsleft);
				bitsleft += 16;
			}
		}

		public uint PeekBits(byte bits)
		{
			return buffer >> 32 - bits;
		}

		public void RemoveBits(byte bits)
		{
			buffer <<= (int)bits;
			bitsleft -= bits;
		}

		public uint ReadBits(byte bits)
		{
			uint result = 0u;
			if (bits > 0)
			{
				EnsureBits(bits);
				result = PeekBits(bits);
				RemoveBits(bits);
			}
			return result;
		}

		public uint GetBuffer()
		{
			return buffer;
		}

		public byte GetBitsLeft()
		{
			return bitsleft;
		}
	}

	private struct LzxState
	{
		public uint R0;

		public uint R1;

		public uint R2;

		public ushort main_elements;

		public int header_read;

		public LzxConstants.BLOCKTYPE block_type;

		public uint block_length;

		public uint block_remaining;

		public uint frames_read;

		public int intel_filesize;

		public int intel_curpos;

		public int intel_started;

		public ushort[] PRETREE_table;

		public byte[] PRETREE_len;

		public ushort[] MAINTREE_table;

		public byte[] MAINTREE_len;

		public ushort[] LENGTH_table;

		public byte[] LENGTH_len;

		public ushort[] ALIGNED_table;

		public byte[] ALIGNED_len;

		public uint actual_size;

		public byte[] window;

		public uint window_size;

		public uint window_posn;
	}

	public static uint[] position_base;

	public static byte[] extra_bits;

	private LzxState m_state;

	public LzxDecoder(int window)
	{
		uint num = (uint)(1 << window);
		if (window < 15 || window > 21)
		{
			throw new UnsupportedWindowSizeRange();
		}
		m_state = default;
		m_state.actual_size = 0u;
		m_state.window = new byte[num];
		for (int i = 0; i < num; i++)
		{
			m_state.window[i] = 220;
		}
		m_state.actual_size = num;
		m_state.window_size = num;
		m_state.window_posn = 0u;
		if (extra_bits == null)
		{
			extra_bits = new byte[52];
			int j = 0;
			int num2 = 0;
			for (; j <= 50; j += 2)
			{
				extra_bits[j] = (extra_bits[j + 1] = (byte)num2);
				if (j != 0 && num2 < 17)
				{
					num2++;
				}
			}
		}
		if (position_base == null)
		{
			position_base = new uint[51];
			int k = 0;
			int num3 = 0;
			for (; k <= 50; k++)
			{
				position_base[k] = (uint)num3;
				num3 += 1 << (int)extra_bits[k];
			}
		}
		if (1 == 0)
		{
		}
		int num4 = window switch
		{
			20 => 42, 
			21 => 50, 
			_ => window << 1, 
		};
		if (1 == 0)
		{
		}
		int num5 = num4;
		m_state.R0 = (m_state.R1 = (m_state.R2 = 1u));
		m_state.main_elements = (ushort)(256 + (num5 << 3));
		m_state.header_read = 0;
		m_state.frames_read = 0u;
		m_state.block_remaining = 0u;
		m_state.block_type = LzxConstants.BLOCKTYPE.INVALID;
		m_state.intel_curpos = 0;
		m_state.intel_started = 0;
		m_state.PRETREE_table = new ushort[104];
		m_state.PRETREE_len = new byte[84];
		m_state.MAINTREE_table = new ushort[5408];
		m_state.MAINTREE_len = new byte[720];
		m_state.LENGTH_table = new ushort[4596];
		m_state.LENGTH_len = new byte[314];
		m_state.ALIGNED_table = new ushort[144];
		m_state.ALIGNED_len = new byte[72];
		for (int l = 0; l < 656; l++)
		{
			m_state.MAINTREE_len[l] = 0;
		}
		for (int m = 0; m < 250; m++)
		{
			m_state.LENGTH_len[m] = 0;
		}
	}

	public int Decompress(Stream inData, int inLen, Stream outData, int outLen)
	{
		BitBuffer bitBuffer = new BitBuffer(inData);
		long position = inData.Position;
		long num = inData.Position + inLen;
		byte[] window = m_state.window;
		uint num2 = m_state.window_posn;
		uint window_size = m_state.window_size;
		uint num3 = m_state.R0;
		uint num4 = m_state.R1;
		uint num5 = m_state.R2;
		int num6 = outLen;
		bitBuffer.InitBitStream();
		if (m_state.header_read == 0)
		{
			if (bitBuffer.ReadBits(1) != 0)
			{
				uint num7 = bitBuffer.ReadBits(16);
				uint num8 = bitBuffer.ReadBits(16);
				m_state.intel_filesize = (int)((num7 << 16) | num8);
			}
			m_state.header_read = 1;
		}
		while (num6 > 0)
		{
			if (m_state.block_remaining == 0)
			{
				if (m_state.block_type == LzxConstants.BLOCKTYPE.UNCOMPRESSED)
				{
					if ((m_state.block_length & 1) == 1)
					{
						inData.ReadByte();
					}
					bitBuffer.InitBitStream();
				}
				m_state.block_type = (LzxConstants.BLOCKTYPE)bitBuffer.ReadBits(3);
				uint num9 = bitBuffer.ReadBits(16);
				uint num10 = bitBuffer.ReadBits(8);
				m_state.block_remaining = (m_state.block_length = (num9 << 8) | num10);
				switch (m_state.block_type)
				{
				case LzxConstants.BLOCKTYPE.ALIGNED:
					num9 = 0u;
					num10 = 0u;
					for (; num9 < 8; num9++)
					{
						num10 = bitBuffer.ReadBits(3);
						m_state.ALIGNED_len[num9] = (byte)num10;
					}
					MakeDecodeTable(8u, 7u, m_state.ALIGNED_len, m_state.ALIGNED_table);
					goto case LzxConstants.BLOCKTYPE.VERBATIM;
				case LzxConstants.BLOCKTYPE.VERBATIM:
					ReadLengths(m_state.MAINTREE_len, 0u, 256u, bitBuffer);
					ReadLengths(m_state.MAINTREE_len, 256u, m_state.main_elements, bitBuffer);
					MakeDecodeTable(656u, 12u, m_state.MAINTREE_len, m_state.MAINTREE_table);
					if (m_state.MAINTREE_len[232] != 0)
					{
						m_state.intel_started = 1;
					}
					ReadLengths(m_state.LENGTH_len, 0u, 249u, bitBuffer);
					MakeDecodeTable(250u, 12u, m_state.LENGTH_len, m_state.LENGTH_table);
					break;
				case LzxConstants.BLOCKTYPE.UNCOMPRESSED:
				{
					m_state.intel_started = 1;
					bitBuffer.EnsureBits(16);
					if (bitBuffer.GetBitsLeft() > 16)
					{
						inData.Seek(-2L, SeekOrigin.Current);
					}
					byte b = (byte)inData.ReadByte();
					byte b2 = (byte)inData.ReadByte();
					byte b3 = (byte)inData.ReadByte();
					byte b4 = (byte)inData.ReadByte();
					num3 = (uint)(b | (b2 << 8) | (b3 << 16) | (b4 << 24));
					b = (byte)inData.ReadByte();
					b2 = (byte)inData.ReadByte();
					b3 = (byte)inData.ReadByte();
					b4 = (byte)inData.ReadByte();
					num4 = (uint)(b | (b2 << 8) | (b3 << 16) | (b4 << 24));
					b = (byte)inData.ReadByte();
					b2 = (byte)inData.ReadByte();
					b3 = (byte)inData.ReadByte();
					b4 = (byte)inData.ReadByte();
					num5 = (uint)(b | (b2 << 8) | (b3 << 16) | (b4 << 24));
					break;
				}
				default:
					return -1;
				}
			}
			if (inData.Position > position + inLen && (inData.Position > position + inLen + 2 || bitBuffer.GetBitsLeft() < 16))
			{
				return -1;
			}
			int num11;
			while ((num11 = (int)m_state.block_remaining) > 0 && num6 > 0)
			{
				if (num11 > num6)
				{
					num11 = num6;
				}
				num6 -= num11;
				m_state.block_remaining -= (uint)num11;
				num2 &= window_size - 1;
				if (num2 + num11 > window_size)
				{
					return -1;
				}
				switch (m_state.block_type)
				{
				case LzxConstants.BLOCKTYPE.VERBATIM:
					while (num11 > 0)
					{
						int num24 = (int)ReadHuffSym(m_state.MAINTREE_table, m_state.MAINTREE_len, 656u, 12u, bitBuffer);
						if (num24 < 256)
						{
							window[num2++] = (byte)num24;
							num11--;
							continue;
						}
						num24 -= 256;
						int num25 = num24 & 7;
						if (num25 == 7)
						{
							int num26 = (int)ReadHuffSym(m_state.LENGTH_table, m_state.LENGTH_len, 250u, 12u, bitBuffer);
							num25 += num26;
						}
						num25 += 2;
						int num27 = num24 >> 3;
						if (num27 > 2)
						{
							if (num27 != 3)
							{
								int num28 = extra_bits[num27];
								int num29 = (int)bitBuffer.ReadBits((byte)num28);
								num27 = (int)(position_base[num27] - 2) + num29;
							}
							else
							{
								num27 = 1;
							}
							num5 = num4;
							num4 = num3;
							num3 = (uint)num27;
						}
						else
						{
							switch (num27)
							{
							case 0:
								num27 = (int)num3;
								break;
							case 1:
								num27 = (int)num4;
								num4 = num3;
								num3 = (uint)num27;
								break;
							default:
								num27 = (int)num5;
								num5 = num3;
								num3 = (uint)num27;
								break;
							}
						}
						int num30 = (int)num2;
						num11 -= num25;
						int num31;
						if (num2 >= num27)
						{
							num31 = num30 - num27;
						}
						else
						{
							num31 = num30 + ((int)window_size - num27);
							int num32 = num27 - (int)num2;
							if (num32 < num25)
							{
								num25 -= num32;
								num2 += (uint)num32;
								while (num32-- > 0)
								{
									window[num30++] = window[num31++];
								}
								num31 = 0;
							}
						}
						num2 += (uint)num25;
						while (num25-- > 0)
						{
							window[num30++] = window[num31++];
						}
					}
					break;
				case LzxConstants.BLOCKTYPE.ALIGNED:
					while (num11 > 0)
					{
						int num12 = (int)ReadHuffSym(m_state.MAINTREE_table, m_state.MAINTREE_len, 656u, 12u, bitBuffer);
						if (num12 < 256)
						{
							window[num2++] = (byte)num12;
							num11--;
							continue;
						}
						num12 -= 256;
						int num13 = num12 & 7;
						if (num13 == 7)
						{
							int num14 = (int)ReadHuffSym(m_state.LENGTH_table, m_state.LENGTH_len, 250u, 12u, bitBuffer);
							num13 += num14;
						}
						num13 += 2;
						int num15 = num12 >> 3;
						if (num15 > 2)
						{
							int num16 = extra_bits[num15];
							num15 = (int)(position_base[num15] - 2);
							if (num16 > 3)
							{
								num16 -= 3;
								int num17 = (int)bitBuffer.ReadBits((byte)num16);
								num15 += num17 << 3;
								int num18 = (int)ReadHuffSym(m_state.ALIGNED_table, m_state.ALIGNED_len, 8u, 7u, bitBuffer);
								num15 += num18;
							}
							else if (num16 == 3)
							{
								int num19 = (int)ReadHuffSym(m_state.ALIGNED_table, m_state.ALIGNED_len, 8u, 7u, bitBuffer);
								num15 += num19;
							}
							else if (num16 > 0)
							{
								int num20 = (int)bitBuffer.ReadBits((byte)num16);
								num15 += num20;
							}
							else
							{
								num15 = 1;
							}
							num5 = num4;
							num4 = num3;
							num3 = (uint)num15;
						}
						else
						{
							switch (num15)
							{
							case 0:
								num15 = (int)num3;
								break;
							case 1:
								num15 = (int)num4;
								num4 = num3;
								num3 = (uint)num15;
								break;
							default:
								num15 = (int)num5;
								num5 = num3;
								num3 = (uint)num15;
								break;
							}
						}
						int num21 = (int)num2;
						num11 -= num13;
						int num22;
						if (num2 >= num15)
						{
							num22 = num21 - num15;
						}
						else
						{
							num22 = num21 + ((int)window_size - num15);
							int num23 = num15 - (int)num2;
							if (num23 < num13)
							{
								num13 -= num23;
								num2 += (uint)num23;
								while (num23-- > 0)
								{
									window[num21++] = window[num22++];
								}
								num22 = 0;
							}
						}
						num2 += (uint)num13;
						while (num13-- > 0)
						{
							window[num21++] = window[num22++];
						}
					}
					break;
				case LzxConstants.BLOCKTYPE.UNCOMPRESSED:
				{
					if (inData.Position + num11 > num)
					{
						return -1;
					}
					byte[] array = new byte[num11];
					inData.Read(array, 0, num11);
					array.CopyTo(window, (int)num2);
					num2 += (uint)num11;
					break;
				}
				default:
					return -1;
				}
			}
		}
		if (num6 != 0)
		{
			return -1;
		}
		int num33 = (int)num2;
		if (num33 == 0)
		{
			num33 = (int)window_size;
		}
		num33 -= outLen;
		outData.Write(window, num33, outLen);
		m_state.window_posn = num2;
		m_state.R0 = num3;
		m_state.R1 = num4;
		m_state.R2 = num5;
		if (m_state.frames_read++ < 32768 && m_state.intel_filesize != 0)
		{
			if (outLen <= 6 || m_state.intel_started == 0)
			{
				m_state.intel_curpos += outLen;
			}
			else
			{
				int num34 = outLen - 10;
				uint num35 = (uint)m_state.intel_curpos;
				m_state.intel_curpos = (int)num35 + outLen;
				while (outData.Position < num34)
				{
					if (outData.ReadByte() != 232)
					{
						num35++;
					}
				}
			}
			return -1;
		}
		return 0;
	}

	private int MakeDecodeTable(uint nsyms, uint nbits, byte[] length, ushort[] table)
	{
		byte b = 1;
		uint num = 0u;
		uint num2 = (uint)(1 << (int)nbits);
		uint num3 = num2 >> 1;
		uint num4 = num3;
		while (b <= nbits)
		{
			for (ushort num5 = 0; num5 < nsyms; num5++)
			{
				if (length[num5] == b)
				{
					uint num6 = num;
					if ((num += num3) > num2)
					{
						return 1;
					}
					uint num7 = num3;
					while (num7-- != 0)
					{
						table[num6++] = num5;
					}
				}
			}
			num3 >>= 1;
			b++;
		}
		if (num != num2)
		{
			for (ushort num8 = (ushort)num; num8 < num2; num8++)
			{
				table[num8] = 0;
			}
			num <<= 16;
			num2 <<= 16;
			num3 = 32768u;
			while (b <= 16)
			{
				for (ushort num9 = 0; num9 < nsyms; num9++)
				{
					if (length[num9] == b)
					{
						uint num10 = num >> 16;
						for (uint num11 = 0u; num11 < b - nbits; num11++)
						{
							if (table[num10] == 0)
							{
								table[num4 << 1] = 0;
								table[(num4 << 1) + 1] = 0;
								table[num10] = (ushort)num4++;
							}
							num10 = (uint)(table[num10] << 1);
							if (((num >> (int)(15 - num11)) & 1) == 1)
							{
								num10++;
							}
						}
						table[num10] = num9;
						if ((num += num3) > num2)
						{
							return 1;
						}
					}
				}
				num3 >>= 1;
				b++;
			}
		}
		if (num == num2)
		{
			return 0;
		}
		for (ushort num12 = 0; num12 < nsyms; num12++)
		{
			if (length[num12] != 0)
			{
				return 1;
			}
		}
		return 0;
	}

	private void ReadLengths(byte[] lens, uint first, uint last, BitBuffer bitbuf)
	{
		uint num;
		for (num = 0u; num < 20; num++)
		{
			uint num2 = bitbuf.ReadBits(4);
			m_state.PRETREE_len[num] = (byte)num2;
		}
		MakeDecodeTable(20u, 6u, m_state.PRETREE_len, m_state.PRETREE_table);
		num = first;
		while (num < last)
		{
			int num3 = (int)ReadHuffSym(m_state.PRETREE_table, m_state.PRETREE_len, 20u, 6u, bitbuf);
			switch (num3)
			{
			case 17:
			{
				uint num5 = bitbuf.ReadBits(4);
				num5 += 4;
				while (num5-- != 0)
				{
					lens[num++] = 0;
				}
				break;
			}
			case 18:
			{
				uint num4 = bitbuf.ReadBits(5);
				num4 += 20;
				while (num4-- != 0)
				{
					lens[num++] = 0;
				}
				break;
			}
			case 19:
			{
				uint num6 = bitbuf.ReadBits(1);
				num6 += 4;
				num3 = (int)ReadHuffSym(m_state.PRETREE_table, m_state.PRETREE_len, 20u, 6u, bitbuf);
				num3 = lens[num] - num3;
				if (num3 < 0)
				{
					num3 += 17;
				}
				while (num6-- != 0)
				{
					lens[num++] = (byte)num3;
				}
				break;
			}
			default:
				num3 = lens[num] - num3;
				if (num3 < 0)
				{
					num3 += 17;
				}
				lens[num++] = (byte)num3;
				break;
			}
		}
	}

	private uint ReadHuffSym(ushort[] table, byte[] lengths, uint nsyms, uint nbits, BitBuffer bitbuf)
	{
		bitbuf.EnsureBits(16);
		uint num;
		uint num2;
		if ((num = table[bitbuf.PeekBits((byte)nbits)]) >= nsyms)
		{
			num2 = (uint)(1 << (int)(32 - nbits));
			do
			{
				num2 >>= 1;
				num <<= 1;
				num |= (((bitbuf.GetBuffer() & num2) != 0) ? 1u : 0u);
				if (num2 == 0)
				{
					return 0u;
				}
			}
			while ((num = table[num]) >= nsyms);
		}
		num2 = lengths[num];
		bitbuf.RemoveBits((byte)num2);
		return num;
	}
}
