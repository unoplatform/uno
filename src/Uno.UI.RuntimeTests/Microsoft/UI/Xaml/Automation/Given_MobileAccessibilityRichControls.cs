#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

// platform-neutral rich accessibility semantics tests — mobile Skia only
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileAccessibilityRichControls
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_On_Mobile_Then_Range_Details_Match_Peer()
	{
		var slider = new Slider
		{
			Minimum = 10,
			Maximum = 90,
			Value = 40,
		};
		AutomationProperties.SetAutomationId(slider, "slider-range-contract");

		await UITestHelper.Load(slider);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(slider);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Range, "Range must be populated for a Slider.");

		var range = snapshot.Details!.Range!;
		Assert.AreEqual(40.0, range.Value, "Range.Value must match Slider.Value.");
		Assert.AreEqual(10.0, range.Minimum, "Range.Minimum must match Slider.Minimum.");
		Assert.AreEqual(90.0, range.Maximum, "Range.Maximum must match Slider.Maximum.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Has_Step_On_Mobile_Then_Range_SmallChange_Matches()
	{
		var slider = new Slider
		{
			Minimum = 0,
			Maximum = 100,
			Value = 50,
			SmallChange = 5,
		};

		await UITestHelper.Load(slider);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(slider);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Range, "Range must be populated for a Slider.");
		Assert.AreEqual(5.0, snapshot.Details!.Range!.SmallChange, "Range.SmallChange must reflect the range provider.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Disabled_On_Mobile_Then_Range_IsReadOnly()
	{
		var slider = new Slider
		{
			Minimum = 0,
			Maximum = 100,
			Value = 50,
			IsEnabled = false,
		};

		await UITestHelper.Load(slider);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(slider);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Range, "Range must be populated for a disabled Slider.");
		Assert.IsTrue(snapshot.Details!.Range!.IsReadOnly, "A disabled Slider must report Range.IsReadOnly=true.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Vertical_Slider_On_Mobile_Then_Range_Orientation_Is_Vertical()
	{
		var slider = new Slider
		{
			Orientation = Orientation.Vertical,
			Minimum = 0,
			Maximum = 100,
			Value = 50,
		};

		await UITestHelper.Load(slider);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(slider);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Range, "Range must be populated for a vertical Slider.");
		Assert.AreEqual(
			AutomationOrientation.Vertical,
			snapshot.Details!.Range!.Orientation,
			"A vertical Slider must project AutomationOrientation.Vertical.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_On_Mobile_Then_Increment_And_Decrement_Actions_Advertised()
	{
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50 };

		await UITestHelper.Load(slider);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(slider);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details, "Details must be populated for a Slider.");

		Assert.IsTrue(
			snapshot.Details!.SupportedActions.Contains(AccessibilityNativeAction.Increment),
			"Slider must advertise Increment action.");
		Assert.IsTrue(
			snapshot.Details.SupportedActions.Contains(AccessibilityNativeAction.Decrement),
			"Slider must advertise Decrement action.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_On_Mobile_Then_TextState_IsEditable_Not_ReadOnly()
	{
		var textBox = new TextBox { Text = "editable content" };
		AutomationProperties.SetAutomationId(textBox, "textbox-editable-contract");

		await UITestHelper.Load(textBox);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(textBox);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.TextState, "TextState must be populated for a TextBox.");

		var ts = snapshot.Details!.TextState!;
		Assert.IsTrue(ts.IsEditable, "An enabled TextBox must report IsEditable=true.");
		Assert.IsFalse(ts.IsReadOnly, "A non-read-only TextBox must report IsReadOnly=false.");
		Assert.IsFalse(ts.IsMultiline, "A single-line TextBox must report IsMultiline=false.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_ReadOnly_On_Mobile_Then_TextState_IsReadOnly()
	{
		var textBox = new TextBox { Text = "read only content", IsReadOnly = true };

		await UITestHelper.Load(textBox);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(textBox);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.TextState, "TextState must be populated for a read-only TextBox.");

		var ts = snapshot.Details!.TextState!;
		Assert.IsTrue(ts.IsReadOnly, "A read-only TextBox must report IsReadOnly=true.");
		Assert.IsFalse(ts.IsEditable, "A read-only TextBox must report IsEditable=false.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_AcceptsReturn_On_Mobile_Then_TextState_IsMultiline()
	{
		var textBox = new TextBox { Text = "line one", AcceptsReturn = true };

		await UITestHelper.Load(textBox);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(textBox);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.TextState, "TextState must be populated for a multiline TextBox.");
		Assert.IsTrue(snapshot.Details!.TextState!.IsMultiline, "AcceptsReturn=true TextBox must report IsMultiline=true.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PasswordBox_On_Mobile_Then_Snapshot_IsPassword_And_Value_Not_Plaintext()
	{
		var passwordBox = new PasswordBox { Password = "s3cr3t!" };
		AutomationProperties.SetAutomationId(passwordBox, "passwordbox-secure-contract");

		await UITestHelper.Load(passwordBox);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(passwordBox);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsTrue(snapshot.Password, "PasswordBox must set Password=true in the native snapshot.");

		Assert.AreNotEqual(
			"s3cr3t!",
			snapshot.Value,
			"Native snapshot value must not expose the plaintext PasswordBox password.");
	}


	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ListView_On_Mobile_Then_Collection_RowCount_Matches_Item_Count()
	{
		var listView = new ListView
		{
			ItemsSource = new List<string> { "Alpha", "Beta", "Gamma", "Delta" },
			SelectionMode = ListViewSelectionMode.Single,
		};
		AutomationProperties.SetAutomationId(listView, "listview-collection-contract");

		await UITestHelper.Load(listView);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(listView);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Collection, "Collection must be populated for a ListView.");
		Assert.AreEqual(4, snapshot.Details!.Collection!.RowCount, "Collection.RowCount must match the number of items.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ListView_Multiple_Selection_On_Mobile_Then_Collection_CanSelectMultiple()
	{
		var listView = new ListView
		{
			ItemsSource = new List<string> { "One", "Two", "Three" },
			SelectionMode = ListViewSelectionMode.Multiple,
		};

		await UITestHelper.Load(listView);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(listView);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Collection, "Collection must be populated for a ListView.");
		Assert.IsTrue(
			snapshot.Details!.Collection!.CanSelectMultiple,
			"Multiple-selection ListView must report CanSelectMultiple=true.");
	}


	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ScrollViewer_Oversized_Content_On_Mobile_Then_IsVerticallyScrollable()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new Border { Width = 50, Height = 2000 },
		};
		AutomationProperties.SetName(scrollViewer, "Tall Content");
		AutomationProperties.SetAutomationId(scrollViewer, "scrollviewer-scroll-contract");

		await UITestHelper.Load(scrollViewer);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(scrollViewer);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Scroll, "Scroll must be populated for a scrollable ScrollViewer.");
		Assert.IsTrue(
			snapshot.Details!.Scroll!.IsVerticallyScrollable,
			"A vertically overflowing ScrollViewer must report IsVerticallyScrollable=true.");
		Assert.IsFalse(
			snapshot.Details.Scroll.IsHorizontallyScrollable,
			"A vertically-only ScrollViewer must report IsHorizontallyScrollable=false.");
	}


	[TestMethod]
	[RunsOnUIThread]
	public async Task When_LabeledBy_Set_On_Mobile_Then_Relations_LabeledByIds_Not_Empty()
	{
		var label = new TextBlock { Text = "Search field" };
		AutomationProperties.SetAutomationId(label, "label-labeledby-contract");

		var field = new TextBox();
		AutomationProperties.SetAutomationId(field, "field-labeledby-contract");
		AutomationProperties.SetLabeledBy(field, label);

		var panel = new StackPanel { Children = { label, field } };

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(field);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Relations, "Relations must be populated for an element with LabeledBy.");
		Assert.IsTrue(
			snapshot.Details!.Relations!.LabeledByIds.Count > 0,
			"LabeledByIds must be non-empty when AutomationProperties.LabeledBy is set.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_LabeledBy_Target_Has_No_AutomationId_Then_Name_Still_Resolves()
	{
		var label = new TextBlock { Text = "Search field" };
		var field = new TextBox();
		AutomationProperties.SetLabeledBy(field, label);
		var panel = new StackPanel { Children = { label, field } };

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(field);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.AreEqual(
			"Search field",
			snapshot.Name,
			"LabeledBy must resolve the spoken name even when the label has no machine identifier.");

		if (snapshot.Details?.Relations is { } relations)
		{
			Assert.AreEqual(
				0,
				relations.LabeledByIds.Count,
				"Relation metadata must not fabricate an identifier for a target without AutomationId.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_DescribedBy_Set_On_Mobile_Then_Relations_DescribedByIds_Not_Empty()
	{
		var description = new TextBlock { Text = "Enter your full name" };
		AutomationProperties.SetAutomationId(description, "desc-describedby-contract");

		var field = new TextBox();
		AutomationProperties.SetAutomationId(field, "field-describedby-contract");
		AutomationProperties.GetDescribedBy(field).Add(description);

		var panel = new StackPanel { Children = { description, field } };

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(field);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Relations, "Relations must be populated for an element with DescribedBy.");
		Assert.IsTrue(
			snapshot.Details!.Relations!.DescribedByIds.Count > 0,
			"DescribedByIds must be non-empty when AutomationProperties.DescribedBy is set.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ControlledPeers_Set_On_Mobile_Then_Relations_ControlledPeerIds_Not_Empty()
	{
		var controlled = new TextBox();
		AutomationProperties.SetAutomationId(controlled, "controlled-contract");

		var controller = new Button { Content = "Open" };
		AutomationProperties.SetAutomationId(controller, "controller-contract");
		AutomationProperties.GetControlledPeers(controller).Add(controlled);

		var panel = new StackPanel { Children = { controller, controlled } };

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(controller);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Relations, "Relations must be populated for an element with ControlledPeers.");
		Assert.IsTrue(
			snapshot.Details!.Relations!.ControlledPeerIds.Count > 0,
			"ControlledPeerIds must be non-empty when AutomationProperties.ControlledPeers is set.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FlowsTo_Set_On_Mobile_Then_Relations_FlowsToIds_Not_Empty()
	{
		var next = new Button { Content = "Next" };
		AutomationProperties.SetAutomationId(next, "flows-to-target-contract");

		var current = new Button { Content = "Current" };
		AutomationProperties.SetAutomationId(current, "flows-to-source-contract");
		AutomationProperties.GetFlowsTo(current).Add(next);

		var panel = new StackPanel { Children = { current, next } };

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(current);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Relations, "Relations must be populated for an element with FlowsTo.");
		Assert.IsTrue(
			snapshot.Details!.Relations!.FlowsToIds.Count > 0,
			"FlowsToIds must be non-empty when AutomationProperties.FlowsTo is set.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Culture_Set_On_Mobile_Then_Details_Culture_Is_LCID()
	{
		const int enUs = 1033;
		var button = new Button { Content = "English" };
		AutomationProperties.SetCulture(button, enUs);

		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(button);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details, "Details must be populated for a culturally tagged element.");
		Assert.AreEqual(enUs, snapshot.Details!.Culture, "Details.Culture must equal the AutomationProperties.Culture LCID.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_IsDataValidForForm_False_On_Mobile_Then_Details_Reflects_Invalid()
	{
		var textBox = new TextBox { Text = "bad value" };
		AutomationProperties.SetIsDataValidForForm(textBox, false);

		await UITestHelper.Load(textBox);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(textBox);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details, "Details must be populated for a form-invalid element.");
		Assert.AreEqual(
			false,
			snapshot.Details!.IsDataValidForForm,
			"Details.IsDataValidForForm must be false when AutomationProperties.IsDataValidForForm is false.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Level_Set_On_Mobile_Then_Hierarchy_Level_Matches()
	{
		var heading = new Button { Content = "Section Title" };
		AutomationProperties.SetLevel(heading, 2);

		await UITestHelper.Load(heading);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(heading);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Hierarchy, "Hierarchy must be populated for a level-tagged element.");
		Assert.AreEqual(2, snapshot.Details!.Hierarchy!.Level, "Hierarchy.Level must equal AutomationProperties.Level.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PositionInSet_And_SizeOfSet_Set_On_Mobile_Then_Hierarchy_Matches()
	{
		var item = new Button { Content = "Item 3 of 5" };
		AutomationProperties.SetPositionInSet(item, 3);
		AutomationProperties.SetSizeOfSet(item, 5);

		await UITestHelper.Load(item);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(item);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details?.Hierarchy, "Hierarchy must be populated for set-positioned elements.");

		var hierarchy = snapshot.Details!.Hierarchy!;
		Assert.AreEqual(3, hierarchy.PositionInSet, "PositionInSet must match AutomationProperties.PositionInSet.");
		Assert.AreEqual(5, hierarchy.SizeOfSet, "SizeOfSet must match AutomationProperties.SizeOfSet.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FullDescription_Set_On_Mobile_Then_Details_FullDescription_Matches()
	{
		const string description = "This button opens the settings panel.";
		var button = new Button { Content = "Settings" };
		AutomationProperties.SetFullDescription(button, description);

		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(button);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details, "Details must be populated for an element with FullDescription.");
		Assert.AreEqual(
			description,
			snapshot.Details!.FullDescription,
			"Details.FullDescription must match AutomationProperties.FullDescription.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_On_Mobile_Then_Details_LocalizedControlType_Not_Empty()
	{
		var button = new Button { Content = "Submit" };
		AutomationProperties.SetAutomationId(button, "button-localizedtype-contract");

		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(button);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsNotNull(snapshot.Details, "Details must be populated for a Button.");
		Assert.IsFalse(
			string.IsNullOrEmpty(snapshot.Details!.LocalizedControlType),
			"Details.LocalizedControlType must be a non-empty localized role string for a Button.");

		if (AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor is not null)
		{
			Assert.AreEqual(snapshot.Details.LocalizedControlType, snapshot.NativeRoleDescription);
		}

		if (AccessibilityPeerHelper.IOSAccessibilityElementAccessor?.Invoke(button) is { } nativeElement)
		{
			var customContent = nativeElement
				.GetType()
				.GetProperty("AccessibilityCustomContent")
				?.GetValue(nativeElement) as Array;
			Assert.IsNotNull(customContent);
			Assert.IsTrue(
				customContent
					.Cast<object>()
					.Any(item =>
						string.Equals(
							item.GetType().GetProperty("Value")?.GetValue(item) as string,
							snapshot.Details.LocalizedControlType,
							StringComparison.Ordinal)),
				"iOS AX custom content must expose the localized control type.");
		}
	}
}
