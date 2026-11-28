// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewItemsSourceTestPage.xaml.cs

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	public sealed partial class TreeViewItemsSourceTestPage : MUXTestPage
	{
		private readonly TreeViewViewModel viewModel = new();

		public TreeViewItemsSourceTestPage()
		{
			this.InitializeComponent();
		}

		private void SetUpContainerReuseTest_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.ItemsSource = null;
			TestTreeView.ItemsSource = PrepareItemsSourceForContainerReuseTest();
		}

		private void ExpandSecondItem_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.RootNodes[1].IsExpanded = true;
		}

		private void ScrollToEndOfContentTreeView_Click(object sender, RoutedEventArgs e)
		{
			var sv = EnumerateDescendants(TestTreeView).OfType<ScrollViewer>().First();
			sv.ScrollToVerticalOffset(9999);
		}

		private void ScrollToTopOfContentTreeView_Click(object sender, RoutedEventArgs e)
		{
			var sv = EnumerateDescendants(TestTreeView).OfType<ScrollViewer>().First();
			sv.ScrollToVerticalOffset(0);
		}

		private static IEnumerable<UIElement> EnumerateDescendants(UIElement reference)
		{
			var children = Enumerable
				.Range(0, VisualTreeHelper.GetChildrenCount(reference))
				.Select(x => VisualTreeHelper.GetChild(reference, x))
				.OfType<UIElement>();
			foreach (var child in children)
			{
				yield return child;
				foreach (var grandchild in EnumerateDescendants(child))
				{
					yield return grandchild;
				}
			}
		}

		public ObservableCollection<TreeViewViewModelItem> PrepareItemsSourceForContainerReuseTest()
		{
			var list = Enumerable.Range(0, 60).Select(i =>
				new TreeViewViewModelItem
				{
					Name = $"Root{i}",
					Items = new ObservableCollection<TreeViewViewModelItem>
					{
					new TreeViewViewModelItem { Name = $"Root{i} Child 1", Items = new ObservableCollection<TreeViewViewModelItem>
						{
							new TreeViewViewModelItem { Name = $"Root{i} Grandchild 1" },
							new TreeViewViewModelItem { Name = $"Root{i} Grandchild 2" }
						}
					},
					new TreeViewViewModelItem { Name = $"Root{i} Child 2" }
					}
				}
			).ToList();

			return new ObservableCollection<TreeViewViewModelItem>(list);
		}
	}
}
