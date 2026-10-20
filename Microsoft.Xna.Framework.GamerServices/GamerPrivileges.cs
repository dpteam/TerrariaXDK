namespace Microsoft.Xna.Framework.GamerServices;

public sealed class GamerPrivileges
{
	public GamerPrivilegeSetting AllowCommunication { get; private set; }

	public bool AllowOnlineSessions { get; private set; }

	public bool AllowPremiumContent { get; private set; }

	public GamerPrivilegeSetting AllowProfileViewing { get; private set; }

	public bool AllowPurchaseContent { get; private set; }

	public bool AllowTradeContent { get; private set; }

	public GamerPrivilegeSetting AllowUserCreatedContent { get; private set; }

	internal GamerPrivileges()
	{
		AllowCommunication = GamerPrivilegeSetting.Everyone;
		AllowOnlineSessions = true;
		AllowPremiumContent = true;
		AllowProfileViewing = GamerPrivilegeSetting.Everyone;
		AllowPurchaseContent = true;
		AllowTradeContent = true;
		AllowUserCreatedContent = GamerPrivilegeSetting.Everyone;
	}
}
