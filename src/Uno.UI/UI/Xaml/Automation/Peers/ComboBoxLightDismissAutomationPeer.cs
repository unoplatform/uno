// MUX Reference ComboBoxLightDismissAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Automation peer for the ComboBox light-dismiss overlay element.
/// Provides an Invoke pattern that allows screen readers to dismiss an open ComboBox dropdown.
/// </summary>
internal partial class ComboBoxLightDismissAutomationPeer : FrameworkElementAutomationPeer, Provider.IInvokeProvider
{
	public ComboBoxLightDismissAutomationPeer(FrameworkElement owner) : base(owner)
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

	protected override string GetClassNameCore() => "ComboBoxLightDismiss";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Button;

	protected override string GetNameCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_UIA_LIGHTDISMISS_NAME);

	protected override string GetAutomationIdCore() => "Light Dismiss";

	/// <summary>
	/// Invokes the light-dismiss action (closes the ComboBox dropdown).
	/// </summary>
	public void Invoke()
	{
		// TODO Uno: ComboBoxLightDismiss element does not exist in Uno yet.
		// When implemented, this should call AutomationClick on the light-dismiss element.
	}
}
