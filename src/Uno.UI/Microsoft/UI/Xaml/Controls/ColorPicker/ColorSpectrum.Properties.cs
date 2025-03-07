using System.Numerics;
using Windows.UI.Xaml;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	public partial class ColorSpectrum
	{
		public Color Color
		{
			get => (Color)GetValue(ColorProperty);
			set => SetValue(ColorProperty, value);
		}

		public static DependencyProperty ColorProperty { get; } =
			DependencyProperty.Register(
				nameof(Color),
				typeof(Color),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					Color.FromArgb(255, 255, 255, 255),
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public ColorSpectrumComponents Components
		{
			get => (ColorSpectrumComponents)GetValue(ComponentsProperty);
			set => SetValue(ComponentsProperty, value);
		}

		public static DependencyProperty ComponentsProperty { get; } =
			DependencyProperty.Register(
				nameof(Components),
				typeof(ColorSpectrumComponents),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					ColorSpectrumComponents.HueSaturation,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public Vector4 HsvColor
		{
			get => (Vector4)GetValue(HsvColorProperty);
			set => SetValue(HsvColorProperty, value);
		}

		public static DependencyProperty HsvColorProperty { get; } =
			DependencyProperty.Register(
				nameof(HsvColor),
				typeof(Vector4),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					new Vector4(0, 0, 1, 1),
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public int MaxHue
		{
			get => (int)GetValue(MaxHueProperty);
			set => SetValue(MaxHueProperty, value);
		}

		public static DependencyProperty MaxHueProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxHue),
				typeof(int),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					359,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public int MaxSaturation
		{
			get => (int)GetValue(MaxSaturationProperty);
			set => SetValue(MaxSaturationProperty, value);
		}

		public static DependencyProperty MaxSaturationProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxSaturation),
				typeof(int),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					100,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public int MaxValue
		{
			get => (int)GetValue(MaxValueProperty);
			set => SetValue(MaxValueProperty, value);
		}

		public static DependencyProperty MaxValueProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxValue),
				typeof(int),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					100,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public int MinHue
		{
			get => (int)GetValue(MinHueProperty);
			set => SetValue(MinHueProperty, value);
		}

		public static DependencyProperty MinHueProperty { get; } =
			DependencyProperty.Register(
				nameof(MinHue),
				typeof(int),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					0,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public int MinSaturation
		{
			get => (int)GetValue(MinSaturationProperty);
			set => SetValue(MinSaturationProperty, value);
		}

		public static DependencyProperty MinSaturationProperty { get; } =
			DependencyProperty.Register(
				nameof(MinSaturation),
				typeof(int),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					0,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public int MinValue
		{
			get => (int)GetValue(MinValueProperty);
			set => SetValue(MinValueProperty, value);
		}

		public static DependencyProperty MinValueProperty { get; } =
			DependencyProperty.Register(
				nameof(MinValue),
				typeof(int),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					0,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public ColorSpectrumShape Shape
		{
			get => (ColorSpectrumShape)GetValue(ShapeProperty);
			set => SetValue(ShapeProperty, value);
		}

		public static DependencyProperty ShapeProperty { get; } =
			DependencyProperty.Register(
				nameof(Shape),
				typeof(ColorSpectrumShape),
				typeof(ColorSpectrum),
				new FrameworkPropertyMetadata(
					ColorSpectrumShape.Box,
					(s, e) => (s as ColorSpectrum)?.OnPropertyChanged(e)));

		public event TypedEventHandler<ColorSpectrum, ColorChangedEventArgs> ColorChanged;
	}
}
