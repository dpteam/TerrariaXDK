using System;
using System.IO;
using System.Threading;

namespace Microsoft.Xna.Framework.Storage;

public sealed class StorageDevice
{
	private class NotAsyncLie : IAsyncResult
	{
		public object AsyncState { get; private set; }

		public bool CompletedSynchronously => true;

		public bool IsCompleted => true;

		public WaitHandle AsyncWaitHandle { get; private set; }

		public NotAsyncLie(object state)
		{
			AsyncState = state;
			AsyncWaitHandle = new ManualResetEvent(initialState: true);
		}
	}

	private class ShowSelectorLie : NotAsyncLie
	{
		public readonly PlayerIndex? PlayerIndex;

		public ShowSelectorLie(object state, PlayerIndex? playerIndex)
			: base(state)
		{
			PlayerIndex = playerIndex;
		}
	}

	private class OpenContainerLie : NotAsyncLie
	{
		public readonly string DisplayName;

		public OpenContainerLie(object state, string displayName)
			: base(state)
		{
			DisplayName = displayName;
		}
	}

	private PlayerIndex? devicePlayer;

	private static readonly string storageRoot = FNAPlatform.GetStorageRoot();

	private static readonly DriveInfo drive = FNAPlatform.GetDriveInfo(storageRoot);

	public long FreeSpace
	{
		get
		{
			try
			{
				if (drive == null)
				{
					return long.MaxValue;
				}
				return drive.AvailableFreeSpace;
			}
			catch (Exception innerException)
			{
				throw new StorageDeviceNotConnectedException("The storage device bound to the container is not connected.", innerException);
			}
		}
	}

	public bool IsConnected
	{
		get
		{
			try
			{
				if (drive == null)
				{
					return true;
				}
				return drive.IsReady;
			}
			catch
			{
				return false;
			}
		}
	}

	public long TotalSpace
	{
		get
		{
			try
			{
				if (drive == null)
				{
					return long.MaxValue;
				}
				return drive.TotalSize;
			}
			catch (Exception innerException)
			{
				throw new StorageDeviceNotConnectedException("The storage device bound to the container is not connected.", innerException);
			}
		}
	}

	public static event EventHandler<EventArgs> DeviceChanged;

	private void OnDeviceChanged()
	{
		if (DeviceChanged != null)
		{
			DeviceChanged(this, null);
		}
	}

	internal StorageDevice(PlayerIndex? player)
	{
		devicePlayer = player;
	}

	public IAsyncResult BeginOpenContainer(string displayName, AsyncCallback callback, object state)
	{
		IAsyncResult asyncResult = new OpenContainerLie(state, displayName);
		callback?.Invoke(asyncResult);
		return asyncResult;
	}

	public StorageContainer EndOpenContainer(IAsyncResult result)
	{
		return new StorageContainer(this, (result as OpenContainerLie).DisplayName, storageRoot, devicePlayer);
	}

	public static IAsyncResult BeginShowSelector(AsyncCallback callback, object state)
	{
		return BeginShowSelector(0, 0, callback, state);
	}

	public static IAsyncResult BeginShowSelector(PlayerIndex player, AsyncCallback callback, object state)
	{
		return BeginShowSelector(player, 0, 0, callback, state);
	}

	public static IAsyncResult BeginShowSelector(int sizeInBytes, int directoryCount, AsyncCallback callback, object state)
	{
		IAsyncResult asyncResult = new ShowSelectorLie(state, null);
		callback?.Invoke(asyncResult);
		return asyncResult;
	}

	public static IAsyncResult BeginShowSelector(PlayerIndex player, int sizeInBytes, int directoryCount, AsyncCallback callback, object state)
	{
		IAsyncResult asyncResult = new ShowSelectorLie(state, player);
		callback?.Invoke(asyncResult);
		return asyncResult;
	}

	public static StorageDevice EndShowSelector(IAsyncResult result)
	{
		return new StorageDevice((result as ShowSelectorLie).PlayerIndex);
	}

	public void DeleteContainer(string titleName)
	{
		throw new NotImplementedException();
	}
}
