using System.Text;
using Microsoft.Xna.Framework.Net;

namespace Terraria;

/// <summary>
/// Turns a server listing into the two lines the world-select screen shows.
///
/// The list used to be one line with markers appended to the name: <c>Bob [pw] [mods] [v3]
/// [relay]</c>. That line answered "can I join" but not "why", and it broke down completely when
/// a host was incompatible for two reasons at once - the markers ran into the player count on the
/// right, and the name was the only thing left readable.
///
/// Two lines instead: the first is who and how full, the second is what you need to know before
/// spending five seconds on a connection attempt. The split is what makes it readable - a marker
/// is a word in a sentence on its own line, not a bracketed tag glued to a name.
///
/// Pure, and separate from the drawing code, because the interesting part is the wording and the
/// order of precedence, not the coordinates.
/// </summary>
internal static class ServerCard
{
	internal sealed class Card
	{
		/// <summary>Who it is, how many are on. Left side of the first line.</summary>
		public string Title;

		/// <summary>Address and caveats. Second line, and empty when there is nothing to say.</summary>
		public string Detail;

		/// <summary>
		/// True when the handshake will refuse this server. The row is drawn in the warning
		/// colour so a dead end is visible before it is selected.
		/// </summary>
		public bool Incompatible;
	}

	/// <summary>
	/// Builds the card for one listing.
	///
	/// <paramref name="pingLabel"/> is the already-formatted ping (<c>"42 ms"</c>), because the
	/// ping is measured elsewhere and shown on the right of the title line where the eye looks
	/// first when choosing between two servers.
	/// </summary>
	internal static Card Build(int index, JoinableSession session, string address, bool favorite, string pingLabel)
	{
		Card card = new Card();
		if (session == null || session.AvailableSession == null)
		{
			card.Title = (index + 1).ToStringLookup() + ".";
			card.Detail = string.Empty;
			return card;
		}
		AvailableNetworkSession available = session.AvailableSession;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append((index + 1).ToStringLookup()).Append(". ");
		if (favorite)
		{
			// A star, not a word: it is the same marker the favorites file uses, and a translated
			// word here would push the name off the row on a narrow screen.
			stringBuilder.Append("* ");
		}
		stringBuilder.Append(session.HostTag);
		card.Title = stringBuilder.ToString();

		bool incompatible = available.ModMismatch
			|| (available.ProtocolVersion > 0 && available.ProtocolVersion != Terraria.Net.TcpPeer.ProtocolVersionPublic);
		card.Incompatible = incompatible;

		// The detail line: address first, because it is the one fact that is always useful - it is
		// what you would read out to a friend, and what a relayed server cannot offer.
		StringBuilder stringBuilder2 = new StringBuilder();
		bool relayed = !string.IsNullOrEmpty(available.RelayRoom);
		if (relayed)
		{
			Append(stringBuilder2, Texts.Get("ui.net.card.relay"));
		}
		else if (!string.IsNullOrEmpty(address))
		{
			Append(stringBuilder2, address);
		}
		if (available.Passworded)
		{
			Append(stringBuilder2, Texts.Get("ui.net.card.password"));
		}
		if (available.ModMismatch)
		{
			Append(stringBuilder2, Texts.Get("ui.net.card.mods"));
		}
		if (available.ProtocolVersion > 0 && available.ProtocolVersion != Terraria.Net.TcpPeer.ProtocolVersionPublic)
		{
			// Both numbers, not just theirs: "v2" alone does not say what this build speaks, and
			// the difference is the whole point of the warning.
			Append(stringBuilder2, Texts.Format("ui.net.card.version",
				available.ProtocolVersion.ToStringLookup(),
				Terraria.Net.TcpPeer.ProtocolVersionPublic.ToStringLookup()));
		}
		card.Detail = stringBuilder2.ToString();
		return card;
	}

	/// <summary>Appends with the separator the card uses, so no field can start a line with one.</summary>
	private static void Append(StringBuilder builder, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.Append("   ");
		}
		builder.Append(text);
	}
}
