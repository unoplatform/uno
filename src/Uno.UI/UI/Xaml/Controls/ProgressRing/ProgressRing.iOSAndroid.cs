#if __IOS__ || __ANDROID__
using Windows.UI.Xaml.Media;
#if __IOS__
using UIKit;
#else
using Uno.UI;
#endif

using Microsoft/*Intentional space for WinUI upgrade tool*/.UI.Xaml.Controls;
using Microsoft/*Intentional space for WinUI upgrade tool*/.UI.Xaml;

namespace Windows.UI.Xaml.Controls;

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
