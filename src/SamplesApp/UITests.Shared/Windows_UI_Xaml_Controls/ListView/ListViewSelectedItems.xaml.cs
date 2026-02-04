using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.UI.Samples.Controls;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListViewSelectedItems")]
	public sealed partial class ListViewSelectedItems : UserControl
	{
#pragma warning disable CS0109
#if HAS_UNO
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#endif
#pragma warning restore CS0109

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
				_log.Warn($"Adding {itemClicked} to SelectedItems.");
				selectedItems.Add(itemClicked);
			}
			else
			{
				_log.Warn($"Removing {itemClicked} from SelectedItems.");
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
			_log.Warn(text);
			SelectionChangedTextBlock.Text = text;
		}

		private static string EnumerableToDisplayString(IEnumerable<object> enumerable)
		{
			return $"({enumerable.Aggregate("", (accum, next) => accum + next?.ToString() + ", ")})";
		}

		private void ClearSelectedItem(object sender, RoutedEventArgs e)
		{
			_log.Warn("Clearing selected items");
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
