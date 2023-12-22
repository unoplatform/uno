using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class SymbolIconSource : IconSource
{
	public SymbolIconSource()
	{
	}

	public Symbol Symbol
	{
		get => (Symbol)GetValue(SymbolProperty);
		set => SetValue(SymbolProperty, value);
	}

	public static DependencyProperty SymbolProperty { get; } =
		DependencyProperty.Register(nameof(Symbol), typeof(Symbol), typeof(SymbolIconSource), new FrameworkPropertyMetadata(Symbol.Emoji));

	public override IconElement CreateIconElement()
	{
		var symbolIcon = new SymbolIcon()
		{
			Symbol = Symbol
		};

		if (Foreground != null)
		{
			symbolIcon.Foreground = Foreground;
		}

		return symbolIcon;
	}
}
