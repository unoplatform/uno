#nullable enable

using Uno;
using Windows.Foundation;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a glyph from the specified font.
/// </summary>
public partial class FontIcon : IconElement, IThemeChangeAware
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
	[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	public bool IsTextScaleFactorEnabled
	{
		get => (bool)this.GetValue(IsTextScaleFactorEnabledProperty);
		set => SetValue(IsTextScaleFactorEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsTextScaleFactorEnabled dependency property.
	/// </summary>
	[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
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

	/// <summary>
	/// Pulls inherited text formatting from parent with FontIcon-specific defaults.
	/// MUX ref: CFontIcon::PullInheritedTextFormatting (icon.cpp:688-747).
	/// FontIcon overrides the base IconElement behavior:
	/// - FontFamily defaults to "Segoe Fluent Icons,Segoe MDL2 Assets" (not parent's)
	/// - FontSize defaults to 20.0 (g_ClientCoreFontSize, not parent's)
	/// - FontWeight/FontStyle are NOT pulled from parent
	/// </summary>
	internal override void PullInheritedTextFormatting()
	{
		var parent = GetParentTextFormatting();

		// MUX ref: icon.cpp:701-706 — FontFamily defaults to Segoe Fluent Icons, NOT parent's
		if (IsPropertyDefault(FontFamilyProperty))
		{
			// Keep whatever FontFamily is in TextFormatting (default or explicitly set).
			// Don't pull from parent — FontIcon uses its own default font.
		}

		// MUX ref: icon.cpp:709-712 — Foreground pulls from parent if default
		if (IsPropertyDefault(ForegroundProperty) && !_textFormatting.FreezeForeground)
		{
			_textFormatting.Foreground = parent.Foreground;
		}

		// MUX ref: icon.cpp:715-720 — Language from parent if default
		if (IsPropertyDefault(FrameworkElement.LanguageProperty))
		{
			_textFormatting.Language = parent.Language;
		}

		// MUX ref: icon.cpp:722-726 — FontSize defaults to 20.0, NOT parent's
		if (IsPropertyDefault(FontSizeProperty))
		{
			_textFormatting.FontSize = 20.0; // g_ClientCoreFontSize
		}

		// MUX ref: icon.cpp:728-730 — FontStretch, CharacterSpacing, TextDecorations from parent (unconditional)
		_textFormatting.FontStretch = parent.FontStretch;
		_textFormatting.CharacterSpacing = parent.CharacterSpacing;
		_textFormatting.TextDecorations = parent.TextDecorations;

		// MUX ref: icon.cpp:732-735 — FlowDirection from parent if default
		if (IsPropertyDefault(FrameworkElement.FlowDirectionProperty))
		{
			_textFormatting.FlowDirection = parent.FlowDirection;
		}

		// MUX ref: icon.cpp:737-741 — IsTextScaleFactorEnabled from parent if default
		if (IsPropertyDefault(IsTextScaleFactorEnabledProperty))
		{
			_textFormatting.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
		}

		// NOTE: FontWeight and FontStyle are NOT pulled from parent.
		// In WinUI CFontIcon::PullInheritedTextFormatting, these properties
		// are not mentioned at all — they keep their default values.
	}

	private void SynchronizeProperties()
	{
		_textBlock.Style = null;
		_textBlock.VerticalAlignment = VerticalAlignment.Center;
		_textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		_textBlock.TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center;
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

	// The way this works in WinUI is by the MarkInheritedPropertyDirty call in CFrameworkElement::NotifyThemeChangedForInheritedProperties
	// There is a special handling for Foreground specifically there.
	void IThemeChangeAware.OnThemeChanged()
	{
		if (_textBlock is not null)
		{
			_textBlock.Foreground = Foreground;
		}
	}
}
