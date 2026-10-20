using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Xna.Framework;

internal static class FNAInternalExtensions
{
	private static readonly FieldInfo f_MemoryStream_Public = typeof(MemoryStream).GetField("_exposable", BindingFlags.Instance | BindingFlags.NonPublic) ?? typeof(MemoryStream).GetField("allowGetBuffer", BindingFlags.Instance | BindingFlags.NonPublic);

	internal static bool TryGetBuffer(this MemoryStream stream, out byte[] buffer)
	{
		if (f_MemoryStream_Public != null)
		{
			if ((bool)f_MemoryStream_Public.GetValue(stream))
			{
				buffer = stream.GetBuffer();
				return true;
			}
			buffer = null;
			return false;
		}
		try
		{
			buffer = stream.GetBuffer();
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			buffer = null;
			return false;
		}
	}
}
