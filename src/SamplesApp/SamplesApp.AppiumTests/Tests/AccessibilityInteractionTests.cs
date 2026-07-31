#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
[TestCategory(TestCategories.HostRequired)]
public sealed class AccessibilityInteractionTests : AppiumFixtureBase
{
	protected override string SampleQuery => AccessibilityScreenReaderSnapshotDefinition.SampleQuery;

	[TestMethod]
	[TestCategory(TestCategories.Interaction)]
	public void EnableNotificationsCheckBox_Click_UpdatesToggleState()
	{
		var fields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.ToggleState | AccessibilitySnapshotFields.Enabled;
		var initial = Session.CaptureElement(AccessibilityScreenReaderIds.EnableNotificationsCheckBox, fields);
		initial.Patterns.Should().Contain("toggle");
		initial.State.ToggleState.Should().Be("on");

		Session.Activate(AccessibilityScreenReaderIds.EnableNotificationsCheckBox);

		var updated = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.EnableNotificationsCheckBox,
			fields,
			snapshot => snapshot.State.ToggleState == "off",
			"observe the checkbox toggle state change");

		updated.State.Enabled.Should().BeTrue();
	}

	[TestMethod]
	[TestCategory(TestCategories.Interaction)]
	public void SizeRadioButton_Click_UpdatesSelectionState()
	{
		var fields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Selected;
		var initialSmall = Session.CaptureElement(AccessibilityScreenReaderIds.SizeSmallRadioButton, fields);
		initialSmall.Patterns.Should().Contain("selectionitem");
		initialSmall.State.Selected.Should().BeTrue();

		Session.Activate(AccessibilityScreenReaderIds.SizeMediumRadioButton);

		var medium = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.SizeMediumRadioButton,
			fields,
			snapshot => snapshot.State.Selected == true,
			"observe the newly selected radio button");
		var small = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.SizeSmallRadioButton,
			fields,
			snapshot => snapshot.State.Selected == false,
			"observe the previously selected radio button becoming unselected");

		medium.State.Selected.Should().BeTrue();
		small.State.Selected.Should().BeFalse();
	}

	[TestMethod]
	[TestCategory(TestCategories.Interaction)]
	public void FavoriteColorComboBox_Selection_UpdatesThePlatformTree()
	{
		var comboFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Value;
		var initialCombo = Session.CaptureElement(AccessibilityScreenReaderIds.FavoriteColorComboBox, comboFields);
		initialCombo.Patterns.Should().Contain("expandcollapse");
		initialCombo.Value.Should().Be("Red");

		Session.Activate(AccessibilityScreenReaderIds.FavoriteColorComboBox);
		Session.Activate(AccessibilityScreenReaderIds.FavoriteColorOptionGreen);

		var combo = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.FavoriteColorComboBox,
			comboFields,
			snapshot => string.Equals(snapshot.Value, "Green", StringComparison.Ordinal),
			"observe the combobox value change");
		Session.Activate(AccessibilityScreenReaderIds.FavoriteColorComboBox);
		var selectedGreen = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.FavoriteColorOptionGreen,
			AccessibilitySnapshotFields.Selected,
			snapshot => snapshot.State.Selected == true,
			"observe the selected combobox item");
		var selectedRed = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.FavoriteColorOptionRed,
			AccessibilitySnapshotFields.Selected,
			snapshot => snapshot.State.Selected == false,
			"observe the previously selected combobox item");

		combo.Value.Should().Be("Green");
		selectedGreen.State.Selected.Should().BeTrue();
		selectedRed.State.Selected.Should().BeFalse();
	}

	[TestMethod]
	[TestCategory(TestCategories.Interaction)]
	public void CommentsTextBox_SendKeys_UpdatesValueAndFocus()
	{
		const string typedValue = "Appium typed comment";
		var fields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Value | AccessibilitySnapshotFields.Focused;

		Session.EnterText(AccessibilityScreenReaderIds.CommentsTextBox, typedValue);

		var snapshot = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.CommentsTextBox,
			fields,
			captured => captured.Value?.Contains(typedValue, StringComparison.Ordinal) == true,
			"observe the typed textbox value");

		snapshot.Patterns.Should().Contain("value");
		snapshot.Value.Should().Contain(typedValue);
		snapshot.State.Focused.Should().BeTrue();
	}

	[TestMethod]
	[TestCategory(TestCategories.Interaction)]
	public void CombinedTextBox_Disable_UpdatesEnabledState()
	{
		var fields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Enabled | AccessibilitySnapshotFields.KeyboardFocusable;
		Session.Activate(AccessibilityScreenReaderIds.CombinedDisableButton);

		var snapshot = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.CombinedTextBox,
			fields,
			captured => captured.State.Enabled == false,
			"observe the disabled textbox state");

		snapshot.State.Enabled.Should().BeFalse();
		if (snapshot.State.KeyboardFocusable is not null)
		{
			snapshot.State.KeyboardFocusable.Should().BeFalse();
		}
	}

	[TestMethod]
	[TestCategory(TestCategories.Interaction)]
	public void LiveRegionText_UpdatesAfterInvokingTheUpdateButton()
	{
		var initial = Session.CaptureElement(
			AccessibilityScreenReaderIds.LiveRegionText,
			AccessibilitySnapshotFields.LiveSetting);
		initial.Name.Should().Be("Status: Ready");

		Session.Activate(AccessibilityScreenReaderIds.LiveRegionUpdateButton);

		var updated = Session.WaitForSnapshot(
			AccessibilityScreenReaderIds.LiveRegionText,
			AccessibilitySnapshotFields.LiveSetting,
			snapshot => snapshot.Name.Contains("Updated (1)", StringComparison.Ordinal),
			"observe the live-region text update");

		updated.Name.Should().Contain("Updated (1)");
		if (updated.State.LiveSetting is not null)
		{
			updated.State.LiveSetting.Should().Be("polite");
		}
	}
}
