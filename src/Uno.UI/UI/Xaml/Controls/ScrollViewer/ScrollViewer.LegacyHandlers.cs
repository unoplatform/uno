#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.Foundation.Logging;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
#if __CROSSRUNTIME__
		private static bool _warnedAboutZoomedContentAlignment;

		[NotImplemented]
		private void UpdateZoomedContentAlignment()
		{
			if (_warnedAboutZoomedContentAlignment)
			{
				return;
			}

			_warnedAboutZoomedContentAlignment = true;
			if (this.Log().IsEnabled(ApiInformation.NotImplementedLogLevel))
			{
				this.Log().Log(ApiInformation.NotImplementedLogLevel, "Zoom-based content alignment is not implemented on this platform.");
			}
		}
#endif

#if !__SKIA__
		// On Skia these are provided by the WinUI port (ScrollViewer.mux.cs).

		/// <summary>
		/// Handles the vertical ScrollBar.Scroll event and updates the UI.
		/// </summary>
		internal void HandleVerticalScroll(ScrollEventType scrollEventType, double offset = 0)
		{
			//UNO TODO: Implement HandleVerticalScroll on ScrollViewer
		}

		/// <summary>
		/// Handles the horizontal ScrollBar.Scroll event and updates the UI.
		/// </summary>
		internal void HandleHorizontalScroll(ScrollEventType scrollEventType, double offset = 0)
		{
			//UNO TODO: Implement HandleHorizontalScroll on ScrollViewer
		}
#endif
	}
}
