#if HAS_UNO
#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Disposables;

namespace Uno.UI.RuntimeTests.Helpers;

public static class AccessibilityBridgeHelper
{
	/// <summary>
	/// Turns the Android and iOS accessibility bridges back off. Any test hook that reads the native tree or its
	/// events enables them for good, which would otherwise change the behavior of every test that runs after it.
	/// </summary>
	public static void DisableMobileAccessibility(XamlRoot? xamlRoot)
	{
		if (xamlRoot is null)
		{
			return;
		}

		AccessibilityPeerHelper.AndroidDisableAccessibilityAction?.Invoke(xamlRoot);
		AccessibilityPeerHelper.IOSDisableAccessibilityAction?.Invoke(xamlRoot);
	}

	/// <summary>Whether the root's mobile bridge keeps a native tree in sync. False on other platforms.</summary>
	internal static bool IsMobileAccessibilityEnabled(XamlRoot xamlRoot)
		=> AccessibilityPeerHelper.AndroidIsAccessibilityEnabledAccessor?.Invoke(xamlRoot)
			?? AccessibilityPeerHelper.IOSIsAccessibilityEnabledAccessor?.Invoke(xamlRoot)
			?? false;

	/// <summary>
	/// Starts the test as an app no accessibility client ever queried, and goes back to that state afterwards,
	/// since the test's own query enables the bridge again.
	/// </summary>
	internal static async Task<IDisposable> SuspendMobileAccessibilityClient()
	{
		// Lets an already queued native tree rebuild run before the content under test exists.
		await TestServices.WindowHelper.WaitForIdle();

		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		DisableMobileAccessibility(xamlRoot);
		if (xamlRoot is not null && IsMobileAccessibilityEnabled(xamlRoot))
		{
			Assert.Inconclusive("An accessibility service is running on this device.");
		}

		return Disposable.Create(() => DisableMobileAccessibility(xamlRoot));
	}
}
#endif
