#if __SKIA__
#nullable enable
using System;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

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

	[TestMethod]
	public void When_Window_Is_Shown_Then_First_Frame_Gate_Is_Open()
	{
		var host = GetHostForCurrentWindow();
		Assert.IsNotNull(host);

		var activity = GetMember(host, "Activity");
		Assert.IsNotNull(activity);

		// The activity can attach its content view before the wrapper subscribes to
		// ContentViewAttachedToWindow, so the wrapper seeds its gate from this state instead.
		// If it is false while a window is shown, ActivationPreDrawListener cancels every draw
		// pass and the window renders nothing at all -- including never z-ordering its
		// SurfaceView behind itself, which shows up as a permanently black app.
		Assert.AreEqual(
			true,
			GetMember(activity, "IsContentViewAttachedToWindow"),
			"A shown window must have its content view attached, or the pre-draw gate never opens.");

		var wrapper = GetMember(activity, "Wrapper");
		Assert.IsNotNull(wrapper);
		Assert.AreEqual(
			0,
			GetField(wrapper, "_awaitingFirstFrame"),
			"The first-frame gate must be released once the window has presented a frame.");
	}

	[TestMethod]
	public void When_Main_Window_Then_Close_Only_Hides_It()
	{
		// Android keeps the process after the main task finishes and shows the main window again
		// on the next launch, so closing it must not be final. Secondary windows go with their task.
		Assert.IsFalse(TestServices.WindowHelper.CurrentTestWindow.NativeWrapper!.ClosesPermanently);

		var secondary = new Window();
		try
		{
			Assert.IsTrue(secondary.NativeWrapper!.ClosesPermanently);
		}
		finally
		{
			secondary.Close();
		}
	}

	[TestMethod]
	public void When_Title_Set_Before_Activation_Then_It_Is_Kept()
	{
		// A secondary window has no activity until it is activated.
		var secondary = new Window();
		try
		{
			secondary.Title = "Secondary window";

			Assert.AreEqual("Secondary window", secondary.Title);
		}
		finally
		{
			secondary.Close();
		}
	}

	[TestMethod]
	public void When_Secondary_Window_Closed_Before_Activation_Then_Host_Is_Released()
	{
		var secondary = new Window();
		var xamlRoot = ((NativeWindowWrapperBase)secondary.NativeWrapper!).XamlRoot!;
		Assert.IsNotNull(XamlRootMap.GetHostForRoot(xamlRoot));

		secondary.Close();

		// No activity will ever be destroyed for it, so the close itself must drop the registration.
		Assert.IsNull(XamlRootMap.GetHostForRoot(xamlRoot));
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

	private static object? GetField(object instance, string name)
		=> instance.GetType()
			.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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
