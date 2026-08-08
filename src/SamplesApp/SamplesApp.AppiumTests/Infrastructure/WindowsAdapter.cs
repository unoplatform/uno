#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
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

	public IWebDriver CreateDriver(AppiumTestOptions options, string sampleQuery)
	{
		var appiumOptions = new AppiumOptions
		{
			AutomationName = "Windows",
			PlatformName = "Windows",
		};

		appiumOptions.AddAdditionalAppiumOption("app", options.AppPath);

		if (!string.IsNullOrEmpty(sampleQuery))
		{
			appiumOptions.AddAdditionalAppiumOption("appArguments", sampleQuery);
		}

		appiumOptions.AddAdditionalAppiumOption("ms:waitForAppLaunch", (int)Math.Ceiling(options.Timeout.TotalSeconds));

		return new WindowsDriver(options.ServerUri, appiumOptions, options.Timeout);
	}

	public By ByAutomationId(string automationId) => MobileBy.AccessibilityId(automationId);

	public void Activate(IWebDriver driver, IWebElement element) => element.Click();

	public void EnterText(IWebDriver driver, IWebElement element, string value)
	{
		element.Click();
		element.SendKeys(value);
	}

	public string GetRole(IWebElement element) => GetAttributeAny(element, "LocalizedControlType", "ControlType") ?? string.Empty;

	public string GetName(IWebDriver driver, IWebElement element) => GetAttributeAny(element, "Name") ?? string.Empty;

	public string? GetDescription(IWebDriver driver, IWebElement element)
		=> EmptyToNull(GetAttributeAny(element, "FullDescription", "HelpText"));

	public IReadOnlyList<IWebElement> GetAllDescendants(IWebDriver driver)
		=> driver.FindElements(By.XPath("//*"));

	public string GetAutomationId(IWebElement element)
		=> GetAttributeAny(element, "AutomationId") ?? string.Empty;

	public string? GetValue(IWebElement element)
		=> EmptyToNull(GetAttributeAny(element, "Value.Value", "RangeValue.Value", "value"));

	public IReadOnlyList<string> GetSupportedPatterns(IWebElement element)
	{
		var patterns = new List<string>();
		var role = CanonicalRole.Normalize(GetRole(element), Platform, GetLevel(element), GetLandmark(element));

		if (role == "button" && ParseBool(GetAttributeAny(element, "IsInvokePatternAvailable")) == true)
		{
			patterns.Add("invoke");
		}

		if ((role == "checkbox" || role == "switch") && ParseBool(GetAttributeAny(element, "IsTogglePatternAvailable")) == true)
		{
			patterns.Add("toggle");
		}

		if (role == "radio"
			&& (ParseBool(GetAttributeAny(element, "IsSelectionItemPatternAvailable")) == true
				|| ParseBool(GetAttributeAny(element, "IsTogglePatternAvailable")) == true))
		{
			patterns.Add("selectionitem");
		}

		if (role == "textbox" && ParseBool(GetAttributeAny(element, "IsValuePatternAvailable")) == true)
		{
			patterns.Add("value");
		}

		if (role == "slider" && ParseBool(GetAttributeAny(element, "IsRangeValuePatternAvailable")) == true)
		{
			patterns.Add("rangevalue");
		}

		if (role == "combobox")
		{
			if (ParseBool(GetAttributeAny(element, "IsExpandCollapsePatternAvailable")) == true)
			{
				patterns.Add("expandcollapse");
			}

			if (ParseBool(GetAttributeAny(element, "IsSelectionPatternAvailable")) == true
				|| ParseBool(GetAttributeAny(element, "IsSelectionItemPatternAvailable")) == true)
			{
				patterns.Add("selection");
			}
		}

		patterns.Sort(StringComparer.Ordinal);
		return patterns;
	}

	public bool? GetEnabled(IWebElement element)
		=> ParseBool(GetAttributeAny(element, "IsEnabled"));

	public bool? GetKeyboardFocusable(IWebElement element)
		=> ParseBool(GetAttributeAny(element, "IsKeyboardFocusable"));

	public bool? GetFocused(IWebDriver driver, IWebElement element)
		=> ParseBool(GetAttributeAny(element, "HasKeyboardFocus"));

	public bool? GetOffscreen(IWebElement element)
		=> ParseBool(GetAttributeAny(element, "IsOffscreen"));

	public string? GetToggleState(IWebElement element)
		=> ParseToggleState(GetAttributeAny(element, "Toggle.ToggleState", "ToggleState"));

	public bool? GetSelected(IWebElement element)
	{
		var selected = ParseBool(GetAttributeAny(element, "SelectionItem.IsSelected"));
		if (selected is not null)
		{
			return selected;
		}

		var toggleState = GetToggleState(element);
		return toggleState is null ? null : toggleState == "on";
	}

	public bool? GetExpanded(IWebElement element)
	{
		var state = GetAttributeAny(element, "ExpandCollapse.ExpandCollapseState", "ExpandCollapseState");
		return state?.Trim().ToLowerInvariant() switch
		{
			"expanded" or "partiallyexpanded" or "1" or "2" => true,
			"collapsed" or "0" => false,
			_ => null,
		};
	}

	public bool? GetRequired(IWebElement element)
		=> ParseBool(GetAttributeAny(element, "IsRequiredForForm"));

	public int? GetLevel(IWebElement element)
		=> ParseInt(GetAttributeAny(element, "Level"));

	public string? GetLandmark(IWebElement element)
	{
		var explicitLandmark = NormalizeLandmark(GetAttributeAny(element, "LandmarkType"));
		if (explicitLandmark is not null)
		{
			return explicitLandmark;
		}

		var localized = EmptyToNull(GetAttributeAny(element, "LocalizedLandmarkType"));
		if (localized is null)
		{
			return null;
		}

		return NormalizeLandmark(localized) ?? "custom";
	}

	public string? GetRoleDescription(IWebElement element)
	{
		var localizedLandmarkType = EmptyToNull(GetAttributeAny(element, "LocalizedLandmarkType"));
		if (localizedLandmarkType is null)
		{
			return null;
		}

		return NormalizeLandmark(localizedLandmarkType) is null
			? localizedLandmarkType
			: null;
	}

	public string? GetLiveSetting(IWebElement element)
		=> NormalizeLiveSetting(GetAttributeAny(element, "LiveSetting"));

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

	private static void AddIfPresent(IWebElement element, string attr, string key, Dictionary<string, string> sink)
	{
		var v = element.GetAttribute(attr);
		if (!string.IsNullOrEmpty(v))
		{
			sink[key] = v;
		}
	}

	private static string? GetAttributeAny(IWebElement element, params string[] names)
	{
		foreach (var name in names)
		{
			var value = element.GetAttribute(name);
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}

		return null;
	}

	private static bool? ParseBool(string? value)
		=> value?.Trim().ToLowerInvariant() switch
		{
			"true" or "1" => true,
			"false" or "0" => false,
			_ => null,
		};

	private static int? ParseInt(string? value)
		=> int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: null;

	private static string? ParseToggleState(string? value)
		=> value?.Trim().ToLowerInvariant() switch
		{
			"1" or "on" or "checked" or "true" => "on",
			"0" or "off" or "unchecked" or "false" => "off",
			"2" or "indeterminate" => "mixed",
			_ => null,
		};

	private static string? NormalizeLandmark(string? value)
		=> value?.Trim().ToLowerInvariant().Replace(" ", string.Empty) switch
		{
			"navigation" => "navigation",
			"search" => "search",
			"main" => "main",
			"form" => "form",
			"banner" => "banner",
			"contentinfo" => "contentinfo",
			"complementary" => "complementary",
			"region" => "region",
			"custom" => "custom",
			_ => null,
		};

	private static string? NormalizeLiveSetting(string? value)
		=> value?.Trim().ToLowerInvariant() switch
		{
			"polite" => "polite",
			"assertive" => "assertive",
			_ => null,
		};

	private static string? EmptyToNull(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
