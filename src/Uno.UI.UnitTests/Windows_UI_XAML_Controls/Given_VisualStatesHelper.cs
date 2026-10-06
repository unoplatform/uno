using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_VisualStatesHelper
{
	private static string[] Get(ListViewBaseItemVisualStatesCriteria criteria)
		=> VisualStatesHelper.GetValidVisualStatesListViewBaseItem(criteria).ToArray();

	private static ListViewBaseItemVisualStatesCriteria Enabled()
		=> new() { isEnabled = true };

	[TestMethod]
	public void When_Default_Then_Seven_States_In_Order()
	{
		var states = Get(Enabled());

		CollectionAssert.AreEqual(
			new[] { "Unfocused", "MultiSelectDisabled", "SelectionIndicatorDisabled", "Enabled", "Normal", "NoReorderHint", "NotDragging" },
			states);
	}

	[TestMethod]
	public void When_Disabled_Then_Seven_States_In_Order()
	{
		var c = new ListViewBaseItemVisualStatesCriteria { isEnabled = false, focusState = FocusState.Keyboard };
		CollectionAssert.AreEqual(
			new[] { "Unfocused", "MultiSelectDisabled", "SelectionIndicatorDisabled", "Disabled", "Normal", "NoReorderHint", "NotDragging" },
			Get(c));

		c.isSelected = true;
		Assert.AreEqual("Selected", Get(c)[4]);
		Assert.AreEqual(7, Get(c).Length);
	}

	[TestMethod]
	[DataRow(FocusState.Unfocused, true, "Unfocused")]
	[DataRow(FocusState.Keyboard, true, "Focused")]
	[DataRow(FocusState.Programmatic, true, "Focused")]
	[DataRow(FocusState.Pointer, true, "PointerFocused")]
	[DataRow(FocusState.Pointer, false, "Unfocused")]
	public void When_Focus_Then_Slot0(FocusState focus, bool enabled, string expected)
	{
		var c = new ListViewBaseItemVisualStatesCriteria { isEnabled = enabled, focusState = focus };
		Assert.AreEqual(expected, Get(c)[0]);
	}

	[TestMethod]
	public void When_MultiSelect_And_Indicator_Then_Slots_1_And_2()
	{
		var c = Enabled();
		c.isMultiSelect = true;
		c.isIndicatorSelect = true;
		var states = Get(c);
		Assert.AreEqual("MultiSelectEnabled", states[1]);
		Assert.AreEqual("SelectionIndicatorEnabled", states[2]);
	}

	[TestMethod]
	[DataRow(false, false, false, false, false, "Normal")]
	[DataRow(false, false, true, false, false, "Pressed")]
	[DataRow(false, false, false, true, false, "PointerOver")]
	[DataRow(false, false, true, true, false, "Pressed")]
	[DataRow(true, false, false, false, false, "Selected")]
	[DataRow(true, false, true, false, false, "PressedSelected")]
	[DataRow(true, false, false, true, false, "PointerOverSelected")]
	[DataRow(false, true, false, false, false, "PointerOver")]
	[DataRow(true, true, false, false, false, "PointerOverSelected")]
	[DataRow(false, false, false, false, true, "Pressed")]
	[DataRow(true, false, false, false, true, "PressedSelected")]
	public void When_Enabled_Then_Common_State(bool selected, bool draggedOver, bool pressed, bool pointerOver, bool retainPress, string expected)
	{
		var c = Enabled();
		c.isSelected = selected;
		c.isDraggedOver = draggedOver;
		c.isPressed = pressed;
		c.isPointerOver = pointerOver;
		if (retainPress)
		{
			c.isDragging = true;
			c.isItemDragPrimary = true;
			c.isDragVisualCaptured = false;
		}

		var states = Get(c);
		Assert.AreEqual("Enabled", states[3]);
		Assert.AreEqual(expected, states[4]);
		Assert.AreEqual("NoReorderHint", states[5]);
	}

	[TestMethod]
	[DataRow(false, false, false, false, false, 0, false, "NotDragging")]
	[DataRow(true, false, false, false, false, 0, false, "NotDragging")]
	[DataRow(true, true, false, false, false, 1, false, "DraggingTarget")]
	[DataRow(true, true, false, false, false, 1, true, "ReorderingTarget")]
	[DataRow(true, true, true, false, false, 1, false, "Dragging")]
	[DataRow(true, true, true, false, false, 1, true, "Reordering")]
	[DataRow(true, true, true, true, false, 1, false, "DraggedPlaceholder")]
	[DataRow(true, true, true, true, false, 1, true, "ReorderedPlaceholder")]
	[DataRow(true, true, true, false, false, 3, false, "MultipleDraggingPrimary")]
	[DataRow(true, true, true, false, false, 3, true, "MultipleReorderingPrimary")]
	[DataRow(true, true, false, false, false, 3, false, "DraggingTarget")]
	[DataRow(true, true, false, false, true, 3, false, "MultipleDraggingSecondary")]
	[DataRow(true, true, false, false, true, 3, true, "ReorderingTarget")]
	public void When_Dragging_Then_Last_Slot(bool dragging, bool inside, bool primary, bool captured, bool selected, int count, bool canReorder, string expected)
	{
		var c = Enabled();
		c.isDragging = dragging;
		c.isInsideListView = inside;
		c.isItemDragPrimary = primary;
		c.isDragVisualCaptured = captured;
		c.isSelected = selected;
		c.dragItemsCount = count;
		c.canReorder = canReorder;

		var states = Get(c);
		Assert.AreEqual(7, states.Length);
		Assert.AreEqual(expected, states[^1]);
	}

	[TestMethod]
	public void When_Holding_Then_Drag_State()
	{
		var c = Enabled();
		c.isHolding = true;
		c.isInsideListView = true;
		c.dragItemsCount = 1;
		Assert.AreEqual("DraggingTarget", Get(c)[^1]);
	}

	[TestMethod]
	public void When_DraggedOver_Then_DragOver()
	{
		var c = Enabled();
		c.isDragging = true;
		c.isInsideListView = true;
		c.isDraggedOver = true;
		Assert.AreEqual("DragOver", Get(c)[^1]);

		c.isSelected = true;
		Assert.AreNotEqual("DragOver", Get(c)[^1]);
	}
}
