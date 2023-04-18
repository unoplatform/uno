using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Uno.UI.RuntimeTests.Extensions;

internal static class TimelineExtensions
{
	public static Storyboard ToStoryboard(this Timeline timeline)
	{
		return new Storyboard { Children = { timeline } };
	}

	public static async Task RunAsync(this Storyboard storyboard, TimeSpan? timeout = null, bool throwsException = false)
	{
		var tcs = new TaskCompletionSource<bool>();
		void OnCompleted(object sender, object e)
		{
			tcs.SetResult(true);
			storyboard.Completed -= OnCompleted;
		}

		storyboard.Completed += OnCompleted;
		storyboard.Begin();

		if (timeout is { })
		{
			if (await Task.WhenAny(tcs.Task, Task.Delay(timeout.Value)) != tcs.Task)
			{
				if (throwsException)
				{
					throw new TimeoutException($"Timeout waiting for the storyboard to complete: {timeout}ms");
				}
			}
		}
		else
		{
			await tcs.Task;
		}
	}

	public static TTimeline BindTo<TTimeline>(this TTimeline timeline, DependencyObject target, string property)
		where TTimeline : Timeline
	{
		Storyboard.SetTarget(timeline, target);
		Storyboard.SetTargetProperty(timeline, property);

		return timeline;
	}
}
