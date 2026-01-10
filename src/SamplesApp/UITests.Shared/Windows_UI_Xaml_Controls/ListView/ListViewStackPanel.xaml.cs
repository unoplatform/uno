using System;
using System.Linq;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "ListViewStackPanel", description: "ListView using StackPanel as ItemsPanel")]
	public sealed partial class ListViewStackPanel : UserControl
	{
		private readonly Random _random = new Random(1812);

		public ListViewStackPanel()
		{
			this.InitializeComponent();
		}

		private void ChangeItemsSource(object sender, RoutedEventArgs e)
		{
			StackPanelListView.ItemsSource = Enumerable.Range(0, 20).Select(_ => _random.Next(10000)).ToArray();
		}
	}
}
