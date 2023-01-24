using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.SymbolIconTests
{
	[Sample("Icons")]
	public sealed partial class SymbolIcon_Generic : Page
	{
		public SymbolIcon_Generic()
		{
			this.InitializeComponent();
		}

		public List<SymbolListItem> Symbols { get; } =
			Enum.GetValues(typeof(Symbol))
				.Cast<Symbol>()
				.Select(symbol => new SymbolListItem(symbol))
				.ToList();
	}

	public class SymbolListItem
	{
		public SymbolListItem(Symbol symbol)
		{
			Symbol = symbol;
			Name = Enum.GetName(typeof(Symbol), symbol);
		}

		public Symbol Symbol { get; }

		public string Name { get; }
	}
}
