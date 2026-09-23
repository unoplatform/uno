#nullable enable

using System;
using Microsoft.UI.Dispatching;

namespace Uno.UI.WebView.Skia.X11;

internal static class X11WebViewNavigationStarting
{
	internal static DispatcherQueueHandler CreateCallback(
		Func<bool> isClosed,
		Func<bool> raiseNavigationStarting,
		Action<Action> queueStopLoading,
		Action stopLoading) =>
		() =>
		{
			if (isClosed())
			{
				return;
			}

			if (raiseNavigationStarting() && !isClosed())
			{
				queueStopLoading(() =>
				{
					if (!isClosed())
					{
						stopLoading();
					}
				});
			}
		};
}
