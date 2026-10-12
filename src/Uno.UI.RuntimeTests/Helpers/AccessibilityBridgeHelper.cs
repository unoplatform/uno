#if HAS_UNO
#nullable enable
using Microsoft.UI.Xaml;

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
}
#endif
