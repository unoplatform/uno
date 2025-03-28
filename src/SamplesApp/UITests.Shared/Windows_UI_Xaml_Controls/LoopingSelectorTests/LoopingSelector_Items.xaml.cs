using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;
using Uno.UI;

namespace UITests.Windows_UI_Xaml_Controls.LoopingSelectorTests
{
	[Sample("LoopingSelector")]
	public sealed partial class LoopingSelector_Items : Page
	{
		private static readonly IList<object> _items = new[] {
			"Ga (0)",
			"Bu (1)",
			"Zo (2)",
			"Meu (3)",
			"Bu-Ga (4)",
			"Bu-Bu (5)",
			"Bu-Zo (6)",
			"Bu-Meu (7)",
			"Zo-Ga (8)",
			"Zo-Bu (9)",
			"Zo-Zo (10)",
			"Zo-Meu (11)",
			"Meu-Ga (12)",
			"Meu-Bu (13)",
			"Meu-Zo (14)",
			"Meu-Meu (15)",
		}
			.Select(x => new LoopingSelector_Items_Item { PrimaryText = x } as object)
			.ToList();

		public LoopingSelector_Items()
		{
			this.InitializeComponent();

#if !WINAPPSDK
			var loopingSelector = new LoopingSelector
			{
				ItemHeight = 30,
				ShouldLoop = true,
				SelectedIndex = 5,
				Items = _items
			};

			loopingSelector.SelectionChanged += OnSelectionChanged;

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				selection.Text =
					$"SelectedIdex={loopingSelector.SelectedIndex} SelectedItem={loopingSelector.SelectedItem}";
			}

			loopingSelectorContainer.Child = loopingSelector;

#if DEBUG
			async void Do()
			{
				await Task.Delay(100);
				var tree = this.ShowLocalVisualTree(0);
				global::System.Diagnostics.Debug.WriteLine(tree);
			}
			Do();
#endif
#else
			loopingSelectorContainer.Child = new TextBlock { Text = "Not supported on Windows." };
#endif
		}
	}
}
