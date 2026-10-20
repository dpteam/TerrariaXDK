using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Xna.Framework.GamerServices;

public abstract class Gamer
{
	internal class GamerAction : IAsyncResult
	{
		public readonly AsyncCallback Callback;

		public object AsyncState { get; private set; }

		public bool CompletedSynchronously => false;

		public bool IsCompleted { get; internal set; }

		public WaitHandle AsyncWaitHandle { get; private set; }

		public GamerAction(object state, AsyncCallback callback)
		{
			AsyncState = state;
			Callback = callback;
			IsCompleted = false;
			AsyncWaitHandle = new ManualResetEvent(initialState: true);
		}
	}

	private static SignedInGamerCollection INTERNAL_signedInGamers;

	public string DisplayName { get; set; }

	public string Gamertag { get; set; }

	public bool IsDisposed { get; internal set; }

	public LeaderboardWriter LeaderboardWriter { get; internal set; }

	public object Tag { get; set; }

	public static SignedInGamerCollection SignedInGamers
	{
		get
		{
			if (INTERNAL_signedInGamers == null)
			{
				INTERNAL_signedInGamers = new SignedInGamerCollection(new List<SignedInGamer>());
			}
			return INTERNAL_signedInGamers;
		}
		internal set
		{
			INTERNAL_signedInGamers = value;
		}
	}

	public Gamer(string gamertag, string displayName = null)
	{
		Gamertag = gamertag;
		DisplayName = displayName ?? gamertag;
		LeaderboardWriter = new LeaderboardWriter();
	}

	public override string ToString()
	{
		return DisplayName;
	}

	public GamerProfile GetProfile()
	{
		IAsyncResult asyncResult = BeginGetProfile(null, null);
		asyncResult.AsyncWaitHandle.WaitOne();
		return EndGetProfile(asyncResult);
	}

	public IAsyncResult BeginGetProfile(AsyncCallback callback, object asyncState)
	{
		return new GamerAction(asyncState, callback)
		{
			IsCompleted = true
		};
	}

	public GamerProfile EndGetProfile(IAsyncResult result)
	{
		return new GamerProfile();
	}

	public static Gamer GetFromGamertag(string gamertag)
	{
		throw new NotSupportedException();
	}

	public static IAsyncResult BeginGetFromGamertag(string gamertag, AsyncCallback callback, object asyncState)
	{
		throw new NotSupportedException();
	}

	public static Gamer EndGetFromGamertag(IAsyncResult result)
	{
		throw new NotSupportedException();
	}

	public static string GetPartnerToken(string audienceUri)
	{
		throw new NotSupportedException();
	}

	public static IAsyncResult BeginGetPartnerToken(string audienceUri, AsyncCallback callback, object asyncState)
	{
		throw new NotSupportedException();
	}

	public static string EndGetPartnerToken(IAsyncResult result)
	{
		throw new NotSupportedException();
	}
}
