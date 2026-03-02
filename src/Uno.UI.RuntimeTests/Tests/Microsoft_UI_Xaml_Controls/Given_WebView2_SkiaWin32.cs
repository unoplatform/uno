#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_WebView2_SkiaWin32
{
	[TestMethod]
	public async Task When_Reloading_WebView2_Basic_Repeatedly_Then_No_Orphan_Windows()
	{
		const string sampleMetadataName = "UITests.Microsoft_UI_Xaml_Controls.WebView2Tests.WebView2_Basic";
		const string fallbackMetadataName = "SamplesApp.Samples.UnitTests.UnitTestsPage";

		var sampleChooser = GetSampleChooserViewModelInstance();
		await SetSelectedSampleAsync(sampleChooser, sampleMetadataName);

		var initialWebView = await WaitForCurrentWebView2Async(sampleChooser);
		await initialWebView.EnsureCoreWebView2Async();
		await TestServices.WindowHelper.WaitFor(
			() => IsPlatformUnoSource(initialWebView.Source),
			timeoutMS: 10000,
			message: "Expected WebView2_Basic to load https://platform.uno.");

		var hostHwnd = GetHostWindowHandle(initialWebView);
		Assert.AreNotEqual(IntPtr.Zero, hostHwnd);

		const int reloadCount = 15;
		for (var i = 0; i < reloadCount; i++)
		{
			ExecuteReloadCurrentTest(sampleChooser);

			// Keep this delay short to stress the rapid-reload path in the shell.
			await Task.Delay(20);

			var webView = await WaitForCurrentWebView2Async(sampleChooser);
			await webView.EnsureCoreWebView2Async();
			await TestServices.WindowHelper.WaitFor(
				() => IsPlatformUnoSource(webView.Source),
				timeoutMS: 10000,
				message: $"Expected WebView2_Basic to keep https://platform.uno after reload iteration {i}.");

			await TestServices.WindowHelper.WaitFor(
				() => CountChildWebViewWindows(hostHwnd) == 1,
				timeoutMS: 4000,
				message: $"Expected a single attached WebView child window after reload iteration {i}.");

			await TestServices.WindowHelper.WaitFor(
				() => CountTopLevelWebViewWindows() == 0,
				timeoutMS: 4000,
				message: $"Expected no orphan top-level WebView windows after reload iteration {i}.");
		}

		await SetSelectedSampleAsync(sampleChooser, fallbackMetadataName);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.WindowHelper.WaitFor(
			() => CountChildWebViewWindows(hostHwnd) == 0,
			timeoutMS: 4000,
			message: "Expected no attached WebView child windows after unloading content.");

		await TestServices.WindowHelper.WaitFor(
			() => CountTopLevelWebViewWindows() == 0,
			timeoutMS: 4000,
			message: "Expected no orphan top-level WebView windows after unloading content.");
	}

	private static object GetSampleChooserViewModelInstance()
	{
		var sampleChooserType = AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(assembly => assembly.GetType("SampleControl.Presentation.SampleChooserViewModel", throwOnError: false))
			.FirstOrDefault(type => type is not null)
			?? throw new InvalidOperationException("Could not find SampleChooserViewModel type.");

		var instanceProperty = sampleChooserType.GetProperty(
			"Instance",
			BindingFlags.Public | BindingFlags.Static)
			?? throw new InvalidOperationException("Could not find SampleChooserViewModel.Instance.");

		return instanceProperty.GetValue(null)
			?? throw new InvalidOperationException("SampleChooserViewModel.Instance is null.");
	}

	private static async Task SetSelectedSampleAsync(object sampleChooserViewModel, string metadataName)
	{
		var method = sampleChooserViewModel.GetType().GetMethod(
			"SetSelectedSample",
			BindingFlags.Public | BindingFlags.Instance,
			binder: null,
			types: [typeof(CancellationToken), typeof(string)],
			modifiers: null)
			?? throw new InvalidOperationException("Could not find SampleChooserViewModel.SetSelectedSample(CancellationToken, string).");

		var task = method.Invoke(sampleChooserViewModel, [CancellationToken.None, metadataName]) as Task
			?? throw new InvalidOperationException("SetSelectedSample did not return a Task.");
		await task;
	}

	private static void ExecuteReloadCurrentTest(object sampleChooserViewModel)
	{
		var commandProperty = sampleChooserViewModel.GetType().GetProperty(
			"ReloadCurrentTestCommand",
			BindingFlags.Public | BindingFlags.Instance)
			?? throw new InvalidOperationException("Could not find ReloadCurrentTestCommand.");

		var command = commandProperty.GetValue(sampleChooserViewModel)
			?? throw new InvalidOperationException("ReloadCurrentTestCommand is null.");

		var commandType = command.GetType();
		var canExecute = (bool?)commandType.GetMethod("CanExecute")?.Invoke(command, [null]) ?? false;
		Assert.IsTrue(canExecute, "Expected ReloadCurrentTestCommand.CanExecute(null) to be true.");
		commandType.GetMethod("Execute")?.Invoke(command, [null]);
	}

	private static async Task<WebView2> WaitForCurrentWebView2Async(object sampleChooserViewModel)
	{
		WebView2? webView = null;
		await TestServices.WindowHelper.WaitFor(
			() =>
			{
				webView = GetCurrentWebView2(sampleChooserViewModel);
				return webView is not null && webView.IsLoaded;
			},
			timeoutMS: 10000,
			message: "Timed out waiting for WebView2_Basic to be loaded in the sample shell.");

		return webView!;
	}

	private static WebView2? GetCurrentWebView2(object sampleChooserViewModel)
	{
		var contentPhoneProperty = sampleChooserViewModel.GetType().GetProperty(
			"ContentPhone",
			BindingFlags.Public | BindingFlags.Instance)
			?? throw new InvalidOperationException("Could not find ContentPhone.");

		var content = contentPhoneProperty.GetValue(sampleChooserViewModel);
		return FindFirstDescendantOfType<WebView2>(content as DependencyObject);
	}

	private static T? FindFirstDescendantOfType<T>(DependencyObject? root)
		where T : class
	{
		if (root is null)
		{
			return null;
		}

		if (root is T match)
		{
			return match;
		}

		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < childCount; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (FindFirstDescendantOfType<T>(child) is { } childMatch)
			{
				return childMatch;
			}
		}

		return null;
	}

	private static bool IsPlatformUnoSource(Uri? source)
		=> source is { IsAbsoluteUri: true }
			&& string.Equals(source.Host, "platform.uno", StringComparison.OrdinalIgnoreCase);

	private static HWND GetHostWindowHandle(FrameworkElement element)
	{
		var xamlRoot = element.XamlRoot ?? throw new InvalidOperationException("XamlRoot is null.");
		var hostWindow = GetPropertyValue(xamlRoot, "HostWindow")
			?? throw new InvalidOperationException("HostWindow is null.");
		var nativeWindow = GetPropertyValue(hostWindow, "NativeWindow")
			?? throw new InvalidOperationException("NativeWindow is null.");
		var hwnd = GetPropertyValue(nativeWindow, "Hwnd");

		return hwnd switch
		{
			IntPtr value => (HWND)value,
			_ => throw new InvalidOperationException("Could not read native window handle.")
		};
	}

	private static object? GetPropertyValue(object instance, string propertyName)
	{
		var property = instance.GetType().GetProperty(
			propertyName,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		return property?.GetValue(instance);
	}

	private static int CountTopLevelWebViewWindows()
	{
		var webViewWindowCount = 0;
		PInvoke.EnumWindows((hwnd, _) =>
		{
			if (IsWebViewWindow(hwnd))
			{
				webViewWindowCount++;
			}

			return true;
		}, 0);

		return webViewWindowCount;
	}

	private static int CountChildWebViewWindows(HWND parentHwnd)
	{
		var childCount = 0;
		PInvoke.EnumChildWindows(parentHwnd, (hwnd, _) =>
		{
			if (IsWebViewWindow(hwnd))
			{
				childCount++;
			}

			return true;
		}, 0);

		return childCount;
	}

	private static unsafe bool IsWebViewWindow(HWND hwnd)
	{
		const string targetClassName = "UnoPlatformWebViewWindow";
		const int classNameBufferSize = 256;

		char* classNameBuffer = stackalloc char[classNameBufferSize];
		var classNameLength = PInvoke.GetClassName(hwnd, new PWSTR(classNameBuffer), classNameBufferSize);
		if (classNameLength <= 0)
		{
			return false;
		}

		var className = new string(classNameBuffer, 0, classNameLength);
		return string.Equals(className, targetClassName, StringComparison.Ordinal);
	}
}
