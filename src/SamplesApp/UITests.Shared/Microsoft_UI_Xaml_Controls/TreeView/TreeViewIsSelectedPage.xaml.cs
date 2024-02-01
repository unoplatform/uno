// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;

using Uno.UI.Samples.Controls;
using TreeView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView;
using TreeViewNode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewNode;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	[Sample("MUX", "TreeView")]
	public sealed partial class TreeViewIsSelectedPage : MUXTestPage, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private ObservableCollection<TreeViewItemSource> m_testTreeViewItemsSource;
		public ObservableCollection<TreeViewItemSource> TestTreeViewItemsSource
		{
			get { return m_testTreeViewItemsSource; }
			set
			{
				if (m_testTreeViewItemsSource != value)
				{
					m_testTreeViewItemsSource = value;
					NotifyPropertyChanged(nameof(TestTreeViewItemsSource));
				}
			}
		}

		private void NotifyPropertyChanged(String propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public TreeViewIsSelectedPage()
		{
			this.DataContext = this;
			this.InitializeComponent();

			//var child1 = new TreeViewItemSource() { Content = "child1" };
			//var children1 = new ObservableCollection<TreeViewItemSource>() { child1 };
			//var item1 = new TreeViewItemSource() { Content = "item1", Children = children1 };

			var child2 = new TreeViewItemSource() { Content = "child1", IsSelected = true };
			var children2 = new ObservableCollection<TreeViewItemSource>() { child2 };
			var item2 = new TreeViewItemSource() { Content = "item2", Children = children2 };

			TestTreeViewItemsSource = new ObservableCollection<TreeViewItemSource>() { /*item1,*/ item2 };


			StackPanel panel = new StackPanel
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			};

			TreeView treeView = new TreeView
			{
				RootNodes =
			{
				new TreeViewNode
				{
					Content = "1111",
				},
				new TreeViewNode
				{
					Content = "2222"
				},
				new TreeViewNode
				{
					Content = "333"
				}
			}
			};

			treeView.Loaded += (s, e) =>
			{
				treeView.SelectedItem = treeView.RootNodes[1];
			};

			panel.Children.Add(treeView);
			SecondTreeView.Children.Add(panel);


		}
	}
}
