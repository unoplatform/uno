#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
public sealed class AccessibilitySnapshotComparerTests
{
	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Compare_ReportsChangedStatesAndMissingElements()
	{
		var expected = new AccessibilitySnapshot
		{
			Schema = SnapshotSerializer.SchemaVersion,
			Sample = "Automation/Accessibility_ScreenReader",
			Flavor = "win32",
			Elements =
			{
				new AccessibilityElementSnapshot
				{
					Id = "EnableNotificationsCheckBox",
					AutomationId = "EnableNotificationsCheckBox",
					Role = "checkbox",
					Name = "Enable notifications",
					Patterns = new List<string> { "toggle" },
					State = new AccessibilityElementState
					{
						ToggleState = "on",
						Enabled = true,
					},
				},
			},
		};

		var actual = new AccessibilitySnapshot
		{
			Schema = SnapshotSerializer.SchemaVersion,
			Sample = "Automation/Accessibility_ScreenReader",
			Flavor = "win32",
			Elements =
			{
				new AccessibilityElementSnapshot
				{
					Id = "EnableNotificationsCheckBox",
					AutomationId = "EnableNotificationsCheckBox",
					Role = "checkbox",
					Name = "Enable notifications",
					Patterns = new List<string> { "toggle" },
					State = new AccessibilityElementState
					{
						ToggleState = "off",
						Enabled = true,
					},
				},
				new AccessibilityElementSnapshot
				{
					Id = "CombinedTextBox",
					AutomationId = "CombinedTextBox",
					Role = "textbox",
					Name = "Editable field",
				},
			},
		};

		var diff = SnapshotComparer.Compare(expected, actual);

		diff.IsMatch.Should().BeFalse();
		diff.Format().Should().Contain("elements[EnableNotificationsCheckBox].state.toggle_state");
		diff.Format().Should().Contain("elements[CombinedTextBox]");
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Serialize_WritesStableLfJson()
	{
		var snapshot = new AccessibilitySnapshot
		{
			Schema = SnapshotSerializer.SchemaVersion,
			Sample = "Automation/Accessibility_ScreenReader",
			Flavor = "win32",
			Elements =
			{
				new AccessibilityElementSnapshot
				{
					Id = "PlainTextBlock",
					AutomationId = "PlainTextBlock",
					Role = "text",
					Name = "Static text element",
				},
			},
		};

		var json = SnapshotSerializer.Serialize(snapshot);
		json.Should().NotContain("\r\n");

		var path = Path.Combine(AppContext.BaseDirectory, $"snapshot-roundtrip-{Guid.NewGuid():N}.json");
		try
		{
			SnapshotSerializer.Write(path, snapshot);
			var roundTrip = SnapshotSerializer.Read(path);

			roundTrip.Should().NotBeNull();
			roundTrip!.Elements.Should().HaveCount(1);
			roundTrip.Elements[0].Id.Should().Be("PlainTextBlock");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}
