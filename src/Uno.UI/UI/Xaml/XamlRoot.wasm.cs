#nullable enable

using System.Diagnostics;
using Uno.Foundation.Logging;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	private bool _invalidateRequested;

	private void InnerInvalidateMeasure()
	{
		if (!_invalidateRequested)
		{
			_invalidateRequested = true;

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug("DispatchInvalidateMeasure scheduled");
			}

			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					_invalidateRequested = false;

					DispatchInvalidateMeasure();
				}
			);
		}
	}

	private void DispatchInvalidateMeasure()
	{
		if (VisualTree.RootElement is not { } rootElement)
		{
			return;
		}

		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			var sw = Stopwatch.StartNew();
			rootElement.Measure(Bounds.Size);
			rootElement.Arrange(Bounds);
			sw.Stop();

			this.Log().Debug($"DispatchInvalidateMeasure: {sw.Elapsed}");
		}
		else
		{
			rootElement.Measure(Bounds.Size);
			rootElement.Arrange(Bounds);
		}
	}
}
