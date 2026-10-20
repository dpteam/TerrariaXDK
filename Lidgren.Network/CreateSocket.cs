using System.Net.Sockets;

namespace Lidgren.Network;

internal delegate INetSocket CreateSocket(AddressFamily addressFamily);
