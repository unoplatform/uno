// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml\test\managed\enterprise\Moco\ListViewBaseTests.cs, commit bac7a9c33ff

#pragma warning disable CS0168 // Disable TestCleanupWrapper warnings
#pragma warning disable 168 // for cleanup imported member

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using UIExecutor = MUXControlsTestApp.Utilities.RunOnUIThread;

namespace Microsoft.UI.Xaml.Tests.Enterprise;

public partial class ListViewBaseTests
{
	[TestMethod]
	public async Task EstimationCorrectionAfterScrollIntoViewUngrouped()
	{
		ListView lv = null;

		// 500 items.
		// item 0 is 50 high and the rest are 200 high
		// if we do a scroll into view item 450 right after setting the itemsource,
		// we will measure the first item (item 0) and then estimate the location of 450
		// the estimate will be 450*50 since 50 is the only item we base the estimate on - it is
		// off by a lot. This will show when you start scrolling the thumb, once we get near
		// the top or bottom of the list we will correct by a lot causing the thumb to jump.
		// This test does a one time correction after a scroll into view once we have more items
		// generated and hence a little better estimate.
		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;

			lv.ContainerContentChanging += (s, args) =>
			{
				// verify that only the first item is first page is loaded
				Log.Comment("CCC Item: " + args.ItemIndex);

				// 400 to account for caching
				if (args.ItemIndex > 0 && args.ItemIndex < 400)
				{
					Log.Error("We should be getting calls only for item 0 and then item 450. Rest of first page should not be loaded");
				}

				if (args.ItemIndex == 0)
				{
					args.ItemContainer.Height = 50;
				}
				else
				{
					args.ItemContainer.Height = 200;
				}
			};

			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			lv.ItemsSource = Enumerable.Range(0, 500);
			lv.ScrollIntoView(450);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Check that we have
		UIExecutor.Execute(() =>
		{
			var isp = lv.ItemsPanelRoot as ItemsStackPanel;

			var container = lv.ContainerFromIndex(450) as ListViewItem;
			double offset = container.TransformToVisual(isp).TransformPoint(new global::Windows.Foundation.Point(0, 0)).Y;

			// the offset of container 450 in the panel should be
			// 50 + 449*200 = 89,850
			// If we accounted only the first item (item 0) as we used to
			// we would estimate item 450 to be at 450*50 = 22500
			Verify.IsTrue(offset > 75000, "offset of the scrolled item is lower than expected");


			var height = isp.ActualHeight;
			// If only the first item was accounted for then the entire panel
			// would only be 500*50 = 25000 high. Once error corrected we should be
			// getting much bigger than that (actual extent is 50 + 199*500 = 99,550)
			Log.Comment("Height is = " + height.ToString());

			// 85000 because we are still working with an estimate that includes
			// 50 for the first item, 200 for the items in the cache - hence the estimate
			// is still lower than the actual extent of 99550
			Verify.IsTrue(height > 85000, "Panel extent is lower than expected after correction");
		});
	}

	[TestMethod]
	public async Task EstimationCorrectionAfterScrollIntoViewGroupedItem()
	{
		await EstimationCorrectionAfterScrollIntoViewGrouped(false);
	}

	[TestMethod]
	public async Task EstimationCorrectionAfterScrollIntoViewGroupedHeader()
	{
		await EstimationCorrectionAfterScrollIntoViewGrouped(true);
	}

	public async Task EstimationCorrectionAfterScrollIntoViewGrouped(bool scrollToHeader)
	{
		ListView lv = null;
		CollectionViewSource cvs = null;
		var groups = GetGroupedData(numGroups: 50, itemsPerGroup: 10);

		// 500 items.
		// item 0 is 50 high and the rest are 200 high
		// if we do a scroll into view item 450 right after setting the itemsource,
		// we will measure the first item (item 0) and then estimate the location of 450
		// the estimate will be 450*50 since 50 is the only item we base the estimate on - it is
		// off by a lot. This will show when you start scrolling the thumb, once we get near
		// the top or bottom of the list we will correct by a lot causing the thumb to jump.
		// This test does a one time correction after a scroll into view once we have more items
		// generated and hence a little better estimate.
		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;
			cvs = new CollectionViewSource() { IsSourceGrouped = true };
			cvs.Source = groups;

			lv.ContainerContentChanging += (s, args) =>
			{
				// verify that only the first item is first page is loaded
				Log.Comment("CCC Item: " + args.ItemIndex);

				// 400 to account for caching
				if (args.ItemIndex > 0 && args.ItemIndex < 400)
				{
					Log.Error("We should be getting calls only for item 0 and then item 450. Rest of first page should not be loaded");
				}

				if (args.ItemIndex == 0)
				{
					args.ItemContainer.Height = 50;
				}
				else
				{
					args.ItemContainer.Height = 200;
				}
			};

			lv.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
			   "     <GroupStyle.HeaderTemplate>                                                       " +
			   "         <DataTemplate>                                                                " +
			   "             <Grid Background='Red' >                                                  " +
			   "                 <TextBlock Text='{Binding}' Foreground='White' FontSize='30'/>    " +
			   "             </Grid>                                                                   " +
			   "         </DataTemplate>                                                               " +
			   "     </GroupStyle.HeaderTemplate>                                                      " +
			   " </GroupStyle>                                                                         "));

			lv.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> " +
			   "     <TextBlock Text='{Binding}' />  " +
			   " </DataTemplate>                         ");

			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			lv.ItemsSource = cvs.View;
			if (scrollToHeader)
			{
				lv.ScrollIntoView(groups[45]);
			}
			else
			{
				lv.ScrollIntoView(groups[45][0]);
			}
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Check that we have
		UIExecutor.Execute(() =>
		{
			var isp = lv.ItemsPanelRoot as ItemsStackPanel;

			var container = lv.ContainerFromItem(groups[45][0]) as ListViewItem;
			double offset = container.TransformToVisual(isp).TransformPoint(new global::Windows.Foundation.Point(0, 0)).Y;

			Log.Comment("Offset is " + offset.ToString());
			// the offset of container 450 in the panel should be
			// 50 + 449*200 = 89,850
			// If we accounted only the first item (item 0) as we used to
			// we would estimate item 450 to be at 450*50 = 22500
			Verify.IsTrue(offset > 75000, "offset of the scrolled item is lower than expected");


			var height = isp.ActualHeight;
			// If only the first item was accounted for then the entire panel
			// would only be 500*50 = 25000 high. Once error corrected we should be
			// getting much bigger than that (actual extent is 50 + 199*500 = 99,550)
			Log.Comment("Height is = " + height.ToString());

			// 85000 because we are still working with an estimate that includes
			// 50 for the first item, 200 for the items in the cache - hence the estimate
			// is still lower than the actual extent of 99550
			Verify.IsTrue(height > 85000, "Panel extent is lower than expected after correction");
		});
	}

	// inserting before the selected index, when the selected index is
	// not in view causes the selected index to be scrolled back into view.
	// If this happens during a group insertion, we end up crashing because
	// group notifications and item notifications are expected to happen in the
	// same tick and a layout in the middle of that is not expected.
	[TestMethod]
	public async Task SelectionChangeDuringGroupInsert()
	{
		ListView lv = null;
		CollectionViewSource cvs = null;
		var groups = GetGroupedData(numGroups: 10, itemsPerGroup: 10);

		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;
			cvs = new CollectionViewSource() { IsSourceGrouped = true };
			cvs.Source = groups;

			lv.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
			   "     <GroupStyle.HeaderTemplate>                                                       " +
			   "         <DataTemplate>                                                                " +
			   "             <Grid Background='Red' >                                                  " +
			   "                 <TextBlock Text='{Binding}' Foreground='White' FontSize='30'/>    " +
			   "             </Grid>                                                                   " +
			   "         </DataTemplate>                                                               " +
			   "     </GroupStyle.HeaderTemplate>                                                      " +
			   " </GroupStyle>                                                                         "));

			lv.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> " +
			   "     <TextBlock Text='{Binding}' />  " +
			   " </DataTemplate>                         ");

			lv.ItemsSource = cvs.View;
			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// scroll down to the end of the list and
		// select an item.
		UIExecutor.Execute(() =>
		{
			lv.ScrollIntoView(groups[9][0]);
			lv.SelectedIndex = 90;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// scroll back up the list
		UIExecutor.Execute(() =>
		{
			lv.ScrollIntoView(groups[0][0]);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// insert a group (with many items). This used
		// to cause selection changed to be fired and
		// a forced scroll item into view for the selected
		// item while the group change was in progress.
		UIExecutor.Execute(() =>
		{
			// insert at 0 a group with 10 items
			ObservableCollection<int> group = new();
			for (int j = 0; j < 10; j++)
			{
				group.Add(j);
			}
			groups.Insert(0, group);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// yay. we didn't crash.
	}

	[TestMethod]
	public async Task InsertGroupInMiddleOfVisibleWindow()
	{
		ListView lv = null;
		CollectionViewSource cvs = null;
		var groups = GetGroupedData(numGroups: 10, itemsPerGroup: 1);

		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;
			cvs = new CollectionViewSource() { IsSourceGrouped = true };
			cvs.Source = groups;

			lv.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
			   "     <GroupStyle.HeaderTemplate>                                                       " +
			   "         <DataTemplate>                                                                " +
			   "             <Grid Background='Red' >                                                  " +
			   "                 <TextBlock Text='{Binding}' Foreground='White' FontSize='30'/>    " +
			   "             </Grid>                                                                   " +
			   "         </DataTemplate>                                                               " +
			   "     </GroupStyle.HeaderTemplate>                                                      " +
			   " </GroupStyle>                                                                         "));

			lv.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> " +
			   "     <TextBlock Text='{Binding}' />  " +
			   " </DataTemplate>");

			lv.ItemsSource = cvs.View;
			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// insert a group in the middle of visible window.
		// This will cause a sentinel to be added for the group header
		UIExecutor.Execute(() =>
		{
			ObservableCollection<int> group = new();
			group.Add(4444);
			groups.Insert(2, group);
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var container = lv.ContainerFromItem(4444);
			Verify.IsNotNull(container, "Container is not null");
			var groupContainer = lv.GroupHeaderContainerFromItemContainer(container);
			Verify.IsNotNull(groupContainer, "GroupContainer is not null");
		});

	}

	// when using mouse to click on the large increase/decrease
	// buttons on the thumb, and using sticky headers - we did not
	// account for the sticky header taking up space in the viewport.
	// this test makes sure that we account for that.
	[TestMethod]
	public async Task MouseLargeClickWithStickyHeaders()
	{
		ListView lv = null;
		CollectionViewSource cvs = null;
		var groups = GetGroupedData(numGroups: 10, itemsPerGroup: 20);
		RepeatButton largeIncreaseButton = null;
		RepeatButton largeDecreaseButton = null;
		ItemsStackPanel isp = null;

		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;
			lv.Width = 400;
			cvs = new CollectionViewSource() { IsSourceGrouped = true };
			cvs.Source = groups;

			lv.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
			   "     <GroupStyle.HeaderTemplate>                                                       " +
			   "         <DataTemplate>                                                                " +
			   "             <Grid Background='Red' Height='45'>                                                  " +
			   "                 <TextBlock Text='{Binding}' Foreground='White' FontSize='30'/>    " +
			   "             </Grid>                                                                   " +
			   "         </DataTemplate>                                                               " +
			   "     </GroupStyle.HeaderTemplate>                                                      " +
			   " </GroupStyle>                                                                         "));

			lv.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> " +
			   "    <Grid Height='45' > " +
			   "        <TextBlock Text='{Binding}' />  " +
			   "    </Grid> " +
			   " </DataTemplate>");
			lv.ItemsSource = cvs.View;

			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();
		TestServices.InputHelper.MoveMouse(lv);
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			isp = lv.ItemsPanelRoot as ItemsStackPanel;
			largeIncreaseButton = lv.FindNameInSubtree("VerticalLargeIncrease") as RepeatButton;
			largeDecreaseButton = lv.FindNameInSubtree("VerticalLargeDecrease") as RepeatButton;

			Log.Comment("FirstVisibleIndex:" + isp.FirstVisibleIndex);
			Verify.IsTrue(isp.FirstVisibleIndex == 0);
		});

		TestServices.InputHelper.MoveMouse(largeIncreaseButton);
		TestServices.InputHelper.LeftMouseClick(largeIncreaseButton);
		await TestServices.WindowHelper.WaitForIdle();

		// 0 to 9 is visible in the first view, after clicking on the
		// mouse large increase button - the new first visible index
		// would have been 10 (but 10 would be hidden by the sticky header)
		// with the fix, the new first visible index is 9. same logic for the
		// rest of the verification.
		UIExecutor.Execute(() =>
		{
			Log.Comment("FirstVisibleIndex:" + isp.FirstVisibleIndex);
			Verify.IsTrue(isp.FirstVisibleIndex == 9);
		});

		TestServices.InputHelper.MoveMouse(largeIncreaseButton);
		TestServices.InputHelper.LeftMouseClick(largeIncreaseButton);
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Log.Comment("FirstVisibleIndex:" + isp.FirstVisibleIndex);
			Verify.IsTrue(isp.FirstVisibleIndex == 19);
		});

		TestServices.InputHelper.MoveMouse(largeDecreaseButton);
		TestServices.InputHelper.LeftMouseClick(largeDecreaseButton);
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Log.Comment("FirstVisibleIndex:" + isp.FirstVisibleIndex);
			Verify.IsTrue(isp.FirstVisibleIndex == 9);
		});

		TestServices.InputHelper.MoveMouse(largeDecreaseButton);
		TestServices.InputHelper.LeftMouseClick(largeDecreaseButton);
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Log.Comment("FirstVisibleIndex:" + isp.FirstVisibleIndex);
			Verify.IsTrue(isp.FirstVisibleIndex == 0);
		});
	}

	private Dictionary<int, int> containerPhaseReached;
	void lv_ContainerContentChanging(Microsoft.UI.Xaml.Controls.ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (args.Phase < 5)
		{
			args.RegisterUpdateCallback(lv_ContainerContentChanging);
		}
		Log.Comment("CCC Item: " + args.ItemIndex, " Phase:" + args.Phase);

		containerPhaseReached[args.ItemIndex]++;
	}

	[TestMethod]
	public async Task PhasingContinuesAfterRenteringLiveTree()
	{
		Grid rootPanel = null;
		ListView lv = null;
		UnoAutoResetEvent middleOfCCC = new(false);
		containerPhaseReached = new Dictionary<int, int>();

		var loadedhandler = new Action<object, RoutedEventArgs>((s, args) =>
		{
			(lv.ItemsPanelRoot as ItemsStackPanel).CacheLength = 0;
		});

		var containerContentChangingHandler = new Action<object, ContainerContentChangingEventArgs>((s, args) =>
		{
			if (args.InRecycleQueue)
			{
				Log.Comment("CCC Recycle Item: " + args.ItemIndex);
			}
			else
			{
				Log.Comment("CCC Item: " + args.ItemIndex, " Phase:" + args.Phase);
				if (args.ItemIndex == 1)
				{
					middleOfCCC.Set();
				}

				if (!containerPhaseReached.ContainsKey(args.ItemIndex))
				{
					containerPhaseReached.Add(args.ItemIndex, 0);
				}

				args.RegisterUpdateCallback(lv_ContainerContentChanging);
			}
		});

		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;
		});

		using (new EventTester<ListView, RoutedEventArgs>(lv, "Loaded", loadedhandler))
		using (new EventTester<ListView, ContainerContentChangingEventArgs>(lv, "ContainerContentChanging", containerContentChangingHandler))
		{
			UIExecutor.Execute(() =>
			{
				lv.ItemsSource = Enumerable.Range(0, 500);
				rootPanel = new Grid();
				rootPanel.Children.Add(lv);
				TestServices.WindowHelper.WindowContent = rootPanel;
			});

			// wait until we are in the middle of ccc events //
			Verify.IsTrue(await middleOfCCC.WaitOne(TimeSpan.FromSeconds(20)), "Reached ccc for item 1");

			UIExecutor.Execute(() =>
			{
				Log.Comment("Removing from live tree");
				rootPanel.Children.Clear();
			});
			await TestServices.WindowHelper.WaitForIdle();

			UIExecutor.Execute(() =>
			{
				Log.Comment("Adding back to live tree");
				rootPanel.Children.Add(lv);
			});
			await TestServices.WindowHelper.WaitForIdle();

			// verify that we got ccc for all
			foreach (var key in containerPhaseReached.Keys)
			{
				Log.Comment("Index:" + key.ToString() + " Phase:" + containerPhaseReached[key].ToString());
				Verify.IsTrue(containerPhaseReached[key] == 5);
			}
		}
	}

	// Regression test
	[TestMethod]
	public async Task SetItemWidthToNanAfterFirstItemIsMeasured()
	{
		Grid rootPanel = null;
		GridView gv = null;
		UnoAutoResetEvent firstItemMeasured = new(false);

		// data containing null. We will replace it later with data //
		ObservableCollection<object> data = new();

		UIExecutor.Execute(() =>
		{
			gv = new GridView();
			gv.ShowsScrollingPlaceholders = false;
			gv.Height = 500;
			gv.Loaded += (s, args) =>
			{
				(gv.ItemsPanelRoot as ItemsWrapGrid).CacheLength = 0;
			};

			gv.ContainerContentChanging += (s, args) =>
			{
				if (args.InRecycleQueue)
				{
					Log.Comment("CCC Recycle Item: " + args.ItemIndex);
				}
				else
				{
					Log.Comment("CCC Item: " + args.ItemIndex, " Phase:" + args.Phase);
					if (args.ItemIndex == 0)
					{
						firstItemMeasured.Set();
					}
				}
			};

			for (int i = 0; i < 20; i++)
			{
				data.Add(null);
			}

			gv.ItemsSource = data;
			rootPanel = new Grid();
			rootPanel.Children.Add(gv);
			TestServices.WindowHelper.WindowContent = rootPanel;

		});

		// wait until we are in the middle of ccc events //
		Verify.IsTrue(await firstItemMeasured.WaitOne(TimeSpan.FromSeconds(20)), "Reached ccc for item 0");

		// we have measured item 0, now change ItemHeight to Nan.
		// This should cause us to re-measure the first item. If not,
		// we will end up crashing when measuring items following that.
		UIExecutor.Execute(() =>
		{
			(gv.ItemsPanelRoot as ItemsWrapGrid).ItemHeight = double.NaN;
			data[0] = "Item0";
			data[0] = "Item1";
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			data[2] = "Item2";
		});

		await TestServices.WindowHelper.WaitForIdle();

		// we didn't crash when measuring the rest of the items.
	}

	partial class CustomGroupStyleSelector : GroupStyleSelector
	{
		protected override GroupStyle SelectGroupStyleCore(object group, uint level)
		{
			var g = group as ICollectionViewGroup;
			int count = g != null ? g.GroupItems.Count : -1;
			string xaml = string.Format(
				@"<GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
							  xmlns:x = 'http://schemas.microsoft.com/winfx/2006/xaml'>
					<GroupStyle.HeaderTemplate>
						<DataTemplate>
							<TextBlock x:Name='TB' Text='{0}'/>
						</DataTemplate>
					</GroupStyle.HeaderTemplate>
				</GroupStyle> ", count);
			return (GroupStyle)XamlReader.Load(xaml);
		}
	}

	[TestMethod]
	public async Task GroupStyleSelectorTest()
	{
		Grid rootPanel = null;
		ListView lv = null;
		CollectionViewSource cvs = null;

		UIExecutor.Execute(() =>
		{
			lv = new ListView();

			List<Object> data = new();

			for (int i = 0; i < 4; i++)
			{
				List<int> group = new();
				for (int j = 0; j < i; j++)
				{
					group.Add(i * 100 + j);
				}
				data.Add(group);
			}

			cvs = new CollectionViewSource();
			cvs.IsSourceGrouped = true;
			cvs.Source = data;
			lv.ItemsSource = cvs.View;
			lv.GroupStyleSelector = new CustomGroupStyleSelector();

			rootPanel = new Grid();
			rootPanel.Children.Add(lv);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			// in below test we'll iterate all groups and check if the group styles are correctly applied.
			// note there is no direct way to iterate all groups, instead i hve to iterate all items
			// and call GroupHeaderContainerFromItemContainer to find the HeaderItem.
			foreach (var item in lv.Items)
			{
				var container = lv.ContainerFromItem(item);
				var header = lv.GroupHeaderContainerFromItemContainer(container) as ListViewBaseHeaderItem;
				var tb = header.FindNameInSubtree("TB") as TextBlock;
				// verify we applied correct group style by checking the TextBlock.Text inside the header template.
				Verify.AreEqual(tb.Text, (header.Content as List<int>).Count.ToString());
			}
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ZoomTest()
	{
		ListView listView = null;
		ItemsStackPanel itemsStackPanel = null;
		ScrollViewer scrollViewer = null;
		int firstVisibleIndex = 0;
		int lastVisibleIndex = 0;
		UnoManualResetEvent viewChangedEvent = new(false);

		UIExecutor.Execute(() =>
		{
			listView = new ListView();
			listView.ShowsScrollingPlaceholders = false;
			listView.Height = 500;
			listView.Loaded += (s, args) =>
			{
				(listView.ItemsPanelRoot as ItemsStackPanel).CacheLength = 0;
			};

			listView.ItemsSource = Enumerable.Range(0, 500);
			TestServices.WindowHelper.WindowContent = listView;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			scrollViewer = listView.FindElementOfTypeInSubtree<ScrollViewer>();
			scrollViewer.ZoomMode = ZoomMode.Enabled;
			scrollViewer.ViewChanged += (sender, e) =>
											{
												if (e.IsIntermediate == false)
												{
													viewChangedEvent.Set();
												}
											};
			itemsStackPanel = (ItemsStackPanel)listView.ItemsPanelRoot;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			firstVisibleIndex = itemsStackPanel.FirstVisibleIndex;
			lastVisibleIndex = itemsStackPanel.LastVisibleIndex;
			Verify.IsTrue(firstVisibleIndex == 0, "First visible index is 0");

			// zoom out
			scrollViewer.ChangeView(null, null, 0.5f, true);
		});

		// wait for the view changed event and then wait for
		// listview to handle the view change.
		await viewChangedEvent.WaitOne();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.IsTrue(firstVisibleIndex == 0, "First visible index is 0");
			Verify.IsTrue(itemsStackPanel.LastVisibleIndex > lastVisibleIndex, "lastvisibleindex is bigger when zoomed out");
			lastVisibleIndex = itemsStackPanel.LastVisibleIndex;

			// scroll
			scrollViewer.ChangeView(null, 500, null, true);
		});

		// wait for the view changed event and then wait for
		// listview to handle the view change.
		Verify.IsTrue(await viewChangedEvent.WaitOne(TimeSpan.FromSeconds(10)));
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.IsTrue(itemsStackPanel.FirstVisibleIndex > 0, "First visible index is not 0");
			Verify.IsTrue(itemsStackPanel.LastVisibleIndex > lastVisibleIndex, "lastvisibleindex is bigger when zoomed out");
		});
	}

	[TestMethod]
	public async Task ItemGenerationAndRecycleTest()
	{
		ListView listView = null;
		Dictionary<object, int> containers = new();
		List<int> data = Enumerable.Range(0, 30).ToList();

		UIExecutor.Execute(() =>
		{
			listView = (ListView)XamlReader.Load(@" <ListView
														  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
														  Height='200'
														  Width='200'>
														<ListView.ItemsPanel>
															<ItemsPanelTemplate>
																<ItemsStackPanel CacheLength='0' />
															</ItemsPanelTemplate>
														</ListView.ItemsPanel>
														<ListView.ItemContainerStyle>
															<Style TargetType='ListViewItem'>
																<Setter Property='Height' Value='50' />
															</Style>
														</ListView.ItemContainerStyle>
													</ListView>");
			listView.ItemsSource = data;
			TestServices.WindowHelper.WindowContent = listView;
			listView.ContainerContentChanging += (sender, args) =>
			{
				if (!args.InRecycleQueue && args.ItemContainer != null)
				{
					if (!containers.ContainsKey(args.ItemContainer))
					{
						containers.Add(args.ItemContainer, args.ItemIndex);
					}
				}
			};
		});

		await TestServices.WindowHelper.WaitForIdle();

		// verify the number of containers created - should be 5 since the viewport
		// is 200 pixels and each item is 50 pixels high, and we go one more before stopping
		Verify.IsTrue(containers.Values.Count == 5, "Container count is not 5");

		UIExecutor.Execute(() =>
		{
			listView.ScrollIntoView(data[15], ScrollIntoViewAlignment.Leading);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// verify that the containers were recycled, one pinned, 4 visible and
		// one more in the forward direction, one more in the backward direction
		Log.Comment("container count - " + containers.Values.Count);
		Verify.IsTrue(containers.Values.Count == 7, "Containers were not recycled");

		UIExecutor.Execute(() =>
		{
			listView.ScrollIntoView(data[0], ScrollIntoViewAlignment.Leading);
		});

		await TestServices.WindowHelper.WaitForIdle();
		Log.Comment("container count - " + containers.Values.Count);
		Verify.IsTrue(containers.Values.Count == 7, "Containers were not recycled");

		UIExecutor.Execute(() =>
		{
			listView.ScrollIntoView(data[5], ScrollIntoViewAlignment.Leading);
		});

		await TestServices.WindowHelper.WaitForIdle();
		Log.Comment("container count - " + containers.Values.Count);
		Verify.IsTrue(containers.Values.Count <= 8, "Containers were not recycled");
	}

	[TestMethod]
	[Ignore("Unreliable upstream: ListViewBaseTests.ThumbDragTest is marked Ignore in WinUI.")]
	public async Task ThumbDragTest()
	{
		ListView listView = null;
		Dictionary<object, int> containers = new();
		List<int> data = Enumerable.Range(0, 50).ToList();
		Thumb verticalThumb = null;

		UIExecutor.Execute(() =>
		{
			listView = (ListView)XamlReader.Load(@" <ListView
														  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
														  Height='200'
														  Width='200'>
														<ListView.ItemsPanel>
															<ItemsPanelTemplate>
																<ItemsStackPanel CacheLength='0' />
															</ItemsPanelTemplate>
														</ListView.ItemsPanel>
														<ListView.ItemContainerStyle>
															<Style TargetType='ListViewItem'>
																<Setter Property='Height' Value='50' />
															</Style>
														</ListView.ItemContainerStyle>
													</ListView>");
			listView.ItemsSource = data;
			TestServices.WindowHelper.WindowContent = listView;
			listView.ContainerContentChanging += (sender, args) =>
			{
				if (!args.InRecycleQueue && args.ItemContainer != null)
				{
					if (!containers.ContainsKey(args.ItemContainer))
					{
						containers.Add(args.ItemContainer, args.ItemIndex);
					}
				}
			};
		});

		await TestServices.WindowHelper.WaitForIdle();

		// verify the number of containers created - should be 5 since the viewport
		// is 200 pixels and each item is 50 pixels high, and we go one more before stopping
		Verify.IsTrue(containers.Values.Count == 5, "Container count is not 5");

		UIExecutor.Execute(() =>
		{
			verticalThumb = (Thumb)listView.FindNameInSubtree("VerticalThumb");
		});

		var thumbBounds = GetBounds(verticalThumb);

		TestServices.InputHelper.LeftMouseClick(verticalThumb); // Click to ensure scroll thumb activation

		TestServices.InputHelper.MouseButtonDown(verticalThumb, 0, 0, MouseButton.Left);

		await ListViewBaseTestsInput.MouseDrag(
						new Point(thumbBounds.Left + (thumbBounds.Width / 2), thumbBounds.Top + (thumbBounds.Height / 2)),
						new Point(thumbBounds.Left + (thumbBounds.Width / 2), thumbBounds.Top + (thumbBounds.Height / 2) + 200),
						MouseButton.Left);

		Log.Comment("Drag injected. Waiting for the thumb to move so we can get its new bounds.");
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.InputHelper.MouseButtonUp(verticalThumb, 0, 0, MouseButton.Left);

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var isp = listView.ItemsPanelRoot as ItemsStackPanel;
			Log.Comment("first visible index is " + isp.FirstVisibleIndex);
			Verify.IsTrue(isp.FirstVisibleIndex == 46, "did not reach end of the list by dragging thumb");
		});
	}

	[TestMethod]
	public async Task ScrollItemIntoViewOnLoaded()
	{
		ListView list = null;
		IList<string> data = Enumerable.Range(0, 500).Select(x => "Item " + x).ToList();
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.ShowsScrollingPlaceholders = false;
			list.Height = 500;
			list.ItemsSource = data;
			list.Loaded += (s, e) =>
			{
				list.ScrollIntoView(data[490]);
				listLoaded.Set();
			};

			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var container = list.ContainerFromIndex(490);
			Verify.IsNotNull(container);
		});

		await ValidateListViewScrollIntoViewDoesntCreateContainerOutsideTree();
	}

	[TestMethod]
	public async Task ScrollGroupIntoViewOnLoaded()
	{
		ListView list = null;
		var data = GetGroupedData(numGroups: 5, itemsPerGroup: 500);
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.ShowsScrollingPlaceholders = false;
			list.Height = 500;

			list.Loaded += (s, e) =>
			{
				list.ScrollIntoView(data[4]);
				listLoaded.Set();
			};

			CollectionViewSource cvs = new() { IsSourceGrouped = true };
			cvs.Source = data;
			list.ItemsSource = cvs.View;

			list.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
			   "     <GroupStyle.HeaderTemplate>                                                       " +
			   "         <DataTemplate>                                                                " +
			   "             <Grid Background='Red' >                                                  " +
			   "                 <TextBlock Text='{Binding}' Foreground='White' FontSize='30'/>    " +
			   "             </Grid>                                                                   " +
			   "         </DataTemplate>                                                               " +
			   "     </GroupStyle.HeaderTemplate>                                                      " +
			   " </GroupStyle>                                                                         "));

			list.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> " +
			   "     <TextBlock Text='{Binding}' />  " +
			   " </DataTemplate>");

			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var isp = list.ItemsPanelRoot as ItemsStackPanel;
			Verify.IsNotNull(isp);
			Verify.IsTrue(isp.FirstVisibleIndex != 0);
		});

		await ValidateListViewScrollIntoViewDoesntCreateContainerOutsideTree();
	}

	private static async Task ValidateListViewScrollIntoViewDoesntCreateContainerOutsideTree()
	{
		ListView list = null;
		UIExecutor.Execute(() =>
		{
			list = (ListView)TestServices.WindowHelper.WindowContent;
			TestServices.WindowHelper.WindowContent = null;
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var targetIndex = 1; // Should not be realized.
			Verify.IsNull(list.ContainerFromIndex(targetIndex));

			// This method validates that we don't raise CCC after a
			// ScrollIntoView call when the list is outside of the visual
			// tree.
			// The reason for this is that we can't prepare a new container
			// correctly outside of the visual tree. The template of the container
			// is simply not applied.
			int cccCounter = 0;
			list.ContainerContentChanging += (o, e) =>
			{
				Verify.IsNotNull(e.ItemContainer.ContentTemplateRoot);
				++cccCounter;
			};

			list.ScrollIntoView(list.Items[targetIndex]);
			Verify.AreEqual(0, cccCounter);
		});
	}

	private static Rect GetBounds(FrameworkElement element)
	{
		Rect rect = new();
		UIExecutor.Execute(() =>
		{
			var point1 = element.TransformToVisual(null).TransformPoint(new Point(0, 0));
			var point2 = element.TransformToVisual(null).TransformPoint(new Point((float)element.ActualWidth, (float)element.ActualHeight));

			rect.X = Math.Min(point1.X, point2.X);
			rect.Y = point1.Y;
			rect.Width = Math.Abs(point1.X - point2.X);
			rect.Height = point2.Y - point1.Y;
		});

		return rect;
	}
}
