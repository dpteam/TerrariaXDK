namespace Microsoft.Xna.Framework.Input.Touch;

public struct TouchPanelCapabilities
{
	public bool IsConnected { get; private set; }

	public int MaximumTouchCount { get; private set; }

	internal TouchPanelCapabilities(bool isConnected, int maximumTouchCount)
	{
		this = default;
		IsConnected = isConnected;
		MaximumTouchCount = maximumTouchCount;
	}
}
