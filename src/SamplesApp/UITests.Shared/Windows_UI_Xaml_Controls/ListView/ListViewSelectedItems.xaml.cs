using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Samples.Controls;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListViewSelectedItems")]
	public sealed partial class ListViewSelectedItems : UserControl
	{
		public ListViewSelectedItems()
		{
			this.InitializeComponent();
			SampleItems = Enumerable.Range(0, 15).Concat(Enumerable.Range(0, 5)).Select(v => new MySelectableItem(v)).ToArray();
			var sampleItemsExtended = SampleItems.Concat(Enumerable.Range(15, 5).Select(v => new MySelectableItem(v))).ToArray();

			SelectorListView.ItemsSource = sampleItemsExtended;
			SetSelectedItemListView.ItemsSource = sampleItemsExtended;
			SelectedItemsListView.ItemsSource = SampleItems;
		}

		public MySelectableItem[] SampleItems { get; }

		private void SelectorListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var itemClicked = e.ClickedItem;
			var selectedItems = SelectedItemsListView.SelectedItems;
			if (!selectedItems.Contains(itemClicked))
			{
				this.Log().Warn($"Adding {itemClicked} to SelectedItems.");
				selectedItems.Add(itemClicked);
			}
			else
			{
				this.Log().Warn($"Removing {itemClicked} from SelectedItems.");
				selectedItems.Remove(itemClicked);
			}
		}

		private void SetSelectedItemListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			SelectedItemsListView.SelectedItem = e.ClickedItem;
		}

		private void SelectedItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

			var text = $"SelectionChanged event: AddedItems={EnumerableToDisplayString(e.AddedItems)}, RemovedItems={EnumerableToDisplayString(e.RemovedItems)}";
			this.Log().Warn(text);
			SelectionChangedTextBlock.Text = text;
		}

		private static string EnumerableToDisplayString(IEnumerable<object> enumerable)
		{
			return $"({enumerable.Aggregate("", (accum, next) => accum + next?.ToString() + ", ")})";
		}

		private void ClearSelectedItem(object sender, RoutedEventArgs e)
		{
			this.Log().Warn("Clearing selected items");
			SelectedItemsListView.SelectedItems.Clear();
		}

		private void SetSelectedItemNull(object sender, RoutedEventArgs e)
		{
			SelectedItemsListView.SelectedItem = null;
		}
	}

	public class MySelectableItem
	{
		public MySelectableItem(int name)
		{
			Name = name.ToString();
		}

		public string Name { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}
