#if XAMARIN || __WASM__

using System;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class SymbolIcon : IconElement
	{
		private FontIcon _icon;

		public SymbolIcon() : this(Symbol.Emoji)
		{
		}

		public SymbolIcon(Symbol symbol)
		{
			_icon = new FontIcon();
			AddIconElementView(_icon);
			_icon.Glyph = new string((char)symbol, 1);
		}

		public Symbol Symbol
		{
			get => (Symbol)GetValue(SymbolProperty);
			set => SetValue(SymbolProperty, value);
		}

		public static DependencyProperty SymbolProperty { get; } =
			DependencyProperty.Register("Symbol", typeof(Symbol), typeof(SymbolIcon), new FrameworkPropertyMetadata(Symbol.Emoji, OnSymbolChanged));

		private static void OnSymbolChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var symbol = dependencyObject as SymbolIcon;

			if (symbol != null)
			{
				symbol._icon.Glyph = new string((char)symbol.Symbol, 1);
			}
		}
	}
}
#endif
