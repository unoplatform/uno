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
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)] // Disabled on macOS for unknown core dump
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
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)] // Disabled on macOS for unknown core dump
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
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)] // Disabled on macOS for unknown core dump
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

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Activate_Then_ActivatedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when Activate() is called on ALC window");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Activate_WithFrameNavigation_Then_ActivatedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync(new[] { "--use-frame" });

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when Activate() is called on ALC window with Frame navigation");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated when using Frame navigation");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ActivatedRegisteredBeforeContent_Then_ActivatedEventRaised()
	{
		var (_, alcWindow) = await StartSecondaryAlcAppWithWindowAsync(new[] { "--defer-content" });

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		ApplyDeferredContentFromSecondaryApp();
		await TestServices.WindowHelper.WaitForIdle();

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when registered before content is set");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated when activated after deferred content is applied");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ActivatedRegisteredBeforeContent_WithFrameNavigation_Then_ActivatedEventRaised()
	{
		var (_, alcWindow) = await StartSecondaryAlcAppWithWindowAsync(new[] { "--defer-content", "--use-frame" });

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		ApplyDeferredContentFromSecondaryApp();
		await TestServices.WindowHelper.WaitForIdle();

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when registered before Frame content is set");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated when activated after deferred Frame content is applied");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Then_VisibleReturnsHostVisibility()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		// Window should be visible when content host is loaded
		Assert.IsTrue(alcWindow.Visible, "ALC window Visible should be true when ContentHostOverride is loaded");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Then_BoundsMatchesHostBounds()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		// Set a specific size on the content host
		contentHost.Width = 400;
		contentHost.Height = 300;
		await TestServices.WindowHelper.WaitForIdle();

		var bounds = alcWindow.Bounds;
		Assert.AreEqual(400, bounds.Width, 1, "ALC window Bounds.Width should match ContentHostOverride.ActualWidth");
		Assert.AreEqual(300, bounds.Height, 1, "ALC window Bounds.Height should match ContentHostOverride.ActualHeight");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_HostSizeChanges_Then_SizeChangedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool sizeChangedFired = false;
		Windows.Foundation.Size? newSize = null;

		alcWindow.SizeChanged += (sender, args) =>
		{
			sizeChangedFired = true;
			newSize = args.Size;
		};

		// Change the content host size
		contentHost.Width = 500;
		contentHost.Height = 400;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(sizeChangedFired, "SizeChanged event should fire when ContentHostOverride size changes");
		Assert.IsNotNull(newSize, "SizeChanged args should contain new size");
		Assert.AreEqual(500, newSize!.Value.Width, 1, "New size width should match");
		Assert.AreEqual(400, newSize!.Value.Height, 1, "New size height should match");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_ClosedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool closedFired = false;
		alcWindow.Closed += (sender, args) =>
		{
			closedFired = true;
		};

		alcWindow.Close();

		Assert.IsTrue(closedFired, "Closed event should fire when Close() is called on ALC window");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_ContentClearedFromHost()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		Assert.IsNotNull(contentHost.Content, "Content should be set before close");

		alcWindow.Close();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(contentHost.Content, "Content should be cleared from host after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_VisibilityChangedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool visibilityChangedFired = false;
		bool? newVisibility = null;

		alcWindow.VisibilityChanged += (sender, args) =>
		{
			visibilityChangedFired = true;
			newVisibility = args.Visible;
		};

		alcWindow.Close();

		Assert.IsTrue(visibilityChangedFired, "VisibilityChanged event should fire when Close() is called");
		Assert.IsFalse(newVisibility, "Visibility should be false after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_VisibleReturnsFalse()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		Assert.IsTrue(alcWindow.Visible, "Window should be visible before close");

		alcWindow.Close();

		Assert.IsFalse(alcWindow.Visible, "Window Visible should be false after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ClosedEventHandled_Then_CloseIsCancelled()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		alcWindow.Closed += (sender, args) =>
		{
			args.Handled = true; // Cancel the close
		};

		alcWindow.Close();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(contentHost.Content, "Content should NOT be cleared when Closed event is handled");
		Assert.IsTrue(alcWindow.Visible, "Window should still be visible when close is cancelled");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ActivateAfterClose_Then_ThrowsException()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		alcWindow.Close();

		bool threwException = false;
		try
		{
			alcWindow.Activate();
		}
		catch (InvalidOperationException)
		{
			threwException = true;
		}

		Assert.IsTrue(threwException, "Activate() should throw InvalidOperationException after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Then_NotInApplicationHelperWindows()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		var windows = Uno.UI.ApplicationHelper.Windows;
		Assert.IsFalse(windows.Contains(alcWindow),
			"ALC window should NOT be in ApplicationHelper.Windows to avoid blocking app closure");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcContentHost_Then_FrameContentNavigates()
	{
		var contentHost = await StartSecondaryAlcAppAsync(new[] { "--use-frame" });

		var frame = contentHost.Content as Frame;
		Assert.IsNotNull(frame, "ContentHost.Content should be a Frame when --use-frame is specified");

		var page = frame!.Content as FrameworkElement;
		Assert.IsNotNull(page, "Frame should navigate to MainPage and set its Content");

		var titleTextBlock = page!.FindName("TitleTextBlock") as TextBlock;
		Assert.IsNotNull(titleTextBlock, "TitleTextBlock should be discoverable in the navigated page");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_SecondaryAlcApp_Then_AlcWindowModeEnabled()
	{
		// This test verifies that the ALC window is properly set up in ALC mode.
		// The _alcState is set when content from a secondary ALC is set on the window,
		// which triggers the ALC-specific behavior (content redirection, event forwarding).

		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		// Get the main app's ContentRoot and verify its InputManager IS initialized
		var mainAppContentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(contentHost);
		Assert.IsNotNull(mainAppContentRoot, "Main app ContentRoot should be found");
		Assert.IsTrue(mainAppContentRoot!.InputManager.Initialized,
			"Main app's InputManager should be initialized");

		var alcWindowType = alcWindow.GetType();

		// Verify the ALC window is operating in ALC mode (content redirected).
		// The _alcState is set when content from a secondary ALC is set, which is
		// the reliable way to detect ALC mode.
		var isAlcWindowProperty = alcWindowType.GetProperty("IsAlcWindow",
			BindingFlags.NonPublic | BindingFlags.Instance);
		Assert.IsNotNull(isAlcWindowProperty, "IsAlcWindow property should exist on Window");

		var isAlcWindow = (bool)isAlcWindowProperty!.GetValue(alcWindow)!;
		Assert.IsTrue(isAlcWindow,
			"ALC window should report IsAlcWindow = true after content is set from secondary ALC");

		// The secondary ALC content is hosted inside the main app's visual tree
		// (via ContentHostOverride), so it should use the main app's InputManager.
		Assert.IsNotNull(contentHost.Content,
			"Secondary ALC content should be hosted in the main app's ContentHostOverride");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_SecondaryAlcApp_Then_KeyboardInputStillWorks()
	{
		// This test verifies that keyboard input continues to work after loading a secondary ALC app.
		// Before the fix, initializing a secondary ALC app would overwrite the TypeScript keyboard
		// handler, breaking keyboard input for the entire application.

		// Create a TextBox in the main app to test keyboard input
		var mainAppTextBox = new TextBox { Name = "MainAppTextBox" };
		var container = new StackPanel
		{
			Children =
			{
				mainAppTextBox,
				new AlcContentHost() // Will host the secondary ALC content
			}
		};

		_contentHost = (AlcContentHost)container.Children[1];

		// Set up the container as the window content
		TestServices.WindowHelper.WindowContent = container;
		await TestServices.WindowHelper.WaitForLoaded(container);
		await TestServices.WindowHelper.WaitForIdle();

		// Verify keyboard input works BEFORE loading secondary ALC
		mainAppTextBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.InputText("before", mainAppTextBox);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("before", mainAppTextBox.Text,
			"Keyboard input should work before loading secondary ALC app");

		// Clear the textbox
		mainAppTextBox.Text = "";
		await TestServices.WindowHelper.WaitForIdle();

		// Now load the secondary ALC app
		WindowHelper.ContentHostOverride = _contentHost;

		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed");

		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlc = new TestAssemblyLoadContext(alcAppDirectory);
		var alcAppAssembly = _testAlc.LoadFromAssemblyPath(alcAppPath);

		var programType = alcAppAssembly.GetType("AlcTestApp.Program");
		var mainMethod = programType!.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
		var appType = alcAppAssembly.GetType("AlcTestApp.App");

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

		// Verify secondary ALC content is loaded
		Assert.IsNotNull(_contentHost.Content,
			"Secondary ALC content should be loaded in ContentHost");

		// Verify keyboard input still works AFTER loading secondary ALC
		mainAppTextBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.InputText("after", mainAppTextBox);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("after", mainAppTextBox.Text,
			"Keyboard input should still work after loading secondary ALC app");
	}

	private async Task<(AlcContentHost contentHost, Window alcWindow)> StartSecondaryAlcAppWithWindowAsync(string[]? launchArguments = null)
	{
		var contentHost = await StartSecondaryAlcAppAsync(launchArguments);

		// Get the Window from the secondary ALC app via reflection
		var alcAppAssembly = _testAlc!.Assemblies.First(a => a.GetName().Name == "Uno.UI.RuntimeTests.AlcApp");
		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found");

		var testWindowField = appType!.GetField("TestWindow", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(testWindowField, "TestWindow field should be found");

		var alcWindow = testWindowField!.GetValue(null) as Window;
		Assert.IsNotNull(alcWindow, "TestWindow should be a Window instance");

		return (contentHost, alcWindow!);
	}

	private async Task<AlcContentHost> StartSecondaryAlcAppAsync(string[]? launchArguments = null)
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
				mainMethod!.Invoke(null, new object[] { launchArguments ?? Array.Empty<string>() });
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

	private void ApplyDeferredContentFromSecondaryApp()
	{
		var alcAppAssembly = _testAlc!.Assemblies.First(a => a.GetName().Name == "Uno.UI.RuntimeTests.AlcApp");
		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found in AlcApp assembly");

		var applyDeferredContent = appType!.GetMethod("ApplyDeferredContent", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(applyDeferredContent, "ApplyDeferredContent should be available on AlcTestApp.App");

		applyDeferredContent!.Invoke(null, null);
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
