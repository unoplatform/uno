using Windows.UI.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

public partial class FontIconSource : global::Windows.UI.Xaml.Controls.IconSource
{
	public bool MirroredWhenRightToLeft
	{
		get => (bool)GetValue(MirroredWhenRightToLeftProperty);
		set => SetValue(MirroredWhenRightToLeftProperty, value);
	}

	public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
		DependencyProperty.Register(nameof(MirroredWhenRightToLeft), typeof(bool), typeof(FontIconSource), new FrameworkPropertyMetadata(false));

	public bool IsTextScaleFactorEnabled
	{
		get => (bool)GetValue(IsTextScaleFactorEnabledProperty);
		set => SetValue(IsTextScaleFactorEnabledProperty, value);
	}

	public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsTextScaleFactorEnabled), typeof(bool), typeof(FontIconSource), new FrameworkPropertyMetadata(true));

	public string Glyph
	{
		get => (string)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}

	public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(FontIconSource), new FrameworkPropertyMetadata(string.Empty));

	public FontWeight FontWeight
	{
		get => (FontWeight)GetValue(FontWeightProperty);
		set => SetValue(FontWeightProperty, value);
	}

	public static DependencyProperty FontWeightProperty { get; } =
		DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(FontIconSource), new FrameworkPropertyMetadata(new FontWeight(400)));

	public FontStyle FontStyle
	{
		get => (FontStyle)GetValue(FontStyleProperty);
		set => SetValue(FontStyleProperty, value);
	}

	public static DependencyProperty FontStyleProperty { get; } =
		DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(FontIconSource), new FrameworkPropertyMetadata(FontStyle.Normal));

	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	public static DependencyProperty FontSizeProperty { get; } =
		DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(FontIconSource), new FrameworkPropertyMetadata(20.0));

	public FontFamily FontFamily
	{
		get => (FontFamily)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	public static DependencyProperty FontFamilyProperty { get; } =
		DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(FontIconSource), new FrameworkPropertyMetadata(new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont)));

	/// <inheritdoc />
	public override IconElement CreateIconElement()
	{
		var fontIcon = new FontIcon()
		{
			Glyph = Glyph,
			FontSize = FontSize,
			FontWeight = FontWeight,
			FontStyle = FontStyle,
			IsTextScaleFactorEnabled = IsTextScaleFactorEnabled,
			MirroredWhenRightToLeft = MirroredWhenRightToLeft,
		};

		if (FontFamily != null)
		{
			fontIcon.FontFamily = FontFamily;
		}

		if (Foreground != null)
		{
			fontIcon.Foreground = Foreground;
		}

		return fontIcon;
	}
}
