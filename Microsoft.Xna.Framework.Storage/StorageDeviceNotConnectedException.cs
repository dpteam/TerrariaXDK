using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Storage;

public class StorageDeviceNotConnectedException : ExternalException
{
	public StorageDeviceNotConnectedException()
	{
	}

	public StorageDeviceNotConnectedException(string message)
		: base(message)
	{
	}

	public StorageDeviceNotConnectedException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
