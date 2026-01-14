using System;
using System.Linq;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Microsoft.UI.Xaml;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "ListView_BringIntoView",
		typeof(ListViewViewModel),
		IsManualTest: true,
		Description: "Select item scroll and then click on the button")]
	public sealed partial class ListView_BringIntoView : UserControl
	{
		private string[] items;
		public ListView_BringIntoView()
		{
			this.InitializeComponent();

			var random = new Random(42);
			// using newlines to vary the height of each item
			myList.ItemsSource = items = Enumerable.Range(0, 100).Select(i => $"item {i}" + new string('\n', random.Next(0, 5))).ToArray();
		}

		public void BringIntoView(object sender, RoutedEventArgs e)
		{
			var list = (Microsoft.UI.Xaml.Controls.ListViewBase)this.myList;

			var alignment = (chkBox.IsChecked ?? false) ? ScrollIntoViewAlignment.Leading : ScrollIntoViewAlignment.Default;
			list.ScrollIntoView(items[(int)nb.Value], alignment);
		}
	}
}
