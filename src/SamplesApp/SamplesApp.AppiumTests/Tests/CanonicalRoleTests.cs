#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
public sealed class CanonicalRoleTests
{
	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Normalize_MapsKnownPlatformRoles()
	{
		var cases = new[]
		{
			(rawRole: "radio button", platform: "Windows", expected: "radio"),
			(rawRole: "AXRadioButton", platform: "Mac", expected: "radio"),
			(rawRole: "XCUIElementTypeComboBox", platform: "Mac", expected: "combobox"),
			(rawRole: "textbox.multiline", platform: "Wasm", expected: "textbox"),
			(rawRole: "input", platform: "Wasm", expected: "textbox"),
			(rawRole: "AXStaticText", platform: "Mac", expected: "text"),
			(rawRole: "tab", platform: "Windows", expected: "tablist"),
			(rawRole: "tab item", platform: "Windows", expected: "tab"),
			(rawRole: "ControlType.Button", platform: "Windows", expected: "button"),
		};

		foreach (var @case in cases)
		{
			var platform = AppiumTestOptions.ParsePlatform(@case.platform);
			var canonical = CanonicalRole.Normalize(@case.rawRole, platform);

			canonical.Should().Be(@case.expected, $"raw role '{@case.rawRole}' on {@case.platform} should normalize predictably.");
		}
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Normalize_PrefersHeadingWhenLevelIsPresent()
	{
		var canonical = CanonicalRole.Normalize("text", AppiumPlatform.Windows, level: 3);

		canonical.Should().Be("heading");
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Normalize_KeepsHierarchicalRolesWhenLevelIsPresent()
	{
		// UIA Level / aria-level is carried by hierarchical items as well as headings, so a level
		// must never clobber a role that already identifies the element.
		var cases = new[]
		{
			(rawRole: "treeitem", platform: AppiumPlatform.Wasm, expected: "treeitem"),
			(rawRole: "listitem", platform: AppiumPlatform.Wasm, expected: "listitem"),
			(rawRole: "ControlType.TreeItem", platform: AppiumPlatform.Windows, expected: "treeitem"),
			(rawRole: "tree item", platform: AppiumPlatform.Windows, expected: "treeitem"),
			(rawRole: "AXRow", platform: AppiumPlatform.Mac, expected: "listitem"),
		};

		foreach (var @case in cases)
		{
			var canonical = CanonicalRole.Normalize(@case.rawRole, @case.platform, level: 2);

			canonical.Should().Be(
				@case.expected,
				$"a level on '{@case.rawRole}' ({@case.platform}) marks hierarchy depth, not a heading.");
		}
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Normalize_PrefersHeadingWhenLevelIsPresentOnRolelessElement()
	{
		CanonicalRole.Normalize(null, AppiumPlatform.Wasm, level: 2).Should().Be("heading");
		CanonicalRole.Normalize("heading", AppiumPlatform.Wasm, level: 2).Should().Be("heading");
		CanonicalRole.Normalize("AXStaticText", AppiumPlatform.Mac, level: 2).Should().Be("heading");
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Normalize_PrefersLandmarkWhenLandmarkIsPresent()
	{
		var canonical = CanonicalRole.Normalize("group", AppiumPlatform.Wasm, landmark: "navigation");

		canonical.Should().Be("landmark");
	}
}
