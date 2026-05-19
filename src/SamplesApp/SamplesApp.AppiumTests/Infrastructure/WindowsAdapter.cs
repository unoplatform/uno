#nullable enable

using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Drives SamplesApp.Skia.Generic on Windows via the Appium Windows driver,
/// which forwards to the Win32 UIAutomation provider tree exposed by
/// Uno.UI.Runtime.Skia.Win32.
/// </summary>
public sealed class WindowsAdapter : IPlatformAdapter
{
	public AppiumPlatform Platform => AppiumPlatform.Windows;

	public IWebDriver CreateDriver(string sampleQuery)
	{
		var options = new AppiumOptions
		{
			AutomationName = "Windows",
			PlatformName = "Windows",
		};

		options.AddAdditionalAppiumOption("app", AppiumPlatformResolver.RequireAppPath());

		if (!string.IsNullOrEmpty(sampleQuery))
		{
			options.AddAdditionalAppiumOption("appArguments", sampleQuery);
		}

		options.AddAdditionalAppiumOption("ms:waitForAppLaunch", 10);

		return new WindowsDriver(new Uri(AppiumPlatformResolver.ServerUrl()), options);
	}

	public By ByAutomationId(string automationId) => MobileBy.AccessibilityId(automationId);

	public string GetRole(IWebElement element)
	{
		// UIAutomation surfaces the control type as LocalizedControlType / ControlType.
		var localized = element.GetAttribute("LocalizedControlType");
		return string.IsNullOrWhiteSpace(localized) ? string.Empty : localized.ToLowerInvariant();
	}

	public string GetName(IWebElement element) => element.GetAttribute("Name") ?? string.Empty;

	public IReadOnlyList<IWebElement> GetAllDescendants(IWebDriver driver)
		=> driver.FindElements(By.XPath("//*"));

	public string GetAutomationId(IWebElement element)
		=> element.GetAttribute("AutomationId") ?? string.Empty;

	public string? GetValue(IWebElement element)
	{
		// UIA Value.Value via Appium maps to "Value.Value".
		var v = element.GetAttribute("Value.Value");
		if (!string.IsNullOrEmpty(v))
		{
			return v;
		}

		// Fallback to the legacy "value" attribute used by some control types.
		var legacy = element.GetAttribute("value");
		return string.IsNullOrEmpty(legacy) ? null : legacy;
	}

	public IReadOnlyList<string> GetSupportedPatterns(IWebElement element)
	{
		var patterns = new List<string>();
		AddIfTrue(element, "IsInvokePatternAvailable", "invoke", patterns);
		AddIfTrue(element, "IsTogglePatternAvailable", "toggle", patterns);
		AddIfTrue(element, "IsValuePatternAvailable", "value", patterns);
		AddIfTrue(element, "IsRangeValuePatternAvailable", "rangevalue", patterns);
		AddIfTrue(element, "IsExpandCollapsePatternAvailable", "expandcollapse", patterns);
		AddIfTrue(element, "IsSelectionPatternAvailable", "selection", patterns);
		AddIfTrue(element, "IsSelectionItemPatternAvailable", "selectionitem", patterns);
		AddIfTrue(element, "IsScrollPatternAvailable", "scroll", patterns);
		AddIfTrue(element, "IsTextPatternAvailable", "text", patterns);
		patterns.Sort(StringComparer.Ordinal);
		return patterns;
	}

	public IReadOnlyList<IWebElement> GetChildren(IWebDriver driver, IWebElement? parent)
	{
		var context = (ISearchContext?)parent ?? driver;
		return context.FindElements(By.XPath("./*"));
	}

	public IReadOnlyDictionary<string, string> GetExtras(IWebElement element)
	{
		var extras = new Dictionary<string, string>(StringComparer.Ordinal);
		AddIfPresent(element, "LocalizedControlType", "win32.LocalizedControlType", extras);
		AddIfPresent(element, "ControlType", "win32.ControlType", extras);
		AddIfPresent(element, "ClassName", "win32.ClassName", extras);
		return extras;
	}

	public void Dispose()
	{
	}

	private static void AddIfTrue(IWebElement element, string attr, string pattern, List<string> sink)
	{
		var v = element.GetAttribute(attr);
		if (string.Equals(v, "True", StringComparison.OrdinalIgnoreCase))
		{
			sink.Add(pattern);
		}
	}

	private static void AddIfPresent(IWebElement element, string attr, string key, Dictionary<string, string> sink)
	{
		var v = element.GetAttribute(attr);
		if (!string.IsNullOrEmpty(v))
		{
			sink[key] = v;
		}
	}
}
