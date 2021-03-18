using Uno.Extensions;
using Uno.UI.Views.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;

// Keep this formatting (with the space) for the WinUI upgrade tooling.
using Microsoft .UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a control that indicates that an operation is ongoing. The typical visual appearance is a ring-shaped "spinner" that cycles an animation as progress continues.
	/// See https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.progressring
	/// </summary>
	public partial class ProgressRing
	{

		private void ApplyForeground()
		{
			if (_native != null && Foreground is SolidColorBrush foregroundColorBrush)
			{
				_native.Color = foregroundColorBrush.ColorWithOpacity;
			}
		}

		partial void OnLoadedPartial() => TrySetNativeAnimating();

		partial void OnUnloadedPartial() => TrySetNativeAnimating();
	
		partial void OnIsActiveChangedPartial(bool _) => TrySetNativeAnimating();

		partial void TrySetNativeAnimating()
		{
			if (_native != null)
			{
				if (IsActive && IsLoaded)
				{
					_native.StartAnimating();
				}
				else
				{
					_native.StopAnimating();
				}
			}
		}
	}
}
