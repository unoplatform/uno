using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public partial class Given_TextBox_ContextMenuPosition
{
#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_GetContextMenuShowPosition_Returns_Valid_Position()
	{
		var textBox = new TextBox
		{
			Text = "Hello World",
			Width = 200,
			Height = 50
		};

		await UITestHelper.Load(textBox);

		// Focus the TextBox
		textBox.Focus(FocusState.Programmatic);
		await Task.Delay(100);

		// Get context menu position
		var position = textBox.GetContextMenuShowPosition();

		Assert.IsNotNull(position, "GetContextMenuShowPosition should return a valid position");

		// Position should be within reasonable bounds of the TextBox
		Assert.IsTrue(position.Value.X >= 0, "X position should be non-negative");
		Assert.IsTrue(position.Value.Y >= 0, "Y position should be non-negative");
		Assert.IsTrue(position.Value.X <= textBox.ActualWidth + 10, "X position should be within TextBox width");
		Assert.IsTrue(position.Value.Y <= textBox.ActualHeight + 10, "Y position should be within TextBox height");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SelectionChanges_Position_Updates()
	{
		var textBox = new TextBox
		{
			Text = "Hello World Test String",
			Width = 300,
			Height = 50
		};

		await UITestHelper.Load(textBox);

		// Focus the TextBox
		textBox.Focus(FocusState.Programmatic);
		await Task.Delay(100);

		// Set caret at beginning
		textBox.Select(0, 0);
		await Task.Delay(50);

		var positionAtStart = textBox.GetContextMenuShowPosition();
		Assert.IsNotNull(positionAtStart, "Position should be valid at start");

		// Set caret at end
		textBox.Select(textBox.Text.Length, 0);
		await Task.Delay(50);

		var positionAtEnd = textBox.GetContextMenuShowPosition();
		Assert.IsNotNull(positionAtEnd, "Position should be valid at end");

		// Position at end should be to the right of position at start
		Assert.IsTrue(positionAtEnd.Value.X > positionAtStart.Value.X,
			"Position at end of text should be to the right of position at start");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Selection_Position_At_Selection_End()
	{
		var textBox = new TextBox
		{
			Text = "Hello World Test String",
			Width = 300,
			Height = 50
		};

		await UITestHelper.Load(textBox);

		// Focus the TextBox
		textBox.Focus(FocusState.Programmatic);
		await Task.Delay(100);

		// Select "Hello" (first 5 characters)
		textBox.Select(0, 5);
		await Task.Delay(50);

		var positionWithSelection = textBox.GetContextMenuShowPosition();
		Assert.IsNotNull(positionWithSelection, "Position should be valid with selection");

		// Get position at end of selection (character index 5)
		textBox.Select(5, 0);
		await Task.Delay(50);
		var positionAtSelectionEnd = textBox.GetContextMenuShowPosition();

		// The position with selection should be at or near the selection end
		// Allow some tolerance for rendering differences
		Assert.IsTrue(Math.Abs(positionWithSelection.Value.X - positionAtSelectionEnd.Value.X) < 5,
			"Position should be at selection end");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Empty_TextBox_Position_Valid()
	{
		var textBox = new TextBox
		{
			Text = "",
			Width = 200,
			Height = 50
		};

		await UITestHelper.Load(textBox);

		// Focus the TextBox
		textBox.Focus(FocusState.Programmatic);
		await Task.Delay(100);

		var position = textBox.GetContextMenuShowPosition();

		// Should still return a valid position for empty TextBox
		Assert.IsNotNull(position, "GetContextMenuShowPosition should return valid position for empty TextBox");
		Assert.IsTrue(position.Value.X >= 0, "X position should be non-negative");
		Assert.IsTrue(position.Value.Y >= 0, "Y position should be non-negative");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Has_ContextFlyout_Keyboard_Trigger_Shows_Flyout()
	{
		var flyoutOpened = false;
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Test Item" });
		flyout.Opened += (s, e) => flyoutOpened = true;

		var textBox = new TextBox
		{
			Text = "Hello World",
			Width = 200,
			Height = 50,
			ContextFlyout = flyout
		};

		await UITestHelper.Load(textBox);

		// Focus the TextBox
		textBox.Focus(FocusState.Programmatic);
		await Task.Delay(100);

		// Simulate keyboard context menu trigger by raising ContextRequested without position
		var args = new ContextRequestedEventArgs();
		args.SetGlobalPoint(new Point(-1, -1)); // -1, -1 indicates keyboard invocation
		textBox.RaiseEvent(UIElement.ContextRequestedEvent, args);

		await Task.Delay(100);

		Assert.IsTrue(args.Handled, "ContextRequested should be handled");
		Assert.IsTrue(flyoutOpened, "ContextFlyout should open on keyboard trigger");

		flyout.Hide();
	}
#endif
}
