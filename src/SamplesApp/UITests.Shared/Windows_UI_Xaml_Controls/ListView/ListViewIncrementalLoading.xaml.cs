using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "ListViewIncrementalLoading", Description: "ListView with source that implements ISupportIncrementalLoading for data virtualization.")]
	public sealed partial class ListViewIncrementalLoading : UserControl
	{
		private readonly SimpleIncrementalCollection<int> _items = new SimpleIncrementalCollection<int>(i => i * 10);
		public ListViewIncrementalLoading()
		{
			this.InitializeComponent();

			TargetListView.ItemsSource = _items;
		}

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			_items.HasMoreItems = false;
		}
	}
}
