#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Drives SamplesApp.Skia.WebAssembly.Browser via ChromeDriver against the
/// running browser. The Skia semantic DOM is the automation tree: Uno publishes
/// <c>xamlautomationid</c> on semantic elements, and roles/states flow from the
/// ARIA mapping done by Uno's Skia WebAssembly accessibility bridge.
/// </summary>
public sealed class WasmAdapter : IPlatformAdapter
{
	public AppiumPlatform Platform => AppiumPlatform.Wasm;

	public IWebDriver CreateDriver(AppiumTestOptions options, string sampleQuery)
	{
		var chromeOptions = new ChromeOptions();
		if (options.ChromeBinaryPath is { } binaryPath)
		{
			chromeOptions.BinaryLocation = binaryPath;
		}

		foreach (var argument in options.ChromeArguments)
		{
			chromeOptions.AddArgument(argument);
		}

		var baseUri = new Uri(options.AppPath.EndsWith("/", StringComparison.Ordinal)
			? options.AppPath
			: options.AppPath + "/");
		var startUri = string.IsNullOrEmpty(sampleQuery)
			? baseUri
			: new Uri(baseUri, "?" + sampleQuery);

		var driver = new RemoteWebDriver(options.ServerUri, chromeOptions.ToCapabilities(), options.Timeout);
		try
		{
			driver.Manage().Timeouts().PageLoad = options.Timeout;
			driver.Manage().Timeouts().AsynchronousJavaScript = options.Timeout;
			driver.Navigate().GoToUrl(startUri);
			EnableSemanticAccessibility(driver, options, startUri);
			return driver;
		}
		catch (Exception startupError)
		{
			var errors = new List<Exception> { startupError };
			try
			{
				driver.Quit();
			}
			catch (Exception quitError)
			{
				errors.Add(quitError);
			}

			try
			{
				driver.Dispose();
			}
			catch (Exception disposeError)
			{
				errors.Add(disposeError);
			}

			if (errors.Count > 1)
			{
				throw new AggregateException("WASM WebDriver startup and cleanup both failed.", errors);
			}

			throw;
		}
	}

	public By ByAutomationId(string automationId)
		=> By.CssSelector($"#uno-semantics-root [xamlautomationid=\"{EscapeCssAttribute(automationId)}\"]");

	public void Activate(IWebDriver driver, IWebElement element)
		=> ((IJavaScriptExecutor)driver).ExecuteScript(
			"arguments[0].scrollIntoView({ block: 'center', inline: 'nearest' }); arguments[0].focus(); arguments[0].click();",
			element);

	public void EnterText(IWebDriver driver, IWebElement element, string value)
	{
		((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].focus();", element);
		element.SendKeys(value);
	}

	public string GetRole(IWebElement element)
	{
		var role = element.GetAttribute("role");
		if (!string.IsNullOrWhiteSpace(role))
		{
			return role.ToLowerInvariant();
		}

		var tagName = element.TagName.ToLowerInvariant();
		var type = element.GetAttribute("type")?.ToLowerInvariant();

		return tagName switch
		{
			"input" => type switch
			{
				"checkbox" => "checkbox",
				"radio" => "radio",
				"range" => "slider",
				"search" => "searchbox",
				_ => "textbox",
			},
			"textarea" => "textbox.multiline",
			"select" => "combobox",
			"h1" or "h2" or "h3" or "h4" or "h5" or "h6" => "heading",
			_ => tagName,
		};
	}

	public string GetName(IWebDriver driver, IWebElement element)
		=> ExecuteScript(driver, @"
const element = arguments[0];
const ariaLabel = element.getAttribute('aria-label');
if (ariaLabel && ariaLabel.trim().length > 0) {
	return ariaLabel.trim();
}
const labelledBy = element.getAttribute('aria-labelledby');
if (labelledBy && labelledBy.trim().length > 0) {
	return labelledBy
		.split(/\s+/)
		.map(id => document.getElementById(id)?.textContent?.trim() ?? '')
		.filter(text => text.length > 0)
		.join(' ')
		.trim();
}
if ('value' in element && element.value) {
	return String(element.value).trim();
}
return (element.textContent || '').trim();
", element) ?? string.Empty;

	public string? GetDescription(IWebDriver driver, IWebElement element)
		=> EmptyToNull(ExecuteScript(driver, @"
const element = arguments[0];
const ariaDescription = element.getAttribute('aria-description');
if (ariaDescription && ariaDescription.trim().length > 0) {
	return ariaDescription.trim();
}
const describedBy = element.getAttribute('aria-describedby');
if (describedBy && describedBy.trim().length > 0) {
	return describedBy
		.split(/\s+/)
		.map(id => document.getElementById(id)?.textContent?.trim() ?? '')
		.filter(text => text.length > 0)
		.join(' ')
		.trim();
}
if (element.hasAttribute('placeholder')) {
	return (element.getAttribute('placeholder') || '').trim();
}
return '';
", element));

	public IReadOnlyList<IWebElement> GetAllDescendants(IWebDriver driver)
		=> driver.FindElements(By.CssSelector("#uno-semantics-root [id^=\"uno-semantics-\"]"));

	public string GetAutomationId(IWebElement element)
		=> element.GetAttribute("xamlautomationid") ?? string.Empty;

	public string? GetValue(IWebElement element)
	{
		var value = element.GetAttribute("value");
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}

		var ariaValue = element.GetAttribute("aria-valuetext")
			?? element.GetAttribute("aria-valuenow");
		return EmptyToNull(ariaValue)
			?? EmptyToNull(element.GetDomProperty("textContent"))
			?? EmptyToNull(element.Text);
	}

	public IReadOnlyList<string> GetSupportedPatterns(IWebElement element)
	{
		var patterns = new List<string>();
		switch (CanonicalRole.Normalize(GetRole(element), Platform))
		{
			case "button":
				patterns.Add("invoke");
				break;
			case "checkbox":
			case "switch":
				patterns.Add("toggle");
				break;
			case "radio":
				patterns.Add("selectionitem");
				break;
			case "textbox":
				patterns.Add("value");
				break;
			case "slider":
				patterns.Add("rangevalue");
				break;
			case "combobox":
				patterns.Add("expandcollapse");
				patterns.Add("selection");
				break;
		}

		patterns.Sort(StringComparer.Ordinal);
		return patterns;
	}

	public bool? GetEnabled(IWebElement element)
	{
		var ariaDisabled = element.GetAttribute("aria-disabled");
		if (ParseBool(ariaDisabled) is { } fromAria)
		{
			return !fromAria;
		}

		return element.GetAttribute("disabled") is null;
	}

	public bool? GetKeyboardFocusable(IWebElement element)
	{
		var tabIndex = element.GetAttribute("tabindex");
		if (int.TryParse(tabIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
		{
			return parsed >= 0;
		}

		return CanonicalRole.Normalize(GetRole(element), Platform) switch
		{
			"button" or "checkbox" or "combobox" or "radio" or "slider" or "textbox" => true,
			_ => false,
		};
	}

	public bool? GetFocused(IWebDriver driver, IWebElement element)
		=> ParseBool(ExecuteScript(driver, "return document.activeElement === arguments[0] ? 'true' : 'false';", element));

	public bool? GetOffscreen(IWebElement element)
	{
		if (ParseBool(element.GetAttribute("aria-hidden")) is { } hidden && hidden)
		{
			return true;
		}

		return null;
	}

	public string? GetToggleState(IWebElement element)
	{
		var ariaChecked = element.GetAttribute("aria-checked");
		if (!string.IsNullOrWhiteSpace(ariaChecked))
		{
			return NormalizeToggleState(ariaChecked);
		}

		var @checked = element.GetAttribute("checked");
		var normalized = NormalizeToggleState(@checked);
		if (normalized is not null)
		{
			return normalized;
		}

		return element.TagName.Equals("input", StringComparison.OrdinalIgnoreCase)
			&& element.GetAttribute("type") is "checkbox" or "radio"
				? element.Selected ? "on" : "off"
				: null;
	}

	public bool? GetSelected(IWebElement element)
	{
		var selected = ParseBool(element.GetAttribute("aria-selected"));
		if (selected is not null)
		{
			return selected;
		}

		if (ParseBool(element.GetAttribute("selected")) is { } selectedAttribute)
		{
			return selectedAttribute;
		}

		if (CanonicalRole.Normalize(GetRole(element), Platform) is "radio" or "option")
		{
			return element.Selected;
		}

		var toggleState = GetToggleState(element);
		return toggleState is null ? null : toggleState == "on";
	}

	public bool? GetExpanded(IWebElement element)
		=> ParseBool(element.GetAttribute("aria-expanded"));

	public bool? GetRequired(IWebElement element)
	{
		var required = ParseBool(element.GetAttribute("aria-required"));
		if (required is not null)
		{
			return required;
		}

		return element.GetAttribute("required") is null ? null : true;
	}

	public int? GetLevel(IWebElement element)
	{
		var ariaLevel = element.GetAttribute("aria-level");
		if (int.TryParse(ariaLevel, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
		{
			return parsed;
		}

		return element.TagName.ToLowerInvariant() switch
		{
			"h1" => 1,
			"h2" => 2,
			"h3" => 3,
			"h4" => 4,
			"h5" => 5,
			"h6" => 6,
			_ => null,
		};
	}

	public string? GetLandmark(IWebElement element)
		=> element.GetAttribute("role")?.Trim().ToLowerInvariant() switch
		{
			"banner" => "banner",
			"complementary" => "complementary",
			"contentinfo" => "contentinfo",
			"form" => "form",
			"main" => "main",
			"navigation" => "navigation",
			"region" => "region",
			"search" => "search",
			_ => null,
		};

	public string? GetRoleDescription(IWebElement element)
		=> EmptyToNull(element.GetAttribute("aria-roledescription"));

	public string? GetLiveSetting(IWebElement element)
		=> element.GetAttribute("aria-live")?.Trim().ToLowerInvariant() switch
		{
			"polite" => "polite",
			"assertive" => "assertive",
			_ => null,
		};

	public IReadOnlyList<IWebElement> GetChildren(IWebDriver driver, IWebElement? parent)
	{
		var context = (ISearchContext?)parent ?? driver;
		return parent is null
			? context.FindElements(By.CssSelector("#uno-semantics-root > [id^=\"uno-semantics-\"]"))
			: context.FindElements(By.CssSelector(":scope > [id^=\"uno-semantics-\"]"));
	}

	public IReadOnlyDictionary<string, string> GetExtras(IWebElement element)
	{
		var extras = new Dictionary<string, string>(StringComparer.Ordinal);
		AddIfPresent(element, "role", "wasm.role", extras);
		AddIfPresent(element, "tag", "wasm.tag", extras);
		AddIfPresent(element, "aria-label", "wasm.aria-label", extras);
		AddIfPresent(element, "xamlautomationid", "wasm.xamlAutomationId", extras);
		AddIfPresent(element, "aria-live", "wasm.aria-live", extras);
		AddIfPresent(element, "aria-level", "wasm.aria-level", extras);
		AddIfPresent(element, "aria-required", "wasm.aria-required", extras);
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

	private static void EnableSemanticAccessibility(IWebDriver driver, AppiumTestOptions options, Uri startUri)
	{
		var deadline = DateTime.UtcNow + options.Timeout;
		var retryAt = DateTime.UtcNow + TimeSpan.FromSeconds(Math.Min(10, options.Timeout.TotalSeconds / 2));
		Exception? lastError = null;
		string? lastState = null;
		var navigationRetryAttempted = false;

		while (DateTime.UtcNow < deadline)
		{
			try
			{
				var state = ExecuteScript(driver, @"
const button = document.getElementById('uno-enable-accessibility');
if (button) {
	button.click();
}
if (document.getElementById('uno-semantics-root')) {
	return 'ready';
}
return document.readyState === 'complete' &&
	!button &&
	document.querySelector('canvas') === null
		? 'bootstrap-missing'
		: 'waiting';
");
				lastState = state;

				if (string.Equals(state, "ready", StringComparison.Ordinal))
				{
					WaitForSemanticTreeToSettle(driver, options, deadline);
					return;
				}

				if (!navigationRetryAttempted &&
					string.Equals(state, "bootstrap-missing", StringComparison.Ordinal) &&
					DateTime.UtcNow >= retryAt)
				{
					navigationRetryAttempted = true;
					driver.Navigate().GoToUrl(startUri);
					continue;
				}
			}
			catch (WebDriverException ex)
			{
				lastError = ex;
			}

			Thread.Sleep(options.PollInterval);
		}

		string pageState;
		try
		{
			pageState = ExecuteScript(driver, """
				const button = document.getElementById('uno-enable-accessibility');
				return JSON.stringify({
					url: window.location.href,
					readyState: document.readyState,
					bodyChildCount: document.body?.childElementCount ?? -1,
					canvasPresent: document.querySelector('canvas') !== null,
					enableButtonPresent: button !== null,
					enableButtonDisabled: button?.getAttribute('aria-disabled') ?? null,
					semanticsRootPresent: document.getElementById('uno-semantics-root') !== null
				});
				""") ?? "unavailable";
		}
		catch (WebDriverException diagnosticError)
		{
			pageState = $"unavailable ({diagnosticError.Message})";
		}

		throw new InvalidOperationException(
			$"Timed out enabling the Skia semantic DOM after {options.Timeout.TotalSeconds:F0}s. " +
			$"Navigation retry attempted: {navigationRetryAttempted}. Last state: {lastState ?? "n/a"}. " +
			$"Page state: {pageState}. Last error: {lastError?.Message ?? "n/a"}");
	}

	private static string? ExecuteScript(IWebDriver driver, string script, IWebElement? element = null)
	{
		var executor = (IJavaScriptExecutor)driver;
		return element is null
			? executor.ExecuteScript(script) as string
			: executor.ExecuteScript(script, element) as string;
	}

	/// <summary>
	/// Blocks until the semantic DOM stops changing, so callers observe a fully populated tree
	/// rather than the first frame the runtime happened to publish. The signature is the element
	/// count plus the concatenated ids, which changes whenever a node is added, removed, or
	/// re-keyed; two identical consecutive readings mean the runtime finished its build pass.
	/// </summary>
	private static void WaitForSemanticTreeToSettle(IWebDriver driver, AppiumTestOptions options, DateTime deadline)
	{
		const string signatureScript = """
			const root = document.getElementById('uno-semantics-root');
			if (!root) {
				return '';
			}
			const nodes = root.querySelectorAll('[id^="uno-semantics-"]');
			return nodes.length + ':' + Array.from(nodes, node => node.id).join(',');
			""";

		string? previousSignature = null;

		while (DateTime.UtcNow < deadline)
		{
			string? signature;
			try
			{
				signature = ExecuteScript(driver, signatureScript);
			}
			catch (WebDriverException)
			{
				// The document can be swapped while the app finishes booting; retry until the deadline.
				signature = null;
			}

			if (!string.IsNullOrEmpty(signature) && string.Equals(signature, previousSignature, StringComparison.Ordinal))
			{
				return;
			}

			previousSignature = signature;
			Thread.Sleep(options.PollInterval);
		}
	}

	private static bool? ParseBool(string? value)
		=> value?.Trim().ToLowerInvariant() switch
		{
			"true" or "1" => true,
			"false" or "0" => false,
			_ => null,
		};

	private static string? NormalizeToggleState(string? value)
		=> value?.Trim().ToLowerInvariant() switch
		{
			"true" or "1" or "on" => "on",
			"false" or "0" or "off" => "off",
			"mixed" => "mixed",
			_ => null,
		};

	internal static string EscapeCssAttribute(string value)
		=> value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("]", "\\]");

	private static string? EmptyToNull(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
