#if __SKIA__
#nullable enable
using System;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Windowing;

/// <summary>
/// Guards the Skia-on-Android de-singletoning: the window's host, its driving activity and its
/// input sources must be resolvable per window rather than from process-wide statics.
///
/// Uses reflection because the RuntimeTests project takes no compile-time dependency on
/// Uno.UI.Runtime.Skia.Android — only the Android Skia host loads it.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public class Given_AndroidSkiaXamlRootHost
{
	[TestMethod]
	public void When_Window_Then_Host_Is_Registered_For_Its_XamlRoot()
	{
		var host = GetHostForCurrentWindow();

		Assert.IsNotNull(host, "The window's XamlRoot must resolve to a host through XamlRootMap.");
		Assert.AreEqual(
			"AndroidSkiaXamlRootHost",
			host.GetType().Name,
			"The Android Skia host must be the registered IXamlRootHost.");
	}

	[TestMethod]
	public void When_Host_Then_Activity_Is_The_Foreground_Activity()
	{
		var host = GetHostForCurrentWindow();
		Assert.IsNotNull(host);

		var activity = GetMember(host, "Activity");
		Assert.IsNotNull(activity, "The host must resolve the activity currently driving its window.");

		// ContextHelper.Current is the foreground activity; with a single window it is the same
		// instance the host resolves. This is what breaks first if the wrapper stops being
		// re-pointed at the activity driving the window.
		var contextHelper = FindType("Uno.UI.ContextHelper");
		Assert.IsNotNull(contextHelper, "Uno.UI.ContextHelper must be present on Android.");
		var current = contextHelper.GetProperty("Current", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);

		Assert.AreSame(current, activity, "The host's activity must be the foreground activity.");
	}

	[TestMethod]
	public void When_Host_Then_Input_Sources_Are_Stable_Per_Window()
	{
		var host = GetHostForCurrentWindow();
		Assert.IsNotNull(host);

		var pointer = GetMember(host, "PointerSource");
		var keyboard = GetMember(host, "KeyboardSource");

		Assert.IsNotNull(pointer, "The window's host must expose its own pointer source.");
		Assert.IsNotNull(keyboard, "The window's host must expose its own keyboard source.");

		// Owned by the window's wrapper, so repeated resolution must yield the same instances
		// rather than newly created (or globally shared) ones.
		Assert.AreSame(pointer, GetMember(host, "PointerSource"));
		Assert.AreSame(keyboard, GetMember(host, "KeyboardSource"));
	}

	private static object? GetHostForCurrentWindow()
	{
		var xamlRoot = TestServices.WindowHelper.CurrentTestWindow.Content?.XamlRoot;
		if (xamlRoot is null)
		{
			return null;
		}

		var xamlRootMapType = typeof(XamlRoot).Assembly.GetType("Uno.UI.Hosting.XamlRootMap")
			?? throw new InvalidOperationException("XamlRootMap type not found.");
		var getHost = xamlRootMapType.GetMethod(
			"GetHostForRoot",
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new InvalidOperationException("XamlRootMap.GetHostForRoot not found.");

		return getHost.Invoke(null, new object[] { xamlRoot });
	}

	private static object? GetMember(object instance, string name)
		=> instance.GetType()
			.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			?.GetValue(instance);

	private static Type? FindType(string fullName)
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.GetType(fullName, throwOnError: false) is { } type)
			{
				return type;
			}
		}

		return null;
	}
}
#endif
