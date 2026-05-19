#nullable enable

using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Remote;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Drives SamplesApp.Wasm via Appium's chromium driver against the running
/// browser. The DOM is the automation tree: AutomationProperties.AutomationId
/// is emitted as the <c>xamlname</c> attribute when SamplesApp.Wasm sets
/// <c>AssignDOMXamlName = true</c>, and roles flow from the ARIA mapping
/// done by Uno's AriaMapper.
/// </summary>
public sealed class WasmAdapter : IPlatformAdapter
{
	public AppiumPlatform Platform => AppiumPlatform.Wasm;

	public IWebDriver CreateDriver(string sampleQuery)
	{
		var options = new AppiumOptions
		{
			AutomationName = "Chromium",
			PlatformName = "Any",
			BrowserName = "Chrome",
		};

		var baseUrl = AppiumPlatformResolver.RequireAppPath().TrimEnd('/');
		var startUrl = string.IsNullOrEmpty(sampleQuery)
			? baseUrl
			: $"{baseUrl}/?{sampleQuery}";

		options.AddAdditionalAppiumOption("appium:initialBrowserUrl", startUrl);

		var driver = new RemoteWebDriver(new Uri(AppiumPlatformResolver.ServerUrl()), options);

		// Some chromium driver builds ignore initialBrowserUrl; navigate explicitly.
		driver.Navigate().GoToUrl(startUrl);
		return driver;
	}

	public By ByAutomationId(string automationId)
		=> By.CssSelector($"[xamlname=\"{Escape(automationId)}\"]");

	public string GetRole(IWebElement element)
	{
		// Uno's AriaMapper writes role= on the element.
		var role = element.GetAttribute("role");
		return string.IsNullOrWhiteSpace(role) ? element.TagName.ToLowerInvariant() : role.ToLowerInvariant();
	}

	public string GetName(IWebElement element)
	{
		var ariaLabel = element.GetAttribute("aria-label");
		if (!string.IsNullOrEmpty(ariaLabel))
		{
			return ariaLabel;
		}

		var text = element.Text;
		return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
	}

	public IReadOnlyList<IWebElement> GetAllDescendants(IWebDriver driver)
		=> driver.FindElements(By.CssSelector("[xamlname]"));

	public string GetAutomationId(IWebElement element)
		=> element.GetAttribute("xamlname") ?? string.Empty;

	public string? GetValue(IWebElement element)
	{
		var v = element.GetAttribute("value");
		if (!string.IsNullOrEmpty(v))
		{
			return v;
		}

		var ariaValue = element.GetAttribute("aria-valuenow");
		return string.IsNullOrEmpty(ariaValue) ? null : ariaValue;
	}

	public IReadOnlyList<string> GetSupportedPatterns(IWebElement element)
	{
		var patterns = new List<string>();
		var role = element.GetAttribute("role")?.ToLowerInvariant();
		switch (role)
		{
			case "button":
			case "menuitem":
			case "link":
				patterns.Add("invoke");
				break;
			case "checkbox":
			case "switch":
				patterns.Add("toggle");
				break;
			case "radio":
				patterns.Add("selectionitem");
				patterns.Add("toggle");
				break;
			case "textbox":
			case "searchbox":
				patterns.Add("value");
				break;
			case "slider":
			case "spinbutton":
			case "progressbar":
				patterns.Add("rangevalue");
				break;
			case "combobox":
			case "listbox":
				patterns.Add("expandcollapse");
				patterns.Add("selection");
				break;
		}

		var ariaExpanded = element.GetAttribute("aria-expanded");
		if (!string.IsNullOrEmpty(ariaExpanded) && !patterns.Contains("expandcollapse"))
		{
			patterns.Add("expandcollapse");
		}

		patterns.Sort(StringComparer.Ordinal);
		return patterns;
	}

	public IReadOnlyList<IWebElement> GetChildren(IWebDriver driver, IWebElement? parent)
	{
		var context = (ISearchContext?)parent ?? driver;
		// Only walk elements that participate in the Uno semantic overlay.
		return context.FindElements(By.CssSelector(":scope > [xamlname], :scope > [role]"));
	}

	public IReadOnlyDictionary<string, string> GetExtras(IWebElement element)
	{
		var extras = new Dictionary<string, string>(StringComparer.Ordinal);
		AddIfPresent(element, "role", "wasm.role", extras);
		AddIfPresent(element, "tag", "wasm.tag", extras);
		AddIfPresent(element, "aria-label", "wasm.aria-label", extras);
		return extras;
	}

	public void Dispose()
	{
	}

	private static void AddIfPresent(IWebElement element, string attr, string key, Dictionary<string, string> sink)
	{
		var v = attr == "tag" ? element.TagName : element.GetAttribute(attr);
		if (!string.IsNullOrEmpty(v))
		{
			sink[key] = v;
		}
	}

	private static string Escape(string value) => value.Replace("\"", "\\\"");
}
