using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_FocusManagerDirection_UITest
{
	// 3x3 grid of TextBoxes, mirroring FocusManager_FocusDirection sample layout.
	// Indices into the returned array:
	// 0 1 2
	// 3 4 5
	// 6 7 8
	private static async Task<TextBox[]> SetupGrid()
	{
		var boxes = new TextBox[9];
		var root = new StackPanel();

		for (var row = 0; row < 3; row++)
		{
			var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
			for (var col = 0; col < 3; col++)
			{
				var index = (row * 3) + col;
				var box = new TextBox { Width = 75, Text = (index + 1).ToString() };
				boxes[index] = box;
				rowPanel.Children.Add(box);
			}

			root.Children.Add(rowPanel);
		}

		await UITestHelper.Load(root);

		return boxes;
	}

	private static bool TryMoveFocus(FocusNavigationDirection direction) =>
		FocusManager.TryMoveFocus(direction, new FindNextElementOptions { SearchRoot = WindowHelper.XamlRoot.Content });

	private static void AssertFocused(TextBox expected) =>
		Assert.AreSame(expected, FocusManager.GetFocusedElement(WindowHelper.XamlRoot) as TextBox);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FocusDirection_Next()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var boxes = await SetupGrid();
		boxes[0].Focus(FocusState.Programmatic);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Next));
		AssertFocused(boxes[1]);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Next));
		AssertFocused(boxes[2]);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FocusDirection_Previous()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var boxes = await SetupGrid();
		boxes[5].Focus(FocusState.Programmatic);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Previous));
		AssertFocused(boxes[4]);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Previous));
		AssertFocused(boxes[3]);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FocusDirection_Up()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var boxes = await SetupGrid();
		boxes[7].Focus(FocusState.Programmatic);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Up));
		AssertFocused(boxes[4]);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Up));
		AssertFocused(boxes[1]);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FocusDirection_Down()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var boxes = await SetupGrid();
		boxes[1].Focus(FocusState.Programmatic);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Down));
		AssertFocused(boxes[4]);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Down));
		AssertFocused(boxes[7]);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FocusDirection_Left()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var boxes = await SetupGrid();
		boxes[5].Focus(FocusState.Programmatic);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Left));
		AssertFocused(boxes[4]);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Left));
		AssertFocused(boxes[3]);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FocusDirection_Right()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var boxes = await SetupGrid();
		boxes[0].Focus(FocusState.Programmatic);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Right));
		AssertFocused(boxes[1]);

		Assert.IsTrue(TryMoveFocus(FocusNavigationDirection.Right));
		AssertFocused(boxes[2]);
	}
}
