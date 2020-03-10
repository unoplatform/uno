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
	[ContentProperty(Name = "Content")]
	[TemplatePart(Name = "PART_Border", Type = typeof(Border))]
	public sealed partial class ElevatedView : Control
#if !NETFX_CORE
		, ICustomClippingElement
#endif
	{
		/*
		 *  +-ElevatedView------------+
		 *  |                         |
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
		 * Elevated is responsible for the shadow
		 * Border responsible for rounded corners (if any)
		 *
		 */

		private Border _border = new Border();

		public ElevatedView()
		{
			DefaultStyleKey = typeof(ElevatedView);
		}

		protected override void OnApplyTemplate()
		{
			_border = GetTemplateChild("PART_Border") as Border;

			UpdateElevation();
		}

		public static readonly DependencyProperty ElevationProperty = DependencyProperty.Register(
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

		public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(
			"ShadowColor", typeof(Color), typeof(ElevatedView), new PropertyMetadata(Color.FromArgb(64, 0, 0, 0), OnChanged));

		public Color ShadowColor
		{
			get => (Color)GetValue(ShadowColorProperty);
			set => SetValue(ShadowColorProperty, value);
		}

		public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
			"Content", typeof(object), typeof(ElevatedView), new PropertyMetadata(default(object)));

		public object Content
		{
			get => GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

#if !NETFX_CORE
		public new static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
			"Background", typeof(Brush), typeof(ElevatedView), new PropertyMetadata(default(Brush)));

		public new Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
			"CornerRadius", typeof(CornerRadius), typeof(ElevatedView), new PropertyMetadata(default(CornerRadius), OnChanged));

		public CornerRadius CornerRadius
		{
			get => (CornerRadius)GetValue(CornerRadiusProperty);
			set => SetValue(CornerRadiusProperty, value);
		}
#endif

		private static void OnChanged(DependencyObject snd, DependencyPropertyChangedEventArgs evt) => ((ElevatedView)snd).UpdateElevation();

		private void UpdateElevation()
		{
			if (_border == null)
			{
				return; // not initialized yet
			}

			if (Background == null)
			{
				this.SetElevationInternal(0, default);
			}
			else
			{
#if __WASM__
				this.SetElevationInternal(Elevation, ShadowColor);
				this.SetCornerRadius(CornerRadius);
#elif __IOS__
				this.SetElevationInternal(Elevation, ShadowColor, _border.BoundsPath);
#elif __ANDROID__
				_border.SetElevationInternal(Elevation, ShadowColor);
#endif
				// TODO: MacOS
				// TODO: UWA (waiting for support v10.0.18362.0+ to use ThemeShadow)
			}
		}

#if !NETFX_CORE
		bool ICustomClippingElement.AllowClippingToLayoutSlot => false; // Never clip, since it will remove the shadow

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
#endif
	}
}
