using System.Net;

namespace Microsoft.Xna.Framework.Net;

public sealed class AvailableNetworkSession
{
	private IPEndPoint _endPoint;

	private IPEndPoint _internalendPoint;

	public int CurrentGamerCount
	{
		get
		{
			return CurrentGamerCount;
		}
		set
		{
			CurrentGamerCount = value;
		}
	}

	public string HostGamertag
	{
		get
		{
			return HostGamertag;
		}
		set
		{
			HostGamertag = value;
		}
	}

	public int OpenPrivateGamerSlots
	{
		get
		{
			return OpenPrivateGamerSlots;
		}
		set
		{
			OpenPrivateGamerSlots = value;
		}
	}

	public int OpenPublicGamerSlots
	{
		get
		{
			return OpenPublicGamerSlots;
		}
		set
		{
			OpenPublicGamerSlots = value;
		}
	}

	public QualityOfService QualityOfService
	{
		get
		{
			return QualityOfService;
		}
		set
		{
			QualityOfService = value;
		}
	}

	public NetworkSessionProperties SessionProperties
	{
		get
		{
			return SessionProperties;
		}
		set
		{
			SessionProperties = value;
		}
	}

	internal IPEndPoint EndPoint
	{
		get
		{
			return _endPoint;
		}
		set
		{
			_endPoint = value;
		}
	}

	internal IPEndPoint InternalEndpont
	{
		get
		{
			return _internalendPoint;
		}
		set
		{
			_internalendPoint = value;
		}
	}

	internal NetworkSessionType SessionType { get; set; }

	internal AvailableNetworkSession(int numGamers, string host, int privateSlots, int publicSlots, NetworkSessionProperties properties, QualityOfService qos)
	{
		CurrentGamerCount = numGamers;
		HostGamertag = host;
		OpenPrivateGamerSlots = privateSlots;
		OpenPublicGamerSlots = publicSlots;
		SessionProperties = properties;
		QualityOfService = qos;
	}

	public AvailableNetworkSession()
	{
		QualityOfService = new QualityOfService();
	}
}
