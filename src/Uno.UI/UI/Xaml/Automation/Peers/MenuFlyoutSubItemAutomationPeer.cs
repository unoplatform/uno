using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal class MenuFlyoutSubItemAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
	public MenuFlyoutSubItemAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(MenuFlyoutSubItem);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;

	protected override int GetPositionInSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetPositionInSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			returnValue = MenuFlyoutPresenter.GetPositionInSetHelper((MenuFlyoutItemBase)Owner);
		}

		return returnValue;
	}

	protected override int GetSizeOfSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetSizeOfSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			returnValue = MenuFlyoutPresenter.GetSizeOfSetHelper((MenuFlyoutItemBase)Owner);
		}

		return returnValue;
	}

	void IExpandCollapseProvider.Expand() => ((MenuFlyoutSubItem)Owner).Open();

	void IExpandCollapseProvider.Collapse() => ((MenuFlyoutSubItem)Owner).Close();

	ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState => ((MenuFlyoutSubItem)Owner).IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

	internal void RaiseExpandCollapseAutomationEvent(bool isOpen)
	{
		ExpandCollapseState oldValue;
		ExpandCollapseState newValue;
		if (isOpen)
		{
			oldValue = ExpandCollapseState.Collapsed;
			newValue = ExpandCollapseState.Expanded;
		}
		else
		{
			oldValue = ExpandCollapseState.Expanded;
			newValue = ExpandCollapseState.Collapsed;
		}

		RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldValue, newValue);
	}
}
