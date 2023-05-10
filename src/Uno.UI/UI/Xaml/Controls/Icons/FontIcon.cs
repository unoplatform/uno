#nullable enable

using Uno;
using Windows.Foundation;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a glyph from the specified font.
/// </summary>
public partial class FontIcon : IconElement
{
	private readonly TextBlock _textBlock;

	private ScaleTransform? _scaleTransform;

	/// <summary>
	/// Initializes a new instance of the FontIcon class.
	/// </summary>
	public FontIcon()
	{
		_textBlock = new TextBlock();
		AddIconChild(_textBlock);

		SynchronizeProperties();
	}

	/// <summary>
	/// Gets or sets the font used to display the icon glyph.
	/// </summary>
	public FontFamily FontFamily
	{
		get => (FontFamily)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontFamily dependency property.
	/// </summary>
	public static DependencyProperty FontFamilyProperty { get; } =
		DependencyProperty.Register(
			nameof(FontFamily),
			typeof(FontFamily),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont),
				(s, e) => ((FontIcon)s)._textBlock.FontFamily = (FontFamily)e.NewValue));

	/// <summary>
	/// Gets or sets the size of the icon glyph.
	/// </summary>
	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontSize dependency property.
	/// </summary>
	public static DependencyProperty FontSizeProperty { get; } =
		DependencyProperty.Register(
			nameof(FontSize),
			typeof(double),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				20.0,
				(s, e) => ((FontIcon)s)._textBlock.FontSize = (double)e.NewValue));

	/// <summary>
	/// Gets or sets the font style for the icon glyph.
	/// </summary>
	public FontStyle FontStyle
	{
		get => (FontStyle)GetValue(FontStyleProperty);
		set => SetValue(FontStyleProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontStyle dependency property.
	/// </summary>
	public static DependencyProperty FontStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(FontStyle),
			typeof(FontStyle),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				FontStyle.Normal,
				(s, e) => ((FontIcon)s)._textBlock.FontStyle = (FontStyle)e.NewValue));

	/// <summary>
	/// Gets or sets the thickness of the icon glyph.
	/// </summary>
	public FontWeight FontWeight
	{
		get => (FontWeight)GetValue(FontWeightProperty);
		set => SetValue(FontWeightProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontWeight dependency property.
	/// </summary>
	public static DependencyProperty FontWeightProperty { get; } =
		DependencyProperty.Register(
			nameof(FontWeight),
			typeof(FontWeight),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				new FontWeight(400),
				(s, e) => ((FontIcon)s)._textBlock.FontWeight = (FontWeight)e.NewValue));

	/// <summary>
	/// Gets or sets the font used to display the icon glyph.
	/// </summary>
	public string Glyph
	{
		get => (string)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontFamily dependency property.
	/// </summary>
	public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(Glyph),
			typeof(string),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				string.Empty,
				(s, e) => ((FontIcon)s)._textBlock.Text = (string)e.NewValue));

	/// <summary>
	/// Gets or sets whether automatic text enlargement, to reflect the system text size setting, is enabled.
	/// </summary>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool IsTextScaleFactorEnabled
	{
		get => (bool)this.GetValue(IsTextScaleFactorEnabledProperty);
		set => SetValue(IsTextScaleFactorEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsTextScaleFactorEnabled dependency property.
	/// </summary>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsTextScaleFactorEnabled),
			typeof(bool),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				true,
				(s, e) => ((FontIcon)s)._textBlock.IsTextScaleFactorEnabled = (bool)e.NewValue));

	/// <summary>
	/// Gets or sets a value that indicates whether the icon is mirrored when the FlowDirection is RightToLeft.
	/// </summary>
	public bool MirroredWhenRightToLeft
	{
		get => (bool)GetValue(MirroredWhenRightToLeftProperty);
		set => SetValue(MirroredWhenRightToLeftProperty, value);
	}

	/// <summary>
	/// Identifies the MirroredWhenRightToLeft dependency property.
	/// </summary>
	public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
		DependencyProperty.Register(
			nameof(MirroredWhenRightToLeft),
			typeof(bool),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				false,
				propertyChangedCallback: (s, e) => ((FontIcon)s).UpdateMirroring()));

	private void SynchronizeProperties()
	{
		_textBlock.Style = null;
		_textBlock.VerticalAlignment = VerticalAlignment.Center;
		_textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		_textBlock.TextAlignment = Windows.UI.Xaml.TextAlignment.Center;
		_textBlock.SetValue(AutomationProperties.AccessibilityViewProperty, AccessibilityView.Raw);

		_textBlock.Text = Glyph;
		_textBlock.FontSize = FontSize;
		_textBlock.FontStyle = FontStyle;
		_textBlock.FontFamily = FontFamily;
		_textBlock.Foreground = Foreground;
		_textBlock.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
	}

	private void UpdateMirroring()
	{
		if (_scaleTransform is null &&
			FlowDirection == FlowDirection.RightToLeft &&
			MirroredWhenRightToLeft)
		{
			_scaleTransform = new ScaleTransform();
			RenderTransform = _scaleTransform;
			RenderTransformOrigin = new Point(0.5, 0.5);
		}

		if (_scaleTransform is not null)
		{
			_scaleTransform.ScaleX = FlowDirection == FlowDirection.RightToLeft && MirroredWhenRightToLeft ? -1 : 1;
		}
	}

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
	{
		// This may occur while executing the base constructor
		// so _textBlock may still be null.
		if (_textBlock is not null)
		{
			_textBlock.Foreground = (Brush)e.NewValue;
		}
	}
}
