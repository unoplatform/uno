#if __APPLE_UIKIT__ || __ANDROID__
using Microsoft.UI.Xaml.Media;
#if __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Uno.UI.Controls.Legacy;

partial class ProgressRing
{
	private NativeProgressRing _native;

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_native = this.FindFirstChild<NativeProgressRing>();

		if (this.IsDependencyPropertySet(ForegroundProperty))
		{
			ApplyForeground();
		}

		TrySetNativeAnimating();
	}

	partial void TrySetNativeAnimating();

	protected override void OnForegroundColorChanged(Brush oldValue, Brush newValue)
	{
		base.OnForegroundColorChanged(oldValue, newValue);

		ApplyForeground();
	}
}

#endif
