using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#if __IOS__
using CoreGraphics;
using _View = UIKit.UIView;
#elif __MACOS__
using CoreGraphics;
using _View = AppKit.NSView;
#endif


namespace Uno.UI.Toolkit
{
	[ContentProperty(Name = "ElevatedContent")]
	[TemplatePart(Name = "PART_Border", Type = typeof(Border))]
	[TemplatePart(Name = "PART_ShadowHost", Type = typeof(Grid))]
	public sealed partial class ElevatedView : Control
#if !NETFX_CORE
		, ICustomClippingElement
#endif
	{
		/*
		 *  +-ElevatedView------------+
		 *  |                         |
		 *  |  +-Grid--------------+  |
		 *  |  |                   |  |
		 *  |  +-------------------+  |
		 *  |  +-Border------------+  |
		 *  |  |                   |  |
		 *  |  |  +-Content-----+  |  |
		 *  |  |  | (...)       |  |  |
		 *  |  |  +-------------+  |  |
		 *  |  |                   |  |
		 *  |  +-------------------+  |
		 *  |                         |
		 *  +-------------------------+
		 *
		 * UWP - Grid is responsible for the shadow
		 * Other Platforms - Elevated is responsible for the shadow
		 * Border responsible for rounded corners (if any)
		 *
		 */

#if __ANDROID__
		private static readonly Color DefaultShadowColor = Colors.Black;
#else
		private static readonly Color DefaultShadowColor = Color.FromArgb(64, 0, 0, 0);
#endif

		private Border _border;
		private Canvas _shadowHost;

		public ElevatedView()
		{
			DefaultStyleKey = typeof(ElevatedView);
			Background = new SolidColorBrush(Colors.Transparent);

#if !NETFX_CORE
			Loaded += (snd, evt) => SynchronizeContentTemplatedParent();

			// Patch to deactivate the clipping by ContentControl
			RenderTransform = new CompositeTransform();
#endif
			SizeChanged += (snd, evt) => UpdateElevation();
		}

		protected override void OnApplyTemplate()
		{
			_border = GetTemplateChild("PART_Border") as Border;
			_shadowHost = GetTemplateChild("PART_ShadowHost") as Canvas;

			UpdateElevation();
		}

		public static DependencyProperty ElevationProperty { get ; } = DependencyProperty.Register(
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

		public static DependencyProperty ShadowColorProperty { get ; } = DependencyProperty.Register(
			"ShadowColor", typeof(Color), typeof(ElevatedView), new PropertyMetadata(DefaultShadowColor, OnChanged));

		public Color ShadowColor
		{
			get => (Color)GetValue(ShadowColorProperty);
			set => SetValue(ShadowColorProperty, value);
		}

		public static DependencyProperty ElevatedContentProperty { get ; } = DependencyProperty.Register(
			"ElevatedContent", typeof(object), typeof(ElevatedView), new PropertyMetadata(default(object)));

		public object ElevatedContent
		{
			get => GetValue(ElevatedContentProperty);
			set => SetValue(ElevatedContentProperty, value);
		}

#if !NETFX_CORE
		public new static DependencyProperty BackgroundProperty { get ; } = DependencyProperty.Register(
			"Background", typeof(Brush), typeof(ElevatedView), new FrameworkPropertyMetadata(default(Brush), OnChanged));

		public new Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public static DependencyProperty CornerRadiusProperty { get ; } = DependencyProperty.Register(
			"CornerRadius", typeof(CornerRadius), typeof(ElevatedView), new FrameworkPropertyMetadata(default(CornerRadius), OnChanged));

		public CornerRadius CornerRadius
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

#if !NETFX_CORE
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
				_border.SetElevationInternal(Elevation, ShadowColor);
#elif NETFX_CORE
				(ElevatedContent as DependencyObject).SetElevationInternal(Elevation, ShadowColor, _shadowHost as DependencyObject, CornerRadius);
#endif
			}
		}

#if !NETFX_CORE
		bool ICustomClippingElement.AllowClippingToLayoutSlot => false; // Never clip, since it will remove the shadow

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

		protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
		{
			return base.ArrangeOverride(this.ApplySizeConstraints(finalSize));
		}
#endif
	}
}
