using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

public partial class AutoSuggestBoxAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	public AutoSuggestBoxAutomationPeer(AutoSuggestBox owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(AutoSuggestBox);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}
		
		return base.GetPatternCore(patternInterface);
	}

	public void Invoke()
	{
		var owner = (AutoSuggestBox)Owner;
		owner.ProgrammaticSubmitQuery();
	}
}
