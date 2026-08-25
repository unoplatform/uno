#nullable enable

using System;
using System.Threading;

namespace Windows.ApplicationModel.Background;

public partial class BackgroundTaskDeferral
{
	private Action? _complete;

	internal BackgroundTaskDeferral(Action complete)
		=> _complete = complete;

	public void Complete()
		=> Interlocked.Exchange(ref _complete, null)?.Invoke();
}
