using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public partial class Given_UIElement_GamepadContextMenu
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_GamepadMenu_Position_Is_Element_Center()
	{
		Point? receivedPosition = null;

		var button = new Button
		{
			Content = "Test Button",
			Width = 200,
			Height = 100
		};

		// Add handler to capture the position
		button.ContextRequested += (s, e) =>
		{
			if (e.TryGetPosition(button, out var pos))
			{
				receivedPosition = pos;
			}
			e.Handled = true;
		};

		await UITestHelper.Load(button);

		// Get ContentRoot's ContextMenuProcessor
		var contentRoot = VisualTree.GetContentRootForElement(button);
		Assert.IsNotNull(contentRoot, "ContentRoot should not be null");

		var processor = contentRoot.ContextMenuProcessor;
		Assert.IsNotNull(processor, "ContextMenuProcessor should not be null");

		// Trigger context menu via GamepadMenu key
		processor.ProcessContextRequestOnKeyboardInput(
			button,
			VirtualKey.GamepadMenu,
			VirtualKeyModifiers.None);

		Assert.IsNotNull(receivedPosition, "Position should be provided for GamepadMenu");

		// Position should be at element center
		var expectedX = button.ActualWidth / 2;
		var expectedY = button.ActualHeight / 2;

		Assert.AreEqual(expectedX, receivedPosition.Value.X, 1.0,
			$"X position should be at center ({expectedX}), but was {receivedPosition.Value.X}");
		Assert.AreEqual(expectedY, receivedPosition.Value.Y, 1.0,
			$"Y position should be at center ({expectedY}), but was {receivedPosition.Value.Y}");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ShiftF10_Position_Is_Keyboard_Default()
	{
		Point? receivedPosition = null;
		bool tryGetPositionSucceeded = false;

		var button = new Button
		{
			Content = "Test Button",
			Width = 200,
			Height = 100
		};

		// Add handler to capture the position
		button.ContextRequested += (s, e) =>
		{
			tryGetPositionSucceeded = e.TryGetPosition(button, out var pos);
			if (tryGetPositionSucceeded)
			{
				receivedPosition = pos;
			}
			e.Handled = true;
		};

		await UITestHelper.Load(button);

		var contentRoot = VisualTree.GetContentRootForElement(button);
		var processor = contentRoot.ContextMenuProcessor;

		// Trigger context menu via Shift+F10 (keyboard default)
		processor.ProcessContextRequestOnKeyboardInput(
			button,
			VirtualKey.F10,
			VirtualKeyModifiers.Shift);

		// For Shift+F10, TryGetPosition should return false (keyboard invocation)
		Assert.IsFalse(tryGetPositionSucceeded,
			"TryGetPosition should return false for Shift+F10 keyboard invocation");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ApplicationKey_Position_Is_Keyboard_Default()
	{
		Point? receivedPosition = null;
		bool tryGetPositionSucceeded = false;

		var button = new Button
		{
			Content = "Test Button",
			Width = 200,
			Height = 100
		};

		// Add handler to capture the position
		button.ContextRequested += (s, e) =>
		{
			tryGetPositionSucceeded = e.TryGetPosition(button, out var pos);
			if (tryGetPositionSucceeded)
			{
				receivedPosition = pos;
			}
			e.Handled = true;
		};

		await UITestHelper.Load(button);

		var contentRoot = VisualTree.GetContentRootForElement(button);
		var processor = contentRoot.ContextMenuProcessor;

		// Trigger context menu via Application key (keyboard default)
		processor.ProcessContextRequestOnKeyboardInput(
			button,
			VirtualKey.Application,
			VirtualKeyModifiers.None);

		// For Application key, TryGetPosition should return false (keyboard invocation)
		Assert.IsFalse(tryGetPositionSucceeded,
			"TryGetPosition should return false for Application key keyboard invocation");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_GamepadMenu_On_Zero_Size_Element_Falls_Back()
	{
		Point? receivedPosition = null;
		bool tryGetPositionSucceeded = false;

		// Element with zero size (not yet measured)
		var button = new Button
		{
			Content = "Test",
			Width = 0,
			Height = 0
		};

		button.ContextRequested += (s, e) =>
		{
			tryGetPositionSucceeded = e.TryGetPosition(button, out var pos);
			if (tryGetPositionSucceeded)
			{
				receivedPosition = pos;
			}
			e.Handled = true;
		};

		await UITestHelper.Load(button);

		var contentRoot = VisualTree.GetContentRootForElement(button);
		var processor = contentRoot.ContextMenuProcessor;

		// Trigger context menu via GamepadMenu key on zero-size element
		processor.ProcessContextRequestOnKeyboardInput(
			button,
			VirtualKey.GamepadMenu,
			VirtualKeyModifiers.None);

		// For zero-size element, should fall back to keyboard default behavior
		Assert.IsFalse(tryGetPositionSucceeded,
			"TryGetPosition should return false for zero-size element");
	}
}
