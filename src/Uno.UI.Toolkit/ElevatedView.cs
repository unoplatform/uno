using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
#if __IOS__
using CoreGraphics;
using _View = UIKit.UIView;
#elif __MACOS__
using CoreGraphics;
using _View = AppKit.NSView;
#elif __ANDROID__
using Android.Views;
#endif


namespace Uno.UI.Toolkit
{
	[ContentProperty(Name = "ElevatedContent")]
	[TemplatePart(Name = "PART_Border", Type = typeof(Border))]
	[TemplatePart(Name = "PART_ShadowHost", Type = typeof(Grid))]
	public sealed partial class ElevatedView : Control
#if HAS_UNO
		, ICustomClippingElement
#endif
	{
		/*
		 *  +-ElevatedView---------------------+
		 *  |                                  |
		 *  |  +-Canvas (PART_ShadowHost)---+  |
		 *  |  |                            |  |
		 *  |  +----------------------------+  |
		 *  |  +-Border (PART_Border)-------+  |
		 *  |  |                            |  |
		 *  |  |  +-Content--------------+  |  |
		 *  |  |  | (...)                |  |  |
		 *  |  |  +----------------------+  |  |
		 *  |  |                            |  |
		 *  |  +----------------------------+  |
		 *  |                                  |
		 *  +----------------------------------+
		 *
		 * UWP - Grid is responsible for the shadow
		 * Other Platforms - Elevated is responsible for the shadow
		 * Border responsible for rounded corners (if any)
		 *
		 */

		private static readonly Color DefaultShadowColor = Color.FromArgb(64, 0, 0, 0);

		private Border _border;
		private Panel _shadowHost;

		public ElevatedView()
		{
			DefaultStyleKey = typeof(ElevatedView);

#if HAS_UNO
			Loaded += (snd, evt) => SynchronizeContentTemplatedParent();
#if __ANDROID__
			Unloaded += (snd, evt) => DisposeShadow();
#endif

			// Patch to deactivate the clipping by ContentControl
			RenderTransform = new CompositeTransform();
#endif
			SizeChanged += (snd, evt) => UpdateElevation();
		}

		protected override void OnApplyTemplate()
		{
			_border = GetTemplateChild("PART_Border") as Border;
			_shadowHost = GetTemplateChild("PART_ShadowHost") as Panel;

#if __IOS__ || __MACOS__
			if (_border != null)
			{
				_border.BoundsPathUpdated += (s, e) => UpdateElevation();
			}
#endif

			UpdateElevation();
		}

		public static DependencyProperty ElevationProperty { get; } = DependencyProperty.Register(
			"Elevation", typeof(double), typeof(ElevatedView), new PropertyMetadata(default(double), OnChanged));

#if __ANDROID__
		public new double Elevation
#else
		public double Elevation
#endif
		{
			get => (double)GetValue(ElevationProperty);
			set => SetValue(ElevationProperty, value);
		}

		public static DependencyProperty ShadowColorProperty { get; } = DependencyProperty.Register(
			"ShadowColor", typeof(Color), typeof(ElevatedView), new PropertyMetadata(DefaultShadowColor, OnChanged));

		public Color ShadowColor
		{
			get => (Color)GetValue(ShadowColorProperty);
			set => SetValue(ShadowColorProperty, value);
		}

		public static DependencyProperty ElevatedContentProperty { get; } = DependencyProperty.Register(
			"ElevatedContent", typeof(object), typeof(ElevatedView), new PropertyMetadata(default(object)));

		public object ElevatedContent
		{
			get => GetValue(ElevatedContentProperty);
			set => SetValue(ElevatedContentProperty, value);
		}

#if HAS_UNO
		public new static DependencyProperty BackgroundProperty { get; } = DependencyProperty.Register(
			"Background",
			typeof(Brush),
			typeof(ElevatedView),
#if __IOS__ || __MACOS__
			new FrameworkPropertyMetadata(default(Brush))
#else
			new FrameworkPropertyMetadata(default(Brush), OnChanged)
#endif
		);

		public new Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public new static DependencyProperty CornerRadiusProperty { get; } = DependencyProperty.Register(
			"CornerRadius",
			typeof(CornerRadius),
			typeof(ElevatedView),
#if __IOS__ || __MACOS__
			new FrameworkPropertyMetadata(default(CornerRadius))
#else
			new FrameworkPropertyMetadata(default(CornerRadius), OnChanged)
#endif
		);

		public new CornerRadius CornerRadius
		{
			get => (CornerRadius)GetValue(CornerRadiusProperty);
			set => SetValue(CornerRadiusProperty, value);
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			// This is required to ensure that FrameworkElement.FindName can dig through the tree after
			// the control has been created.
			SynchronizeContentTemplatedParent();
		}

		private void SynchronizeContentTemplatedParent()
		{
			// Manual propagation of the templated parent to the content property
			// until we get the propagation running properly
			if (ElevatedContent is FrameworkElement content)
			{
				content.TemplatedParent = this.TemplatedParent;
			}
		}
#endif

		private static void OnChanged(DependencyObject snd, DependencyPropertyChangedEventArgs evt) => ((ElevatedView)snd).UpdateElevation();

		private void UpdateElevation()
		{
			if (_border == null)
			{
				return; // not initialized yet
			}

#if HAS_UNO
			SynchronizeContentTemplatedParent();
#endif

			if (Background == null)
			{
				this.SetElevationInternal(0, default);
			}
			else
			{
#if __WASM__
				this.SetElevationInternal(Elevation, ShadowColor);
				this.SetCornerRadius(CornerRadius);
#elif __IOS__ || __MACOS__
				this.SetElevationInternal(Elevation, ShadowColor, _border.BoundsPath);
#elif __ANDROID__
				_invalidateShadow = true;
				((ViewGroup)this).Invalidate();
#elif __SKIA__
				this.SetElevationInternal(Elevation, ShadowColor);
#elif (NETFX_CORE || NETCOREAPP) && !HAS_UNO
				_border.SetElevationInternal(Elevation, ShadowColor, _shadowHost as DependencyObject, CornerRadius);
#endif
			}
		}

#if HAS_UNO
		bool ICustomClippingElement.AllowClippingToLayoutSlot => false; // Never clip, since it will remove the shadow

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

		protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
		{
#if __ANDROID__
			_invalidateShadow = true;
			((ViewGroup)this).Invalidate();
#endif

			return base.ArrangeOverride(this.ApplySizeConstraints(finalSize));
		}
#endif
	}
}
