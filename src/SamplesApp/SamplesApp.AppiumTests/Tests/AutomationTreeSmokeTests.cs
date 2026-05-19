#nullable enable

using System.Linq;
using AwesomeAssertions;
using NUnit.Framework;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

/// <summary>
/// Smoke tests that prove the per-platform automation tree is created and
/// queryable end-to-end:
/// <list type="bullet">
///   <item>Win32 UIAutomation (via Appium Windows driver)</item>
///   <item>macOS NSAccessibility (via Appium Mac2 driver)</item>
///   <item>WASM ARIA/DOM (via Appium chromium driver)</item>
/// </list>
/// Each test uses AutomationIds that already exist on
/// <c>AccessibilityScreenReaderPage.xaml</c>.
/// </summary>
[TestFixture]
public sealed class AutomationTreeSmokeTests : AppiumFixtureBase
{
	private const string AccessibilityPageQuery =
		"sample=Automation/Accessibility_ScreenReader";

	private const string VisibilityButtonId = "VisibilityTargetButton";
	private const string CombinedTextBoxId = "CombinedTextBox";
	private const string PlainTextBlockId = "PlainTextBlock";

	protected override string SampleQuery => AccessibilityPageQuery;

	[Test]
	public void Tree_HasPopulatedAutomationElements()
	{
		var descendants = Adapter.GetAllDescendants(Driver);

		descendants.Should()
			.HaveCountGreaterThan(10,
				"the SamplesApp page should expose more than a handful of " +
				"elements through the OS automation tree.");
	}

	[Test]
	public void Button_FoundByAutomationId_HasButtonRole()
	{
		var button = WaitForAutomationId(VisibilityButtonId);

		button.Displayed.Should().BeTrue();

		var role = Adapter.GetRole(button);
		role.Should().Contain("button",
			"the platform automation backend must surface VisibilityTargetButton as a button.");
	}

	[Test]
	public void Button_FoundByAutomationId_CanBeInvoked()
	{
		var button = WaitForAutomationId(VisibilityButtonId);

		button.Click();

		// Re-resolve - on WASM the element survives, on Win/Mac it may have moved.
		// We're not asserting a side-effect (the button is purely a target for
		// visibility toggles by sibling buttons); the assertion is that
		// Click() round-trips through the driver without throwing.
		Assert.Pass();
	}

	[Test]
	public void TextBox_FoundByAutomationId_AcceptsValue()
	{
		var textBox = WaitForAutomationId(CombinedTextBoxId);

		const string typed = "appium-was-here";
		textBox.Click();
		textBox.SendKeys(typed);

		var observed = textBox.GetAttribute("Value")
			?? textBox.GetAttribute("value")
			?? textBox.Text;

		observed.Should().Contain(typed,
			"the value pattern (UIA Value / AX value / DOM value) should reflect typed input.");
	}

	[Test]
	public void TextBlock_FoundByAutomationId_HasAccessibleName()
	{
		var textBlock = WaitForAutomationId(PlainTextBlockId);

		var name = Adapter.GetName(textBlock);
		name.Should().NotBeNullOrWhiteSpace(
			"non-control elements should still surface their AutomationProperties.Name.");
	}

	[Test]
	public void Tree_ExposesMoreThanRootElement()
	{
		var descendants = Adapter.GetAllDescendants(Driver);
		var distinctTags = descendants
			.Select(e => Adapter.GetRole(e))
			.Where(role => !string.IsNullOrWhiteSpace(role))
			.Distinct()
			.ToList();

		distinctTags.Should()
			.HaveCountGreaterThan(2,
				"a real SamplesApp page should expose several different roles " +
				"(buttons, text, groups, etc.) through the automation tree.");
	}
}
