// MUX Reference TitleBarAutomationPeer.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Exposes TitleBar types to Microsoft UI Automation.
/// </summary>
public partial class TitleBarAutomationPeer : Automation.Peers.FrameworkElementAutomationPeer
{
	public TitleBarAutomationPeer(TitleBar owner) : base(owner)
	{
	}

	protected override Automation.Peers.AutomationControlType GetAutomationControlTypeCore()
		=> Automation.Peers.AutomationControlType.TitleBar;

	protected override string GetClassNameCore() => nameof(TitleBar);

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();

		if (string.IsNullOrEmpty(name))
		{
			if (Owner is TitleBar titleBar)
			{
				name = titleBar.Title;
			}
		}

		return name ?? string.Empty;
	}
}
