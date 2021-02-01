using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
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
			DependencyProperty.Register(nameof(Symbol), typeof(Symbol), typeof(SymbolIconSource), new PropertyMetadata(Symbol.Emoji));
	}
}
