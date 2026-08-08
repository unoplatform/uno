#nullable enable

using System;
using System.Collections.Generic;
using OpenQA.Selenium;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Per-platform driver lifecycle plus locator translation. Lets the test code
/// stay platform-agnostic while the per-OS automation backends differ
/// significantly (UIA on Win32, NSAccessibility on macOS, ARIA/DOM on WASM).
/// </summary>
public interface IPlatformAdapter : IDisposable
{
	AppiumPlatform Platform { get; }

	IWebDriver CreateDriver(AppiumTestOptions options, string sampleQuery);

	By ByAutomationId(string automationId);

	void Activate(IWebDriver driver, IWebElement element);

	void EnterText(IWebDriver driver, IWebElement element, string value);

	/// <summary>
	/// Reads the role/role-description that the platform automation API reports
	/// for the given element. Normalized to lowercase. Used for cross-platform
	/// role assertions (e.g. button, edit/textbox).
	/// </summary>
	string GetRole(IWebElement element);

	/// <summary>
	/// Returns the readable accessible name as the platform exposes it
	/// (UIA Name, AX title, or computed ARIA name).
	/// </summary>
	string GetName(IWebDriver driver, IWebElement element);

	string? GetDescription(IWebDriver driver, IWebElement element);

	/// <summary>
	/// Returns every reachable descendant. Used to assert that the
	/// automation tree is populated, not just the root.
	/// </summary>
	IReadOnlyList<IWebElement> GetAllDescendants(IWebDriver driver);

	/// <summary>
	/// AutomationId equivalent on this platform (UIA AutomationId / AXIdentifier /
	/// the DOM <c>xamlname</c> attribute). Empty string when none.
	/// </summary>
	string GetAutomationId(IWebElement element);

	/// <summary>
	/// Value-pattern string when the element exposes one (UIA Value.Value /
	/// AXValue / DOM <c>value</c>). Null when the element has no value pattern.
	/// </summary>
	string? GetValue(IWebElement element);

	/// <summary>
	/// Canonical names of supported automation patterns. Lower-cased,
	/// stable across platforms. Implementations infer these from the role
	/// plus platform-specific pattern-availability attributes.
	/// </summary>
	IReadOnlyList<string> GetSupportedPatterns(IWebElement element);

	bool? GetEnabled(IWebElement element);

	bool? GetKeyboardFocusable(IWebElement element);

	bool? GetFocused(IWebDriver driver, IWebElement element);

	bool? GetOffscreen(IWebElement element);

	string? GetToggleState(IWebElement element);

	bool? GetSelected(IWebElement element);

	bool? GetExpanded(IWebElement element);

	bool? GetRequired(IWebElement element);

	int? GetLevel(IWebElement element);

	string? GetLandmark(IWebElement element);

	string? GetRoleDescription(IWebElement element);

	string? GetLiveSetting(IWebElement element);

	/// <summary>
	/// Direct children in tree order. Returns the visible top-level
	/// elements when <paramref name="parent"/> is null (driver root).
	/// </summary>
	IReadOnlyList<IWebElement> GetChildren(IWebDriver driver, IWebElement? parent);

	/// <summary>
	/// Optional per-element platform-specific extras (raw role, role
	/// description, control type) added to <see cref="AccessibilityNode.Extras"/>
	/// for debugging.
	/// </summary>
	IReadOnlyDictionary<string, string> GetExtras(IWebElement element);
}
