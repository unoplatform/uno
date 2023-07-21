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
using Uno.Foundation.Logging;

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

			var native = Native;
			var element = Element;

			// IsInOverflow
			var showAsAction = element.IsInOverflow
				? ShowAsAction.Never
				: ShowAsAction.Always;
			native.SetShowAsAction(showAsAction);

			// (Icon ?? Content) and Label
			if (element.IsInOverflow)
			{
				native.SetActionView(null);
				native.SetIcon(null);
				native.SetTitle(element.Label);
			}
			else
			{
				var iconOrContent = element.Icon ?? element.Content;

				switch (iconOrContent)
				{
					case BitmapIcon bitmap:
						var drawable = DrawableHelper.FromUri(bitmap.UriSource);
						native.SetIcon(drawable);
						native.SetActionView(null);
						native.SetTitle(null);
						break;

					case string text:
						native.SetIcon(null);
						native.SetActionView(null);
						native.SetTitle(text);
						break;

					case FrameworkElement fe:
						var currentParent = element.GetParent();
						_appBarButtonWrapper.Child = element;

						//Restore the original parent if any, as we
						// want the DataContext to flow properly from the
						// CommandBar.
						element.SetParent(currentParent);

						native.SetIcon(null);
						native.SetActionView(fe.Visibility == Visibility.Visible ? _appBarButtonWrapper : null);
						native.SetTitle(null);
						break;

					default:
						native.SetIcon(null);
						native.SetActionView(null);
						native.SetTitle(null);
						break;
				}
			}

			// IsEnabled
			native.SetEnabled(element.IsEnabled);
			// According to the Material Design guidelines, the opacity inactive icons should be:
			// - Light background: 38%
			// - Dark background: 50%
			// Source: https://material.io/guidelines/style/icons.html
			// For lack of a reliable way to identify whether the background is light or dark, 
			// we'll go with 50% opacity until this no longer satisfies projects requirements.
			var isEnabledOpacity = (element.IsEnabled ? 1.0 : 0.5);

			// Visibility
			native.SetVisible(element.Visibility == Visibility.Visible);

			// Foreground
			var foreground = (element.Icon?.Foreground) as SolidColorBrush;

			var foregroundOpacity = foreground?.Opacity ?? 0;
			if (native.Icon != null)
			{
				if (foreground != null)
				{
					DrawableCompat.SetTint(native.Icon, (Android.Graphics.Color)foreground.Color);
				}
				else
				{
					DrawableCompat.SetTintList(native.Icon, null);
				}
			}

			// Background
			var backgroundColor = Brush.GetColorWithOpacity(element.Background);
			if (backgroundColor != null)
			{
				_appBarButtonWrapper.SetBackgroundColor((Android.Graphics.Color)backgroundColor);
			}

			// Opacity
			var opacity = element.Opacity;
			var finalOpacity = isEnabledOpacity * foregroundOpacity * opacity;
			var alpha = (int)(finalOpacity * 255);
			native.Icon?.SetAlpha(alpha);
		}
	}

	internal partial class AppBarButtonWrapper : Border
	{
		// By default, the custom view of a MenuItem fills up the whole available area unless you 
		// explicitly collapse it by calling Native.CollapseActionView or calling SetShowAsAction with the extra flag
		// ShowAsAction.CollapseActionView. This is for instance the case of the search view used in a lot of scenarios.
		// To avoid this use case, we must explicitly set the size of the action view based on the real size of its content.
		// That being said, at some point in the future, we will need to support advanced scenarios where the AppBarButton needs to be expandable.
		private Size _measuredLogicalSize;

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
