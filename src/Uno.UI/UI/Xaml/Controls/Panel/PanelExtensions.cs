#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Media;

#if XAMARIN_IOS_UNIFIED
using GenericColor = UIKit.UIColor;
#elif XAMARIN_IOS
using GenericColor = MonoTouch.UIKit.UIColor;
#elif XAMARIN_ANDROID
using GenericColor = Android.Graphics.Color;
#elif __MACOS__
using GenericColor = Windows.UI.Color;
#else
using GenericColor = System.Drawing.Color;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public static class PanelExtensions
	{
		public static FrameworkElement Background(this FrameworkElement panel, GenericColor color)
		{
			panel.Background = new SolidColorBrush(color);
			return panel;
		}
	}
}
#endif
