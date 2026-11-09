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

[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public partial class ListViewBaseTests
{
	[ClassInitialize]
	public static void ClassSetup()
	{
		CommonTestSetupHelper.CommonTestClassSetup();
	}

	[TestCleanup]
	public async Task TestCleanup()
	{
		UIExecutor.Execute(() => TestServices.WindowHelper.WindowContent = null);
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.WindowHelper.VerifyTestCleanup();
	}

	// TODO Uno: WinUI reads this file from the test deployment directory
	// (resources\managed\controls\ListViewBase\ListView.xaml); the content is inlined here.
	private const string ListViewXamlFile = """
		<ListView xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			<ListView.ItemTemplate>
				<DataTemplate>
					<Border Background="Red" Padding="20">
						<TextBlock Text="{Binding}" />
					</Border>
				</DataTemplate>
			</ListView.ItemTemplate>
		</ListView>
		""";

	public ListView LoadXamlFileIntoWindow(string fileName)
	{
		string text = ListViewXamlFile;
		ListView target = null;
		UIExecutor.Execute(() =>
		{
			Log.Comment("Loading xaml: ");
			target = (ListView)XamlReader.Load(text);
			Verify.IsNotNull(target, "Unable to load xaml.");

			StackPanel sp = new();
			sp.Children.Add(target);

			TestServices.WindowHelper.WindowContent = sp;
		});

		return target;
	}

	[TestMethod]
	public async Task CanChangeSelectionWithTap()
	{
		ListView list = null;
		IList<string> data = Enumerable.Range(0, 5).Select(x => "Item " + x).ToList();
		FrameworkElement itemContainerToTap = null;
		int indexOfItemToTap = 2;
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = LoadXamlFileIntoWindow(@"resources\managed\controls\ListViewBase\ListView.xaml");
			list.ItemsSource = data;
			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};

			Verify.AreEqual(-1, list.SelectedIndex);
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			itemContainerToTap = (FrameworkElement)list.ContainerFromIndex(indexOfItemToTap);
		});

		TestServices.InputHelper.Tap(itemContainerToTap);
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(indexOfItemToTap, list.SelectedIndex);
		});
	}

	[TestMethod]
	public async Task RaiseVectorChangedEvent()
	{
		UnoAutoResetEvent vectorChanged = new(false);

		UIExecutor.Execute(() =>
		{
			ObservableCollection<string> source = new(Enumerable.Range(0, 20).Select(x => "Item " + x));
			ListView list = new();
			list.ItemsSource = source;
			ItemCollection items = list.Items;

			items.VectorChanged += (sender, args) =>
			{
				Log.Comment("ItemCollection.VectorChanged event raised");

				Verify.AreEqual(items, list.Items);
				Verify.AreEqual(items, sender);
				Verify.AreEqual(source, list.ItemsSource);

				vectorChanged.Set();
			};

			Log.Comment("Removing item at index 3");
			Verify.IsTrue(source.Remove(source[3]));
		});

		Verify.IsTrue(await vectorChanged.WaitOne(TimeSpan.FromSeconds(3)), "Received vectorChanged event from items");
	}

	[TestMethod]
	public async Task CanUpdateVisualStateForPinnedContainersOnSelectionModeChange()
	{
		ListView list = null;
		UnoAutoResetEvent listLoaded = new(false);
		UIExecutor.Execute(() =>
		{
			list = (ListView)XamlReader.Load(
			"<ListView ItemContainerStyle='{ThemeResource ListViewItemExpanded}' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />");
			list.ItemsSource = Enumerable.Range(0, 500).Select(i => "Item #" + i).ToList();
			TestServices.WindowHelper.WindowContent = list;
			list.Loaded += (s, e) => listLoaded.Set();
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "List failed to raise Loaded event.");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			list.ScrollIntoView(list.Items.Last());
			list.UpdateLayout();
			list.SelectionMode = ListViewSelectionMode.Multiple;
			list.ScrollIntoView(list.Items.First());
			list.UpdateLayout();

			var pinnedContainer = (UIElement)list.ContainerFromIndex(0);
			var groups = VisualStateManager.GetVisualStateGroups((FrameworkElement)VisualTreeHelper.GetChild(pinnedContainer, 0));
			VisualStateGroup multiSelectStatesGroup = null;

			foreach (VisualStateGroup group in groups)
			{
				if (group.Name == "MultiSelectStates")
				{
					multiSelectStatesGroup = group;
					break;
				}
			}
			Verify.AreEqual("MultiSelectEnabled", multiSelectStatesGroup.CurrentState.Name);
		});
	}

	[TestMethod]
	public async Task CanScrollAndChangeSelectionDuringTheSameTick()
	{
		ListView list = null;
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView
			{
				ItemsSource = new ObservableCollection<string>(Enumerable.Range(0, 1000).Select(x => "Item " + x)),
				Height = 500
			};
			list.Loaded += (s, e) => { listLoaded.Set(); };
			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Didn't receive loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.IsTrue(list.Focus(FocusState.Keyboard));
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Log.Comment("Selecting index 200.");
			list.SelectedIndex = 200;
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Log.Comment("Scrolling to index 400 and selecting index 500.");
			list.ScrollIntoView(list.Items[400]);
			list.SelectedIndex = 500;
			Verify.AreEqual(500, list.SelectedIndex);
			Verify.AreEqual(500, ((ItemsStackPanel)list.ItemsPanelRoot).LastVisibleIndex);
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task VerifyListDoesntScrollDueToSelectionChangeOnCollectionChange()
	{
		ListView list = null;
		ObservableCollection<string> data = new(Enumerable.Range(0, 100).Select(x => "Item " + x));
		UnoAutoResetEvent listLoaded = new(false);

		// In this test, we have a list of 100 items.
		// We select and give focus to the first item.
		// Then we scroll to index 10 making it the first visible index.
		// We insert an item at index 0 and we validate we haven't scrolled
		// to the selected item at (now) index 1. We then remove the item we
		// inserted and validate again we haven't scrolled to index 0.
		// The goal is to validate that we don't bring into view the selected
		// item if the selection change was due to a collection change.

		UIExecutor.Execute(() =>
		{
			list = new ListView
			{
				ItemsSource = data,
				Height = 200
			};

			list.Loaded += (s, e) => { listLoaded.Set(); };
			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Log.Comment("Selecting and giving focus to the first item.");
			list.SelectedIndex = 0;
			((Control)list.ContainerFromIndex(0)).Focus(FocusState.Keyboard);

			Log.Comment("Scrolling to index 10.");
			list.ScrollIntoView(data[10], ScrollIntoViewAlignment.Leading);
			list.UpdateLayout();

			Log.Comment("Inserting item at index 0.");
			data.Insert(0, "Inserted item");
			list.UpdateLayout();
			Verify.AreEqual(11, ((ItemsStackPanel)list.ItemsPanelRoot).FirstVisibleIndex);

			Log.Comment("Removing inserted item at index 0.");
			data.RemoveAt(0);
			list.UpdateLayout();
			Verify.AreEqual(10, ((ItemsStackPanel)list.ItemsPanelRoot).FirstVisibleIndex);
		});
	}

	[TestMethod]
	public async Task VerifyFocusedContainerIndexIsUpdatedAfterCollectionChange()
	{
		ListView list = null;
		ObservableCollection<string> data = new(Enumerable.Range(0, 4).Select(x => "Item " + x));
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView
			{
				ItemsSource = data,
				SelectionMode = ListViewSelectionMode.None
			};

			list.Loaded += (s, e) => { listLoaded.Set(); };
			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			list.Focus(FocusState.Keyboard);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Down();
		await TestServices.KeyboardHelper.Down();

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var thirdContainer = list.ContainerFromIndex(2);
			Verify.AreEqual(thirdContainer, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));

			// Remove an item before the focused item and make sure it
			// still got focus.
			data.RemoveAt(1);
			list.UpdateLayout();
			Verify.AreEqual(thirdContainer, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
		});

		// To validate that the internal Selector's focus indices are not
		// out-of-sync with the UI, we use keyboard navigation to move the focus
		// to the next item then back, essentially no-oping.
		await TestServices.KeyboardHelper.Down();
		await TestServices.KeyboardHelper.Up();

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(list.ContainerFromIndex(1), FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));

			data.Insert(1, "New Item");
			list.UpdateLayout();
			Verify.AreEqual(list.ContainerFromIndex(2), FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));

			// Remove the focused item and make sure the item that follows it now have focus.
			data.RemoveAt(2);
			Verify.AreEqual(list.ContainerFromIndex(2), FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));

			// Remove the last item in the data who also happen to be focused and make sure
			// we update focus correctly.
			{
				data.RemoveAt(2);
				list.UpdateLayout();
				Verify.AreEqual(list.ContainerFromIndex(1), FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));

				data.RemoveAt(1);
				list.UpdateLayout();
				Verify.AreEqual(list.ContainerFromIndex(0), FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			}
		});
	}

	[TestMethod]
	public async Task VerifyReorderOfInlineItems()
	{
		ListView lv = null;
		ListViewItem dragContainer = null;

		UIExecutor.Execute(() =>
		{
			lv = (ListView)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			 @"<ListView ReorderMode='Enabled' AllowDrop='True' CanDragItems='True' CanReorderItems='True'
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' >
					<ListView.Items>
						<TextBlock>1</TextBlock>
						<TextBlock>2</TextBlock>
						<TextBlock>3</TextBlock>
						<TextBlock>4</TextBlock>
						<TextBlock>5</TextBlock>
					</ListView.Items>
			   </ListView>");

			TestServices.WindowHelper.WindowContent = lv;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			dragContainer = (ListViewItem)lv.ContainerFromIndex(0);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await PressHoldAndDragFromCenter(dragContainer, 20, 50, .1, 500);

		await TestServices.WindowHelper.WaitForIdle();
		// yay. we didn't crash.
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			int index = lv.IndexFromContainer(dragContainer);
			Verify.AreNotSame(0, index);
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task VerifyDragItemsEventsFiredUsingMouse()
	{
		ListView list = null;
		IList<string> data = Enumerable.Range(0, 5).Select(x => "Item " + x).ToList();
		FrameworkElement itemContainerToStartDrag = null;
		var dropResult = DataPackageOperation.None;
		UnoAutoResetEvent dragEnterEvent = new(false);
		UnoAutoResetEvent dropEvent = new(false);
		UnoAutoResetEvent dragItemsStartingEvent = new(false);
		UnoAutoResetEvent dragItemsCompletedEvent = new(false);
		UnoAutoResetEvent listLoaded = new(false);

		DragEventHandler dragEnterEventAction = (sender, args) =>
			{
				Log.Comment("Set AcceptedOperation in DragEnter handler.");
				args.AcceptedOperation = DataPackageOperation.Move;
				dragEnterEvent.Set();
			};

		DragEventHandler dropEventAction = (sender, args) =>
			{
				Log.Comment("Set AcceptedOperation in Drop handler.");
				args.AcceptedOperation = DataPackageOperation.Move;
				dropEvent.Set();
			};

		DragItemsStartingEventHandler dragItemsStartingEventAction =
			(sender, args) =>
			{
				Log.Comment("Verifying event args in DragItemsStarting handler.");

				Verify.IsNotNull(args.Items, "Items on DragItemsEventArgs should not be null");
				Verify.IsNotNull(args.Data, "Data on DragItemsEventArgs should not be null");

				dragItemsStartingEvent.Set();
			};

		global::Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Controls.ListViewBase, Microsoft.UI.Xaml.Controls.DragItemsCompletedEventArgs> dragItemsCompletedEventAction =
			(sender, args) =>
			{
				Log.Comment("Verifying event args in DragItemsCompleted handler.");

				Verify.IsNotNull(args.Items, "Items on DragItemsEventArgs should not be null");

				Log.Comment("DropResult: " + args.DropResult.ToString());
				dropResult = args.DropResult;

				dragItemsCompletedEvent.Set();
			};

		UIExecutor.Execute(() =>
		{
			list = LoadXamlFileIntoWindow(@"resources\managed\controls\ListViewBase\ListView.xaml");
			list.CanDragItems = true;
			list.AllowDrop = true;
			list.ItemsSource = data;
			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list.");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			itemContainerToStartDrag = (FrameworkElement)list.ContainerFromIndex(index: 1);

			list.DragEnter += dragEnterEventAction;
			list.Drop += dropEventAction;
			list.DragItemsStarting += dragItemsStartingEventAction;
			list.DragItemsCompleted += dragItemsCompletedEventAction;
		});

		// Let's force the InputHelper to initialize before doing the real test
		Log.Comment("Tapping list item.");
		TestServices.InputHelper.Tap(itemContainerToStartDrag);
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Dragging list item.");
		await ListViewBaseTestsInput.DragFromCenter(itemContainerToStartDrag, relX: 150, relY: 0, velocityFactor: 0.1);
		await TestServices.WindowHelper.WaitForIdle();
		Verify.IsTrue(await dragItemsStartingEvent.WaitOne(TimeSpan.FromSeconds(5)), "Element didn't get the DragItemsStarting event.");
		Verify.IsTrue(await dragEnterEvent.WaitOne(TimeSpan.FromSeconds(2)), "Element didn't get the DragEnter event.");
		Verify.IsTrue(await dropEvent.WaitOne(TimeSpan.FromSeconds(2)), "Element didn't get the Drop event.");
		Verify.IsTrue(await dragItemsCompletedEvent.WaitOne(TimeSpan.FromSeconds(2)), "Element didn't get the DragItemsCompleted event.");
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			Verify.IsTrue(dropResult == DataPackageOperation.Move, "Drop result is set correctly.");

			list.DragEnter -= dragEnterEventAction;
			list.Drop -= dropEventAction;
			list.DragItemsStarting -= dragItemsStartingEventAction;
			list.DragItemsCompleted -= dragItemsCompletedEventAction;
		});
	}

	private static UIElement GetInnerMostTarget(FrameworkElement target)
	{
		GeneralTransform transform = target.TransformToVisual(null);
		Rect bounds = transform.TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
		Point center = new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

		Log.Comment("Bounds: " + bounds.ToString());
		Log.Comment("Center: " + center.ToString());

#if HAS_UNO
		// TODO Uno: FindElementsInHostCoordinates yields document order, where WinUI yields the topmost element first.
		return VisualTreeHelper.FindElementsInHostCoordinates(center, target).Last();
#else
		return VisualTreeHelper.FindElementsInHostCoordinates(center, target).First();
#endif
	}

	public static async Task PressHoldAndDragFromCenter(FrameworkElement target, int relX, int relY, double velocity, uint holdTime)
	{
		Log.Comment("PressHoldAndDragFromCenter");

		if (target == null)
		{
			throw new ArgumentNullException("target", "You cannot call Drag with a null reference target. Make sure you call Commit before you call PressHoldAndDragFromCenter.");
		}

		UnoAutoResetEvent resetEvent = new(false);
		PointerEventHandler resetEventAction = delegate { resetEvent.Set(); };
		UIElement innerMostTarget = null;

		UIExecutor.Execute(() =>
		{
			innerMostTarget = GetInnerMostTarget(target);
			innerMostTarget.PointerPressed += resetEventAction;
		});

		await ListViewBaseTestsInput.PressHoldAndPanFromCenter(target, relX, relY, velocity, holdTime);

		Verify.IsTrue(await resetEvent.WaitOne(TimeSpan.FromSeconds(10)), "Drag didn't start on the element.");
		UIExecutor.Execute(() =>
		{
			innerMostTarget.PointerPressed -= resetEventAction;
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task VerifyDragItemsEventsFiredUsingTouch()
	{
		ListView list = null;
		bool gotStartingEvent = false;
		IList<string> data = Enumerable.Range(0, 5).Select(x => "Item " + x).ToList();
		FrameworkElement itemContainerToStartDrag = null;
		UnoAutoResetEvent completedEvent = new(false);
		UnoAutoResetEvent listLoaded = new(false);


		DragEventHandler dragOverEventAction =
			(sender, args) =>
			{
				Log.Comment("Set AcceptedOperation in DragOver handler");
				args.AcceptedOperation = DataPackageOperation.Move;
			};

		DragEventHandler dropEventAction =
			(sender, args) =>
			{
				Log.Comment("Set AcceptedOperation in DropEvent handler");
				args.AcceptedOperation = DataPackageOperation.Move;
			};

		DragItemsStartingEventHandler startingEventAction =
			(sender, args) =>
			{
				gotStartingEvent = true;
				Log.Comment("Verifying event args");

				if (args.Items == null)
				{
					Verify.Fail("Items on DragItemsEventArgs should not be null");
				}

				if (args.Data == null)
				{
					Verify.Fail("Data on DragItemsEventArgs should not be null");
				}
			};

		global::Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Controls.ListViewBase, Microsoft.UI.Xaml.Controls.DragItemsCompletedEventArgs> completedEventAction =
			(sender, args) =>
			{
				if (args.Items == null)
				{
					Verify.Fail("Items on DragItemsEventArgs should not be null");
				}

				Verify.IsTrue(args.DropResult == DataPackageOperation.Move, "Drop result is set correctly");

				completedEvent.Set();
			};

		int indexOfItemToStartDrag = 1;

		UIExecutor.Execute(() =>
		{
			list = LoadXamlFileIntoWindow(@"resources\managed\controls\ListViewBase\ListView.xaml");
			list.CanDragItems = true;
			list.AllowDrop = true;
			list.ItemsSource = data;
			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};
		});
		await TestServices.WindowHelper.WaitForIdle();

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			itemContainerToStartDrag = (FrameworkElement)list.ContainerFromIndex(indexOfItemToStartDrag);
			list.DragOver += dragOverEventAction;
			list.Drop += dropEventAction;
			list.DragItemsStarting += startingEventAction;
			list.DragItemsCompleted += completedEventAction;
			Log.Comment("****1. validate press hold and drag succeeds");
		});
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Calling into PressHoldAndDragFromCenter.");
		await PressHoldAndDragFromCenter(itemContainerToStartDrag, 200, 0, .1, 3500);
		Log.Comment("PressHoldAndDragFromCenter complete.");
		await TestServices.WindowHelper.WaitForIdle();

		Verify.IsTrue(await completedEvent.WaitOne(TimeSpan.FromSeconds(10)), "Element didn't get the DragItemsCompleted event.");
		TestServicesExtensions.EnsureForegroundWindow();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			if (!gotStartingEvent)
			{
				Verify.Fail("ListView did not get DragItemsStarting event");
			}
			gotStartingEvent = false;

			Log.Comment("****2. validate drag fails if hold is too short");
		});
		await TestServices.WindowHelper.WaitForIdle();

		await PressHoldAndDragFromCenter(itemContainerToStartDrag, 200, 0, .1, 50);
		await TestServices.WindowHelper.WaitForIdle();
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			if (gotStartingEvent)
			{
				Verify.Fail("ListView should not get DragItemsStarting event");
			}

			completedEvent.Reset();
			gotStartingEvent = false;

			Log.Comment("****3. validate press hold and drag succeeds after a failed attempt");
		});
		await TestServices.WindowHelper.WaitForIdle();

		await PressHoldAndDragFromCenter(itemContainerToStartDrag, 200, 0, .1, 3000);
		await TestServices.WindowHelper.WaitForIdle();

		Verify.IsTrue(await completedEvent.WaitOne(TimeSpan.FromSeconds(10)), "Element didn't get the DragItemsCompleted event.");
		TestServicesExtensions.EnsureForegroundWindow();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			if (!gotStartingEvent)
			{
				Verify.Fail("ListView did not get DragItemsStarting event");
			}
			list.DragOver -= dragOverEventAction;
			list.Drop -= dropEventAction;
			list.DragItemsStarting -= startingEventAction;
			list.DragItemsCompleted -= completedEventAction;
		});
	}

	[TestMethod]
	public async Task CanCancelDrag()
	{
		ListView list = null;
		bool dragDropEventFired = false;
		bool dragCompletedEventFired = false;
		IList<string> data = Enumerable.Range(0, 5).Select(x => "Item " + x).ToList();
		FrameworkElement itemContainerToStartDrag = null;
		UnoAutoResetEvent CompletedEvent = new(false);
		UnoAutoResetEvent listLoaded = new(false);

		DragItemsStartingEventHandler startingEventAction =
			(sender, args) =>
			{
				Log.Comment("Drag event canceled");
				args.Cancel = true;
			};

		global::Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Controls.ListViewBase, Microsoft.UI.Xaml.Controls.DragItemsCompletedEventArgs> completedEventAction = delegate { dragCompletedEventFired = true; };
		DragEventHandler dragEventAction = delegate { dragDropEventFired = true; };
		int indexOfItemToStartDrag = 1;

		UIExecutor.Execute(() =>
		{
			list = LoadXamlFileIntoWindow(@"resources\managed\controls\ListViewBase\ListView.xaml");
			list.CanDragItems = true;
			list.AllowDrop = true;
			list.ItemsSource = data;
			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			itemContainerToStartDrag = (FrameworkElement)list.ContainerFromIndex(indexOfItemToStartDrag);
			list.DragItemsStarting += startingEventAction;
			list.DragEnter += dragEventAction;
			list.DragOver += dragEventAction;
			list.DragLeave += dragEventAction;
			list.Drop += dragEventAction;
			list.DragItemsCompleted += completedEventAction;
		});

		await ListViewBaseTestsInput.DragFromCenter(itemContainerToStartDrag, 300, 0, .1);
		await TestServices.WindowHelper.WaitForIdle();
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			if (dragDropEventFired)
			{
				Verify.Fail("Drag drop events still fired after Cancel is set on DragItemsStartingEventArgs");
			}

			if (dragCompletedEventFired)
			{
				Verify.Fail("DragItemsCompleted event still fired after Cancel is set on DragItemsStartingEventArgs");
			}
			list.DragItemsStarting -= startingEventAction;
			list.DragEnter -= dragEventAction;
			list.DragOver -= dragEventAction;
			list.DragLeave -= dragEventAction;
			list.Drop -= dragEventAction;
			list.DragItemsCompleted -= completedEventAction;
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task CanReorderUsingMouse()
	{
		ListView list = null;
		FrameworkElement itemContainerToStartDrag = null;
		UnoAutoResetEvent completedEvent = new(false);
		UnoAutoResetEvent listLoaded = new(false);
		ObservableCollection<string> collection = new(Enumerable.Range(0, 5).Select(x => "Item " + x));

		global::Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Controls.ListViewBase, Microsoft.UI.Xaml.Controls.DragItemsCompletedEventArgs> completedEventAction = delegate { completedEvent.Set(); };

		UIExecutor.Execute(() =>
		{
			list = LoadXamlFileIntoWindow(@"resources\managed\controls\ListViewBase\ListView.xaml");
			list.CanReorderItems = true;
			list.AllowDrop = true;
			list.ItemsSource = collection;
			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};

			//
			// Reorder scenario one: CanDragItems == true
			//
			list.CanDragItems = true;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			itemContainerToStartDrag = (FrameworkElement)list.ContainerFromIndex(1);

			list.DragItemsCompleted += completedEventAction;
		});

		await ListViewBaseTestsInput.DragFromCenter(itemContainerToStartDrag, 0, 150, .1);
		Verify.IsTrue(await completedEvent.WaitOne(TimeSpan.FromSeconds(5)), "Element didn't get the DragItemsCompleted event.");
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			Verify.IsTrue((list.Items[0] as string).Equals("Item 0"), "Item 0 at the wrong position");
			Verify.IsTrue((list.Items[1] as string).Equals("Item 2"), "Item 2 at the wrong position");
			Verify.IsTrue((list.Items[2] as string).Equals("Item 3"), "Item 3 at the wrong position");
			Verify.IsTrue((list.Items[3] as string).Equals("Item 1"), "Item 1 at the wrong position");
			Verify.IsTrue((list.Items[4] as string).Equals("Item 4"), "Item 4 at the wrong position");
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task CanReorderUsingTouch()
	{
		ListView list = null;
		FrameworkElement itemContainerToStartDrag = null;
		UnoAutoResetEvent completedEvent = new(false);
		ObservableCollection<string> collection = new(Enumerable.Range(0, 5).Select(x => "Item " + x));
		UnoAutoResetEvent listLoaded = new(false);
		global::Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Controls.ListViewBase, Microsoft.UI.Xaml.Controls.DragItemsCompletedEventArgs> completedEventAction = delegate { completedEvent.Set(); };

		UIExecutor.Execute(() =>
		{
			list = LoadXamlFileIntoWindow(@"resources\managed\controls\ListViewBase\ListView.xaml");
			list.CanReorderItems = true;
			list.AllowDrop = true;
			list.ItemsSource = collection;
			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};

			//
			// Reorder scenario one: CanDragItems == true
			//
			list.CanDragItems = true;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			itemContainerToStartDrag = (FrameworkElement)list.ContainerFromIndex(1);

			list.DragItemsCompleted += completedEventAction;
		});

		await PressHoldAndDragFromCenter(itemContainerToStartDrag, 0, 150, .1, 3000);
		Verify.IsTrue(await completedEvent.WaitOne(TimeSpan.FromSeconds(5)), "Element didn't get the DragItemsCompleted event.");
		TestServicesExtensions.EnsureForegroundWindow();

		UIExecutor.Execute(() =>
		{
			Verify.IsTrue((list.Items[0] as string).Equals("Item 0"), "Item 0 at the wrong position");
			Verify.IsTrue((list.Items[1] as string).Equals("Item 2"), "Item 2 at the wrong position");
			Verify.IsTrue((list.Items[2] as string).Equals("Item 3"), "Item 3 at the wrong position");
			Verify.IsTrue((list.Items[3] as string).Equals("Item 1"), "Item 1 at the wrong position");
			Verify.IsTrue((list.Items[4] as string).Equals("Item 4"), "Item 4 at the wrong position");
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task ValidateListViewVisualStateOnCaptureLostTouch()
	{
		await ValidateListViewItemPointerOverVisualStateOnCaptureLost(false /* isPenInsteadOfTouch */);
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task ValidateListViewVisualStateOnCaptureLostPen()
	{
		await ValidateListViewItemPointerOverVisualStateOnCaptureLost(true /* isPenInsteadOfTouch */);
	}

	// Validate that if we lose pointer capture, we land back in normal state and dont end up
	// in PointerOver state accidentally.
	private async Task ValidateListViewItemPointerOverVisualStateOnCaptureLost(bool isPenInsteadOfTouch)
	{
		ListView list = null;
		bool isCommonVisualStateNormal = false;

		UIExecutor.Execute(() =>
		{
			// Listview with items that can be dragged but not reordered
			list = (ListView)XamlReader.Load("<ListView ItemContainerStyle='{ThemeResource ListViewItemExpanded}' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />");
			list.CanDragItems = true;
			// Just one item is enough to repro
			list.ItemsSource = Enumerable.Range(0, 1).Select(x => "Item " + x);

			TestServices.WindowHelper.WindowContent = list;
		});

		await TestServices.WindowHelper.WaitForIdle();

		FrameworkElement itemContainerToStartDrag = null;
		VisualStateGroup commonState = null;
		UnoAutoResetEvent pointerCaptureLostEvent = new(false);

		UIExecutor.Execute(() =>
		{
			itemContainerToStartDrag = (FrameworkElement)list.ContainerFromIndex(0);
			commonState = GetCommonStates(itemContainerToStartDrag as ListViewItem);
		});

		var startDraghandler = new Action<object, PointerRoutedEventArgs >((s, e) =>
		{
			pointerCaptureLostEvent.Set();
		});

		var stateChangedhandler = new Action<object, VisualStateChangedEventArgs>( (s, e) =>
		{
			Log.Comment("Target State Changed: {0}->{1}", e.OldState.Name, e.NewState.Name);
			isCommonVisualStateNormal = e.NewState.Name == "Normal";
		});

		using (new EventTester<FrameworkElement, PointerRoutedEventArgs>(itemContainerToStartDrag, "PointerCaptureLost", startDraghandler))
		using (new EventTester<VisualStateGroup, VisualStateChangedEventArgs>(commonState, "CurrentStateChanged", stateChangedhandler))
		{
			if (isPenInsteadOfTouch)
			{
				await ListViewBaseTestsInput.PenStrokeFromCenter(itemContainerToStartDrag, 0, 150, .1);
			}
			else
			{
				// Touch
				await PressHoldAndDragFromCenter(itemContainerToStartDrag, 0, 150, .1, 3000);
			}

			Verify.IsTrue(await pointerCaptureLostEvent.WaitOne(TimeSpan.FromSeconds(10)), "PointerCaptureLost event not fired");
			// Drag operation can cause the window to lose focus so set the app back in focus.
			TestServicesExtensions.EnsureForegroundWindow();

			// Verify that we reached the normal state at the end.
			Verify.IsTrue(isCommonVisualStateNormal);
		}
	}

	[TestMethod]
	[Description("Validates that we can drag an item over a ListView that hasn't run layout yet. We should not crash in those circumstances.")]
	public async Task CanDragItemOverListViewWithPendingLayoutPass()
	{
		ListView list = null;
		Microsoft.UI.Xaml.Shapes.Rectangle rect = null;
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = new ListView
			{
				CanReorderItems = true,
				CanDragItems = true,
				AllowDrop = true,
				Height = 500,
				Background = new SolidColorBrush(Colors.LightGray)
			};

			rect = new Microsoft.UI.Xaml.Shapes.Rectangle
			{
				CanDrag = true,
				Fill = new SolidColorBrush(Colors.Red),
				Width = 100,
				Height = 100
			};

			StackPanel host = new();
			host.Children.Add(rect);
			host.Children.Add(list);

			list.Loaded += (s, e) => { listLoaded.Set(); };
			list.DragEnter += (s, e) =>
			{
				e.AcceptedOperation = DataPackageOperation.Copy;

				// The crash happened during the following drag over events because
				// we haven't run layout yet to realize the data.
				Log.Comment("Setting ItemsSource.");
				list.ItemsSource = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => $"Item #{i}"));
			};

			TestServices.WindowHelper.WindowContent = host;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list.");
		await TestServices.WindowHelper.WaitForIdle();

		await ListViewBaseTestsInput.DragFromCenter(rect, relX: 0, relY: 500, velocityFactor: 0.1);
		await TestServices.WindowHelper.WaitForIdle();
	}
}
