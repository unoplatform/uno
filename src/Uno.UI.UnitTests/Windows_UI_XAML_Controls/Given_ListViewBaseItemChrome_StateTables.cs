using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.UI.Xaml.Controls.Primitives.ListViewBaseItemPresenter;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ListViewBaseItemChrome_StateTables
{
	[TestMethod]
	public void When_Name_Recognised_And_Changed()
	{
		var state = CommonStates2.Normal;

		var changed = UpdateVisualStateGroup("PointerOverSelected", ref state, out var found);

		Assert.IsTrue(changed);
		Assert.IsTrue(found);
		Assert.AreEqual(CommonStates2.PointerOverSelected, state);
	}

	[TestMethod]
	public void When_Name_Recognised_But_Unchanged_Then_Found_Without_Change()
	{
		var state = DisabledStates.Disabled;

		var changed = UpdateVisualStateGroup("Disabled", ref state, out var found);

		// The caller stops probing the remaining groups once found, even though nothing changed.
		Assert.IsFalse(changed);
		Assert.IsTrue(found);
		Assert.AreEqual(DisabledStates.Disabled, state);
	}

	[TestMethod]
	public void When_Name_Unknown_Then_Not_Found()
	{
		var state = MultiSelectStates.MultiSelectEnabled;

		var changed = UpdateVisualStateGroup("Selected", ref state, out var found);

		Assert.IsFalse(changed);
		Assert.IsFalse(found);
		Assert.AreEqual(MultiSelectStates.MultiSelectEnabled, state);
	}

	[TestMethod]
	[DataRow("normal")]
	[DataRow("NORMAL")]
	[DataRow("Normal ")]
	[DataRow("")]
	public void When_Name_Differs_By_Case_Or_Whitespace_Then_Not_Found(string name)
	{
		var state = CommonStates2.Pressed;

		var changed = UpdateVisualStateGroup(name, ref state, out var found);

		Assert.IsFalse(changed);
		Assert.IsFalse(found);
		Assert.AreEqual(CommonStates2.Pressed, state);
	}

	[TestMethod]
	public void When_Probing_Groups_In_Chrome_Order_Then_First_Group_Recognising_The_Name_Wins()
	{
		var states = new VisualStates
		{
			commonState2 = CommonStates2.Normal,
			focusState = FocusStates.Unfocused,
		};

		// Mirrors GoToChromedStateNewStyle: a later group is only probed while the name is still unknown.
		var dirty = UpdateVisualStateGroup("Normal", ref states.commonState2, out var found);
		if (!found)
		{
			dirty |= UpdateVisualStateGroup("Normal", ref states.focusState, out found);
		}

		Assert.IsTrue(found);
		Assert.IsFalse(dirty);
		Assert.IsTrue(states.HasState(CommonStates2.Normal));
		Assert.IsTrue(states.HasState(FocusStates.Unfocused));
	}

	[TestMethod]
	public void When_Tables_Then_Names_Match_WinUI_In_Order()
	{
		AssertTable(new[] { "Focused", "Unfocused", "PointerFocused" }, Mapping<FocusStates>.s_map);
		AssertTable(
			new[]
			{
				"NotDragging", "Dragging", "DraggingTarget", "MultipleDraggingPrimary", "MultipleDraggingSecondary", "DraggedPlaceholder",
				"Reordering", "ReorderingTarget", "MultipleReorderingPrimary", "ReorderedPlaceholder", "DragOver",
			},
			Mapping<DragStates>.s_map);
		AssertTable(new[] { "NoReorderHint", "BottomReorderHint", "TopReorderHint", "RightReorderHint", "LeftReorderHint" }, Mapping<ReorderHintStates>.s_map);
		AssertTable(new[] { "DataAvailable", "DataPlaceholder" }, Mapping<DataVirtualizationStates>.s_map);
		AssertTable(new[] { "Normal", "PointerOver", "Pressed", "Selected", "PointerOverSelected", "PressedSelected" }, Mapping<CommonStates2>.s_map);
		AssertTable(new[] { "Enabled", "Disabled" }, Mapping<DisabledStates>.s_map);
		AssertTable(new[] { "MultiSelectDisabled", "MultiSelectEnabled" }, Mapping<MultiSelectStates>.s_map);
		AssertTable(new[] { "SelectionIndicatorDisabled", "SelectionIndicatorEnabled" }, Mapping<SelectionIndicatorStates>.s_map);
	}

	private static void AssertTable<TEnum>(string[] expectedNames, MapItem<TEnum>[] map)
		where TEnum : struct, Enum
	{
		CollectionAssert.AreEqual(expectedNames, map.Select(item => item.m_pName).ToArray());

		// Every state name maps to the enum member of the same name, and every member is reachable.
		foreach (var item in map)
		{
			Assert.AreEqual(item.m_pName, item.m_pEnumValue.ToString());
		}

		CollectionAssert.AreEquivalent(Enum.GetValues<TEnum>(), map.Select(item => item.m_pEnumValue).ToArray());
	}
}
