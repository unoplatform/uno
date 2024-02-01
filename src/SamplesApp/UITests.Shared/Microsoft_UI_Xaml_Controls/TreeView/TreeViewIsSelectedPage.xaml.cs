// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

using TreeViewSelectionMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewSelectionMode;
using TreeViewNode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewNode;
using TreeView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView;
using TreeViewItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewItemInvokedEventArgs;
using TreeViewExpandingEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewExpandingEventArgs;
using TreeViewDragItemsStartingEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs;
using TreeViewDragItemsCompletedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewDragItemsCompletedEventArgs;
using TreeViewList = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewList;
using TreeViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewItem;
//using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;
using System.Threading.Tasks;

// Uno specific
using Uno.UI.Samples.Controls;
using MUXControlsTestApp.Utilities;
using Uno.Extensions.Specialized;
using System.ComponentModel;

// Replace listControl.GetItems().Count() with listControl.GetItems().Count()

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
		}
	}
}
