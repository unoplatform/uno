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
	public async Task CanSetSelectedItemBeforeItemsSource()
	{
		var data = Enumerable.Range(0, 10).Select(i => $"Item #{i}").ToList();
		Grid host = null;

		Func<UIElement, bool> isCommonStateSelected = (container) =>
		{
			var groups = VisualStateManager.GetVisualStateGroups((FrameworkElement)VisualTreeHelper.GetChild(container, 0));
			return groups[0].CurrentState.Name == "Selected";
		};
		Func<ListView> createList = () =>
		{
			var list = (ListView)XamlReader.Load("<ListView ItemContainerStyle='{ThemeResource ListViewItemExpanded}' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />");
			list.ItemContainerTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection(); // Speeds up the test execution.
			return list;
		};

		Log.Comment("Validate we can set SelectedItem before ItemsSource.");
		UIExecutor.Execute(() =>
		{
			host = new Grid();
			TestServices.WindowHelper.WindowContent = host;

			var list = createList();
			list.SelectedItem = data[3];
			list.ItemsSource = data;
			host.Children.Add(list);
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var list = (ListView)host.Children[0];

			Verify.AreEqual(3, list.SelectedIndex);
			Verify.AreEqual(data[3], list.SelectedItem);

			list.ContainerFromIndex(list.SelectedIndex);
			Verify.IsTrue(isCommonStateSelected((ContentControl)list.ContainerFromItem(list.SelectedItem)));
		});
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Validate we can set SelectedItem before ItemsSource when synchronising two lists with a collection view.");
		UIExecutor.Execute(() =>
		{
			var leftList = createList();
			var rightList = createList();

			CollectionViewSource cvs = new();
			cvs.Source = data;

			leftList.SelectedItem = data[3];
			leftList.ItemsSource = cvs.View;
			rightList.ItemsSource = cvs.View;

			host.Children.Clear();
			host.Children.Add(leftList);
			host.Children.Add(rightList);
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			foreach (ListView list in host.Children)
			{
				Verify.AreEqual(3, list.SelectedIndex);
				Verify.AreEqual(data[3], list.SelectedItem);
				Verify.IsTrue(isCommonStateSelected((ContentControl)list.ContainerFromItem(list.SelectedItem)));
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Validate we can set SelectedItem before ItemsSource when in multiple selection mode.");
		UIExecutor.Execute(() =>
		{
			var list = createList();
			list.SelectedItem = data[3];
			list.SelectionMode = ListViewSelectionMode.Multiple;
			list.SelectedItems.Add(data[1]);
			list.ItemsSource = data;
			list.SelectedItems.Add(data[5]);

			host.Children.Clear();
			host.Children.Add(list);
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var list = (ListView)host.Children[0];

			Verify.AreEqual(3, list.SelectedIndex);
			Verify.AreEqual(data[3], list.SelectedItem);

			foreach (var item in list.SelectedItems)
			{
				isCommonStateSelected((UIElement)list.ContainerFromItem(item));
			}
		});
	}

	[TestMethod]
	public async Task ValidateDoNotRestorePendingSelectionIfNotFound()
	{
		ListView list = null;
		bool attemptedToRestorePendingSelection = false;

		// This test is a follow-up to CanSetSelectedItemBeforeItemsSource.
		// Here we validate that if SelectedItem is set before ItemsSource and if it's
		// not in the data, then we don't attempt to reset it at all after the ItemsSource
		// is set.
		// When we set SelectedItem to a value that's not in the data, the framework
		// automatically undo the operation. However, it may cause issues if there is a
		// two-way binding on the SelectedItem. In that case, the source of the binding
		// will be set to null and some apps don't expect that.

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.SelectedItem = "This string (pending for selection) is not going to be found in the ItemsSource.";

			// Speeds up the test execution.
			{
				list.ShowsScrollingPlaceholders = false;
				list.ItemContainerTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection();
			}

			list.RegisterPropertyChangedCallback(Selector.SelectedItemProperty, new DependencyPropertyChangedCallback((obj, property) =>
			{
				// In the repro, we hit this path because Selector::OnItemsSourceChanged is attempting to restore
				// an item that's not in the data. It's causing an issue because of the side effects
				// of two way bindings on the SelectedItem.
				attemptedToRestorePendingSelection = true;
			}));

			list.ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item #{i}").ToList();
			TestServices.WindowHelper.WindowContent = list;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.IsFalse(attemptedToRestorePendingSelection);
			Verify.IsNull(list.SelectedItem);
			Verify.AreEqual(-1, list.SelectedIndex);
		});
	}

	[TestMethod]
	public async Task VerifyPhasingUsesIDataTemplateComponent()
	{
		ListView lv = null;
		int numPhases = 5;
		int numElements = 3;
		ObservableCollection<int> data = new(Enumerable.Range(0, numElements));
		TemplateComponentLogger logger = new();

		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;

			lv.ContainerContentChanging += (s, args) =>
			{
				if (!args.InRecycleQueue && args.Phase == 0)
				{
					XamlBindingHelper.SetDataTemplateComponent(args.ItemContainer.ContentTemplateRoot, new ElementTemplateComponent(numPhases, logger));
				}
			};

			lv.ItemsSource = data;
			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			List<Tuple<int, int>> expected = new();
			for (int i = 0; i < numPhases; i++)
			{
				for (int j = 0; j < numElements; j++)
				{
					expected.Add(new Tuple<int, int>(j /* data */, i /* phase */));
					Log.Comment(i + " " + j);
				}
			}

			Verify.AreEqual(expected.Count, logger.ProcessBindingCalls.Count);
			for (int i = 0; i < expected.Count; i++)
			{
				var actualValue = logger.ProcessBindingCalls[i];
				var expectedValue = expected[i];
				Verify.AreEqual(expectedValue.Item1, actualValue.Item1);
				Verify.AreEqual(expectedValue.Item2, actualValue.Item2);
			}

			Verify.AreEqual(0, logger.RecycleCalls.Count);

			data.RemoveAt(0);
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(1, logger.RecycleCalls.Count);
			Verify.AreEqual(0, logger.RecycleCalls[0]);
		});
	}

	[TestMethod]
	public async Task VerifyVisibleIndicesAtDifferentZoomFactors()
	{
		ListView listView = null;
		ScrollViewer scroller = null;
		ItemsStackPanel panel = null;

		UIExecutor.Execute(() =>
		{
			listView = (ListView)XamlReader.Load(@"<ListView Width='400' Height='400'
													   xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
													   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
													   <ListView.ItemTemplate>
															<DataTemplate>
																<TextBlock Text='{Binding}' Height='100' />
															</DataTemplate>
													   </ListView.ItemTemplate >
												   </ListView>");
			listView.ItemsSource = Enumerable.Range(0, 1000);
			TestServices.WindowHelper.WindowContent = listView;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			scroller = listView.FindElementOfTypeInSubtree<ScrollViewer>();
			panel = (ItemsStackPanel)listView.ItemsPanelRoot;
			Verify.AreEqual(0, panel.FirstVisibleIndex);
			Verify.AreEqual(3, panel.LastVisibleIndex);

			Log.Comment("Scrolling to offset 5000 ZoomFactor 1");
			scroller.ChangeView(0, 5000, null, true);
		});

		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(50, panel.FirstVisibleIndex);
			Verify.AreEqual(53, panel.LastVisibleIndex);

			Log.Comment("Scrolling to offset 5000 ZoomFactor .5");
			scroller.ChangeView(0, 5000, 0.5f, true);
		});

		await TestServices.WindowHelper.WaitForIdle();
		// WaitForIdle is not sufficient here as a wait for
		// the visuals to get updated.
		// Adding sleep as a workaround.
		await Task.Delay(150);
		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(100, panel.FirstVisibleIndex);
			Verify.AreEqual(107, panel.LastVisibleIndex);
			Log.Comment("Scrolling to offset 5000 ZoomFactor 1.5");
			scroller.ChangeView(0, 5000, 1.5f, true);
		});

		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(33, panel.FirstVisibleIndex);
			Verify.AreEqual(35, panel.LastVisibleIndex);
		});
	}

	// Regression Test: ListView crash on RS3
	[TestMethod]
	public async Task ValidateCollectionChangeAndResetBeforeLayout()
	{
		ListView lv = null;
		var groups = GetGroupedData(numGroups: 10, itemsPerGroup: 10);

		UIExecutor.Execute(() =>
		{
			lv = new ListView();
			lv.ShowsScrollingPlaceholders = false;
			lv.Height = 500;
			CollectionViewSource cvs = new() { IsSourceGrouped = true };
			cvs.Source = groups;

			lv.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />"));

			lv.ItemsSource = cvs.View;
			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Scroll down to the end of the list
		UIExecutor.Execute(() =>
		{
			lv.ScrollIntoView(groups[9][0]);
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			// Add an item before the viewport so that we start tracking
			groups[0].Add(10);
			// Reset before layout runs. We used to crash here.
			groups.Clear();
		});

		// Yay. we didn't crash.
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(0, groups.Count);
			Verify.IsNull(lv.ContainerFromIndex(0));
		});
	}

	[TestMethod]
	[Ignore("Ignored upstream: TODO 20928844: Re-enable after investigating why software injection causes this to inconsistently fail.")]
	public async Task ValidateVisibilityChangeDuringDrag()
	{
		ListView listView = null;
		DragEventHandler dragHandler = null;
		FrameworkElement dragContainer = null;
		UnoManualResetEvent dragStarting = new(false);
		UIExecutor.Execute(() =>
		{
			listView = (ListView)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			  @"<ListView
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					CanReorderItems='True'
					CanDragItems='True'
					AllowDrop='True'>
					<ListView.ItemTemplate>
						<DataTemplate>
							<Grid>
								<TextBlock Text='{Binding}' />
							</Grid>
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>");
			dragHandler = new DragEventHandler((s, args) => { listView.Visibility = Visibility.Collapsed; });
			listView.AddHandler(UIElement.DragEnterEvent, dragHandler, true);
			listView.ItemsSource = new ObservableCollection<int>(Enumerable.Range(0, 2));
			TestServices.WindowHelper.WindowContent = listView;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(Visibility.Visible, listView.Visibility);
			dragContainer = (FrameworkElement)listView.ContainerFromIndex(0);
		});

		await PressHoldAndDragFromCenter(dragContainer, 20, 100, .1, 500);
		await TestServices.WindowHelper.WaitForIdle();
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			// Our drag handler will Collapse the ListView and drag should be cancelled.
			Verify.AreEqual(Visibility.Collapsed, listView.Visibility);
			var item = listView.ItemFromContainer(dragContainer);
			Verify.AreEqual(0, item);
			listView.RemoveHandler(UIElement.DragEnterEvent, dragHandler);
			listView.Visibility = Visibility.Visible;
			listView.DragItemsStarting += (sender, args) => { dragStarting.Set(); };
		});

		// Verify that the ListView is still kicking by trying a drag
		await TestServices.WindowHelper.WaitForIdle();
		await PressHoldAndDragFromCenter(dragContainer, 20, 0, .1, 500);
		await TestServices.WindowHelper.WaitForIdle();
		TestServicesExtensions.EnsureForegroundWindow();
		Verify.IsTrue(await dragStarting.WaitOne(1000));

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(Visibility.Visible, listView.Visibility);
			var item = listView.ItemFromContainer(dragContainer);
			Verify.AreEqual(0, item);
		});

		// Do a reorder
		await TestServices.WindowHelper.WaitForIdle();
		await PressHoldAndDragFromCenter(dragContainer, 20, 100, .1, 500);
		await TestServices.WindowHelper.WaitForIdle();
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			// The items should be reordered now.
			Verify.AreEqual(Visibility.Visible, listView.Visibility);
			var item = listView.ItemFromContainer(dragContainer);
			Verify.AreNotEqual(0, item);
		});
	}

	[TestMethod]
	public async Task ValidateFocusAfterItemDeletion()
	{
		ListView list = null;
		Button button = null;
		ObservableCollection<int> data = new(Enumerable.Range(0, 2));
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.ItemsSource = data;
			StackPanel sp = new();
			button = new Button() { Content = " Hello " };
			sp.Children.Add(button);
			sp.Children.Add(list);
			TestServices.WindowHelper.WindowContent = list;
			list.Loaded += (s, e) => listLoaded.Set();
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "List failed to raise Loaded event.");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			list.Focus(FocusState.Keyboard);
		});

		await TestServices.WindowHelper.WaitForIdle();
		// Move to second item so that ListView tracks lastfocused index.
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			button.Focus(FocusState.Keyboard);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Remove previously focused element.
		UIExecutor.Execute(() =>
		{
			data.RemoveAt(1);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Tab should now move focus to first listview item.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var item = list.ContainerFromIndex(0);
			var focused = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot) as ListViewItem;
			Verify.IsNotNull(focused);
			Verify.AreEqual("0", focused.Content.ToString());
		});
	}

	[TestMethod]
	public async Task VerifyContainerAtRootOfItemTemplate()
	{
		await VerifyContainerAtRootOfItemTemplate(hookToContainerContentChangingEvent: false);
	}

	[TestMethod]
	public async Task VerifyContainerAtRootOfItemTemplateWithCCC()
	{
		await VerifyContainerAtRootOfItemTemplate(hookToContainerContentChangingEvent: true);
	}

	// Verify that if the ItemTemplate has a valid container at the root,
	// we don't create an implicit container on top of that.
	private async Task VerifyContainerAtRootOfItemTemplate(bool hookToContainerContentChangingEvent)
	{
		ListView lv = null;
		UIExecutor.Execute(() =>
		{
			lv = (ListView)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			 @"<ListView
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Height='400'>
				  <ListView.ItemTemplate>
					<DataTemplate>
						<ListViewItem Height='120'>
							<TextBlock Text='{Binding}' />
						</ListViewItem >
					</DataTemplate >
				  </ListView.ItemTemplate>
			   </ListView>");

			if(hookToContainerContentChangingEvent)
			{
				lv.ContainerContentChanging += (sender, args) =>
				{
					// just register to the event to exercise code path
					// that used to set the content.
				};
			}

			lv.ItemsSource = Enumerable.Range(0, 1000);
			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var container0 = (ListViewItem)lv.ContainerFromIndex(0);
			Verify.IsNotNull(container0);
			Verify.AreEqual(120.0, container0.Height);
			lv.ScrollIntoView(500);
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var container500 = (ListViewItem)lv.ContainerFromIndex(500);
			Verify.IsNotNull(container500);
			Verify.AreEqual(120, (int)container500.Height);
		});
	}

	// Verify that if the ItemTemplateSelector has a valid container at the root of the template it returns,
	// we do not create an implicit container on top of that.
	[TestMethod]
	public async Task VerifyContainerAtRootWithTemplateSelector()
	{
		ListView lv = null;
		UIExecutor.Execute(() =>
		{
			lv = new ListView()
			{
				Height = 400
			};

			MyTemplateSelector selector = new()
			{
				Templates =
				{
					 (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
						<ListViewItem Height='120'>
							<Button Content='{Binding}' />
						</ListViewItem >
					</DataTemplate >"),
					(DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
						<ListViewItem  Height='120'>
							<TextBlock Text='{Binding}' />
						</ListViewItem >
					</DataTemplate>")
				},
				SelectTemplateFunc = (object item) => { return ((int)item) % 2; }
			};

			lv.ItemTemplateSelector = selector;

			lv.ItemsSource = Enumerable.Range(0, 1000);
			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var container0 = (ListViewItem)lv.ContainerFromIndex(0);
			Verify.IsNotNull(container0);
			var button = container0.Content as Button;
			Verify.IsNotNull(button);
			Verify.AreEqual(0, (int)button.DataContext);

			var container1 = (ListViewItem)lv.ContainerFromIndex(1);
			Verify.IsNotNull(container1);
			var tb = container1.Content as TextBlock;
			Verify.IsNotNull(tb);
			Verify.AreEqual("1", tb.Text.ToString());

			lv.ScrollIntoView(500);
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var container500 = (ListViewItem)lv.ContainerFromIndex(500);
			Verify.IsNotNull(container500);
			var button = container500.Content as Button;
			Verify.IsNotNull(container500);
			Verify.AreEqual(500, (int)button.DataContext);
		});
	}

	[TestMethod]
	public async Task VerifySettingSelectedIndexBeforeItemsInMarkup()
	{
		ListView list = null;
		UnoManualResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = (ListView)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			 @"<ListView xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					SelectedIndex='2'>
					<ListView.Items>
						<TextBlock>1</TextBlock>
						<TextBlock>2</TextBlock>
						<TextBlock>3</TextBlock>
						<TextBlock>4</TextBlock>
						<TextBlock>5</TextBlock>
					</ListView.Items>
			   </ListView>");
			list.Loaded += (sender, args) =>
			{
				listLoaded.Set();
			};

			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(2, list.SelectedIndex);
		});
	}

	[TestMethod]
	public async Task VerifySettingSelectedIndexBeforeItemsInCode()
	{
		ListView list = null;
		UnoManualResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.Loaded += (sender, args) =>
			{
				listLoaded.Set();
			};

			list.SelectedIndex = 1;
			list.Items.Add(0);
			list.Items.Add(1);
			list.Items.Add(2);
			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(1, list.SelectedIndex);
		});
	}

	[TestMethod]
	public async Task VerifySettingSelectedIndexBeforeItemsSource()
	{
		ListView list = null;
		UnoManualResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.Loaded += (sender, args) =>
			{
				listLoaded.Set();
			};

			list.SelectedIndex = 1;
			list.ItemsSource = Enumerable.Range(0, 5);
			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(1, list.SelectedIndex);
		});
	}

	// Regression test: [CRASH] Crash on updating list view with empty headers
	[TestMethod]
	public async Task ValidateAddingGroupWithItemInMiddle()
	{
		ListView lv = null;
		CollectionViewSource cvs = null;
		var groups = GetGroupedData(numGroups: 3, itemsPerGroup: 0);

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

			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			lv.ItemsSource = cvs.View;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			groups.RemoveAt(1);
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			ObservableCollection<int> newGroup = new();
			newGroup.Add(500);
			groups.Insert(1, newGroup);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateTabNavAfterDeletingFocusedElement()
	{
		GridView gridView = null;
		Button beforeButton = null;
		Button afterButton = null;
		UIExecutor.Execute(() =>
		{
			var root = (StackPanel)XamlReader.Load(@" <StackPanel  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
																 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
														<Button Content='BeforeButton' x:Name='beforeButton' />
														<GridView x:Name='gridView' IsTabStop='False' SelectionMode='None'>
															<GridView.Items>
																<x:String>Item</x:String>
															</GridView.Items>
															<GridView.ItemTemplate>
																<DataTemplate>
																	<Button Content='{Binding}' />
																</DataTemplate>
															</GridView.ItemTemplate>
															<GridView.ItemContainerStyle>
																<Style TargetType='GridViewItem'>
																	<Setter Property='IsTabStop' Value='False'/>
																</Style>
															</GridView.ItemContainerStyle>
															<GridView.ItemContainerTransitions>
																<TransitionCollection />
															</GridView.ItemContainerTransitions>
														</GridView>
														<Button Content='AfterButton' x:Name='afterButton' />
													</StackPanel>");
			TestServices.WindowHelper.WindowContent = root;
			gridView = (GridView)root.FindName("gridView");
			beforeButton = (Button)root.FindName("beforeButton");
			afterButton = (Button)root.FindName("afterButton");
		});

		await TestServices.WindowHelper.WaitForIdle();

		// set focus on the first item
		UIExecutor.Execute(() =>
		{
			gridView.Focus(FocusState.Keyboard);
		});

		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() =>
		{
			var focusedItem  =  (Button)FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.AreEqual("Item", focusedItem.Content.ToString());

			// Delete the focused Item
			gridView.Items.RemoveAt(0);
		});

		// set focus on the before button and add a new item
		UIExecutor.Execute(() =>
		{
			beforeButton.Focus(FocusState.Keyboard);
			gridView.Items.Add("Item1");
		});

		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.KeyboardHelper.Tab();

		// Validate that tabbing from before button, goes into the gridview and sets focus on the new first item
		UIExecutor.Execute(() =>
		{
			var focusedItem  =  (Button)FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.AreEqual("Item1", focusedItem.Content.ToString());
		});
	}
}
