using Uno;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls;

partial class ScrollViewer
{
	private static bool _warnedAboutZoomedContentAlignment;

	[NotImplemented]
	private void UpdateZoomedContentAlignment()
	{
		if (_warnedAboutZoomedContentAlignment)
		{
			return;
		}

		_warnedAboutZoomedContentAlignment = true;
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().LogWarning("Zoom-based content alignment is not implemented on this platform.");
		}
	}
}
