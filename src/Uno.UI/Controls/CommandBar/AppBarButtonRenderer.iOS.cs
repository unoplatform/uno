#nullable enable
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
using Uno.Foundation.Logging;
using Uno.UI.Extensions;

using ObjCRuntime;

namespace Uno.UI.Controls
{
	internal class AppBarButtonRenderer : Renderer<AppBarButton, UIBarButtonItem>
	{
		private AppBarButtonWrapper? _appBarButtonWrapper;

		public AppBarButtonRenderer(AppBarButton element) : base(element)
		{
			element.RegisterParentChangedCallback(this, OnElementParentChanged);
		}

		private void OnElementParentChanged(object instance, object? key, DependencyObjectParentChangedEventArgs args)
		{
			if (ReferenceEquals(args.NewParent, _appBarButtonWrapper))
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
			if (_appBarButtonWrapper == null)
			{
				return; // not initialized
			}
			// Icon & Content
			var native = Native;
			var element = Element;

			var iconOrContent = element.Icon ?? element.Content;
			switch (iconOrContent)
			{
				case BitmapIcon bitmap:
					native.Image = UIImageHelper.FromUri(bitmap.UriSource);
					native.ClearCustomView();
					native.Title = null;
					break;

				case string text:
					native.Image = null;
					native.ClearCustomView();
					native.Title = text;
					break;

				case FrameworkElement fe:
					var currentParent = element.GetParent();
					_appBarButtonWrapper.Child = element;

					//Restore the original parent if any, as we
					// want the DataContext to flow properly from the
					// CommandBar.
					element.SetParent(currentParent);

					native.Image = null;
					native.CustomView = fe.Visibility == Visibility.Visible ? _appBarButtonWrapper : null;
					// iOS doesn't add the UIBarButtonItem to the native logical tree unless it has an Image or Title set.
					// We default to an empty string to ensure it is added, in order to support late-bound Content.
					native.Title = string.Empty;
					break;

				default:
					native.Image = null;
					native.ClearCustomView();
					// iOS doesn't add the UIBarButtonItem to the native logical tree unless it has an Image or Title set.
					// We default to an empty string to ensure it is added.
					native.Title = string.Empty;
					break;
			}

			// Label
			native.AccessibilityHint = element.Label;
			native.AccessibilityLabel = element.Label;

			// Foreground
			if (Brush.TryGetColorWithOpacity(element.Foreground, out var foreground))
			{
				var color = (UIColor)foreground;
				native.TintColor = color.ColorWithAlpha((nfloat)element.Opacity);
			}
			else
			{
				native.TintColor = default(UIColor); // TODO .Clear;
			}

			// IsEnabled
			native.Enabled = element.IsEnabled;

			// Background
			if (Brush.TryGetColorWithOpacity(element.Background, out var backgroundColor))
			{
				if (HasContent)
				{
					// Setup the background color when a custom content is set.
					_appBarButtonWrapper.BackgroundColor = backgroundColor;
				}
				else
				{
					var backgroundImage = backgroundColor.IsTransparent
						? new UIImage() // Clears the background
						: ((UIColor)backgroundColor).ToUIImage(); // Applies the solid color;

					// We're using SetBackgroundImage instead of SetBackgroundColor 
					// because it extends all the way up under the status bar.
					native.SetBackgroundImage(backgroundImage, UIControlState.Normal, UIBarMetrics.Default);
				}
			}
		}

		private void OnNativeClicked(object? sender, EventArgs e) => Element.RaiseClick();
	}

	/// <summary>
	/// Used to correctly lay out the AppBarButton in the UIBarButtonItem
	/// </summary>
	internal partial class AppBarButtonWrapper : Border
	{
		public AppBarButtonWrapper()
		{
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			// Giving it full available space so that its child can properly be measured and positioned after
			var height = availableSize.Height > 0 ? availableSize.Height : double.PositiveInfinity;
			availableSize = new Size(double.PositiveInfinity, height);

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
