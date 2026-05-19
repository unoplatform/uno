#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenQA.Selenium;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

/// <summary>
/// Golden-file snapshots of the accessibility tree per sample, per platform.
/// On a normal run: loads <c>Snapshots/&lt;flavor&gt;/&lt;sample-id&gt;.json</c>,
/// dumps the live tree, and asserts they match (via
/// <see cref="SnapshotComparer"/>). With <c>UNO_APPIUM_RECORD_SNAPSHOTS=1</c>:
/// writes/overwrites the baseline instead. Bootstrap baselines once per
/// platform with that env var, then commit the JSON files.
/// </summary>
[TestFixture]
[Category("Baseline")]
public sealed class AccessibilityBaselineTests
{
	/// <summary>
	/// Samples covered by the snapshot suite. Add entries here when you want
	/// a new sample's a11y tree under regression protection.
	/// </summary>
	public static IEnumerable<BaselineCase> Cases()
	{
		yield return new BaselineCase(
			Sample: "Automation/Accessibility_ScreenReader",
			SnapshotId: "Automation_AccessibilityScreenReader");
	}

	[TestCaseSource(nameof(Cases))]
	public void Tree_MatchesBaseline(BaselineCase testCase)
	{
		var platform = AppiumPlatformResolver.TryResolve();
		if (platform is null)
		{
			Assert.Ignore(
				$"Set {AppiumPlatformResolver.EnvVarPlatform}=windows|mac|wasm and " +
				$"{AppiumPlatformResolver.EnvVarAppPath} to enable baseline fixtures.");
			return;
		}

		IPlatformAdapter adapter = platform.Value switch
		{
			AppiumPlatform.Windows => new WindowsAdapter(),
			AppiumPlatform.Mac => new MacAdapter(),
			AppiumPlatform.Wasm => new WasmAdapter(),
			_ => throw new NotSupportedException(),
		};

		IWebDriver? driver = null;
		try
		{
			driver = adapter.CreateDriver($"sample={testCase.Sample}");
			driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);

			// Give the SamplesApp shell a moment to navigate and lay out the
			// requested sample before we snapshot.
			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(3));

			var actual = TreeDumper.Capture(driver, adapter);
			var flavor = FlavorOf(adapter.Platform);
			var baselinePath = ResolveBaselinePath(flavor, testCase.SnapshotId);

			if (IsRecordMode())
			{
				SnapshotSerializer.Write(baselinePath, actual, testCase.Sample, flavor);
				TestContext.WriteLine($"Recorded baseline -> {baselinePath}");
				Assert.Pass($"Recorded baseline for {testCase.Sample} ({flavor}).");
				return;
			}

			var expected = SnapshotSerializer.Read(baselinePath);
			if (expected is null)
			{
				Assert.Fail(
					$"Baseline file missing at {baselinePath}. " +
					"Set UNO_APPIUM_RECORD_SNAPSHOTS=1 and rerun to bootstrap.");
				return;
			}

			var diff = SnapshotComparer.Compare(expected, actual);
			if (diff.IsMatch)
			{
				return;
			}

			var actualJson = SnapshotSerializer.Serialize(actual, testCase.Sample, flavor);
			var actualPath = Path.Combine(
				TestContext.CurrentContext.WorkDirectory ?? Path.GetTempPath(),
				"snapshot-actual",
				flavor,
				testCase.SnapshotId + ".json");
			Directory.CreateDirectory(Path.GetDirectoryName(actualPath)!);
			File.WriteAllText(actualPath, actualJson);
			TestContext.AddTestAttachment(actualPath, "actual snapshot");

			Assert.Fail(
				$"Accessibility tree diverged from baseline.{Environment.NewLine}" +
				$"  baseline: {baselinePath}{Environment.NewLine}" +
				$"  actual:   {actualPath}{Environment.NewLine}" +
				diff.Format());
		}
		finally
		{
			try
			{
				driver?.Quit();
			}
			catch
			{
				// best-effort teardown
			}
			driver?.Dispose();
			adapter.Dispose();
		}
	}

	private static bool IsRecordMode()
	{
		var value = Environment.GetEnvironmentVariable("UNO_APPIUM_RECORD_SNAPSHOTS");
		return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}

	private static string FlavorOf(AppiumPlatform platform) => platform switch
	{
		AppiumPlatform.Windows => "win32",
		AppiumPlatform.Mac => "macos",
		AppiumPlatform.Wasm => "wasm",
		_ => throw new NotSupportedException(),
	};

	private static string ResolveBaselinePath(string flavor, string snapshotId)
	{
		// Prefer the source-tree location so committed baselines are the
		// source of truth even when running from bin/. The override lets a
		// CI job point at a generated directory.
		var overrideDir = Environment.GetEnvironmentVariable("UNO_APPIUM_SNAPSHOTS_DIR");
		var root = overrideDir is { Length: > 0 } ? overrideDir : SourceTreeSnapshotsDir();
		return Path.Combine(root, flavor, snapshotId + ".json");
	}

	private static string SourceTreeSnapshotsDir([CallerFilePath] string? thisFile = null)
	{
		// thisFile is .../SamplesApp.AppiumTests/Tests/AccessibilityBaselineTests.cs
		var testsDir = Path.GetDirectoryName(thisFile)!;
		var projectDir = Path.GetDirectoryName(testsDir)!;
		return Path.Combine(projectDir, "Snapshots");
	}

	public sealed record BaselineCase(string Sample, string SnapshotId)
	{
		public override string ToString() => SnapshotId;
	}
}
