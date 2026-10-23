#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal sealed class WebViewActivityLaunch<TActivity>(CancellationToken cancellationToken)
	where TActivity : class
{
	private readonly TaskCompletionSource<TActivity> _arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);

	internal string Id { get; } = Guid.NewGuid().ToString("N");

	internal Task<TActivity> StartAsync(Action<string> startActivity)
	{
		cancellationToken.ThrowIfCancellationRequested();
		startActivity(Id);
		// An accepted launch remains owned until its matching lifecycle notification arrives.
		return _arrival.Task;
	}

	internal void OnActivityCreated(TActivity activity, string? launchId, Action<TActivity> finishActivity)
	{
		if (launchId != Id || _arrival.Task.IsCompleted)
		{
			return;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			finishActivity(activity);
			_arrival.TrySetCanceled(cancellationToken);
		}
		else
		{
			_arrival.TrySetResult(activity);
		}
	}
}
