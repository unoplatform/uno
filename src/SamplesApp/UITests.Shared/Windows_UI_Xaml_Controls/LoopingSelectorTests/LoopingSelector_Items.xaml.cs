using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;
using Uno.UI;

namespace UITests.Windows_UI_Xaml_Controls.LoopingSelectorTests
{
	[Sample("Primitives")]
	public sealed partial class LoopingSelector_Items : Page
	{
		private static readonly IList<object> _items = new [] {
			"Ga",
			"Bu",
			"Zo",
			"Meu",
			"Bu-Ga",
			"Bu-Bu",
			"Bu-Zo",
			"Bu-Meu",
			"Zo-Ga",
			"Zo-Bu",
			"Zo-Zo",
			"Zo-Meu",
			"Meu-Ga",
			"Meu-Bu",
			"Meu-Zo",
			"Meu-Meu",
		}
			.Select(x => new LoopingSelector_Items_Item { PrimaryText = x } as object)
			.ToList();

		public LoopingSelector_Items()
		{
			this.InitializeComponent();

#if !NETFX_CORE
			var loopingSelector = new LoopingSelector
			{
				ItemHeight = 30, ShouldLoop = true, SelectedIndex = 5, Items = _items
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

	public partial class LoopingSelector_Items_Item : DependencyObject
	{

		public string PrimaryText
		{
			get => (string)GetValue(PrimaryTextProperty);
			set => SetValue(PrimaryTextProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty PrimaryTextProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				nameof(PrimaryText), typeof(string),
				typeof(LoopingSelector_Items_Item),
				new FrameworkPropertyMetadata("default"));

		public override string ToString()
		{
			return PrimaryText;
		}
	}
}
