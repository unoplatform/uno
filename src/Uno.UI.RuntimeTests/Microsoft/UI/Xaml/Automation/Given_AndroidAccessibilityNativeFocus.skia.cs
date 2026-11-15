#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Private.Infrastructure;
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

	// Goes through the bridge's own hooks: NativeAOT trims the render view members reflection would need.
	private static void RefocusRenderView(XamlRoot xamlRoot)
		=> (AccessibilityPeerHelper.AndroidRefocusRenderViewAction ?? throw new InvalidOperationException("No Android accessibility bridge."))(xamlRoot);

	private static void DispatchKeyDown(XamlRoot xamlRoot, int keyCode)
		=> (AccessibilityPeerHelper.AndroidRenderViewKeyDownAction ?? throw new InvalidOperationException("No Android accessibility bridge."))(xamlRoot, keyCode);

	private static bool IsAccessibilityEnabled(XamlRoot xamlRoot) => AccessibilityBridgeHelper.IsMobileAccessibilityEnabled(xamlRoot);

	private static Task<IDisposable> SuspendAccessibilityClient() => AccessibilityBridgeHelper.SuspendMobileAccessibilityClient();
}
