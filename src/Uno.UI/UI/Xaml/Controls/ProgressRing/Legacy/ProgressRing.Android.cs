using Android.Graphics;
using Microsoft.UI.Xaml.Media;
using Android.Views;

using Microsoft.UI.Xaml.Controls;
using AndroidX.Core.Graphics;

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
			var colorFilter = new PorterDuffColorFilter((Color)foregroundColor.Color, PorterDuff.Mode.SrcIn);
			_native.IndeterminateDrawable?.SetColorFilter(colorFilter);
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
