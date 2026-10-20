namespace Lidgren.Network;

internal sealed class NetUnreliableSequencedReceiver(NetConnection connection) : NetReceiverChannelBase(connection)
{
	private int m_lastReceivedSequenceNumber = -1;

	internal override void ReceiveMessage(NetIncomingMessage msg)
	{
		int sequenceNumber = msg.m_sequenceNumber;
		m_connection.QueueAck(msg.m_receivedMessageType, sequenceNumber);
		if (NetUtility.RelativeSequenceNumber(sequenceNumber, m_lastReceivedSequenceNumber + 1) >= 0)
		{
			m_lastReceivedSequenceNumber = sequenceNumber;
			m_peer.ReleaseMessage(msg);
		}
	}
}
