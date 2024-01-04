#nullable enable

using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a glyph from the Segoe MDL2 Assets font as its content.
/// </summary>
public sealed partial class SymbolIcon : IconElement
{
	private const double DefaultFontSize = 20.0;

	private readonly TextBlock _textBlock;

	private static FontFamily? _symbolIconFontFamily;

	/// <summary>
	/// Initializes a new instance of the SymbolIcon class.
	/// </summary>
	public SymbolIcon() : this(Symbol.Emoji)
	{
	}

	/// <summary>
	/// Initializes a new instance of the SymbolIcon class using the specified symbol.
	/// </summary>
	/// <param name="symbol"></param>
	public SymbolIcon(Symbol symbol)
	{
		_textBlock = new TextBlock();
		AddIconChild(_textBlock);
		Symbol = symbol;

		SynchronizeProperties();
	}

	/// <summary>
	/// Gets or sets the Segoe MDL2 Assets glyph used as the icon content.
	/// </summary>
	public Symbol Symbol
	{
		get => (Symbol)GetValue(SymbolProperty);
		set => SetValue(SymbolProperty, value);
	}

	/// <summary>
	/// Identifies the Symbol dependency property.
	/// </summary>
	public static DependencyProperty SymbolProperty { get; } =
		DependencyProperty.Register(
			nameof(Symbol),
			typeof(Symbol),
			typeof(SymbolIcon),
			new FrameworkPropertyMetadata(Symbol.Emoji, propertyChangedCallback: (d, e) => ((SymbolIcon)d).SetSymbolText()));

	private void SynchronizeProperties()
	{
		_textBlock.Style = null;
		_textBlock.TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center;
		_textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
		_textBlock.VerticalAlignment = VerticalAlignment.Center;
		_textBlock.FontSize = DefaultFontSize;
		_textBlock.FontStyle = FontStyle.Normal;
		_textBlock.FontFamily = GetSymbolFontFamily();
		_textBlock.IsTextScaleFactorEnabled = false;
		_textBlock.SetValue(AutomationProperties.AccessibilityViewProperty, AccessibilityView.Raw);

		SetSymbolText();
		_textBlock.Foreground = Foreground;
	}

	private void SetSymbolText() => _textBlock.Text = new string((char)Symbol, 1);

	private static FontFamily GetSymbolFontFamily() =>
		 _symbolIconFontFamily ??= new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont);

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
