#if __IOS__
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CoreGraphics;
using UIKit;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Uno.UI.Controls
{
	internal class AppBarButtonRenderer : Renderer<AppBarButton, UIBarButtonItem>
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


		private bool HasContent => Element?.Content is FrameworkElement;

		protected override UIBarButtonItem CreateNativeInstance() => new UIBarButtonItem();

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

			Native.Clicked += OnNativeClicked;
			yield return Disposable.Create(() => { Native.Clicked -= OnNativeClicked; });
		}

		protected override void Render()
		{
			// Icon & Content
			switch (Element.Icon)
			{
				case BitmapIcon bitmap:
					Native.Title = null;
					Native.Image = UIImageHelper.FromUri(bitmap.UriSource);
					break;
				default:
					// iOS doesn't add the UIBarButtonItem to the native logical tree unless it has an Image or Title set. We 
					// default to an empty string to ensure it is added, in order to support late-bound Content.
					Native.Title = string.Empty;

					switch (Element.Content)
					{
						case string str:
							Native.Title = str ?? string.Empty;
							_appBarButtonWrapper.Child = null;
							Native.CustomView = null;
							break;
						case FrameworkElement content:
							var currentParent = Element.GetParent();

							_appBarButtonWrapper.Child = Element;

							// Restore the original parent if any, as we 
							// want the DataContext to flow properly from the 
							// CommandBar.
							Element.SetParent(currentParent);

							if (content.Visibility == Visibility.Visible)
							{
								Native.CustomView = _appBarButtonWrapper;
							}
							else
							{
								Native.CustomView = null;
							}
							break;
					};
					break;
			}

			// Label
			Native.AccessibilityHint = Element.Label;
			Native.AccessibilityLabel = Element.Label;

			// Foreground
			if (Element.Foreground is SolidColorBrush foreground)
			{
				var color = (UIColor)foreground.Color as UIColor;
				var alpha = (nfloat)(foreground.Opacity * Element.Opacity);
				Native.TintColor = color.ColorWithAlpha(alpha);
			}
			else
			{
				Native.TintColor = default(UIColor); // TODO .Clear;
			}

			// IsEnabled
			Native.Enabled = Element.IsEnabled;

			// Background
			var backgroundColor = (Element.Background as SolidColorBrush)?.ColorWithOpacity;
			if (backgroundColor != null)
			{
				if (HasContent)
				{
					// Setup the background color when a custom content is set.
					_appBarButtonWrapper.BackgroundColor = backgroundColor;
				}
				else
				{
					var isTransparent = backgroundColor.Value.A == 0;
					var backgroundImage = isTransparent
						? new UIImage() // Clears the background
						: ((UIColor)backgroundColor).ToUIImage(); // Applies the solid color;

					// We're using SetBackgroundImage instead of SetBackgroundColor 
					// because it extends all the way up under the status bar.
					Native.SetBackgroundImage(backgroundImage, UIControlState.Normal, UIBarMetrics.Default);
				}
			}
		}

		private void OnNativeClicked(object sender, EventArgs e) => Element.RaiseClick();
	}

	/// <summary>
	/// Used to correctly lay out the AppBarButton in the UIBarButtonItem
	/// </summary>
	internal partial class AppBarButtonWrapper : Border
	{
		public AppBarButtonWrapper()
		{
			// Giving it full available space so that its child can properly be sized and positioned
			// The frame will be restricted later
			Frame = new CGRect(new CGPoint(0, 0), UILayoutFittingExpandedSize);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			// The frame needs to be explicitly set in order to render the CustomView of the UIBarButtonItem.
			var childSize = base.MeasureOverride(availableSize);

			if (!childSize.Width.IsNaN()
				&& !childSize.Height.IsNaN()
				&& childSize != default(Size)
				&& (Frame.Width != childSize.Width
					|| Frame.Height != childSize.Height)
				)
			{
				Frame = new CGRect(Frame.X, Frame.Y, childSize.Width, childSize.Height);
				// Request layout since previous (enormous) Frame may have squeezed out navigation bar title
				SetNeedsLayout();
			}

			return childSize;
		}
	}
}
#endif