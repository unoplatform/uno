using Android.Graphics;
using Microsoft.UI.Xaml.Media;
using Android.Views;

using Microsoft/*Intentional space for WinUI upgrade tool*/.UI.Xaml.Controls;

namespace Uno.UI.Controls.Legacy;

/// <summary>
/// Represents a control that indicates that an operation is ongoing. The typical visual appearance is a ring-shaped "spinner" that cycles an animation as progress continues.
/// See https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.progressring
/// </summary>
public partial class ProgressRing
{

	private void ApplyForeground()
	{
		//We only support SolidColorBrush for now
		if (_native != null && Foreground is SolidColorBrush foregroundColor)
		{
#if __ANDROID_28__
#pragma warning disable 618 // SetColorFilter is deprecated
			_native.IndeterminateDrawable?.SetColorFilter(foregroundColor.Color, PorterDuff.Mode.SrcIn);
#pragma warning restore 618 // SetColorFilter is deprecated
#else
			_native.IndeterminateDrawable?.SetColorFilter(new BlendModeColorFilter(foregroundColor.Color, BlendMode.SrcIn));
#endif
		}
	}

	partial void OnIsActiveChangedPartial(bool isActive)
	{
		if (_native == null)
		{
			return;
		}

		if (isActive)
		{
			_native.Visibility = ViewStates.Visible;
			_native.Invalidate();
		}
		else
		{
			_native.Visibility = ViewStates.Gone;
		}
	}
}
