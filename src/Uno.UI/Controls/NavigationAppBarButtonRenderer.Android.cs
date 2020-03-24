#if __ANDROID__
using AndroidX.Core.Graphics.Drawable;
using AndroidX.AppCompat.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Controls
{
	internal class NavigationAppBarButtonRenderer : Renderer<AppBarButton, Toolbar>
	{
		public NavigationAppBarButtonRenderer(AppBarButton element) : base(element) { }

		protected override Toolbar CreateNativeInstance() => throw new NotSupportedException("The Native instance must be provided.");

		protected override IEnumerable<IDisposable> Initialize()
		{
			yield return Element.RegisterDisposableNestedPropertyChangedCallback(
				(s, e) => Invalidate(),
				new[] { AppBarButton.IconProperty },
				new[] { AppBarButton.IconProperty, BitmapIcon.UriSourceProperty },
				new[] { AppBarButton.VisibilityProperty },
				new[] { AppBarButton.OpacityProperty },
				new[] { AppBarButton.ForegroundProperty },
				new[] { AppBarButton.ForegroundProperty, SolidColorBrush.ColorProperty },
				new[] { AppBarButton.ForegroundProperty, SolidColorBrush.OpacityProperty },
				new[] { AppBarButton.LabelProperty }
			);
		}

		protected override void Render()
		{
			// Visibility
			if (Element.Visibility == Visibility.Visible)
			{
				// Icon
				var iconUri = (Element.Icon as BitmapIcon)?.UriSource;

				if (iconUri != null)
				{
					Native.NavigationIcon = DrawableHelper.FromUri(iconUri);
				}

				// Foreground
				var foreground = (Element.Foreground as SolidColorBrush);
				var foregroundColor = foreground?.Color;
				var foregroundOpacity = foreground?.Opacity ?? 0;
				if (Native.NavigationIcon != null)
				{
					if (foreground != null)
					{
						DrawableCompat.SetTint(Native.NavigationIcon, (Android.Graphics.Color)foregroundColor);
					}
					else
					{
						DrawableCompat.SetTintList(Native.NavigationIcon, null);
					}
				}

				// Label
				Native.NavigationContentDescription = Element.Label;

				// Opacity
				var opacity = Element.Opacity;
				var finalOpacity = foregroundOpacity * opacity;
				var alpha = (int)(finalOpacity * 255);
				Native.NavigationIcon?.SetAlpha(alpha);
			}
			else
			{
				Native.NavigationIcon = null;
				Native.NavigationContentDescription = null;
			}
		}
	}
}
#endif
