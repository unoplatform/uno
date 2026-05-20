// MUX Reference AppBarLightDismissAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Automation peer for the AppBar light-dismiss overlay element.
/// Provides an Invoke pattern that allows screen readers to dismiss an open AppBar.
/// </summary>
internal partial class AppBarLightDismissAutomationPeer : FrameworkElementAutomationPeer, Provider.IInvokeProvider
{
	public AppBarLightDismissAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => "AppBarLightDismiss";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Button;

	protected override string GetNameCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_UIA_LIGHTDISMISS_NAME);

	protected override string GetAutomationIdCore() => "Light Dismiss";

	/// <summary>
	/// Invokes the light-dismiss action (closes the AppBar).
	/// </summary>
	public void Invoke()
	{
		// TODO Uno: AppBarLightDismiss element does not exist in Uno yet.
		// When implemented, this should call AutomationClick on the light-dismiss element.
	}
}
