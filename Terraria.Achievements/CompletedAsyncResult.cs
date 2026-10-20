using System;
using System.Threading;

namespace Terraria.Achievements;

internal class CompletedAsyncResult : IAsyncResult
{
	public object AsyncState { get; set; }

	public WaitHandle AsyncWaitHandle => new ManualResetEvent(initialState: true);

	public bool CompletedSynchronously => true;

	public bool IsCompleted => true;

	public CompletedAsyncResult(object State)
	{
		AsyncState = State;
	}
}
