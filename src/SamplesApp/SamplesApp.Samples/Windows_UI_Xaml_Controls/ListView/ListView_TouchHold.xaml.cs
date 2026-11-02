using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_TouchHold", IsManualTest = true,
		Description = "A touch long-press on a record opens its menu without raising ItemClick nor selecting it, and does not select the page.")]
	public sealed partial class ListView_TouchHold : Page
	{
		private readonly List<string> _entries = new();
		private int _itemClicks;
		private int _selectionChanges;
		private int _menuOpenings;
		private int _buttonClicks;
		private int _taps;
		private int _doubleTaps;

		public ListView_TouchHold()
		{
			this.InitializeComponent();

			Records.ItemsSource = Enumerable.Range(1, 30).Select(i => $"Record {i}").ToArray();
			((MenuFlyout)Resources["ItemMenu"]).Opened += (_, _) => Report(ref _menuOpenings, "Menu opened");

			UpdateCounters();
		}

		private void OnItemClick(object sender, ItemClickEventArgs e) => Report(ref _itemClicks, $"ItemClick {e.ClickedItem}");

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			=> Report(ref _selectionChanges, $"Selected {e.AddedItems.FirstOrDefault() ?? "nothing"}");

		private void OnButtonClick(object sender, RoutedEventArgs e) => Report(ref _buttonClicks, $"Click {((Button)sender).Content}");

		private void OnAreaTapped(object sender, TappedRoutedEventArgs e) => Report(ref _taps, $"Tapped ({e.PointerDeviceType})");

		private void OnAreaDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => Report(ref _doubleTaps, $"DoubleTapped ({e.PointerDeviceType})");

		private void OnMenuItemClick(object sender, RoutedEventArgs e) => AddLog($"Menu item {((MenuFlyoutItem)sender).Text}");

		private void OnClearClick(object sender, RoutedEventArgs e)
		{
			Records.SelectedIndex = -1;
			_itemClicks = _selectionChanges = _menuOpenings = _buttonClicks = _taps = _doubleTaps = 0;
			_entries.Clear();
			Log.Text = "";
			UpdateCounters();
		}

		private void Report(ref int counter, string message)
		{
			counter++;
			AddLog(message);
		}

		private void AddLog(string message)
		{
			_entries.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {message}");
			if (_entries.Count > 50)
			{
				_entries.RemoveAt(_entries.Count - 1);
			}

			Log.Text = string.Join(Environment.NewLine, _entries);
			UpdateCounters();
		}

		private void UpdateCounters()
			=> Counters.Text = $"ItemClick: {_itemClicks}   Selection changes: {_selectionChanges}   Menu opened: {_menuOpenings}   "
				+ $"Button clicks: {_buttonClicks}   Area Tapped: {_taps}   Area DoubleTapped: {_doubleTaps}";
	}
}
