using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace Terraria.Net;

/// <summary>
/// Opens the game's port on the router, so a host behind NAT can be reached directly.
///
/// This is the first step of the hosting order in <c>IDEAS.md</c>: UPnP, then a manually
/// forwarded <see cref="NetworkConfig.PublicAddress"/>, then the relay. It goes first because
/// it is the only option that costs neither latency nor somebody else's bandwidth.
///
/// Why not the COM route the PC build used (<c>refs/NATUPNPLib</c>, <c>HNetCfg.NATUPnP</c>):
/// that is Windows-only COM, and this port targets Windows XP and Linux with no third-party
/// libraries. IGD (the router side of UPnP) is a documented pair of protocols - SSDP discovery
/// and SOAP control - so it is implemented here on plain sockets.
///
/// Everything that can be decided without a network is a pure function, because a router is
/// the one thing that cannot be part of a test run: the self-test drives the whole exchange
/// against a stub gateway.
/// </summary>
internal static class UPnP
{
	/// <summary>SSDP multicast group every IGD listens on.</summary>
	internal const string SsdpHost = "239.255.255.250";

	internal const int SsdpPort = 1900;

	/// <summary>Service types to try, in order. Some routers expose only one of them.</summary>
	private static readonly string[] ServiceTypes = new string[2]
	{
		"urn:schemas-upnp-org:service:WANIPConnection:1",
		"urn:schemas-upnp-org:service:WANPPPConnection:1"
	};

	/// <summary>What a discovered gateway needs in order to be talked to.</summary>
	internal sealed class Gateway
	{
		/// <summary>Absolute URL of the SOAP control endpoint.</summary>
		public string ControlUrl;

		/// <summary>Service type, which also goes into the SOAPAction header.</summary>
		public string ServiceType;

		/// <summary>Address the description was served from, used to pick the local address.</summary>
		public string Host;
	}

	/// <summary>
	/// Finds a gateway. <paramref name="ssdpHost"/>/<paramref name="ssdpPort"/> exist for the
	/// test, which points discovery at a stub instead of the multicast group.
	///
	/// Returns null when there is none, and says why in <paramref name="error"/>. A router
	/// without UPnP, or with it switched off, is a normal situation, not a failure.
	/// </summary>
	internal static Gateway Discover(int timeoutMs, out string error, string ssdpHost = SsdpHost, int ssdpPort = SsdpPort)
	{
		error = null;
		string location = null;
		try
		{
			using UdpClient client = new UdpClient();
			client.Client.ReceiveTimeout = timeoutMs;
			IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ssdpHost), ssdpPort);
			byte[] bytes = Encoding.ASCII.GetBytes(
				"M-SEARCH * HTTP/1.1\r\n"
				+ "HOST: " + SsdpHost + ":" + SsdpPort.ToString(CultureInfo.InvariantCulture) + "\r\n"
				+ "MAN: \"ssdp:discover\"\r\n"
				+ "MX: 2\r\n"
				+ "ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n"
				+ "\r\n");
			client.Send(bytes, bytes.Length, endPoint);
			// Several devices answer; the first one that names a description wins, and a device
			// that answers with garbage is skipped rather than aborting the search.
			DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
			while (location == null && DateTime.UtcNow < deadline)
			{
				IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
				byte[] array;
				try
				{
					array = client.Receive(ref remote);
				}
				catch (SocketException)
				{
					break;
				}
				location = FindLocation(Encoding.ASCII.GetString(array));
			}
		}
		catch (Exception ex)
		{
			error = "SSDP search failed: " + ex.Message;
			return null;
		}
		if (location == null)
		{
			error = "no gateway answered the UPnP search";
			return null;
		}
		string text;
		try
		{
			text = HttpGet(location);
		}
		catch (Exception ex)
		{
			error = "cannot read the gateway description: " + ex.Message;
			return null;
		}
		Gateway gateway = ParseDescription(text, location);
		if (gateway == null)
		{
			error = "the gateway description has no WAN connection service";
			return null;
		}
		return gateway;
	}

	/// <summary>
	/// Pulls the LOCATION header out of an SSDP reply. Pure.
	///
	/// Header names are case-insensitive in HTTP and routers are inconsistent about it, so the
	/// comparison ignores case rather than trusting one spelling.
	/// </summary>
	internal static string FindLocation(string response)
	{
		if (string.IsNullOrEmpty(response))
		{
			return null;
		}
		string[] lines = response.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			string text = lines[i].TrimEnd('\r').Trim();
			int num = text.IndexOf(':');
			if (num <= 0)
			{
				continue;
			}
			if (!string.Equals(text.Substring(0, num).Trim(), "LOCATION", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string text2 = text.Substring(num + 1).Trim();
			if (text2.Length > 0)
			{
				return text2;
			}
		}
		return null;
	}

	/// <summary>
	/// Reads the device description and finds the WAN connection control URL. Pure.
	///
	/// <paramref name="descriptionUrl"/> is used to resolve a relative <c>controlURL</c>: the
	/// spec allows both, and most routers use the relative form.
	/// </summary>
	internal static Gateway ParseDescription(string xml, string descriptionUrl)
	{
		if (string.IsNullOrEmpty(xml))
		{
			return null;
		}
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			xmlDocument.LoadXml(xml);
		}
		catch (XmlException)
		{
			return null;
		}
		Uri uri = null;
		try
		{
			uri = new Uri(descriptionUrl);
		}
		catch (UriFormatException)
		{
			return null;
		}
		// A service is a serviceType next to its controlURL, so they are read as pairs rather
		// than by walking the tree: routers nest them differently and the order is what matters.
		XmlNodeList services = xmlDocument.SelectNodes("//*[local-name()='service']");
		if (services == null)
		{
			return null;
		}
		for (int i = 0; i < ServiceTypes.Length; i++)
		{
			for (int j = 0; j < services.Count; j++)
			{
				string type = ChildText(services[j], "serviceType");
				if (type == null || !type.StartsWith(ServiceTypes[i], StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string control = ChildText(services[j], "controlURL");
				if (string.IsNullOrEmpty(control))
				{
					continue;
				}
				Gateway gateway = new Gateway();
				gateway.ServiceType = type;
				gateway.Host = uri.Host;
				gateway.ControlUrl = Resolve(uri, control);
				return gateway;
			}
		}
		return null;
	}

	/// <summary>
	/// The SOAP body for <c>GetSpecificPortMappingEntry</c>, used to find out who holds a port
	/// that the router says is already taken. Pure.
	/// </summary>
	internal static string BuildGetSpecificPortMappingEntry(string serviceType, int externalPort)
	{
		return Envelope(serviceType, "GetSpecificPortMappingEntry",
			"<NewRemoteHost></NewRemoteHost>"
			+ "<NewExternalPort>" + externalPort.ToString(CultureInfo.InvariantCulture) + "</NewExternalPort>"
			+ "<NewProtocol>TCP</NewProtocol>");
	}

	/// <summary>
	/// Reads an existing mapping: the internal client and whether it is enabled. Pure. Null when
	/// the reply has no mapping in it.
	/// </summary>
	internal static string ParseSpecificMapping(string soapResponse, out bool enabled)
	{
		enabled = FindElementText(soapResponse, "NewEnabled") == "1";
		string text = FindElementText(soapResponse, "NewInternalClient");
		if (text == null)
		{
			return null;
		}
		text = text.Trim();
		return (text.Length == 0) ? null : text;
	}

	private static string Resolve(Uri baseUri, string control)
	{
		Uri result;
		if (Uri.TryCreate(baseUri, control, out result))
		{
			return result.AbsoluteUri;
		}
		return control;
	}

	private static string ChildText(XmlNode parent, string localName)
	{
		XmlNodeList childNodes = parent.ChildNodes;
		for (int i = 0; i < childNodes.Count; i++)
		{
			if (string.Equals(childNodes[i].LocalName, localName, StringComparison.OrdinalIgnoreCase))
			{
				return childNodes[i].InnerText.Trim();
			}
		}
		return null;
	}

	/// <summary>
	/// The SOAP body for <c>AddPortMapping</c>. Pure, so the exact field names - which are the
	/// part routers are strict about - are checked by a test rather than by hoping.
	/// </summary>
	internal static string BuildAddPortMapping(string serviceType, int externalPort, int internalPort, string internalClient, string description, int leaseSeconds)
	{
		return Envelope(serviceType, "AddPortMapping",
			"<NewRemoteHost></NewRemoteHost>"
			+ "<NewExternalPort>" + externalPort.ToString(CultureInfo.InvariantCulture) + "</NewExternalPort>"
			+ "<NewProtocol>TCP</NewProtocol>"
			+ "<NewInternalPort>" + internalPort.ToString(CultureInfo.InvariantCulture) + "</NewInternalPort>"
			+ "<NewInternalClient>" + XmlEscape(internalClient) + "</NewInternalClient>"
			+ "<NewEnabled>1</NewEnabled>"
			+ "<NewPortMappingDescription>" + XmlEscape(description) + "</NewPortMappingDescription>"
			+ "<NewLeaseDuration>" + leaseSeconds.ToString(CultureInfo.InvariantCulture) + "</NewLeaseDuration>");
	}

	internal static string BuildDeletePortMapping(string serviceType, int externalPort)
	{
		return Envelope(serviceType, "DeletePortMapping",
			"<NewRemoteHost></NewRemoteHost>"
			+ "<NewExternalPort>" + externalPort.ToString(CultureInfo.InvariantCulture) + "</NewExternalPort>"
			+ "<NewProtocol>TCP</NewProtocol>");
	}

	internal static string BuildGetExternalAddress(string serviceType)
	{
		return Envelope(serviceType, "GetExternalIPAddress", "");
	}

	private static string Envelope(string serviceType, string action, string arguments)
	{
		return "<?xml version=\"1.0\"?>"
			+ "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
			+ "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
			+ "<s:Body><u:" + action + " xmlns:u=\"" + XmlEscape(serviceType) + "\">"
			+ arguments
			+ "</u:" + action + "></s:Body></s:Envelope>";
	}

	private static string XmlEscape(string text)
	{
		if (text == null)
		{
			return "";
		}
		return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
	}

	/// <summary>
	/// Finds the text of the first element with this local name, ignoring any namespace prefix.
	///
	/// Deliberately not an XML parser. Router replies are frequently not well-formed - undeclared
	/// prefixes, stray bytes, a `&amp;` where it should be escaped - and refusing to read a reply
	/// because of a missing xmlns would turn "the router said 718" into "the router said
	/// nothing". Only leaf elements are supported, which is all UPnP control answers contain.
	/// </summary>
	internal static string FindElementText(string xml, string localName)
	{
		if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(localName))
		{
			return null;
		}
		int num = 0;
		while (true)
		{
			int num2 = xml.IndexOf('<', num);
			if (num2 < 0)
			{
				return null;
			}
			int num3 = num2 + 1;
			if (num3 >= xml.Length || xml[num3] == '/')
			{
				num = num3;
				continue;
			}
			int num4 = num3;
			while (num4 < xml.Length)
			{
				char c = xml[num4];
				if (!char.IsLetterOrDigit(c) && c != ':' && c != '_' && c != '-' && c != '.')
				{
					break;
				}
				num4++;
			}
			string text = xml.Substring(num3, num4 - num3);
			int num5 = text.LastIndexOf(':');
			string text2 = (num5 >= 0) ? text.Substring(num5 + 1) : text;
			if (!string.Equals(text2, localName, StringComparison.OrdinalIgnoreCase))
			{
				num = num4;
				continue;
			}
			int num6 = xml.IndexOf('>', num4);
			if (num6 < 0)
			{
				return null;
			}
			if (num6 > num3 && xml[num6 - 1] == '/')
			{
				return "";
			}
			int num7 = xml.IndexOf("</", num6);
			if (num7 < 0)
			{
				return null;
			}
			return xml.Substring(num6 + 1, num7 - num6 - 1).Trim();
		}
	}

	/// <summary>
	/// True when the reply is a SOAP fault, with its UPnP error code. Pure.
	///
	/// A fault is recognised by the presence of an error code rather than by the word "Fault":
	/// success replies never carry one, and some routers spell the fault envelope differently.
	/// </summary>
	internal static bool IsFault(string soapResponse, out int code, out string description)
	{
		code = 0;
		description = FindElementText(soapResponse, "errorDescription");
		string text = FindElementText(soapResponse, "errorCode");
		if (text == null)
		{
			return false;
		}
		int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out code);
		return true;
	}

	/// <summary>The external address a gateway reports. Pure: null when there is none.</summary>
	internal static string ParseExternalAddress(string soapResponse)
	{
		string text = FindElementText(soapResponse, "NewExternalIPAddress");
		if (text == null)
		{
			return null;
		}
		text = text.Trim();
		return (text.Length == 0 || text == "0.0.0.0") ? null : text;
	}

	/// <summary>
	/// The local address this machine uses to reach <paramref name="host"/>, which is what the
	/// router has to forward to. A UDP connect sends nothing: it only asks the stack to pick a
	/// route, which is the standard way to learn the address that is actually routable.
	/// </summary>
	internal static string LocalAddressFor(string host)
	{
		try
		{
			using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.Connect(host, 9);
			IPEndPoint iPEndPoint = socket.LocalEndPoint as IPEndPoint;
			return (iPEndPoint == null) ? null : iPEndPoint.Address.ToString();
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Opens the port on the gateway. Returns true when the mapping is in place.
	///
	/// <paramref name="created"/> says whether this call created the mapping or found an existing
	/// one that already points here. The difference decides whether it may be removed later:
	/// <c>DeletePortMapping</c> removes the entry whoever made it, so a game that cleaned up a
	/// mapping it merely found would delete the port forward the player set up by hand.
	///
	/// Error 718 (conflict) is not treated as success on its own: it means the external port is
	/// already forwarded, and possibly to a different machine. On a home network that is exactly
	/// the case a host needs to hear about - "7777 already goes to 192.168.1.9" is the answer to
	/// "why can nobody join me", while silently reporting success would hide it. So the existing
	/// entry is read back, and the port counts as open only when it points at this machine.
	/// </summary>
	internal static bool Map(Gateway gateway, int port, string localAddress, out bool created, out string error)
	{
		created = false;
		error = null;
		string text = Call(gateway, "AddPortMapping", BuildAddPortMapping(
			gateway.ServiceType, port, port, localAddress, "TerrariaXDK", 0), out error);
		if (text == null)
		{
			return false;
		}
		int num;
		string text2;
		if (!IsFault(text, out num, out text2))
		{
			created = true;
			return true;
		}
		if (num != 718)
		{
			error = "the router refused the mapping: " + num + (text2 == null ? "" : " (" + text2 + ")");
			return false;
		}
		string text3 = DescribeMapping(gateway, port, out error);
		if (text3 == null)
		{
			error = "port " + port + " is already forwarded, and the router would not say where ("
				+ (error ?? "no answer") + ")";
			return false;
		}
		if (text3 == localAddress)
		{
			Program.LogInfo("UPnP", "port " + port + " was already forwarded to this machine; leaving it alone");
			return true;
		}
		error = "port " + port + " is already forwarded to " + text3 + ", not to " + localAddress;
		return false;
	}

	internal static bool Unmap(Gateway gateway, int port, out string error)
	{
		error = null;
		string text = Call(gateway, "DeletePortMapping", BuildDeletePortMapping(gateway.ServiceType, port), out error);
		if (text == null)
		{
			return false;
		}
		int num;
		string text2;
		if (!IsFault(text, out num, out text2))
		{
			return true;
		}
		// 714 means there was nothing to remove, which is what we wanted anyway.
		if (num == 714)
		{
			return true;
		}
		error = "the router did not remove the mapping: " + num + (text2 == null ? "" : " (" + text2 + ")");
		return false;
	}

	/// <summary>The address the gateway reports for itself, or null.</summary>
	internal static string ExternalAddress(Gateway gateway, out string error)
	{
		error = null;
		string text = Call(gateway, "GetExternalIPAddress", BuildGetExternalAddress(gateway.ServiceType), out error);
		return (text == null) ? null : ParseExternalAddress(text);
	}

	/// <summary>
	/// Who the port currently goes to, or null when nothing is forwarded there. Read-only: used
	/// by the <c>--upnp</c> diagnostic and by <see cref="Map"/>'s conflict path.
	/// </summary>
	internal static string DescribeMapping(Gateway gateway, int port, out string error)
	{
		string text = Call(gateway, "GetSpecificPortMappingEntry",
			BuildGetSpecificPortMappingEntry(gateway.ServiceType, port), out error);
		if (text == null)
		{
			return null;
		}
		int num;
		string text2;
		if (IsFault(text, out num, out text2))
		{
			// 714 is "no such entry", which is a normal answer, not an error.
			error = (num == 714) ? "no entry for this port" : (num + (text2 == null ? "" : " (" + text2 + ")"));
			return null;
		}
		bool flag;
		return ParseSpecificMapping(text, out flag);
	}

	/// <summary>
	/// Sends one SOAP action. Returns the reply body, or null with a reason.
	///
	/// Both success and fault replies come back as HTTP 500 with a SOAP body on some routers and
	/// as 200 on others, so the body is what decides; the status code is only used when there is
	/// no body at all.
	/// </summary>
	private static string Call(Gateway gateway, string action, string body, out string error)
	{
		error = null;
		string text;
		try
		{
			text = HttpPost(gateway.ControlUrl, "\"" + gateway.ServiceType + "#" + action + "\"", body);
		}
		catch (WebException ex)
		{
			// A fault arrives as HTTP 500; the body is still the answer, not a transport error.
			HttpWebResponse httpWebResponse = ex.Response as HttpWebResponse;
			if (httpWebResponse == null)
			{
				error = action + " failed: " + ex.Message;
				return null;
			}
			using Stream stream = httpWebResponse.GetResponseStream();
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
			return streamReader.ReadToEnd();
		}
		catch (Exception ex2)
		{
			error = action + " failed: " + ex2.Message;
			return null;
		}
		return text;
	}

	private static string HttpGet(string url)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = "GET";
		httpWebRequest.Timeout = 4000;
		httpWebRequest.ReadWriteTimeout = 4000;
		httpWebRequest.KeepAlive = false;
		using WebResponse webResponse = httpWebRequest.GetResponse();
		using Stream stream = webResponse.GetResponseStream();
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	private static string HttpPost(string url, string soapAction, string body)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(body);
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = "POST";
		httpWebRequest.ContentType = "text/xml; charset=\"utf-8\"";
		httpWebRequest.Headers.Add("SOAPAction", soapAction);
		httpWebRequest.ContentLength = bytes.Length;
		httpWebRequest.Timeout = 4000;
		httpWebRequest.ReadWriteTimeout = 4000;
		httpWebRequest.KeepAlive = false;
		using (Stream requestStream = httpWebRequest.GetRequestStream())
		{
			requestStream.Write(bytes, 0, bytes.Length);
		}
		using WebResponse webResponse = httpWebRequest.GetResponse();
		using Stream stream = webResponse.GetResponseStream();
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
		return streamReader.ReadToEnd();
	}
}
