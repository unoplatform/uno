using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample]
	public sealed partial class ListView_ObservableCollection_CreationCount : UserControl
	{
		private readonly IEnumerator _automationFlow;
		private readonly ObservableCollection<Item> _collection = new ObservableCollection<Item>(Enumerable.Range(0, 2).Select(i => GetItem(i, i)));

		public ListView_ObservableCollection_CreationCount()
		{
			_automationFlow = AutomationFlow().GetEnumerator();
			this.InitializeComponent();

			this.Loaded += (_, _) =>
			{
				CounterGrid.WasUpdated += CounterGrid_WasUpdated;
				CounterGrid2.WasUpdated += CounterGrid_WasUpdated;
			};

			this.Unloaded += (_, _) =>
			{
				CounterGrid.WasUpdated -= CounterGrid_WasUpdated;
				CounterGrid2.WasUpdated -= CounterGrid_WasUpdated;
			};

			SubjectList.ItemsSource = _collection;
			ModificationSelector.ItemsSource = new[]
			{
				NotifyCollectionChangedAction.Add,
				NotifyCollectionChangedAction.Remove,
				NotifyCollectionChangedAction.Replace
			};
			UpdateModificationSelectionList();
		}

		private void CounterGrid_WasUpdated()
		{
			CreationCountText.Text = CounterGrid.CreationCount.ToString();
			BindCountText.Text = CounterGrid.BindCount.ToString();
			CreationCount2Text.Text = CounterGrid2.CreationCount.ToString();
		}
		private void Automate_Click(object sender, RoutedEventArgs args)
		{
			_automationFlow.MoveNext();
		}

		private IEnumerable AutomationFlow()
		{
			for (int i = 0; i < 6; i++)
			{
				AddIndex(0);
			}
			AutomationStepTextBlock.Text = "Added";
			yield return null;

			var sv = SubjectList.FindFirstChild<ScrollViewer>();
			sv.ChangeView(null, verticalOffset: 60, null, disableAnimation: true);
			AutomationStepTextBlock.Text = "Scrolled1";
			yield return null;

			sv.ChangeView(null, verticalOffset: 120, null, disableAnimation: true);
			AutomationStepTextBlock.Text = "Scrolled2";
			yield return null;

			AddIndex(0);
			AddIndex(0);
			AutomationStepTextBlock.Text = "Added above";
			yield return null;

			RemoveIndex(0);
			AutomationStepTextBlock.Text = "Removed above";
			yield return null;
		}

		private void ApplyModifications(object sender, RoutedEventArgs e)
		{
			if (ModificationSelector.SelectedItem is NotifyCollectionChangedAction action && ModificationSelectionList.SelectedItems.Count > 0)
			{
				var indices = ModificationSelectionList.SelectedItems.OfType<int>().OrderByDescending(i => i).ToList();
				switch (action)
				{
					case NotifyCollectionChangedAction.Add:
						foreach (var index in indices)
						{
							AddIndex(index);
						}
						break;
					case NotifyCollectionChangedAction.Remove:
						foreach (var index in indices)
						{
							RemoveIndex(index);
						}
						break;
					case NotifyCollectionChangedAction.Replace:
						foreach (var index in indices)
						{
							ReplaceIndex(index);
						}
						break;
				}
				UpdateModificationSelectionList();
			}
		}

		private void ReplaceIndex(int index)
		{
			var newVal = (GetItemValFor(index) + GetItemValFor(index + 1)) / 2;
			_collection[index] = GetItem(newVal, index);
		}

		private void RemoveIndex(int index) => _collection.RemoveAt(index);

		private void AddIndex(int index)
		{
			var newVal = (GetItemValFor(index - 1) + GetItemValFor(index)) / 2;
			_collection.Insert(index, GetItem(newVal, index));
		}

		private static Item GetItem(double itemVal, int initialIndex) =>
			new Item
			{
				Val = itemVal,
				Display = GetItemsString(initialIndex, itemVal),
				ItemHeight = GetItemHeight(initialIndex)
			};

		private static double GetItemHeight(int initialIndex) => 50 + ((initialIndex % 3) * 10);

		private void UpdateModificationSelectionList()
		{
			ModificationSelectionList.ItemsSource = Enumerable.Range(0, _collection.Count).ToList();
		}

		private double GetItemValFor(int index)
		{
			if (_collection.Count == 0)
			{
				return 1;
			}
			if (index < 0)
			{
				return -1;
			}
			if (index >= _collection.Count)
			{
				return _collection[_collection.Count - 1].Val * 2;
			}

			return _collection[index].Val;
		}

		private static string GetItemsString(int index, double itemVal) => $"Item {itemVal}-{index} H={GetItemHeight(index)}";

		public class Item
		{
			public double Val { get; set; }
			public string Display { get; set; }
			public double ItemHeight { get; set; }
		}
	}
}
