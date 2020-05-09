#if __ANDROID__
using AndroidX.Core.Graphics.Drawable;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Uno.Disposables;
using Android.Graphics.Drawables;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.UI.Controls
{
	internal class AppBarButtonRenderer : Renderer<AppBarButton, IMenuItem>
	{
		private AppBarButtonWrapper _appBarButtonWrapper;

		public AppBarButtonRenderer(AppBarButton element) : base(element)
		{
			element.RegisterParentChangedCallback(this, OnElementParentChanged);
		}

		private void OnElementParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			if (args.NewParent == _appBarButtonWrapper)
			{
				// if the new Parent is the wrapper, restore it to
				// its original value.
				Element?.SetParent(args.PreviousParent);
			}
		}

		protected override IMenuItem CreateNativeInstance() => throw new NotSupportedException("Cannot create instance of IMenuItem.");

		protected override IEnumerable<IDisposable> Initialize()
		{
			// Content
			_appBarButtonWrapper = new AppBarButtonWrapper();
			if (Element.Content is FrameworkElement content && content.Visibility == Visibility.Visible)
			{
				var elementsParent = Element.Parent;
				_appBarButtonWrapper.SetParent(elementsParent);
			}
			yield return Disposable.Create(() => _appBarButtonWrapper = null);


			yield return Element.RegisterDisposableNestedPropertyChangedCallback(
				(s, e) => Invalidate(),
				new[] { AppBarButton.LabelProperty },
				new[] { AppBarButton.IconProperty },
				new[] { AppBarButton.IconProperty, BitmapIcon.UriSourceProperty },
				new[] { AppBarButton.ContentProperty },
				new[] { AppBarButton.ContentProperty, FrameworkElement.VisibilityProperty },
				new[] { AppBarButton.OpacityProperty },
				new[] { AppBarButton.ForegroundProperty },
				new[] { AppBarButton.ForegroundProperty, SolidColorBrush.ColorProperty },
				new[] { AppBarButton.ForegroundProperty, SolidColorBrush.OpacityProperty },
				new[] { AppBarButton.VisibilityProperty },
				new[] { AppBarButton.IsEnabledProperty },
				new[] { AppBarButton.IsInOverflowProperty }
			);
		}

		protected override void Render()
		{
			// CommandBar::PrimaryCommands -> !IsInOverflow -> AsAction.Never -> displayed directly on command bar
			// CommandBar::SecondaryCommands -> IsInOverflow -> AsAction.Awalys -> (displayed as flyout menu items under [...])

			// IsInOverflow
			var showAsAction = Element.IsInOverflow
				? ShowAsAction.Never
				: ShowAsAction.Always;
			Native.SetShowAsAction(showAsAction);

			// (Icon ?? Content) and Label
			if (Element.IsInOverflow)
			{
				Native.SetActionView(null);
				Native.SetIcon(null);
				Native.SetTitle(Element.Label);
			}
			else if (Element.Icon != null)
			{
				switch (Element.Icon)
				{
					case BitmapIcon bitmap:
						var drawable = DrawableHelper.FromUri(bitmap.UriSource);
						Native.SetIcon(drawable);
						break;

					case FontIcon font: // not supported
					case PathIcon path: // not supported
					case SymbolIcon symbol: // not supported
					default:
						this.Log().WarnIfEnabled(() => $"{GetType().Name ?? "FontIcon, PathIcon and SymbolIcon"} are not supported. Use BitmapIcon instead with UriSource.");
						Native.SetIcon(null);
						break;
				}
				Native.SetActionView(null);
				Native.SetTitle(null);
			}
			else
			{
				switch (Element.Content)
				{
					case string text:
						Native.SetIcon(null);
						Native.SetActionView(null);
						Native.SetTitle(text);
						break;

					case FrameworkElement fe:
						var currentParent = Element.GetParent();
						_appBarButtonWrapper.Child = Element;

						//Restore the original parent if any, as we
						// want the DataContext to flow properly from the
						// CommandBar.
						Element.SetParent(currentParent);

						Native.SetIcon(null);
						Native.SetActionView(fe.Visibility == Visibility.Visible ? _appBarButtonWrapper : null);
						Native.SetTitle(null);
						break;

					default:
						Native.SetIcon(null);
						Native.SetActionView(null);
						Native.SetTitle(null);
						break;
				}
			}

			// IsEnabled
			Native.SetEnabled(Element.IsEnabled);
			// According to the Material Design guidelines, the opacity inactive icons should be:
			// - Light background: 38%
			// - Dark background: 50%
			// Source: https://material.io/guidelines/style/icons.html
			// For lack of a reliable way to identify whether the background is light or dark, 
			// we'll go with 50% opacity until this no longer satisfies projects requirements.
			var isEnabledOpacity = (Element.IsEnabled ? 1.0 : 0.5);

			// Visibility
			Native.SetVisible(Element.Visibility == Visibility.Visible);

			// Foreground
			var foreground = Element.Foreground as SolidColorBrush;
			var foregroundColor = foreground?.Color;
			var foregroundOpacity = foreground?.Opacity ?? 0;
			if (Native.Icon != null)
			{
				if (foreground != null)
				{
					DrawableCompat.SetTint(Native.Icon, (Android.Graphics.Color)foregroundColor);
				}
				else
				{
					DrawableCompat.SetTintList(Native.Icon, null);
				}
			}

			// Background
			var backgroundColor = (Element.Background as SolidColorBrush)?.ColorWithOpacity;
			if (backgroundColor != null)
			{
				_appBarButtonWrapper.SetBackgroundColor((Android.Graphics.Color)backgroundColor);
			}

			// Opacity
			var opacity = Element.Opacity;
			var finalOpacity = isEnabledOpacity * foregroundOpacity * opacity;
			var alpha = (int)(finalOpacity * 255);
			Native.Icon?.SetAlpha(alpha);
		}
	}

	internal partial class AppBarButtonWrapper : Border
	{
		// By default, the custom view of a MenuItem fills up the whole available area unless you 
		// explicitly collapse it by calling Native.CollapseActionView or calling SetShowAsAction with the extra flag
		// ShowAsAction.CollapseActionView. This is for instance the case of the search view used in a lot of scenarios.
		// To avoid this use case, we must explicitly set the size of the action view based on the real size of its content.
		// That being said, at some point in the future, we will need to support advanced scenarios where the AppBarButton needs to be expandable.
		private Size _measuredLogicalSize = default(Size);

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			var realSize = _measuredLogicalSize.LogicalToPhysicalPixels();

			this.SetMeasuredDimension((int)realSize.Width, (int)realSize.Height);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			_measuredLogicalSize = base.MeasureOverride(availableSize);

			return _measuredLogicalSize;
		}
	}
}
#endif
