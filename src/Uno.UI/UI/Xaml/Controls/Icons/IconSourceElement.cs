#nullable enable

using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses an IconSource as its content.
/// </summary>
[ContentProperty(Name = nameof(IconSource))]
public partial class IconSourceElement : IconElement
{
	private IconElement? _iconElement;

	/// <summary>
	/// Initializes a new instance of the IconSourceElement class.
	/// </summary>
	public IconSourceElement()
	{
	}

	/// <summary>
	/// Gets or sets the IconSource used as the icon content.
	/// </summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>
	/// Identifies the IconSource dependency property.
	/// </summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(IconSource),
			typeof(IconSource),
			typeof(IconSourceElement),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: (s, e) => ((IconSourceElement)s).UpdateIconElement()));

	private void UpdateIconElement()
	{
		if (_iconElement is not null)
		{
			RemoveIconChild();
			_iconElement = null;
		}

		if (IconSource is null)
		{
			return;
		}

		if (IconSource is FontIconSource fontIconSource)
		{
			_iconElement = new FontIcon();

			SetIconBinding(_iconElement, nameof(fontIconSource.Glyph), FontIcon.GlyphProperty);
			SetIconBinding(_iconElement, nameof(fontIconSource.FontFamily), FontIcon.FontFamilyProperty);
			SetIconBinding(_iconElement, nameof(fontIconSource.FontSize), FontIcon.FontSizeProperty);
			SetIconBinding(_iconElement, nameof(fontIconSource.FontStyle), FontIcon.FontStyleProperty);
			SetIconBinding(_iconElement, nameof(fontIconSource.FontWeight), FontIcon.FontWeightProperty);
			SetIconBinding(_iconElement, nameof(fontIconSource.IsTextScaleFactorEnabled), FontIcon.IsTextScaleFactorEnabledProperty);
			SetIconBinding(_iconElement, nameof(fontIconSource.MirroredWhenRightToLeft), FontIcon.MirroredWhenRightToLeftProperty);
		}
		else if (IconSource is SymbolIconSource symbolIconSource)
		{
			_iconElement = new SymbolIcon();

			SetIconBinding(_iconElement, nameof(symbolIconSource.Symbol), SymbolIcon.SymbolProperty);
		}
		else if (IconSource is BitmapIconSource bitmapIconSource)
		{
			_iconElement = new BitmapIcon();

			SetIconBinding(_iconElement, nameof(bitmapIconSource.UriSource), BitmapIcon.UriSourceProperty);
			SetIconBinding(_iconElement, nameof(bitmapIconSource.ShowAsMonochrome), BitmapIcon.ShowAsMonochromeProperty);
		}
		else if (IconSource is PathIconSource pathIconSource)
		{
			_iconElement = new PathIcon();

			SetIconBinding(_iconElement, nameof(pathIconSource.Data), PathIcon.DataProperty);
		}

		if (_iconElement is not null)
		{
			if (IconSource.GetCurrentHighestValuePrecedence(IconSource.ForegroundProperty) < DependencyPropertyValuePrecedences.DefaultValue)
			{
				SetIconBinding(_iconElement, nameof(IconSource.Foreground), IconElement.ForegroundProperty);
			}

			AddIconChild(_iconElement);
		}
	}

	private void SetIconBinding(IconElement iconElement, string path, DependencyProperty property)
	{
		var binding = new Binding()
		{
			Source = IconSource,
			Path = new(path),
			Mode = BindingMode.OneWay,
		};
		BindingOperations.SetBinding(iconElement, property, binding);
	}
}
