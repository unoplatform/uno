// MUX Reference SplitViewLightDismissAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Automation peer for the SplitView light-dismiss overlay element.
/// Provides an Invoke pattern that allows screen readers to dismiss the SplitView pane
/// when it is in a light-dismissible display mode.
/// </summary>
internal partial class SplitViewLightDismissAutomationPeer : FrameworkElementAutomationPeer, Provider.IInvokeProvider
{
	public SplitViewLightDismissAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke && IsLightDismissEnabled())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => "SplitViewLightDismiss";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Button;

	protected override string GetNameCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_UIA_LIGHTDISMISS_NAME);

	protected override string GetAutomationIdCore() => "LightDismiss";

	/// <summary>
	/// Invokes the light-dismiss action (closes the SplitView pane).
	/// </summary>
	public void Invoke()
	{
		if (Owner is FrameworkElement fe)
		{
			var splitView = fe.TemplatedParent as SplitView;
			if (splitView is not null && splitView.IsPaneOpen)
			{
				splitView.IsPaneOpen = false;
			}
		}
	}

	private bool IsLightDismissEnabled()
	{
		if (Owner is FrameworkElement fe)
		{
			var splitView = fe.TemplatedParent as SplitView;
			if (splitView is not null)
			{
				// Light dismiss is available in Overlay and CompactOverlay modes
				return splitView.DisplayMode == SplitViewDisplayMode.Overlay ||
					   splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay;
			}
		}

		return false;
	}
}
