#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using System.Collections.ObjectModel;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using IconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.IconSource;
using SymbolIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SymbolIconSource;

namespace UITests.Microsoft_UI_Xaml_Controls.TabViewTests
{
	public class MyData
	{
		public string DataHeader { get; set; }
		public IconSource DataIconSource { get; set; }
		public object DataContent { get; set; }
	}

	public sealed partial class TabViewTabItemsSourcePage : Page
	{
		const int initialTabsCount = 3;
		ListView innerListView = null;
		ObservableCollection<MyData> myDatas;

		public TabViewTabItemsSourcePage()
		{
			this.InitializeComponent();

			Loaded += TabViewTabItemsSourcePage_Loaded;
			TabViewItemsSourceSample.TabItemsChanged += TabViewItemsSourceSample_TabItemsChanged;

			InitializeDataBindingSampleData();
		}

		private void TabViewItemsSourceSample_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
		{
			tblIVectorChangedEventArgsCollectionChange.Text = args.CollectionChange.ToString();
			tblIVectorChangedEventArgsIndex.Text = args.Index.ToString();
		}

		private void TabViewTabItemsSourcePage_Loaded(object sender, RoutedEventArgs e)
		{
			innerListView = TabViewItemsSourceSample.FindElementOfTypeInSubtree<ListView>();

			for (int tabIndex = 0; tabIndex < initialTabsCount; tabIndex++)
			{
				var tabViewItem = TabViewItemsSourceSample.ContainerFromIndex(tabIndex) as TabViewItem;

				if (tabViewItem != null)
				{
					tabViewItem.Name = "tabViewItem" + tabIndex;
					AutomationProperties.SetName(tabViewItem, tabViewItem.Name);
				}
			}
		}

		private void ChkCanDragItems_Checked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.CanDragItems = true;
			}
		}

		private void ChkCanDragItems_Unchecked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.CanDragItems = false;
			}
		}

		private void ChkCanReorderItems_Checked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.CanReorderItems = true;
			}
		}

		private void ChkCanReorderItems_Unchecked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.CanReorderItems = false;
			}
		}

		private void ChkCanDrag_Checked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.CanDrag = true;
			}
		}

		private void ChkCanDrag_Unchecked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.CanDrag = false;
			}
		}

		private void ChkAllowDrop_Checked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.AllowDrop = true;
			}
		}

		private void ChkAllowDrop_Unchecked(object sender, RoutedEventArgs e)
		{
			if (innerListView != null)
			{
				innerListView.AllowDrop = false;
			}
		}

		private void InitializeDataBindingSampleData()
		{
			myDatas = new ObservableCollection<MyData>();

			for (int index = 0; index < initialTabsCount; index++)
			{
				myDatas.Add(CreateNewMyData(index));
			}
		}

		private MyData CreateNewMyData(int index)
		{
			var textBlock = new TextBlock();
			textBlock.Text = "Sample" + index;

			var newData = new MyData
			{
				DataHeader = $"MyData {index}",
				DataIconSource = new SymbolIconSource() { Symbol = Symbol.Caption + index },
				DataContent = textBlock
			};

			return newData;
		}

		private void TabViewItemsSourceSample_AddTabButtonClick(TabView sender, object args)
		{
			// Add a new MyData item to the collection. TabView automatically generates a TabViewItem.
			myDatas.Add(CreateNewMyData(myDatas.Count));
		}

		private void TabViewItemsSourceSample_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			// Remove the requested MyData object from the collection. 
			myDatas.Remove(args.Item as MyData);
		}
	}
}
