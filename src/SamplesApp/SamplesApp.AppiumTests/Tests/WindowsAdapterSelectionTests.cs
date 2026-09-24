#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
[TestCategory(TestCategories.HostIndependent)]
public sealed class WindowsAdapterSelectionTests
{
	[TestMethod]
	public void Closed_SelectOnly_ComboBox_Snapshot_Keeps_Name_Value_And_Selection_Distinct()
	{
		var combo = Element(ComboBoxAttributes());
		var redAttributes = new Dictionary<string, string?> { ["SelectionItem.IsSelected"] = "true" };
		var greenAttributes = new Dictionary<string, string?> { ["SelectionItem.IsSelected"] = "false" };
		var red = Element(redAttributes);
		var green = Element(greenAttributes);
		IReadOnlyList<string> selection = new[] { "Red" };
		var queries = 0;
		var adapter = new WindowsAdapter(element =>
		{
			element.Should().BeSameAs(combo);
			queries++;
			return selection;
		});
		var fields = AccessibilitySnapshotFields.Value | AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Expanded;
		var driver = DispatchProxy.Create<IWebDriver, ElementProxy>();

		var initial = AccessibilitySnapshotBuilder.Capture(driver, adapter, "color", fields, combo);

		initial.Name.Should().Be("Favorite color");
		initial.Value.Should().Be("Red");
		initial.State.Expanded.Should().BeFalse();
		initial.Patterns.Should().Equal("expandcollapse", "selection");
		initial.Patterns.Should().NotContain("value");
		adapter.GetSelected(red).Should().BeTrue();
		adapter.GetSelected(green).Should().BeFalse();

		selection = new[] { "Green" };
		redAttributes["SelectionItem.IsSelected"] = "false";
		greenAttributes["SelectionItem.IsSelected"] = "true";
		var changed = AccessibilitySnapshotBuilder.Capture(driver, adapter, "color", fields, combo);

		changed.Name.Should().Be("Favorite color");
		changed.Value.Should().Be("Green");
		changed.State.Expanded.Should().BeFalse();
		changed.Patterns.Should().Equal(initial.Patterns);
		adapter.GetSelected(red).Should().BeFalse();
		adapter.GetSelected(green).Should().BeTrue();
		queries.Should().Be(2);
	}

	[TestMethod]
	public void SelectOnly_ComboBox_Does_Not_Infer_Value_From_An_Unavailable_Pattern()
	{
		var attributes = ComboBoxAttributes();
		attributes["Value.Value"] = "stale value";
		attributes["value"] = "Favorite color";
		var adapter = new WindowsAdapter(_ => new[] { "Red" });

		adapter.GetValue(Element(attributes)).Should().Be("Red");
	}

	[TestMethod]
	[DataRow("Typed color", "Typed color")]
	[DataRow("", null)]
	public void Editable_ComboBox_Uses_Value_Pattern_Even_When_Empty(string value, string? expected)
	{
		var attributes = ComboBoxAttributes();
		attributes["IsValuePatternAvailable"] = "true";
		attributes["Value.Value"] = value;
		var adapter = WithoutSelectionQueries();

		adapter.GetValue(Element(attributes)).Should().Be(expected);
	}

	[TestMethod]
	[DataRow("Edit", "Value.Value", "Text")]
	[DataRow("Slider", "RangeValue.Value", "42")]
	public void Other_Controls_Keep_Their_Value_Pattern_Path(string role, string attribute, string expected)
	{
		var attributes = new Dictionary<string, string?>
		{
			["ControlType"] = role,
			[attribute] = expected,
		};

		WithoutSelectionQueries().GetValue(Element(attributes)).Should().Be(expected);
	}

	[TestMethod]
	public void Empty_Selection_Does_Not_Use_The_ComboBox_Name_As_A_Value()
	{
		var adapter = new WindowsAdapter(_ => Array.Empty<string>());

		adapter.GetValue(Element(ComboBoxAttributes())).Should().BeNull();
	}

	[TestMethod]
	public void Missing_Selection_Pattern_Is_Not_Invented()
	{
		var attributes = ComboBoxAttributes();
		attributes["IsSelectionPatternAvailable"] = "false";

		WithoutSelectionQueries().GetValue(Element(attributes)).Should().BeNull();
	}

	[TestMethod]
	public void Disabled_ComboBox_Selection_Is_Read_Without_Activating_It()
	{
		var attributes = ComboBoxAttributes();
		attributes["IsEnabled"] = "false";
		var adapter = new WindowsAdapter(_ => new[] { "Red" });

		adapter.GetValue(Element(attributes)).Should().Be("Red");
	}

	[TestMethod]
	public void Multiple_Selected_Items_Fail_The_SingleSelection_Contract()
	{
		var adapter = new WindowsAdapter(_ => new[] { "Red", "Green" });

		var read = () => adapter.GetValue(Element(ComboBoxAttributes()));

		read.Should().Throw<InvalidOperationException>().WithMessage("*multiple selected UIA elements*");
	}

	[TestMethod]
	public void Selection_Query_Failure_Is_Not_Converted_To_An_Empty_Value()
	{
		var failure = new COMException("Selection provider failed");
		var adapter = new WindowsAdapter(_ => throw failure);

		var read = () => adapter.GetValue(Element(ComboBoxAttributes()));

		read.Should().Throw<COMException>().Which.Should().BeSameAs(failure);
	}

	[TestMethod]
	public void Native_Selection_Requires_An_Initialized_Local_Session()
	{
		var adapter = new WindowsAdapter();

		var read = () => adapter.GetValue(Element(ComboBoxAttributes()));

		read.Should().Throw<InvalidOperationException>().WithMessage("*local Windows Appium session*");
	}

	private static WindowsAdapter WithoutSelectionQueries()
		=> new(_ => throw new AssertFailedException("This value must not query Selection."));

	private static Dictionary<string, string?> ComboBoxAttributes()
		=> new()
		{
			["AutomationId"] = "FavoriteColorComboBox",
			["ControlType"] = "ComboBox",
			["Name"] = "Favorite color",
			["IsValuePatternAvailable"] = "false",
			["IsSelectionPatternAvailable"] = "true",
			["IsExpandCollapsePatternAvailable"] = "true",
			["ExpandCollapse.ExpandCollapseState"] = "Collapsed",
		};

	private static IWebElement Element(Dictionary<string, string?> attributes)
	{
		var element = DispatchProxy.Create<IWebElement, ElementProxy>();
		((ElementProxy)(object)element).Attributes = attributes;
		return element;
	}

	public class ElementProxy : DispatchProxy
	{
		public Dictionary<string, string?> Attributes { get; set; } = new();

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod?.Name == nameof(IWebElement.GetAttribute) && args?[0] is string name)
			{
				return Attributes.GetValueOrDefault(name);
			}

			throw new AssertFailedException(
				$"Value capture must not activate the ComboBox or require its collapsed item subtree: {targetMethod?.Name}.");
		}
	}
}
