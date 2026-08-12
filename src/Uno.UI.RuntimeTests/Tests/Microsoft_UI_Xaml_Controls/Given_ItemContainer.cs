#if HAS_UNO
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.Toolkit.DevTools.Input;
using ItemContainer = Microsoft.UI.Xaml.Controls.ItemContainer;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public class Given_ItemContainer
{
	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23892")]
	public async Task When_ItemContainer_In_ListView_ItemTemplate_Mouse_Click_Selects_Item()
	{
		var listView = CreateListViewWithItemContainerTemplate();

		var pressedReceived = false;
		var releasedReceived = false;
		var captureLostReceived = false;
		listView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) => pressedReceived = true), handledEventsToo: true);
		listView.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => releasedReceived = true), handledEventsToo: true);
		listView.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((_, _) => captureLostReceived = true), handledEventsToo: true);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		try
		{
			await UITestHelper.Load(listView);

			Assert.IsNull(listView.SelectedItem);

			var center = GetItemCenter(listView, 1);

			mouse.MoveTo(center);
			await TestServices.WindowHelper.WaitForIdle();
			mouse.Press();
			await TestServices.WindowHelper.WaitForIdle();
			mouse.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(pressedReceived, "PointerPressed should bubble up to the ListView.");
			Assert.IsTrue(releasedReceived, "PointerReleased should bubble up to the ListView.");
			// WinUI's ItemContainer doesn't capture the pointer, so a simple click must not surface a PointerCaptureLost.
			Assert.IsFalse(captureLostReceived, "PointerCaptureLost should not be raised for a simple click.");
			Assert.AreEqual("Item2", listView.SelectedItem, "Item should be selected after a mouse press+release on it.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23892")]
	public async Task When_ItemContainer_In_ListView_ItemTemplate_Touch_Tap_Selects_Item()
	{
		var listView = CreateListViewWithItemContainerTemplate();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		try
		{
			await UITestHelper.Load(listView);

			Assert.IsNull(listView.SelectedItem);

			var center = GetItemCenter(listView, 1);

			finger.Press(center);
			await TestServices.WindowHelper.WaitForIdle();
			finger.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("Item2", listView.SelectedItem, "Item should be selected after a touch press+release on it.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23892")]
	public async Task When_ItemContainer_In_ListView_ItemTemplate_Touch_Scroll_Does_Not_Select_Item()
	{
		var listView = CreateListViewWithItemContainerTemplate(itemsCount: 20);
		listView.Height = 150;

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		try
		{
			await UITestHelper.Load(listView);

			Assert.IsNull(listView.SelectedItem);

			var center = GetItemCenter(listView, 1);

			finger.Press(center);
			await TestServices.WindowHelper.WaitForIdle();
			finger.MoveBy(0, -80);
			await TestServices.WindowHelper.WaitForIdle();
			finger.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(listView.SelectedItem, "Scrolling with a finger pressed on an item should not select it.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static ListView CreateListViewWithItemContainerTemplate(int itemsCount = 3)
		=> new ListView
		{
			SelectionMode = ListViewSelectionMode.Single,
			ItemsSource = Enumerable.Range(1, itemsCount).Select(i => $"Item{i}").ToArray(),
			ItemTemplate = new DataTemplate(() =>
			{
				var textBlock = new TextBlock();
				textBlock.SetBinding(TextBlock.TextProperty, new Binding());

				return new ItemContainer
				{
					Child = textBlock,
				};
			}),
		};

	private static Point GetItemCenter(ListView listView, int index)
	{
		var item = (ListViewItem)listView.ContainerFromIndex(index);
		Assert.IsNotNull(item, $"Container for index {index} should exist.");

		var bounds = item.GetAbsoluteBoundsRect();
		return new Point(bounds.GetMidX(), bounds.GetMidY());
	}
}
#endif
