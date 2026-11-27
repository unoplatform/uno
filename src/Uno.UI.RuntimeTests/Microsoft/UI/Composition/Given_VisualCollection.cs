using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;
using Color = Windows.UI.Color;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_VisualCollection
{
	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25281")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26745")]
	public async Task When_InsertAbove_Then_Directly_Above_Sibling(int siblingIndex)
	{
		var (container, children) = await CreateContainer(3);
		var newChild = container.Compositor.CreateSpriteVisual();

		container.Children.InsertAbove(newChild, children[siblingIndex]);

		var expected = children.ToList();
		expected.Insert(siblingIndex + 1, newChild);
		AssertOrder(expected, container);
		Assert.AreSame(container, newChild.Parent);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25281")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26745")]
	public async Task When_InsertBelow_Then_Directly_Below_Sibling(int siblingIndex)
	{
		var (container, children) = await CreateContainer(3);
		var newChild = container.Compositor.CreateSpriteVisual();

		container.Children.InsertBelow(newChild, children[siblingIndex]);

		var expected = children.ToList();
		expected.Insert(siblingIndex, newChild);
		AssertOrder(expected, container);
		Assert.AreSame(container, newChild.Parent);
	}

	[TestMethod]
	public async Task When_InsertAbove_Then_Drawn_Over_Sibling()
	{
		var (host, container, red, blue) = await CreateOverlappingSprites();

		container.Children.InsertAtTop(red);
		container.Children.InsertAbove(blue, red);

		await AssertOverlapColor(host, Colors.Blue);
	}

	[TestMethod]
	public async Task When_InsertBelow_Then_Drawn_Under_Sibling()
	{
		var (host, container, red, blue) = await CreateOverlappingSprites();

		container.Children.InsertAtTop(red);
		container.Children.InsertBelow(blue, red);

		await AssertOverlapColor(host, Colors.Red);
	}

	[TestMethod]
	[DataRow(true, false)]
	[DataRow(false, false)]
	[DataRow(true, true)]
	[DataRow(false, true)]
	public async Task When_Sibling_Not_In_Collection_Then_Throws(bool above, bool siblingInOtherContainer)
	{
		var (container, children) = await CreateContainer(2);
		var sibling = container.Compositor.CreateSpriteVisual();
		if (siblingInOtherContainer)
		{
			container.Compositor.CreateContainerVisual().Children.InsertAtTop(sibling);
		}
		var newChild = container.Compositor.CreateSpriteVisual();

		Assert.ThrowsExactly<ArgumentException>(() =>
		{
			if (above)
			{
				container.Children.InsertAbove(newChild, sibling);
			}
			else
			{
				container.Children.InsertBelow(newChild, sibling);
			}
		});

		AssertOrder(children, container);
		Assert.IsNull(newChild.Parent);
	}

	// An indexed insert into a panel puts the new element below the element that was at that index.
	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	public async Task When_Panel_Children_Insert_Then_ZOrder_Follows_Index(int insertIndex)
	{
		var red = CreateStrip(Colors.Red, 0);
		var blue = CreateStrip(Colors.Blue, 40);
		var green = CreateStrip(Colors.Green, 20);
		var grid = new Grid
		{
			Width = 100,
			Height = 20,
			Background = new SolidColorBrush(Colors.White),
		};
		grid.Children.Add(red);
		grid.Children.Add(blue);
		await UITestHelper.Load(grid);

		grid.Children.Insert(insertIndex, green);
		await UITestHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(grid);

		// red covers 0-40, green 20-60, blue 40-80.
		ImageAssert.HasColorAt(screenshot, 30, 10, insertIndex == 0 ? Colors.Red : Colors.Green, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 50, 10, Colors.Blue, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 70, 10, Colors.Blue, tolerance: 10);
	}

	// The item chrome inserts its backplate at index 0, under the content that is already there.
	[TestMethod]
	public async Task When_ListViewItem_Selected_Then_Content_Drawn_Over_Backplate()
	{
		var content = new Border
		{
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Colors.Red),
		};
		var item = new ListViewItem { Content = content };
		item.Resources["ListViewItemBackgroundSelected"] = new SolidColorBrush(Colors.Blue);
		var list = new ListView
		{
			Width = 200,
			Height = 100,
			Background = new SolidColorBrush(Colors.White),
			SelectionMode = ListViewSelectionMode.Single,
			// No entrance transition: WinUI would capture its first, faded-out frame.
			ItemContainerTransitions = new TransitionCollection(),
		};
		list.Items.Add(item);
		await UITestHelper.Load(list);

		list.SelectedIndex = 0;
		await UITestHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(list);
		var contentCenter = content.TransformToVisual(list).TransformPoint(new Windows.Foundation.Point(10, 10));

		// Left of the content, inside the item: the selected backplate.
		ImageAssert.HasColorAt(screenshot, (float)(contentCenter.X - 12), (float)contentCenter.Y, Colors.Blue, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, (float)contentCenter.X, (float)contentCenter.Y, Colors.Red, tolerance: 10);
	}

	private static Border CreateStrip(Color color, double left) => new()
	{
		Width = 40,
		Height = 20,
		HorizontalAlignment = HorizontalAlignment.Left,
		Margin = new Thickness(left, 0, 0, 0),
		Background = new SolidColorBrush(color),
	};

	private static async Task<(ContainerVisual Container, Visual[] Children)> CreateContainer(int count)
	{
		var anchor = new Border { Width = 1, Height = 1 };
		await UITestHelper.Load(anchor);
		var compositor = ElementCompositionPreview.GetElementVisual(anchor).Compositor;

		var container = compositor.CreateContainerVisual();
		var children = new Visual[count];
		for (var i = 0; i < count; i++)
		{
			children[i] = compositor.CreateSpriteVisual();
			container.Children.InsertAtTop(children[i]);
		}

		return (container, children);
	}

	private static void AssertOrder(IReadOnlyList<Visual> expected, ContainerVisual container)
	{
		var actual = container.Children.ToList();
		Assert.AreEqual(expected.Count, container.Children.Count);
		CollectionAssert.AreEqual(
			expected.ToList(),
			actual,
			$"Expected [{string.Join(", ", expected.Select(v => actual.IndexOf(v)))}] in bottom-to-top order.");
	}

	private static async Task<(Border Host, ContainerVisual Container, SpriteVisual Red, SpriteVisual Blue)> CreateOverlappingSprites()
	{
		var host = new Border
		{
			Width = 100,
			Height = 40,
			Background = new SolidColorBrush(Colors.White),
		};
		await UITestHelper.Load(host);
		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;

		var container = compositor.CreateContainerVisual();
		container.Size = new Vector2(100, 40);
		ElementCompositionPreview.SetElementChildVisual(host, container);

		var red = compositor.CreateSpriteVisual();
		red.Brush = compositor.CreateColorBrush(Colors.Red);
		red.Size = new Vector2(60, 40);

		var blue = compositor.CreateSpriteVisual();
		blue.Brush = compositor.CreateColorBrush(Colors.Blue);
		blue.Size = new Vector2(60, 40);
		blue.Offset = new Vector3(40, 0, 0);

		return (host, container, red, blue);
	}

	private static async Task AssertOverlapColor(Border host, Color expected)
	{
		await UITestHelper.WaitForIdle();
		var screenshot = await UITestHelper.ScreenShot(host);

		// red covers 0-60, blue 40-100.
		ImageAssert.HasColorAt(screenshot, 20, 20, Colors.Red, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 50, 20, expected, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 80, 20, Colors.Blue, tolerance: 10);
	}
}
