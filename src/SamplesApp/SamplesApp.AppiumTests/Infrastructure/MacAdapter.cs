#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
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
	private Process? _spawnedApp;

	public AppiumPlatform Platform => AppiumPlatform.Mac;

	public IWebDriver CreateDriver(string sampleQuery)
	{
		var options = new AppiumOptions
		{
			AutomationName = "Mac2",
			PlatformName = "Mac",
		};

		var appPath = Path.GetFullPath(AppiumPlatformResolver.RequireAppPath());

		string bundleId;
		if (IsAppBundle(appPath))
		{
			bundleId = ReadBundleId(appPath)
				?? throw new InvalidOperationException(
					$"App bundle at '{appPath}' is missing CFBundleIdentifier in Info.plist.");
			if (!IsBundleRunning(bundleId))
			{
				LaunchAppBundle(appPath, sampleQuery);
			}
		}
		else
		{
			bundleId = WrapperBundleId;
			if (IsBundleRunning(bundleId))
			{
				// A previous test session (or the user) already launched the
				// wrapper; just attach. This is the recommended flow when
				// running from a sandboxed parent (e.g. VS Code's helper tree)
				// where LaunchServices ignores `open` requests from the test
				// process. Launch manually once via:
				//   open -n /tmp/SamplesAppAppium.app
			}
			else
			{
				_wrapperBundlePath = CreateWrapperBundle(appPath, sampleQuery);
				_spawnedApp = LaunchBundle(_wrapperBundlePath);
			}
		}

		WaitForBundleRunning(bundleId, TimeSpan.FromSeconds(15));

		options.AddAdditionalAppiumOption("bundleId", bundleId);
		options.AddAdditionalAppiumOption("noReset", true);

		return new MacDriver(new Uri(AppiumPlatformResolver.ServerUrl()), options);
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

		try
		{
			var psi = new ProcessStartInfo("/usr/bin/defaults",
				$"read \"{plist.Replace("\"", "\\\"")}\" CFBundleIdentifier")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
			};
			using var p = Process.Start(psi);
			if (p is null) return null;
			var output = p.StandardOutput.ReadToEnd().Trim();
			p.WaitForExit();
			return string.IsNullOrEmpty(output) ? null : output;
		}
		catch
		{
			return null;
		}
	}

	private static void LaunchAppBundle(string bundlePath, string sampleQuery)
	{
		var args = string.IsNullOrEmpty(sampleQuery)
			? new[] { "-n", "-a", bundlePath }
			: new[] { "-n", "-a", bundlePath, "--args", sampleQuery };
		StartProcess("/usr/bin/open", args);
	}

	private static Process LaunchBundle(string bundlePath)
	{
		TestContext.Progress.WriteLine($"[MacAdapter] Launching wrapper bundle at {bundlePath}");

		// When invoked from a sandboxed parent (e.g. VS Code's helper tree),
		// `Process.Start("/usr/bin/open", ...)` returns 0 but LaunchServices
		// never registers the new app. Routing the activation through
		// `osascript`'s AppleScript runtime forces the user-session launchd
		// domain to handle the activation, which always succeeds.
		var psi = new ProcessStartInfo("/usr/bin/osascript")
		{
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
		};
		psi.ArgumentList.Add("-e");
		// `do shell script` routes through the user's login shell, escaping
		// the sandboxed launchd domain we inherited from VS Code's helper
		// process tree.
		psi.ArgumentList.Add($"do shell script \"/usr/bin/open -n '{bundlePath}'\"");

		var p = Process.Start(psi)
			?? throw new InvalidOperationException("Failed to start `osascript`.");
		p.WaitForExit(TimeSpan.FromSeconds(10));
		var stdout = p.StandardOutput.ReadToEnd();
		var stderr = p.StandardError.ReadToEnd();
		TestContext.Progress.WriteLine(
			$"[MacAdapter] osascript launch exit={p.ExitCode}, stdout='{stdout.Trim()}', stderr='{stderr.Trim()}'");
		return p;
	}

	private static Process StartProcess(string fileName, string[] args)
	{
		var psi = new ProcessStartInfo
		{
			FileName = fileName,
			UseShellExecute = false,
		};
		foreach (var a in args)
		{
			psi.ArgumentList.Add(a);
		}
		return Process.Start(psi)
			?? throw new InvalidOperationException($"Failed to start {fileName}.");
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
				TestContext.Progress.WriteLine($"[MacAdapter] Bundle '{bundleId}' detected as running after {attempts} attempts.");
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
		try
		{
			var psi = new ProcessStartInfo("/usr/bin/osascript",
				$"-e 'tell application \"System Events\" to (bundle identifier of every process) contains \"{bundleId}\"'")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			using var p = Process.Start(psi);
			if (p is null) return false;
			var output = p.StandardOutput.ReadToEnd().Trim();
			p.WaitForExit();
			return string.Equals(output, "true", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public By ByAutomationId(string automationId)
	{
		// Mac2's MobileBy.AccessibilityId maps to XCUIElement.identifier, which
		// only works for elements XCUITest has fully snapshotted into its own
		// element store. Custom NSAccessibilityElement subclasses (like Uno's
		// UNOAccessibilityElement) are reachable via XPath but not always via
		// the identifier shortcut, so we filter by the underlying AXIdentifier
		// attribute that Uno publishes from AutomationProperties.AutomationId.
		var escaped = automationId.Replace("'", "&apos;");
		return By.XPath($"//*[@AXIdentifier='{escaped}']");
	}

	public string GetRole(IWebElement element)
	{
		// NSAccessibility exposes AXRole / AXSubrole.
		var role = element.GetAttribute("AXRole");
		return string.IsNullOrWhiteSpace(role) ? string.Empty : role.ToLowerInvariant();
	}

	public string GetName(IWebElement element)
		=> element.GetAttribute("AXTitle") ?? element.GetAttribute("AXDescription") ?? string.Empty;

	public IReadOnlyList<IWebElement> GetAllDescendants(IWebDriver driver)
		=> driver.FindElements(By.XPath("//*"));

	public string GetAutomationId(IWebElement element)
		=> element.GetAttribute("AXIdentifier") ?? element.GetAttribute("identifier") ?? string.Empty;

	public string? GetValue(IWebElement element)
	{
		var v = element.GetAttribute("AXValue");
		if (!string.IsNullOrEmpty(v))
		{
			return v;
		}

		var legacy = element.GetAttribute("value");
		return string.IsNullOrEmpty(legacy) ? null : legacy;
	}

	public IReadOnlyList<string> GetSupportedPatterns(IWebElement element)
	{
		// NSAccessibility doesn't expose discrete pattern-availability flags
		// the way UIA does. Infer from the role: this mirrors what Uno's
		// MacOSAccessibility.cs already maps when populating the AX tree.
		var patterns = new List<string>();
		var role = GetRole(element);
		switch (role)
		{
			case "axbutton":
			case "axpopupbutton":
				patterns.Add("invoke");
				break;
			case "axcheckbox":
			case "axradiobutton":
				patterns.Add("toggle");
				break;
			case "axtextfield":
			case "axtextarea":
				patterns.Add("value");
				break;
			case "axslider":
			case "axprogressindicator":
				patterns.Add("rangevalue");
				break;
			case "axcombobox":
				patterns.Add("expandcollapse");
				patterns.Add("selectionitem");
				break;
		}
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
		AddIfPresent(element, "AXRole", "macos.AXRole", extras);
		AddIfPresent(element, "AXSubrole", "macos.AXSubrole", extras);
		AddIfPresent(element, "AXRoleDescription", "macos.AXRoleDescription", extras);
		return extras;
	}

	public void Dispose()
	{
		try
		{
			if (_spawnedApp is { HasExited: false })
			{
				_spawnedApp.Kill(entireProcessTree: true);
			}

			TerminateBundle(WrapperBundleId);

			if (_wrapperBundlePath is not null && Directory.Exists(_wrapperBundlePath)
				&& Environment.GetEnvironmentVariable("UNO_APPIUM_KEEP_BUNDLE") != "1")
			{
				Directory.Delete(_wrapperBundlePath, recursive: true);
			}
		}
		catch
		{
			// best-effort teardown
		}
		finally
		{
			_spawnedApp?.Dispose();
			_spawnedApp = null;
			_wrapperBundlePath = null;
		}
	}

	private static void TerminateBundle(string bundleId)
	{
		try
		{
			var psi = new ProcessStartInfo("/usr/bin/osascript",
				$"-e 'tell application id \"{bundleId}\" to quit'")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			using var p = Process.Start(psi);
			p?.WaitForExit(TimeSpan.FromSeconds(3));
		}
		catch
		{
		}
	}

	private static string CreateWrapperBundle(string dllPath, string sampleQuery)
	{
		var dotnetPath = ResolveDotnet();
		var slug = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
		var bundleRoot = Path.Combine(Path.GetTempPath(),
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

		var script = $@"#!/bin/bash
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

	private static Process SpawnDotnet(string dllPath, string sampleQuery)
	{
		if (!File.Exists(dllPath))
		{
			throw new FileNotFoundException(
				$"SamplesApp dll not found at '{dllPath}'. " +
				$"Set {AppiumPlatformResolver.EnvVarAppPath} to an absolute path " +
				"or run dotnet test from the repo root.",
				dllPath);
		}

		var dotnetPath = ResolveDotnet();
		var psi = new ProcessStartInfo
		{
			FileName = dotnetPath,
			UseShellExecute = false,
			WorkingDirectory = Path.GetDirectoryName(dllPath)!,
		};
		psi.ArgumentList.Add(dllPath);
		if (!string.IsNullOrEmpty(sampleQuery))
		{
			psi.ArgumentList.Add(sampleQuery);
		}

		psi.Environment["DOTNET_NOLOGO"] = "1";

		return Process.Start(psi)
			?? throw new InvalidOperationException("Failed to start dotnet process for SamplesApp.");
	}

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

	private static void WaitForWindow(Process process)
	{
		// Give the runtime time to boot and put a window on screen so the AX
		// tree contains our elements before we open the Appium session.
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
		while (DateTime.UtcNow < deadline)
		{
			if (process.HasExited)
			{
				throw new InvalidOperationException(
					$"SamplesApp dotnet process exited with code {process.ExitCode} before reaching its main window.");
			}

			Thread.Sleep(500);
			process.Refresh();
			if (process.MainWindowHandle != IntPtr.Zero)
			{
				// On macOS this is rarely set, but if it is we can stop waiting.
				return;
			}
		}

		// Fallback to a fixed wait - Mac2 will retry if the window isn't ready.
		Thread.Sleep(TimeSpan.FromSeconds(2));
	}
}
