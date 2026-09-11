using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.ItemContainerTests
{
	[Sample("ItemContainer", "ListView", Description = "ItemContainer as the root of a ListView ItemTemplate: clicking an item must select it and raise SelectionChanged (issue #23892).")]
	public sealed partial class ItemContainer_In_ListView_ItemTemplate : Page
	{
		private int _itemContainerSelectionCount;
		private int _standardSelectionCount;
		private int _pressedCount;
		private int _releasedCount;
		private int _captureLostCount;

		public ItemContainer_In_ListView_ItemTemplate()
		{
			this.InitializeComponent();

			var items = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToArray();
			ItemContainerListView.ItemsSource = items;
			StandardListView.ItemsSource = items;

			ItemContainerListView.SelectionChanged += (s, e) =>
			{
				_itemContainerSelectionCount++;
				ItemContainerListViewStatus.Text = $"Selected: {ItemContainerListView.SelectedItem ?? "None"} / SelectionChanged: {_itemContainerSelectionCount}";
			};

			StandardListView.SelectionChanged += (s, e) =>
			{
				_standardSelectionCount++;
				StandardListViewStatus.Text = $"Selected: {StandardListView.SelectedItem ?? "None"} / SelectionChanged: {_standardSelectionCount}";
			};

			ItemContainerListView.AddHandler(PointerPressedEvent, new PointerEventHandler((s, e) => OnPointerDiagnostic(nameof(PointerPressedEvent), ref _pressedCount)), handledEventsToo: true);
			ItemContainerListView.AddHandler(PointerReleasedEvent, new PointerEventHandler((s, e) => OnPointerDiagnostic(nameof(PointerReleasedEvent), ref _releasedCount)), handledEventsToo: true);
			ItemContainerListView.AddHandler(PointerCaptureLostEvent, new PointerEventHandler((s, e) => OnPointerDiagnostic(nameof(PointerCaptureLostEvent), ref _captureLostCount)), handledEventsToo: true);
		}

		private void OnPointerDiagnostic(string eventName, ref int count)
		{
			count++;
			PointerDiagnostics.Text = $"Last event: {eventName} / Pressed: {_pressedCount} Released: {_releasedCount} CaptureLost: {_captureLostCount}";
		}
	}
}
