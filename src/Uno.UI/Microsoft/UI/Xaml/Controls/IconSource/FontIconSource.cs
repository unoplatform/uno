using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FontStyle = Windows.UI.Text.FontStyle;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FontIconSource : IconSource
	{
		public FontIconSource()
		{
		}

		public bool MirroredWhenRightToLeft
		{
			get => (bool)GetValue(MirroredWhenRightToLeftProperty);
			set => SetValue(MirroredWhenRightToLeftProperty, value);
		}

		public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
			DependencyProperty.Register(nameof(MirroredWhenRightToLeft), typeof(bool), typeof(FontIconSource), new PropertyMetadata(false));

		public bool IsTextScaleFactorEnabled
		{
			get => (bool)GetValue(IsTextScaleFactorEnabledProperty);
			set => SetValue(IsTextScaleFactorEnabledProperty, value);
		}

		public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
			DependencyProperty.Register(nameof(IsTextScaleFactorEnabled), typeof(bool), typeof(FontIconSource), new PropertyMetadata(true));

		public string Glyph
		{
			get => (string)GetValue(GlyphProperty);
			set => SetValue(GlyphProperty, value);
		}

		public static DependencyProperty GlyphProperty { get; } =
			DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(FontIconSource), new PropertyMetadata(default(string)));

		public FontWeight FontWeight
		{
			get => (FontWeight)GetValue(FontWeightProperty);
			set => SetValue(FontWeightProperty, value);
		}

		public static DependencyProperty FontWeightProperty { get; } = 
			DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(FontIconSource), new PropertyMetadata(new FontWeight(400)));

		public FontStyle FontStyle
		{
			get => (FontStyle)GetValue(FontStyleProperty);
			set => SetValue(FontStyleProperty, value);
		}

		public static DependencyProperty FontStyleProperty { get; } =
			DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(FontIconSource), new PropertyMetadata(FontStyle.Normal));

		public double FontSize
		{
			get => (double)GetValue(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}

		public static DependencyProperty FontSizeProperty { get; } =
			DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(FontIconSource), new PropertyMetadata(20.0));

		public FontFamily FontFamily
		{
			get => (FontFamily)GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		public static DependencyProperty FontFamilyProperty { get; } =
			DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(FontIconSource), new PropertyMetadata(new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont)));

		/// <inheritdoc />
		internal override IconElement CreateIconElement()
			=> new FontIcon
			{
				MirroredWhenRightToLeft = MirroredWhenRightToLeft,
				IsTextScaleFactorEnabled = IsTextScaleFactorEnabled,
				Glyph = Glyph,
				FontWeight = FontWeight,
				FontStyle = FontStyle,
				FontSize = FontSize,
				FontFamily = FontFamily,
			};
	}
}
