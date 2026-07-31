#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace SamplesApp.AppiumTests.Infrastructure;

public sealed class AppiumTestSession : IDisposable
{
	private static readonly JsonSerializerOptions s_treeJsonOptions = new()
	{
		WriteIndented = true,
	};

	private readonly TestContext _testContext;
	private bool _disposed;

	private AppiumTestSession(
		TestContext testContext,
		AppiumTestOptions options,
		IPlatformAdapter adapter,
		IWebDriver driver,
		string sampleQuery)
	{
		_testContext = testContext;
		Options = options;
		Adapter = adapter;
		Driver = driver;
		SampleQuery = sampleQuery;
		DiagnosticContext = options.DiagnosticContext(sampleQuery);
	}

	public AppiumTestOptions Options { get; }

	public IPlatformAdapter Adapter { get; }

	public IWebDriver Driver { get; }

	public string SampleQuery { get; }

	public string DiagnosticContext { get; }

	public static AppiumTestSession Create(TestContext testContext, string sampleQuery)
	{
		var defaultArtifactsDirectory = ResolveArtifactsDirectory(testContext);
		var options = AppiumTestOptions.LoadRequired(defaultArtifactsDirectory);
		Directory.CreateDirectory(options.ArtifactsDirectory);

		var adapter = CreateAdapter(options.Platform);
		IWebDriver? driver = null;

		try
		{
			driver = adapter.CreateDriver(options, sampleQuery);
			var timeouts = driver.Manage().Timeouts();
			timeouts.ImplicitWait = TimeSpan.Zero;
			timeouts.PageLoad = options.Timeout;
			timeouts.AsynchronousJavaScript = options.Timeout;
			testContext.WriteLine($"Started Appium session ({options.DiagnosticContext(sampleQuery)}).");
			return new AppiumTestSession(testContext, options, adapter, driver, sampleQuery);
		}
		catch (Exception ex)
		{
			var disposalErrors = new List<Exception>();

			if (driver is not null)
			{
				try
				{
					driver.Quit();
				}
				catch (Exception quitError)
				{
					disposalErrors.Add(quitError);
				}

				try
				{
					driver.Dispose();
				}
				catch (Exception disposeError)
				{
					disposalErrors.Add(disposeError);
				}
			}

			try
			{
				adapter.Dispose();
			}
			catch (Exception adapterError)
			{
				disposalErrors.Add(adapterError);
			}

			if (disposalErrors.Count > 0)
			{
				throw new AggregateException(
					$"Failed to create the Appium session and then failed to dispose partial resources ({options.DiagnosticContext(sampleQuery)}).",
					new[] { ex }.Concat(disposalErrors));
			}

			throw new InvalidOperationException(
				$"Failed to create the Appium session ({options.DiagnosticContext(sampleQuery)}).",
				ex);
		}
	}

	public AccessibilitySnapshot CaptureSnapshot(
		AccessibilitySnapshotDefinition definition,
		CancellationToken cancellationToken = default)
	{
		var snapshot = new AccessibilitySnapshot
		{
			Schema = SnapshotSerializer.SchemaVersion,
			Sample = definition.Sample,
			Flavor = SnapshotPaths.FlavorOf(Options.Platform),
		};

		foreach (var element in definition.ElementsFor(Options.Platform))
		{
			snapshot.Elements.Add(WaitForSnapshot(
				element.Id,
				element.AutomationId,
				element.FieldsFor(Options.Platform),
				_ => true,
				$"capture canonical snapshot for '{element.AutomationId}'",
				cancellationToken));
		}

		snapshot.Elements = snapshot.Elements
			.OrderBy(element => element.Id, StringComparer.Ordinal)
			.ToList();

		return snapshot;
	}

	public AccessibilityElementSnapshot CaptureElement(
		string automationId,
		AccessibilitySnapshotFields fields,
		CancellationToken cancellationToken = default)
		=> WaitForSnapshot(
			automationId,
			automationId,
			fields,
			_ => true,
			$"capture accessibility state for '{automationId}'",
			cancellationToken);

	public AccessibilityElementSnapshot WaitForSnapshot(
		string automationId,
		AccessibilitySnapshotFields fields,
		Func<AccessibilityElementSnapshot, bool> predicate,
		string description,
		CancellationToken cancellationToken = default)
		=> WaitForSnapshot(
			automationId,
			automationId,
			fields,
			predicate,
			description,
			cancellationToken);

	public IWebElement WaitForElement(
		string automationId,
		CancellationToken cancellationToken = default)
		=> WaitUntil(
			() => TryFindVisibleElement(automationId),
			element => element is not null,
			$"find element '{automationId}'",
			cancellationToken)
			?? throw new InvalidOperationException($"Failed to resolve element '{automationId}' ({DiagnosticContext}).");

	public void Activate(
		string automationId,
		CancellationToken cancellationToken = default)
		=> Adapter.Activate(Driver, WaitForElement(automationId, cancellationToken));

	public void EnterText(
		string automationId,
		string value,
		CancellationToken cancellationToken = default)
		=> Adapter.EnterText(Driver, WaitForElement(automationId, cancellationToken), value);

	public string WriteActualSnapshot(string snapshotId, AccessibilitySnapshot snapshot)
	{
		var actualPath = Path.Combine(
			Options.ArtifactsDirectory,
			"snapshot-actual",
			Options.Flavor,
			snapshotId + ".json");
		SnapshotSerializer.Write(actualPath, snapshot);
		_testContext.AddResultFile(actualPath);
		return actualPath;
	}

	public string? TryWriteDiagnosticTree(string snapshotId)
	{
		try
		{
			var tree = TreeDumper.Capture(Driver, Adapter);
			var path = Path.Combine(
				Options.ArtifactsDirectory,
				"snapshot-actual",
				Options.Flavor,
				snapshotId + ".tree.json");
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, JsonSerializer.Serialize(tree, s_treeJsonOptions).Replace("\r\n", "\n"));
			_testContext.AddResultFile(path);
			return path;
		}
		catch (WebDriverException ex)
		{
			_testContext.WriteLine($"Unable to capture the raw accessibility tree for diagnostics ({DiagnosticContext}): {ex}");
			return null;
		}
		catch (InvalidOperationException ex)
		{
			_testContext.WriteLine($"Unable to capture the raw accessibility tree for diagnostics ({DiagnosticContext}): {ex}");
			return null;
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		var failures = new List<Exception>();

		try
		{
			Driver.Quit();
		}
		catch (Exception ex)
		{
			failures.Add(new InvalidOperationException($"Driver.Quit failed ({DiagnosticContext}).", ex));
		}

		try
		{
			Driver.Dispose();
		}
		catch (Exception ex)
		{
			failures.Add(new InvalidOperationException($"Driver.Dispose failed ({DiagnosticContext}).", ex));
		}

		try
		{
			Adapter.Dispose();
		}
		catch (Exception ex)
		{
			failures.Add(new InvalidOperationException($"Adapter.Dispose failed ({DiagnosticContext}).", ex));
		}

		if (failures.Count == 1)
		{
			throw failures[0];
		}

		if (failures.Count > 1)
		{
			throw new AggregateException(
				$"One or more failures occurred while cleaning up the Appium session ({DiagnosticContext}).",
				failures);
		}
	}

	private AccessibilityElementSnapshot WaitForSnapshot(
		string id,
		string automationId,
		AccessibilitySnapshotFields fields,
		Func<AccessibilityElementSnapshot, bool> predicate,
		string description,
		CancellationToken cancellationToken)
		=> WaitUntil(
			() => TryCaptureElement(id, automationId, fields),
			predicate,
			description,
			cancellationToken)
			?? throw new InvalidOperationException($"Failed to capture '{automationId}' ({DiagnosticContext}).");

	private AccessibilityElementSnapshot? TryCaptureElement(
		string id,
		string automationId,
		AccessibilitySnapshotFields fields)
	{
		var element = TryFindVisibleElement(automationId);
		return element is null
			? null
			: AccessibilitySnapshotBuilder.Capture(Driver, Adapter, id, fields, element);
	}

	private IWebElement? TryFindVisibleElement(string automationId)
	{
		var matches = Driver.FindElements(Adapter.ByAutomationId(automationId));
		return matches.FirstOrDefault(element => element.Displayed) ?? matches.FirstOrDefault();
	}

	private T? WaitUntil<T>(
		Func<T?> probe,
		Func<T, bool> predicate,
		string description,
		CancellationToken cancellationToken)
		where T : class
	{
		var deadline = DateTime.UtcNow + Options.Timeout;
		Exception? lastError = null;

		while (DateTime.UtcNow < deadline)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var value = probe();
				if (value is not null && predicate(value))
				{
					return value;
				}
			}
			catch (StaleElementReferenceException ex)
			{
				lastError = ex;
			}
			catch (NoSuchElementException ex)
			{
				lastError = ex;
			}
			catch (WebDriverException ex)
			{
				lastError = ex;
			}

			if (cancellationToken.WaitHandle.WaitOne(Options.PollInterval))
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		var details = lastError is null ? string.Empty : $" Last error: {lastError.Message}";
		throw new AssertFailedException(
			$"Timed out after {Options.Timeout.TotalSeconds:F0}s while trying to {description} ({DiagnosticContext}).{details}");
	}

	private static IPlatformAdapter CreateAdapter(AppiumPlatform platform)
		=> platform switch
		{
			AppiumPlatform.Windows => new WindowsAdapter(),
			AppiumPlatform.Mac => new MacAdapter(),
			AppiumPlatform.Wasm => new WasmAdapter(),
			_ => throw new NotSupportedException($"Unsupported Appium platform '{platform}'."),
		};

	private static string ResolveArtifactsDirectory(TestContext testContext)
	{
		var root = testContext.ResultsDirectory;
		if (string.IsNullOrWhiteSpace(root))
		{
			root = AppContext.BaseDirectory;
		}

		return Path.Combine(
			root!,
			"AppiumArtifacts",
			SanitizePathSegment(testContext.FullyQualifiedTestClassName ?? "UnknownClass"),
			SanitizePathSegment(testContext.TestName ?? "UnknownTest"));
	}

	private static string SanitizePathSegment(string value)
	{
		foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
		{
			value = value.Replace(invalidCharacter, '_');
		}

		return value.Replace(Path.DirectorySeparatorChar, '_')
			.Replace(Path.AltDirectorySeparatorChar, '_');
	}
}
