#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI
{
	internal static class AppBarButtonExtensions
	{
		public static bool TryGetIconColor(this AppBarButton appBarButton, out Color iconColor)
		{
			iconColor = default;

			if (appBarButton.Icon?.ReadLocalValue(IconElement.ForegroundProperty) != DependencyProperty.UnsetValue &&
				Brush.TryGetColorWithOpacity(appBarButton.Icon?.Foreground, out var iconForeground))
			{
				iconColor = iconForeground;
				return true;
			}

			if (Brush.TryGetColorWithOpacity(appBarButton.Foreground, out var buttonForeground))
			{
				iconColor = buttonForeground;
				return true;
			}

			return false;
		}
	}
}
