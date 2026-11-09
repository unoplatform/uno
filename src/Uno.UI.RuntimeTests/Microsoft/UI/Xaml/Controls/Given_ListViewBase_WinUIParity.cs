#if HAS_UNO
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.UI.DevTools.Input;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Behaviours read directly off the WinUI C++ sources (ListViewBase_Partial*.cpp, ItemsPresenter_Partial.cpp).
[TestClass]
[RunsOnUIThread]
public class Given_ListViewBase_WinUIParity
{
	[TestMethod]
	[DataRow(ListViewSelectionMode.None, false)]
	[DataRow(ListViewSelectionMode.Single, false)]
	[DataRow(ListViewSelectionMode.Multiple, true)]
	[DataRow(ListViewSelectionMode.Extended, true)]
	public void When_SelectionMode_Then_Peer_CanSelectMultiple(ListViewSelectionMode mode, bool expected)
	{
		var list = new ListView { SelectionMode = mode };

		var peer = (ISelectionProvider)FrameworkElementAutomationPeer.CreatePeerForElement(list);

		Assert.AreEqual(expected, peer.CanSelectMultiple);
	}

	[TestMethod]
	public async Task When_App_Prepares_Container_Then_Draggable()
	{
		var list = new ListView { CanReorderItems = true, AllowDrop = true, ItemsSource = new[] { "A", "B" } };
		list.ChoosingItemContainer += (s, e) =>
		{
			e.ItemContainer = new ListViewItem();
			e.IsContainerPrepared = true;
		};

		await UITestHelper.Load(list, _ => list.ContainerFromIndex(1) is not null);

		Assert.IsTrue(((UIElement)list.ContainerFromIndex(0)).CanDrag);
	}

	[TestMethod]
	public async Task When_TryStartConnectedAnimation_Then_Not_Started()
	{
		var list = new ListView { ItemsSource = new[] { "A" } };

		await UITestHelper.Load(list);

		Assert.IsFalse(await list.TryStartConnectedAnimationAsync(null, "A", "Element"));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Tab_Into_Unrealized_Last_Focused_Item(bool backward)
	{
		var before = new Button { Content = "Before" };
		var after = new Button { Content = "After" };
		var list = new ListView
		{
			Height = 200,
			ItemsSource = Enumerable.Range(0, 200).Select(x => $"Item {x}").ToArray(),
		};
		var root = new StackPanel { Children = { before, list, after } };

		await UITestHelper.Load(root, _ => list.ContainerFromIndex(0) is not null);

		const int focusIndex = 2;
		((ListViewItem)list.ContainerFromIndex(focusIndex)).Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		(backward ? after : before).Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		var scrollViewer = list.FindFirstDescendant<ScrollViewer>();
		scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
		await WindowHelper.WaitForIdle();
		await UITestHelper.WaitFor(() => list.ContainerFromIndex(focusIndex) is null, message: "precondition: the last focused item must be unrealized");

		if (backward)
		{
			await KeyboardHelper.ShiftTab();
		}
		else
		{
			await KeyboardHelper.Tab();
		}
		await WindowHelper.WaitForIdle();

		var focused = FocusManager.GetFocusedElement(root.XamlRoot) as ListViewItem;
		Assert.IsNotNull(focused, $"Focus went to {FocusManager.GetFocusedElement(root.XamlRoot)}");
		Assert.AreEqual(focusIndex, list.IndexFromContainer(focused));
		Assert.IsLessThan(list.ActualHeight, focused.TransformToVisual(list).TransformPoint(default).Y, "The focused item must have been brought into view.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Reorder_Plain_List_Then_Source_Moved()
	{
		// ListViewBase::ReorderItemsTo takes any IBindableVector. A List<T> raises no change, so containers stay.
		var source = Enumerable.Range(0, 8).Select(x => $"Item {x}").ToList();
		var list = new ListView
		{
			Height = 400,
			AllowDrop = true,
			CanDragItems = true,
			CanReorderItems = true,
			ItemsSource = source,
		};

		await UITestHelper.Load(list, x => x.IsLoaded && list.ContainerFromIndex(7) is not null);
		await UITestHelper.WaitForIdle();

		var dragged = source[2];
		var containers = Enumerable.Range(0, 8).Select(list.ContainerFromIndex).ToArray();
		var from = ((ListViewItem)containers[2]).GetAbsoluteBoundsRect();
		var to = ((ListViewItem)containers[5]).GetAbsoluteBoundsRect();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		mouse.MoveTo(from.GetCenter());
		await WindowHelper.WaitForIdle();
		mouse.Press();
		await WindowHelper.WaitForIdle();
		mouse.MoveTo(to.GetCenter(), 16);
		await WindowHelper.WaitForIdle();
		await UITestHelper.WaitForRender();
		mouse.MoveTo(to.GetCenter(), 2);
		await WindowHelper.WaitForIdle();
		mouse.Release();
		await WindowHelper.WaitForIdle();
		await UITestHelper.WaitForIdle();

		Assert.AreNotEqual(2, source.IndexOf(dragged), "The drop must move the item in the source.");
		Assert.HasCount(8, source);
		CollectionAssert.AreEqual(containers, Enumerable.Range(0, 8).Select(list.ContainerFromIndex).ToArray(), "Containers must not change without a change notification.");
	}

	[TestMethod]
	public async Task When_Footer_Far_From_Viewport_Then_Loaded_On_Approach()
	{
		var footer = new Border { Height = 100, Width = 100 };
		var list = new ListView
		{
			Height = 200,
			Footer = footer,
			ItemsSource = Enumerable.Range(0, 200).Select(x => $"Item {x}").ToArray(),
		};

		await UITestHelper.Load(list, _ => list.ContainerFromIndex(0) is not null);
		await WindowHelper.WaitForIdle();

		var presenter = list.FindFirstDescendant<ItemsPresenter>();
		Assert.IsNull(presenter.FooterContentControl.Content, "The footer must wait for the viewport.");

		var scrollViewer = list.FindFirstDescendant<ScrollViewer>();
		scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
		await WindowHelper.WaitForIdle();

		Assert.AreSame(footer, presenter.FooterContentControl.Content);
		Assert.IsTrue(footer.IsLoaded, "The footer must have entered the tree.");
	}

	[TestMethod]
	[DataRow(2)]
	[DataRow(0)]
	[DataRow(11)] // Below the viewport, but within the 200 DIP offset.
	public async Task When_Footer_Within_Delay_Offset_Then_Loaded_Immediately(int itemCount)
	{
		var footer = new Border { Height = 100, Width = 100 };
		var list = new ListView
		{
			Height = 400,
			Footer = footer,
			ItemsSource = Enumerable.Range(0, itemCount).Select(x => $"Item {x}").ToArray(),
		};

		await UITestHelper.Load(list);
		await WindowHelper.WaitForIdle();

		Assert.AreSame(footer, list.FindFirstDescendant<ItemsPresenter>().FooterContentControl.Content);
	}
}
#endif
