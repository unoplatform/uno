using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ListBoxTests
{
	[Sample("ListBox", IsManualTest = true, Description = "Manual check of the selection modes: clicks with Ctrl/Shift, keyboard (arrows, Space, Ctrl+A, Shift ranges), SelectAll and item changes.")]
	public sealed partial class ListBox_SelectionModes : UserControl
	{
		private readonly ObservableCollection<string> _items = new(Enumerable.Range(0, 50).Select(i => $"Item {i}"));
		private readonly List<string> _events = new();
		private int _inserted;

		public ListBox_SelectionModes()
		{
			this.InitializeComponent();

			SUT.ItemsSource = _items;
			ModeCombo.SelectedIndex = 0;
			ModeCombo.SelectionChanged += OnModeChanged;
			FollowsFocusCheck.Checked += OnFollowsFocusChanged;
			FollowsFocusCheck.Unchecked += OnFollowsFocusChanged;
			UpdateState();
		}

		private void OnModeChanged(object sender, SelectionChangedEventArgs e)
		{
			SUT.SelectionMode = Enum.Parse<SelectionMode>((string)ModeCombo.SelectedItem);
			UpdateState();
		}

		private void OnFollowsFocusChanged(object sender, RoutedEventArgs e)
			=> SUT.SingleSelectionFollowsFocus = FollowsFocusCheck.IsChecked == true;

		private void OnSelectAll(object sender, RoutedEventArgs e)
		{
			try
			{
				SUT.SelectAll();
			}
			catch (Exception ex)
			{
				Log($"SelectAll threw {ex.GetType().Name}");
			}
		}

		private void OnInsert(object sender, RoutedEventArgs e)
			=> _items.Insert(0, $"Inserted {++_inserted}");

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Log($"+[{string.Join(", ", e.AddedItems)}] -[{string.Join(", ", e.RemovedItems)}]");
			UpdateState();
		}

		private void Log(string message)
		{
			_events.Insert(0, message);
			if (_events.Count > 10)
			{
				_events.RemoveAt(_events.Count - 1);
			}

			EventsText.Text = string.Join(Environment.NewLine, _events);
		}

		private void UpdateState()
		{
			SelectedIndexText.Text = SUT.SelectedIndex.ToString();
			SelectedItemsText.Text = string.Join(", ", SUT.SelectedItems);
		}
	}
}
