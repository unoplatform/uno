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
			var native = Native;
			var element = Element;

			// Visibility
			if (element.Visibility == Visibility.Visible)
			{
				// Icon
				var iconUri = (element.Icon as BitmapIcon)?.UriSource;

				if (iconUri != null)
				{
					native.NavigationIcon = DrawableHelper.FromUri(iconUri);
				}

				// Foreground
				var foreground = (element.Foreground as SolidColorBrush);
				var foregroundOpacity = foreground?.Opacity ?? 0;

				if (FeatureConfiguration.AppBarButton.EnableBitmapIconTint)
				{
					var foregroundColor = foreground?.Color;
					if (native.NavigationIcon != null)
					{
						if (foreground != null)
						{
							DrawableCompat.SetTint(native.NavigationIcon, (Android.Graphics.Color)foregroundColor);
						}
						else
						{
							DrawableCompat.SetTintList(native.NavigationIcon, null);
						}
					}
				}

				// Label
				native.NavigationContentDescription = element.Label;

				// Opacity
				var opacity = element.Opacity;
				var finalOpacity = foregroundOpacity * opacity;
				var alpha = (int)(finalOpacity * 255);
				native.NavigationIcon?.SetAlpha(alpha);
			}
			else
			{
				native.NavigationIcon = null;
				native.NavigationContentDescription = null;
			}
		}
	}
}
