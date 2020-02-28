#if __IOS__ || __MACOS__
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using CoreGraphics;
#if XAMARIN_IOS_UNIFIED
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#endif


namespace Uno.UI.Toolkit
{
	[ContentProperty(Name = "Content")]
	public partial class ElevatedView : FrameworkElement, ICustomClippingElement
	{
		/*
		 *  +-Elevated----------------+
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

		private readonly Border _border = new Border();

		public ElevatedView()
		{
			InitUI();

			var x = Background;
		}

		private void InitUI()
		{

			// Bind the Border content to this control
			_border.Binding("Child", "Content", this, BindingMode.OneWay);

			// Bind Border's CornerRadius with this control
			_border.Binding("CornerRadius", "CornerRadius", this, BindingMode.OneWay);

			_border.Binding("Background", "Background", this, BindingMode.OneWay);

			// Add the border in the Visual Tree
			AddSubview(_border);
		}

		public new static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
			"Background", typeof(Brush), typeof(ElevatedView), new PropertyMetadata(default(Brush)));

		public new Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
			"Content", typeof(object), typeof(ElevatedView), new PropertyMetadata(default(object)));

		public object Content
		{
			get => GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
			"CornerRadius", typeof(CornerRadius), typeof(ElevatedView), new PropertyMetadata(default(CornerRadius), OnChanged));

		public CornerRadius CornerRadius
		{
			get => (CornerRadius)GetValue(CornerRadiusProperty);
			set => SetValue(CornerRadiusProperty, value);
		}

		public static readonly DependencyProperty ElevationProperty = DependencyProperty.Register(
			"Elevation", typeof(double), typeof(ElevatedView), new PropertyMetadata(default(double), OnChanged));

		public double Elevation
		{
			get => (double)GetValue(ElevationProperty);
			set => SetValue(ElevationProperty, value);
		}

		private static void OnChanged(DependencyObject snd, DependencyPropertyChangedEventArgs evt) => ((ElevatedView)snd).UpdateElevation();

		private void UpdateElevation()
		{
			if (Background == null)
			{
				this.SetElevationInternal(0);
			}
			else
			{
				this.SetElevationInternal(Elevation, _border.BoundsPath);
			}
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => false; // Never clip, since it will remove the shadow on iOS

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
#endif
