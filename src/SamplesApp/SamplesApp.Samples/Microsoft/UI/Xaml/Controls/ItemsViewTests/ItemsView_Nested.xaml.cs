using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace UITests.Microsoft_UI_Xaml_Controls.ItemsViewTests
{
	[Sample("ItemsView", IgnoreInSnapshotTests = true)]
	public sealed partial class ItemsView_Nested : Page
	{
		List<Category> Categories { get; set; }

		public ItemsView_Nested()
		{
			InitializeComponent();

			Categories = GenerateRandomCategories(5, 10);

			Categories[0].Collapsed = !Categories[0].Collapsed;
		}

		private static List<Category> GenerateRandomCategories(int categoryCount, int maxItemsPerCategory)
		{
			var random = new Random();
			var categories = new List<Category>();

			for (int i = 1; i <= categoryCount; i++)
			{
				var itemCount = random.Next(1, maxItemsPerCategory + 1);
				var items = Enumerable.Range(1, itemCount)
									  .Select(j => new Item($"Item {j}"))
									  .ToList();
				var category = new Category($"Category {i}", items, random.Next(0, 2) == 0);
				categories.Add(category);
			}

			return categories;
		}
	}

	public partial class Item(string label)
	{
		public string Label { get; } = label;
	}

	public partial class Category : ObservableCollection<Item>
	{
		private bool _collapsed;

		private readonly List<Item> _collapsedItems = [];

		public string Label { get; }

		public bool Collapsed
		{
			get => _collapsed;
			set
			{
				if (_collapsed != value)
				{
					if (_collapsed)
					{
						foreach (Item item in _collapsedItems)
							Add(item);
						_collapsedItems.Clear();
					}
					else
					{
						_collapsedItems.AddRange(Items);
						Clear();
					}
					_collapsed = value;
				}
			}
		}

		public Category(string label, IEnumerable<Item> items, bool collapsed = false) : base(items)
		{
			Label = label;
			Collapsed = collapsed;
		}
	}
}
