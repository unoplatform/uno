using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Microsoft.UI.Xaml;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_BringIntoView",
		typeof(ListViewViewModel),
		isManualTest: true,
		description: "Select item scroll and then click on the button")]
	public sealed partial class ListView_BringIntoView : UserControl
	{
		public ListView_BringIntoView()
		{
			this.InitializeComponent();
		}

		public void BringIntoView(object sender, RoutedEventArgs e)
		{
			var list = (Microsoft.UI.Xaml.Controls.ListViewBase)this.myList;

			list.ScrollIntoView(list.SelectedItem, ScrollIntoViewAlignment.Leading);
		}
	}
}
