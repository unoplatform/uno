using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Exposes TitleBar types to Microsoft UI Automation.
/// </summary>
public partial class TitleBarAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the TitleBarAutomationPeer class.
	/// </summary>
	/// <param name="owner"></param>
	public TitleBarAutomationPeer(TitleBar owner) : base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TitleBar;

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

		return name;
	}
}
