#nullable enable

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Private.Infrastructure;
using Uno.UI.Hosting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// The Android render view gains native focus and receives keys as ordinary input (e.g. a TextBox showing the
/// keyboard requests focus on it). Neither is an accessibility client, so neither may enable the bridge or move XAML focus.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public class Given_AndroidAccessibilityNativeFocus
{
	private const int KeyEventActionDown = 0;
	private const int KeycodeTab = 61;

	[TestMethod]
	public async Task When_No_Client_Then_Native_Focus_Does_Not_Enable_The_Bridge()
	{
		using var _ = await SuspendAccessibilityClient();

		var button = new Button { Content = "Focus target" };
		await UITestHelper.Load(button);

		RefocusRenderView(button.XamlRoot!);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(IsAccessibilityEnabled(button.XamlRoot!), "Native focus on the render view is not an accessibility client.");
	}

	[TestMethod]
	public async Task When_No_Client_Then_Keys_Do_Not_Enable_The_Bridge()
	{
		using var _ = await SuspendAccessibilityClient();

		var button = new Button { Content = "Key target" };
		await UITestHelper.Load(button);

		DispatchKeyDown(button.XamlRoot!, KeycodeTab);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(IsAccessibilityEnabled(button.XamlRoot!), "A key on the render view is not an accessibility client.");
	}

	[TestMethod]
	public async Task When_Render_View_Gains_Native_Focus_Then_Xaml_Focus_Does_Not_Move()
	{
		var top = new Button { Content = "Top" };
		var target = new Button { Content = "Target" };
		await UITestHelper.Load(new StackPanel { Children = { top, target } });

		// A tree reader is active, as when a screen reader runs.
		var xamlRoot = target.XamlRoot!;
		_ = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot);
		Assert.IsTrue(IsAccessibilityEnabled(xamlRoot));

		target.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreSame(target, FocusManager.GetFocusedElement(xamlRoot));

		RefocusRenderView(xamlRoot);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreSame(target, FocusManager.GetFocusedElement(xamlRoot), "Native focus on the render view must not pick a XAML element.");
	}

	private static object GetRenderView(XamlRoot xamlRoot)
	{
		var host = XamlRootMap.GetHostForRoot(xamlRoot) ?? throw new InvalidOperationException("No host for the XamlRoot.");
		var activity = host.GetType().GetProperty("Activity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(host)
			?? throw new InvalidOperationException("The host has no activity.");
		return activity.GetType().GetProperty("RenderView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(activity)
			?? throw new InvalidOperationException("The activity has no render view.");
	}

	// Clearing the only focusable view hands focus straight back in touch mode, so either call makes it gain focus.
	private static void RefocusRenderView(XamlRoot xamlRoot)
	{
		var renderView = GetRenderView(xamlRoot);
		renderView.GetType().GetMethod("ClearFocus", Type.EmptyTypes)!.Invoke(renderView, null);
		renderView.GetType().GetMethod("RequestFocus", Type.EmptyTypes)!.Invoke(renderView, null);
	}

	private static void DispatchKeyDown(XamlRoot xamlRoot, int keyCode)
	{
		var keyEventType = Type.GetType("Android.Views.KeyEvent, Mono.Android", throwOnError: true)!;
		var actionType = Type.GetType("Android.Views.KeyEventActions, Mono.Android", throwOnError: true)!;
		var keycodeType = Type.GetType("Android.Views.Keycode, Mono.Android", throwOnError: true)!;
		var keyEvent = Activator.CreateInstance(
			keyEventType,
			Enum.ToObject(actionType, KeyEventActionDown),
			Enum.ToObject(keycodeType, keyCode));

		var renderView = GetRenderView(xamlRoot);
		renderView.GetType().GetMethod("DispatchKeyEvent", new[] { keyEventType })!.Invoke(renderView, new[] { keyEvent });
	}

	private static object? GetAccessibility(XamlRoot xamlRoot)
		=> XamlRootMap.GetHostForRoot(xamlRoot) is { } host
			? host.GetType().GetProperty("Accessibility", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(host)
			: null;

	private static bool IsAccessibilityEnabled(XamlRoot xamlRoot)
		=> GetAccessibility(xamlRoot) is { } accessibility &&
			accessibility.GetType().GetProperty("IsAccessibilityEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(accessibility) is true;

	// Earlier tests read the tree, which keeps the bridge enabled: start again as an app no client ever queried.
	private static async Task<IDisposable> SuspendAccessibilityClient()
	{
		await TestServices.WindowHelper.WaitForIdle();

		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		var accessibility = GetAccessibility(xamlRoot) ?? throw new InvalidOperationException("No Android accessibility bridge.");
		var type = accessibility.GetType();
		var requestedField = type.GetField("_clientRequestedTree", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("_clientRequestedTree not found.");
		var ensureRequested = type.GetMethod("EnsureTreeRequested", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new InvalidOperationException("EnsureTreeRequested not found.");

		var wasRequested = requestedField.GetValue(accessibility) is true;
		requestedField.SetValue(accessibility, false);
		var resume = new Resume(() =>
		{
			if (wasRequested)
			{
				ensureRequested.Invoke(accessibility, null);
			}
		});

		if (IsAccessibilityEnabled(xamlRoot))
		{
			resume.Dispose();
			Assert.Inconclusive("An accessibility service is running on this device.");
		}

		return resume;
	}

	private sealed class Resume(Action? resume) : IDisposable
	{
		private Action? _resume = resume;

		public void Dispose()
		{
			_resume?.Invoke();
			_resume = null;
		}
	}
}
