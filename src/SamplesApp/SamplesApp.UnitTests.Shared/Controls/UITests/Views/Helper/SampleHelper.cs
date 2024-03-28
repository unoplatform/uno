using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Samples.Helper
{
	/// <summary>
	/// Provides a set of helpers for samples to ease the writing of samples.
	/// </summary>
	public static class SampleHelper
	{
		/// <summary>
		/// Runs the provided action when the <paramref name="element"/> is loaded, then cancels it when the <paramref name="element"/> is unloaded.
		/// </summary>
		/// <param name="element">The element to track</param>
		/// <param name="action">The async action to execute</param>
		public static void RunWhileLoaded(this FrameworkElement element, Func<CancellationToken, Task> action)
		{
			element.Loaded += async delegate
			{

				var cts = new CancellationTokenSource();

				RoutedEventHandler unloadedHandler = null;

				unloadedHandler = delegate
				{
					cts.Cancel();
					element.Unloaded -= unloadedHandler;
				};

				element.Unloaded += unloadedHandler;

				try
				{
					await action(cts.Token);
				}
				finally
				{
					element.Unloaded -= unloadedHandler;
				}
			};
		}
	}
}
