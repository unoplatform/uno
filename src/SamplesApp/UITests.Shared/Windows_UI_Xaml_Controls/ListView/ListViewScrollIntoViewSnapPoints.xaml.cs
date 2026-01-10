using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "ListViewScrollIntoViewSnapPoints", typeof(ListViewViewModel))]
	public sealed partial class ListViewScrollIntoViewSnapPoints : UserControl
	{
		public ListViewScrollIntoViewSnapPoints()
		{
			this.InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			listView.SelectedIndex = listView.SelectedIndex + 1;
			listView.ScrollIntoView(listView.SelectedItem);
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			listView.SelectedIndex = listView.SelectedIndex - 1;
			listView.ScrollIntoView(listView.SelectedItem);
		}
	}
}
