#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

internal static class MobileAccessibilityTestHelper
{
	internal static IReadOnlyList<AccessibilityPeerNode> GetPeerTree(AutomationPeer root)
		=> AccessibilityPeerHelper.GetPeerTree(root);

	internal static IReadOnlyList<AccessibilityPeerNode> GetPeerTree(UIElement root)
		=> AccessibilityPeerHelper.GetPeerTree(root);

	/// <summary>
	/// Returns the native node snapshot for <paramref name="element"/> on the current platform.
	/// Android accessor is tried first; iOS accessor is the fallback.
	/// Returns null when neither hook is registered (e.g. desktop Skia) or when the element
	/// has no corresponding native node.
	/// </summary>
	internal static AccessibilityNativeNodeSnapshot? TryGetNativeSnapshot(UIElement element)
		=> AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(element)
			?? AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(element);
}

internal sealed class OwnerlessLiveRegionAutomationPeer : AutomationPeer
{
	private readonly string _name;

	internal OwnerlessLiveRegionAutomationPeer(string name)
		=> _name = name;

	protected override string GetNameCore() => _name;

	protected override AutomationLiveSetting GetLiveSettingCore() => AutomationLiveSetting.Assertive;

	protected override bool IsControlElementCore() => true;

	protected override bool IsContentElementCore() => true;
}
