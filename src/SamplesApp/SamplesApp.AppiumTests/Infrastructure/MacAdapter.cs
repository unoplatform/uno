#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Drives the SamplesApp desktop target on macOS via the Appium Mac2 driver, which
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
		_keepWrapperBundle = options.KeepMacBundle;
		var appiumOptions = new AppiumOptions
		{
			AutomationName = "Mac2",
			PlatformName = "Mac",
		};

		var appPath = Path.GetFullPath(options.AppPath);

		string bundleId;
		if (IsAppBundle(appPath))
		{
			var bundleIdValue = ReadBundleId(appPath, out var bundleIdDiagnostic);
			bundleId = bundleIdValue
				?? throw new InvalidOperationException(
					$"App bundle at '{appPath}' does not provide a readable CFBundleIdentifier. {bundleIdDiagnostic}");
			if (!IsBundleRunning(bundleId))
			{
				_startedBundleId = bundleId;
				LaunchAppBundle(appPath, sampleQuery);
			}
		}
		else
		{
			bundleId = WrapperBundleId;
			if (!IsBundleRunning(bundleId))
			{
				_wrapperBundlePath = CreateWrapperBundle(options.ArtifactsDirectory, appPath, sampleQuery);
				_startedBundleId = WrapperBundleId;
				LaunchWrapperBundle(_wrapperBundlePath);
			}
		}

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

	private static string? ReadBundleId(string appBundle, out string? diagnostic)
	{
		var plist = Path.Combine(appBundle, "Contents", "Info.plist");
		if (!File.Exists(plist))
		{
			diagnostic = $"Info.plist was not found at '{plist}'.";
			return null;
		}

		var result = RunProcess(
			"/usr/bin/defaults",
			new[] { "read", plist, "CFBundleIdentifier" },
			TimeSpan.FromSeconds(10),
			throwOnNonZeroExit: false);
		if (result.ExitCode != 0)
		{
			diagnostic =
				$"defaults exited with code {result.ExitCode}. stdout='{result.StandardOutput}' stderr='{result.StandardError}'.";
			return null;
		}

		if (string.IsNullOrWhiteSpace(result.StandardOutput))
		{
			diagnostic = "defaults returned an empty CFBundleIdentifier.";
			return null;
		}

		diagnostic = null;
		return result.StandardOutput;
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

	internal static ProcessResult RunProcess(
		string fileName,
		IEnumerable<string> arguments,
		TimeSpan timeout,
		bool throwOnNonZeroExit = true)
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

		var output = new StringBuilder();
		var error = new StringBuilder();
		var outputComplete = false;
		var errorComplete = false;
		var sync = new object();
		using var process = new Process { StartInfo = startInfo };
		process.OutputDataReceived += (_, args) =>
		{
			lock (sync)
			{
				if (args.Data is null)
				{
					outputComplete = true;
				}
				else
				{
					output.AppendLine(args.Data);
				}
				Monitor.PulseAll(sync);
			}
		};
		process.ErrorDataReceived += (_, args) =>
		{
			lock (sync)
			{
				if (args.Data is null)
				{
					errorComplete = true;
				}
				else
				{
					error.AppendLine(args.Data);
				}
				Monitor.PulseAll(sync);
			}
		};

		var elapsed = Stopwatch.StartNew();
		if (!process.Start())
		{
			throw new InvalidOperationException($"Failed to start '{fileName}'.");
		}
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		var completed = process.WaitForExit(timeout);
		if (completed)
		{
			lock (sync)
			{
				while (!outputComplete || !errorComplete)
				{
					var remaining = timeout - elapsed.Elapsed;
					if (remaining <= TimeSpan.Zero || !Monitor.Wait(sync, remaining))
					{
						completed = false;
						break;
					}
				}
			}
		}

		if (!completed)
		{
			var timeoutError = new TimeoutException(
				$"Process '{fileName}' did not exit and finish redirecting output within {timeout.TotalSeconds:F0}s.");
			try
			{
				if (!process.HasExited)
				{
					process.Kill(entireProcessTree: true);
					if (!process.WaitForExit(TimeSpan.FromSeconds(5)))
					{
						throw new TimeoutException($"Process '{fileName}' did not exit after termination.");
					}
				}
			}
			catch (Exception cleanupError) when (!AppiumExceptionPolicy.IsCritical(cleanupError))
			{
				throw new AggregateException("Process timeout and termination both failed.", timeoutError, cleanupError);
			}

			throw timeoutError;
		}

		var result = new ProcessResult(
			process.ExitCode,
			output.ToString().Trim(),
			error.ToString().Trim());

		if (throwOnNonZeroExit && result.ExitCode != 0)
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
		string? lastDiagnostic = null;
		while (DateTime.UtcNow < deadline)
		{
			attempts++;
			if (IsBundleRunning(bundleId, out var diagnostic))
			{
				// Give the runtime a moment to publish its accessibility tree.
				Thread.Sleep(TimeSpan.FromSeconds(2));
				return;
			}
			lastDiagnostic = diagnostic;
			Thread.Sleep(500);
		}

		var diagnosticSuffix = lastDiagnostic is null ? string.Empty : $" Last query error: {lastDiagnostic}";
		throw new InvalidOperationException(
			$"App with bundle id '{bundleId}' did not start within {timeout.TotalSeconds:F0}s ({attempts} polls).{diagnosticSuffix}");
	}

	private static bool IsBundleRunning(string bundleId, TimeSpan? timeout = null)
	{
		var running = IsBundleRunning(bundleId, out var diagnostic, timeout);
		if (diagnostic is not null)
		{
			throw new InvalidOperationException($"Unable to query app '{bundleId}': {diagnostic}");
		}
		return running;
	}

	private static bool IsBundleRunning(string bundleId, out string? diagnostic, TimeSpan? timeout = null)
	{
		var result = RunProcess(
			"/usr/bin/osascript",
			new[] { "-e", $"tell application \"System Events\" to (bundle identifier of every process) contains {ToAppleScriptStringLiteral(bundleId)}" },
			timeout ?? TimeSpan.FromSeconds(10),
			throwOnNonZeroExit: false);
		if (result.ExitCode != 0)
		{
			diagnostic = $"osascript exited with code {result.ExitCode}. stdout='{result.StandardOutput}' stderr='{result.StandardError}'.";
			return false;
		}

		if (bool.TryParse(result.StandardOutput, out var running))
		{
			diagnostic = null;
			return running;
		}

		diagnostic = $"osascript returned '{result.StandardOutput}' instead of true or false.";
		return false;
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

		if (_startedBundleId is not null)
		{
			try
			{
				if (IsBundleRunning(_startedBundleId))
				{
					TerminateBundle(_startedBundleId);
				}
			}
			catch (Exception ex) when (!AppiumExceptionPolicy.IsCritical(ex))
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
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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
		var timeout = TimeSpan.FromSeconds(10);
		RunProcess(
			"/usr/bin/osascript",
			new[] { "-e", $"tell application id {ToAppleScriptStringLiteral(bundleId)} to quit" },
			timeout);

		var elapsed = Stopwatch.StartNew();
		while (true)
		{
			var remaining = timeout - elapsed.Elapsed;
			if (remaining <= TimeSpan.Zero)
			{
				throw new TimeoutException($"App '{bundleId}' did not terminate after its quit request.");
			}
			if (!IsBundleRunning(bundleId, remaining))
			{
				return;
			}
			Thread.Sleep(TimeSpan.FromMilliseconds(100));
		}
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

		File.WriteAllText(Path.Combine(contents, "Info.plist"),
			$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>CFBundleExecutable</key><string>{executableName}</string>
	<key>CFBundleIdentifier</key><string>{WrapperBundleId}</string>
	<key>CFBundleName</key><string>SamplesApp Appium Wrapper</string>
	<key>CFBundlePackageType</key><string>APPL</string>
	<key>CFBundleVersion</key><string>1.0</string>
	<key>CFBundleShortVersionString</key><string>1.0</string>
	<key>NSPrincipalClass</key><string>NSApplication</string>
	<key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
");

		var script = CreateWrapperScript(dotnetPath, dllPath, sampleQuery);
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

	private static string ToAppleScriptStringLiteral(string value)
		=> $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

	internal static string CreateWrapperScript(string dotnetPath, string dllPath, string sampleQuery)
	{
		var command = $"exec {BashSingleQuote(dotnetPath)} {BashSingleQuote(dllPath)}";
		if (!string.IsNullOrEmpty(sampleQuery))
		{
			command += $" {BashSingleQuote(sampleQuery)}";
		}

		return $"#!/bin/bash\n{command}\n";
	}

	private static string BashSingleQuote(string value)
		=> $"'{value.Replace("'", "'\\''")}'";

	internal readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
