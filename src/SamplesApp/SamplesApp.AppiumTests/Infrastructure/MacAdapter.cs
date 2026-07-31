#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Drives SamplesApp.Skia.Generic on macOS via the Appium Mac2 driver, which
/// forwards to the native NSAccessibility tree built by
/// Uno.UI.Runtime.Skia.MacOS (see UNOAccessibilityElement + MacOSAccessibility.cs).
/// </summary>
public sealed class MacAdapter : IPlatformAdapter
{
	private const string WrapperBundleId = "io.platform.uno.SamplesAppAppium";

	private string? _wrapperBundlePath;
	private string? _startedBundleId;
	private bool _keepWrapperBundle;

	public AppiumPlatform Platform => AppiumPlatform.Mac;

	public IWebDriver CreateDriver(AppiumTestOptions options, string sampleQuery)
	{
		var appiumOptions = new AppiumOptions
		{
			AutomationName = "Mac2",
			PlatformName = "Mac",
		};

		var appPath = Path.GetFullPath(options.AppPath);

		string bundleId;
		if (IsAppBundle(appPath))
		{
			bundleId = ReadBundleId(appPath)
				?? throw new InvalidOperationException(
					$"App bundle at '{appPath}' is missing CFBundleIdentifier in Info.plist.");
			if (!IsBundleRunning(bundleId))
			{
				LaunchAppBundle(appPath, sampleQuery);
				_startedBundleId = bundleId;
			}
		}
		else
		{
			bundleId = WrapperBundleId;
			if (!IsBundleRunning(bundleId))
			{
				_wrapperBundlePath = CreateWrapperBundle(options.ArtifactsDirectory, appPath, sampleQuery);
				LaunchWrapperBundle(_wrapperBundlePath);
				_startedBundleId = WrapperBundleId;
			}
		}

		_keepWrapperBundle = options.KeepMacBundle;
		WaitForBundleRunning(bundleId, options.Timeout);

		appiumOptions.AddAdditionalAppiumOption("bundleId", bundleId);
		appiumOptions.AddAdditionalAppiumOption("noReset", true);

		return new MacDriver(options.ServerUri, appiumOptions, options.Timeout);
	}

	private static void AddIfPresent(IWebElement element, string attr, string key, Dictionary<string, string> sink)
	{
		var v = element.GetAttribute(attr);
		if (!string.IsNullOrEmpty(v))
		{
			sink[key] = v;
		}
	}

	private static string? ReadBundleId(string appBundle)
	{
		var plist = Path.Combine(appBundle, "Contents", "Info.plist");
		if (!File.Exists(plist))
		{
			return null;
		}

		var result = RunProcess(
			"/usr/bin/defaults",
			new[] { "read", plist, "CFBundleIdentifier" },
			TimeSpan.FromSeconds(10));
		return string.IsNullOrWhiteSpace(result.StandardOutput) ? null : result.StandardOutput;
	}

	private static void LaunchAppBundle(string bundlePath, string sampleQuery)
	{
		var args = new List<string> { "-n", "-a", bundlePath };
		if (!string.IsNullOrEmpty(sampleQuery))
		{
			args.Add("--args");
			args.Add(sampleQuery);
		}

		RunProcess("/usr/bin/open", args, TimeSpan.FromSeconds(10));
	}

	private static void LaunchWrapperBundle(string bundlePath)
		=> RunProcess("/usr/bin/open", new[] { "-n", bundlePath }, TimeSpan.FromSeconds(10));

	private static ProcessResult RunProcess(string fileName, IEnumerable<string> arguments, TimeSpan timeout)
	{
		var startInfo = new ProcessStartInfo(fileName)
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = Process.Start(startInfo)
			?? throw new InvalidOperationException($"Failed to start '{fileName}'.");

		if (!process.WaitForExit(timeout))
		{
			process.Kill(entireProcessTree: true);
			throw new TimeoutException($"Process '{fileName}' did not exit within {timeout.TotalSeconds:F0}s.");
		}

		var result = new ProcessResult(
			process.ExitCode,
			process.StandardOutput.ReadToEnd().Trim(),
			process.StandardError.ReadToEnd().Trim());

		if (result.ExitCode != 0)
		{
			throw new InvalidOperationException(
				$"Process '{fileName}' exited with code {result.ExitCode}. stdout='{result.StandardOutput}' stderr='{result.StandardError}'.");
		}

		return result;
	}

	private static void WaitForBundleRunning(string bundleId, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		var attempts = 0;
		while (DateTime.UtcNow < deadline)
		{
			attempts++;
			if (IsBundleRunning(bundleId))
			{
				// Give the runtime a moment to publish its accessibility tree.
				Thread.Sleep(TimeSpan.FromSeconds(2));
				return;
			}
			Thread.Sleep(500);
		}

		throw new InvalidOperationException(
			$"App with bundle id '{bundleId}' did not start within {timeout.TotalSeconds:F0}s ({attempts} polls).");
	}

	private static bool IsBundleRunning(string bundleId)
	{
		var result = RunProcess(
			"/usr/bin/osascript",
			new[] { "-e", $"tell application \"System Events\" to (bundle identifier of every process) contains \"{bundleId}\"" },
			TimeSpan.FromSeconds(10));
		return string.Equals(result.StandardOutput, "true", StringComparison.OrdinalIgnoreCase);
	}

	public By ByAutomationId(string automationId)
	{
		var literal = ToXPathLiteral(automationId);
		return By.XPath($"//*[@identifier={literal} or @AXIdentifier={literal}]");
	}

	public void Activate(IWebDriver driver, IWebElement element) => element.Click();

	public void EnterText(IWebDriver driver, IWebElement element, string value)
	{
		element.Click();
		element.SendKeys(value);
	}

	public string GetRole(IWebElement element)
		=> GetAttributeAny(element, "AXRole", "role", "elementType") ?? element.TagName;

	public string GetName(IWebDriver driver, IWebElement element)
		=> GetAttributeAny(element, "AXTitle", "title", "label", "AXDescription", "description") ?? string.Empty;

	public string? GetDescription(IWebDriver driver, IWebElement element)
		=> EmptyToNull(GetAttributeAny(element, "AXDescription", "description", "placeholderValue"));

	public IReadOnlyList<IWebElement> GetAllDescendants(IWebDriver driver)
		=> driver.FindElements(By.XPath("//*"));

	public string GetAutomationId(IWebElement element)
		=> GetAttributeAny(element, "AXIdentifier", "identifier") ?? string.Empty;

	public string? GetValue(IWebElement element)
	{
		var v = GetAttributeAny(element, "AXValue", "value");
		if (!string.IsNullOrEmpty(v))
		{
			return v;
		}

		var placeholder = GetAttributeAny(element, "placeholderValue");
		return string.IsNullOrEmpty(placeholder) ? null : placeholder;
	}

	public IReadOnlyList<string> GetSupportedPatterns(IWebElement element)
	{
		var patterns = new List<string>();
		var role = CanonicalRole.Normalize(GetRole(element), Platform, GetLevel(element), GetLandmark(element));
		switch (role)
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
		=> ParseBool(GetAttributeAny(element, "AXEnabled", "enabled"));

	public bool? GetKeyboardFocusable(IWebElement element)
		=> null;

	public bool? GetFocused(IWebDriver driver, IWebElement element)
		=> ParseBool(GetAttributeAny(element, "AXFocused", "focused"));

	public bool? GetOffscreen(IWebElement element)
		=> ParseBool(GetAttributeAny(element, "AXHidden", "hidden"));

	public string? GetToggleState(IWebElement element)
	{
		var value = GetAttributeAny(element, "AXValue", "value");
		if (string.IsNullOrWhiteSpace(value))
		{
			value = GetAttributeAny(element, "AXSelected", "selected");
		}

		return NormalizeToggleState(value);
	}

	public bool? GetSelected(IWebElement element)
	{
		var selected = ParseBool(GetAttributeAny(element, "AXSelected", "selected"));
		if (selected is not null)
		{
			return selected;
		}

		var toggleState = GetToggleState(element);
		return toggleState is null ? null : toggleState == "on";
	}

	public bool? GetExpanded(IWebElement element)
		=> ParseBool(GetAttributeAny(element, "AXExpanded", "expanded"));

	public bool? GetRequired(IWebElement element)
		=> ParseBool(GetAttributeAny(element, "AXRequired", "required"));

	public int? GetLevel(IWebElement element)
		=> ParseInt(GetAttributeAny(element, "AXLevel", "AXDOMHeadingLevel", "level"));

	public string? GetLandmark(IWebElement element)
	{
		var roleDescription = NormalizeLandmark(GetAttributeAny(element, "AXSubrole", "AXRoleDescription", "roleDescription"));
		return roleDescription;
	}

	public string? GetRoleDescription(IWebElement element)
	{
		var roleDescription = EmptyToNull(GetAttributeAny(element, "AXRoleDescription", "roleDescription"));
		if (roleDescription is null)
		{
			return null;
		}

		return NormalizeLandmark(roleDescription) is null
			? roleDescription
			: null;
	}

	public string? GetLiveSetting(IWebElement element)
		=> NormalizeLiveSetting(GetAttributeAny(element, "AXLiveRegionPoliteness", "AXARIALive", "aria-live"));

	public IReadOnlyList<IWebElement> GetChildren(IWebDriver driver, IWebElement? parent)
	{
		var context = (ISearchContext?)parent ?? driver;
		return context.FindElements(By.XPath("./*"));
	}

	public IReadOnlyDictionary<string, string> GetExtras(IWebElement element)
	{
		var extras = new Dictionary<string, string>(StringComparer.Ordinal);
		AddIfPresent(element, "AXRole", "macos.AXRole", extras);
		AddIfPresent(element, "AXSubrole", "macos.AXSubrole", extras);
		AddIfPresent(element, "AXRoleDescription", "macos.AXRoleDescription", extras);
		AddIfPresent(element, "identifier", "macos.identifier", extras);
		AddIfPresent(element, "label", "macos.label", extras);
		AddIfPresent(element, "title", "macos.title", extras);
		return extras;
	}

	public void Dispose()
	{
		var errors = new List<Exception>();

		if (_startedBundleId is not null && IsBundleRunning(_startedBundleId))
		{
			try
			{
				TerminateBundle(_startedBundleId);
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
		}

		if (_wrapperBundlePath is not null
			&& Directory.Exists(_wrapperBundlePath)
			&& !_keepWrapperBundle)
		{
			try
			{
				Directory.Delete(_wrapperBundlePath, recursive: true);
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
		}

		_startedBundleId = null;
		_wrapperBundlePath = null;
		_keepWrapperBundle = false;

		if (errors.Count == 1)
		{
			throw errors[0];
		}

		if (errors.Count > 1)
		{
			throw new AggregateException("One or more macOS Appium cleanup operations failed.", errors);
		}
	}

	private static void TerminateBundle(string bundleId)
	{
		RunProcess(
			"/usr/bin/osascript",
			new[] { "-e", $"tell application id \"{bundleId}\" to quit" },
			TimeSpan.FromSeconds(10));
	}

	private static string CreateWrapperBundle(string artifactsDirectory, string dllPath, string sampleQuery)
	{
		var dotnetPath = ResolveDotnet();
		var slug = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
		var bundleRoot = Path.Combine(
			artifactsDirectory,
			"mac-bundles",
			$"SamplesAppAppium-{slug}.app");
		var contents = Path.Combine(bundleRoot, "Contents");
		var macOS = Path.Combine(contents, "MacOS");
		Directory.CreateDirectory(macOS);

		const string executableName = "SamplesAppAppium";
		const string bundleId = "io.platform.uno.SamplesAppAppium";

		File.WriteAllText(Path.Combine(contents, "Info.plist"),
			$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>CFBundleExecutable</key><string>{executableName}</string>
	<key>CFBundleIdentifier</key><string>{bundleId}</string>
	<key>CFBundleName</key><string>SamplesApp Appium Wrapper</string>
	<key>CFBundlePackageType</key><string>APPL</string>
	<key>CFBundleVersion</key><string>1.0</string>
	<key>CFBundleShortVersionString</key><string>1.0</string>
	<key>NSPrincipalClass</key><string>NSApplication</string>
	<key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
");

		var script = string.IsNullOrEmpty(sampleQuery)
			? $@"#!/bin/bash
exec ""{dotnetPath}"" ""{dllPath}""
"
			: $@"#!/bin/bash
exec ""{dotnetPath}"" ""{dllPath}"" ""{sampleQuery}""
";
		var executablePath = Path.Combine(macOS, executableName);
		File.WriteAllText(executablePath, script);
		File.SetUnixFileMode(executablePath,
			UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
			UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
			UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

		return bundleRoot;
	}

	private static bool IsAppBundle(string path)
		=> path.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
			|| path.TrimEnd('/').EndsWith(".app", StringComparison.OrdinalIgnoreCase);

	private static string ResolveDotnet()
	{
		var env = Environment.GetEnvironmentVariable("UNO_APPIUM_DOTNET_PATH");
		if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
		{
			return env;
		}

		foreach (var candidate in new[]
		{
			"/opt/homebrew/bin/dotnet",
			"/usr/local/share/dotnet/dotnet",
			"/usr/local/bin/dotnet",
			"/usr/bin/dotnet",
		})
		{
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		return "dotnet";
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
		=> int.TryParse(value, out var parsed) ? parsed : null;

	private static string? NormalizeToggleState(string? value)
		=> value?.Trim().ToLowerInvariant() switch
		{
			"true" or "1" or "on" => "on",
			"false" or "0" or "off" => "off",
			"mixed" => "mixed",
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

	private static string ToXPathLiteral(string value)
	{
		if (!value.Contains('\''))
		{
			return $"'{value}'";
		}

		if (!value.Contains('"'))
		{
			return $"\"{value}\"";
		}

		var parts = value.Split('\'');
		return "concat('" + string.Join("', \"'\", '", parts) + "')";
	}

	private readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
