#nullable enable

using System;
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
using Uno.UI.Xaml.Controls;
using Private.Infrastructure;

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
		Microsoft.UI.Xaml.Window.ContentHostOverride = null;

		_contentHost = null;

		if (_testAlc is not null)
		{
			_testAlc.Unload();
			_testAlc = null;
		}
	}


	[TestMethod]
	public async Task When_AlcContentHost_Then_ResourcesInherited()
	{
		// Arrange
		var testApp = new TestApplication();
		var testBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
		testApp.Resources["TestBrush"] = testBrush;

		// Act
		var contentHost = new AlcContentHost
		{
			SourceApplication = testApp
		};

		// Force the control to load and update resources
		await TestServices.WindowHelper.WaitForIdle();

		// Assert
		Assert.IsTrue(contentHost.Resources.ContainsKey("TestBrush"), "ContentHost should inherit resources from SourceApplication");
		Assert.AreEqual(testBrush, contentHost.Resources["TestBrush"], "Resource should match the one from SourceApplication");
	}

	[TestMethod]
	public async Task When_AlcContentHost_Then_MergedDictionariesInherited()
	{
		// Arrange
		var testApp = new TestApplication();
		var mergedDict = new ResourceDictionary();
		var testBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 0));
		mergedDict["MergedBrush"] = testBrush;
		testApp.Resources.MergedDictionaries.Add(mergedDict);

		// Act
		var contentHost = new AlcContentHost
		{
			SourceApplication = testApp
		};

		// Force the control to load and update resources
		await TestServices.WindowHelper.WaitForIdle();

		// Assert
		Assert.AreEqual(1, contentHost.Resources.MergedDictionaries.Count, "ContentHost should have merged dictionaries");
		Assert.IsTrue(contentHost.Resources.ContainsKey("MergedBrush"), "ContentHost should have access to merged dictionary resources");
	}

	[TestMethod]
	public async Task When_SecondaryAlcApp_Then_ContentHosted()
	{
		// Arrange
		// Build the AlcApp project
		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed");
		Assert.IsTrue(File.Exists(alcAppPath), $"AlcApp assembly should exist at {alcAppPath}");

		// Create ALC and load the assembly
		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlc = new TestAssemblyLoadContext(alcAppDirectory);
		var alcAppAssembly = _testAlc.LoadFromAssemblyPath(alcAppPath);
		Assert.IsNotNull(alcAppAssembly, "AlcApp assembly should be loaded");

		// Get the Program type with the Main entry point
		var programType = alcAppAssembly.GetType("AlcTestApp.Program");
		Assert.IsNotNull(programType, "Program type should be found in AlcApp assembly");

		// Get the Main method
		var mainMethod = programType.GetMethod("Main",
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		Assert.IsNotNull(mainMethod, "Main method should exist");

		// Get the App type to access TestWindow property later
		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found in AlcApp assembly");

		// Set up ContentHostOverride before starting the secondary app
		// This is set in the PRIMARY ALC and will be used by the secondary ALC's Window.Content setter
		_contentHost = new AlcContentHost();
		Microsoft.UI.Xaml.Window.ContentHostOverride = _contentHost;

		// Set the content host as the window content using TestServices
		TestServices.WindowHelper.WindowContent = _contentHost;

		// Act
		// Run Main in a background thread (it will call host.Run() which blocks)
		var mainThread = new System.Threading.Thread(() =>
		{
			try
			{
				mainMethod.Invoke(null, new object[] { Array.Empty<string>() });
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Main execution exception: {ex}");
			}
		})
		{
			IsBackground = true,
			Name = "AlcApp-Main"
		};
		mainThread.Start();

		// Wait for the app to initialize and window to be created
		// The App.TestWindow will be set by OnLaunched
		var maxWaitTime = TimeSpan.FromSeconds(5);
		var startTime = DateTime.Now;
		var testWindowProperty = appType.GetProperty("TestWindow",
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

		while (DateTime.Now - startTime < maxWaitTime)
		{
			var testWindow = testWindowProperty?.GetValue(null);
			if (testWindow != null)
			{
				break;
			}
			await Task.Delay(100);
		}

		// Get the secondary app instance from Application.Current in the ALC

		// Wait for UI to settle
		await TestServices.WindowHelper.WaitForIdle();

		// Assert
		Assert.IsNotNull(_contentHost.Content, "ContentHost.Content should be set by the secondary app");

		// Verify resources are available from the secondary app
		Assert.IsTrue(_contentHost.Resources.ContainsKey("TestAccentBrush"),
			"TestAccentBrush should be available from secondary app resources");

		var testBrush = _contentHost.Resources["TestAccentBrush"] as SolidColorBrush;
		Assert.IsNotNull(testBrush, "TestAccentBrush should be a SolidColorBrush");
		Assert.AreEqual(Windows.UI.Color.FromArgb(0xFF, 0x6B, 0x4C, 0xE0), testBrush.Color,
			"TestAccentBrush should have the expected color");
	}

	/// <summary>
	/// Builds the AlcApp test project and returns the path to the compiled assembly.
	/// </summary>
	private async Task<string?> BuildAlcAppAsync()
	{
		var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		Assert.IsNotNull(testDirectory, "Test directory should be found");

		// Navigate from the test binary location to the AlcApp project
		// Typical path: .../Uno.UI.RuntimeTests/bin/Debug/net10.0/...
		// Target path: .../Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/AlcApp/
		var runtimeTestsRoot = Path.GetDirectoryName(testDirectory);
		while (runtimeTestsRoot != null && !Directory.Exists(Path.Combine(runtimeTestsRoot, "Uno.UI.RuntimeTests")))
		{
			runtimeTestsRoot = Directory.GetParent(runtimeTestsRoot)?.FullName;
		}

		Assert.IsNotNull(runtimeTestsRoot, "RuntimeTests root directory should be found");

		var alcAppProjectPath = Path.Combine(runtimeTestsRoot, "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp",
			"Uno.UI.RuntimeTests.AlcApp.csproj");
		Assert.IsTrue(File.Exists(alcAppProjectPath), $"AlcApp project should exist at {alcAppProjectPath}");

		// Determine target framework and configuration
		var targetFramework = "net10.0";
		var configuration = "Debug";
#if RELEASE
		configuration = "Release";
#endif

		// Build the project using dotnet CLI
		var alcAppDir = Path.GetDirectoryName(alcAppProjectPath)!;
		var outputPath = Path.Combine(alcAppDir, "bin", configuration, targetFramework);
		var assemblyPath = Path.Combine(outputPath, "Uno.UI.RuntimeTests.AlcApp.dll");

		// Check if already built
		if (File.Exists(assemblyPath))
		{
			var assemblyWriteTime = File.GetLastWriteTime(assemblyPath);
			var projectWriteTime = File.GetLastWriteTime(alcAppProjectPath);

			// If assembly is newer than project, skip rebuild
			if (assemblyWriteTime > projectWriteTime)
			{
				return assemblyPath;
			}
		}

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

		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			Assert.Fail($"AlcApp build failed with exit code {process.ExitCode}.\nOutput: {output}\nError: {error}");
		}

		Assert.IsTrue(File.Exists(assemblyPath),
			$"AlcApp assembly should exist after build at {assemblyPath}.\nBuild output: {output}");

		return assemblyPath;
	}

	/// <summary>
	/// Test application for verifying resource inheritance
	/// </summary>
	private class TestApplication : Application
	{
		public TestApplication()
		{
			Resources = new ResourceDictionary();
		}
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
			Console.WriteLine($"Searching assembly: {assemblyName}");

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
				Console.WriteLine($"Assembly skipped: {assemblyName}");
				return null; // Use default ALC
			}

			// Try to load from the secondary app's directory
			var assemblyPath = Path.Combine(_basePath, name + ".dll");
			if (File.Exists(assemblyPath))
			{
				Console.WriteLine($"Loading assembly from: {assemblyPath}");
				return LoadFromAssemblyPath(assemblyPath);
			}

			Console.WriteLine($"Assembly not found: {assemblyName}");

			// Fall back to default resolution
			return null;
		}
	}
}
