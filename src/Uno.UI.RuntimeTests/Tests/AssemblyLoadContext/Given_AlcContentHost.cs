#if HAS_UNO
#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.Foundation.Logging;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

[TestClass]
[RunsOnUIThread]
public class Given_AlcContentHost
{
	private TestAssemblyLoadContext? _testAlc;
	private AlcContentHost? _contentHost;

	[TestCleanup]
	public void Cleanup()
	{
		// Clean up the static content host override
		WindowHelper.ContentHostOverride = null;

		_contentHost = null;

		if (_testAlc is not null)
		{
			_testAlc.Unload();
			_testAlc = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_AlcContentHost_Then_ResourcesInherited()
	{
		var contentHost = await StartSecondaryAlcAppAsync();

		Assert.IsTrue(contentHost.Resources.ContainsKey("TestAccentBrush"), "ContentHost should project resources from the secondary Application");
		var accentBrush = contentHost.Resources["TestAccentBrush"] as SolidColorBrush;
		Assert.IsNotNull(accentBrush, "TestAccentBrush should be a SolidColorBrush");

		var root = contentHost.Content as FrameworkElement;
		Assert.IsNotNull(root, "Secondary content should be a FrameworkElement");
		var titleTextBlock = root.FindName("TitleTextBlock") as TextBlock;
		Assert.IsNotNull(titleTextBlock, "TitleTextBlock should be discoverable via FindName");

		Assert.AreSame(accentBrush, titleTextBlock!.Foreground, "Sub-app visuals should consume brushes from the projected resources");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_AlcContentHost_Then_MergedDictionariesInherited()
	{
		var contentHost = await StartSecondaryAlcAppAsync();
		Assert.IsTrue(contentHost.Resources.MergedDictionaries.Count > 0, "ContentHost should surface merged dictionaries from the secondary Application");

		var mergedDictionary = contentHost.Resources.MergedDictionaries
			.FirstOrDefault(dict => dict.ContainsKey("TestTextBlockStyle"));
		Assert.IsNotNull(mergedDictionary, "Merged dictionaries should contain TestTextBlockStyle");

		var projectedStyle = mergedDictionary!["TestTextBlockStyle"] as Style;
		Assert.IsNotNull(projectedStyle, "TestTextBlockStyle should be available as a Style");

		var root = contentHost.Content as FrameworkElement;
		Assert.IsNotNull(root, "Secondary content should be a FrameworkElement");
		var statusTextBlock = root.FindName("StatusTextBlock") as TextBlock;
		Assert.IsNotNull(statusTextBlock, "StatusTextBlock should be discoverable via FindName");

		Assert.AreSame(projectedStyle, statusTextBlock!.Style, "Merged dictionary styles should apply to secondary visuals");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_SecondaryAlcApp_Then_ContentHosted()
	{
		var contentHost = await StartSecondaryAlcAppAsync();

		Assert.IsNotNull(contentHost.Content, "ContentHost.Content should be set by the secondary app");
		Assert.IsTrue(contentHost.Resources.ContainsKey("TestAccentBrush"),
			"TestAccentBrush should be available from secondary app resources");

		var testBrush = contentHost.Resources["TestAccentBrush"] as SolidColorBrush;
		Assert.IsNotNull(testBrush, "TestAccentBrush should be a SolidColorBrush");
		Assert.AreEqual(Windows.UI.Color.FromArgb(0xFF, 0x6B, 0x4C, 0xE0), testBrush!.Color,
			"TestAccentBrush should have the expected color");
	}

	private async Task<AlcContentHost> StartSecondaryAlcAppAsync()
	{
		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed");
		Assert.IsTrue(File.Exists(alcAppPath), $"AlcApp assembly should exist at {alcAppPath}");

		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlc = new TestAssemblyLoadContext(alcAppDirectory);
		var alcAppAssembly = _testAlc.LoadFromAssemblyPath(alcAppPath);
		Assert.IsNotNull(alcAppAssembly, "AlcApp assembly should be loaded");

		var programType = alcAppAssembly.GetType("AlcTestApp.Program");
		Assert.IsNotNull(programType, "Program type should be found in AlcApp assembly");

		var mainMethod = programType!.GetMethod("Main",
			BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(mainMethod, "Main method should exist");

		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found in AlcApp assembly");

		_contentHost = new AlcContentHost();
		WindowHelper.ContentHostOverride = _contentHost;
		TestServices.WindowHelper.WindowContent = _contentHost;

		var mainThread = new System.Threading.Thread(() =>
		{
			try
			{
				mainMethod!.Invoke(null, new object[] { Array.Empty<string>() });
			}
			catch (Exception ex)
			{
				this.Log().Error("Secondary ALC app execution failed", ex);
			}
		})
		{
			IsBackground = true,
			Name = "AlcApp-Main"
		};
		mainThread.Start();

		await WaitForSecondaryWindowAsync(appType!);
		await TestServices.WindowHelper.WaitForIdle();

		return _contentHost!;
	}

	private static async Task WaitForSecondaryWindowAsync(Type appType)
	{
		var testWindowProperty = appType.GetField("TestWindow", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(testWindowProperty, "App.TestWindow property should be discoverable via reflection");

		var maxWaitTime = TimeSpan.FromSeconds(30);
		var waitTimer = Stopwatch.StartNew();
		while (waitTimer.Elapsed < maxWaitTime)
		{
			if (testWindowProperty!.GetValue(null) is not null)
			{
				return;
			}

			await Task.Delay(100);
		}

		throw new InvalidOperationException("Timed out waiting for AlcTestApp.App.TestWindow to be assigned.");
	}

	private static string GetAlcAppPath()
	{
		var basePath = Path.GetDirectoryName(Application.Current.GetType().Assembly.Location)!;

		var searchPaths = new[] {
			Path.Combine(basePath, "..", "..", "..", "..", "..", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", "src", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", ".."), // CI
		};

		var hrAppPath = searchPaths
			.Where(p => File.Exists(Path.Combine(p, "Uno.UI.RuntimeTests.AlcApp.csproj")))
			.FirstOrDefault();

		if (hrAppPath is null)
		{
			throw new InvalidOperationException("Unable to find AlcApp folder in " + string.Join(", ", searchPaths));
		}

		return hrAppPath;
	}

	/// <summary>
	/// Builds the AlcApp test project and returns the path to the compiled assembly.
	/// </summary>
	private async Task<string?> BuildAlcAppAsync()
	{
		var alcAppProjectPath = Path.Combine(GetAlcAppPath(), "Uno.UI.RuntimeTests.AlcApp.csproj");
		Assert.IsTrue(File.Exists(alcAppProjectPath), $"AlcApp project should exist at {alcAppProjectPath}");

		// Determine target framework and configuration
		var targetFramework =
#if NET10_0
			"net10.0";
#elif NET9_0
			"net9.0";
#else
#error This .NET version is not yet supported by the test project build script. Supported versions: NET10_0, NET9_0. To add support, add a new '#elif NETXX_X' block with the appropriate targetFramework string.
#endif

		// The CI environment builds build tooling in debug (related to the HR tests)
		var configuration = "Debug";

		// Build the project using dotnet CLI
		var alcAppDir = Path.GetDirectoryName(alcAppProjectPath)!;
		var outputPath = Path.Combine(alcAppDir, "bin", configuration, targetFramework);
		var assemblyPath = Path.Combine(outputPath, "Uno.UI.RuntimeTests.AlcApp.dll");

		// Need to build the project
		var startInfo = new System.Diagnostics.ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"build \"{alcAppProjectPath}\" -c {configuration} -f {targetFramework}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = alcAppDir
		};

		using var process = System.Diagnostics.Process.Start(startInfo);
		Assert.IsNotNull(process, "dotnet build process should start");

		var outputTask = process.StandardOutput.ReadToEndAsync();
		var errorTask = process.StandardError.ReadToEndAsync();

		await Task.WhenAll(outputTask, errorTask);
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			Assert.Fail($"AlcApp build failed with exit code {process.ExitCode}.\nOutput: {outputTask.Result}\nError: {errorTask.Result}");
		}

		Assert.IsTrue(File.Exists(assemblyPath),
			$"AlcApp assembly should exist after build at {assemblyPath}.\nBuild output: {outputTask.Result}");

		return assemblyPath;
	}

	/// <summary>
	/// Custom AssemblyLoadContext for testing ALC scenarios.
	/// Loads all assemblies from the secondary app directory except Uno assemblies,
	/// which are shared with the default ALC.
	/// </summary>
	private class TestAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
	{
		private readonly string _basePath;

		public TestAssemblyLoadContext(string basePath) : base(name: "TestALC", isCollectible: true)
		{
			_basePath = basePath;
		}

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			this.Log().Debug($"Searching assembly: {assemblyName}");

			var name = assemblyName.Name;

			// Let Uno assemblies be loaded from the default ALC (shared)
			if (name != null && (
				name.StartsWith("Uno.", StringComparison.OrdinalIgnoreCase) ||
				name.Equals("Uno", StringComparison.OrdinalIgnoreCase) ||
				name.StartsWith("Microsoft.UI.", StringComparison.OrdinalIgnoreCase) ||
				name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase) ||
				name.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase) ||
				name.StartsWith("SkiaSharp", StringComparison.OrdinalIgnoreCase) ||
				name.StartsWith("HarfBuzzSharp", StringComparison.OrdinalIgnoreCase))
			)
			{
				this.Log().Debug($"Assembly skipped: {assemblyName}");
				return null; // Use default ALC
			}

			// Try to load from the secondary app's directory
			var assemblyPath = Path.Combine(_basePath, name + ".dll");
			if (File.Exists(assemblyPath))
			{
				this.Log().Debug($"Loading assembly from: {assemblyPath}");
				return LoadFromAssemblyPath(assemblyPath);
			}

			this.Log().Debug($"Assembly not found: {assemblyName}");

			// Fall back to default resolution
			return null;
		}
	}
}
#endif
