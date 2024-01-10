using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers
{
	public partial class SplitButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IInvokeProvider
	{
		private readonly SplitButton _owner;

		public SplitButtonAutomationPeer(SplitButton owner) : base(owner)
		{
			_owner = owner;
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.ExpandCollapse ||
				patternInterface == PatternInterface.Invoke)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore() => nameof(SplitButton);

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.SplitButton;

		private SplitButton GetImpl() => _owner;

		// IExpandCollapseProvider 
		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				ExpandCollapseState currentState = ExpandCollapseState.Collapsed;
				var splitButton = GetImpl();
				if (splitButton != null)
				{
					if (splitButton.IsFlyoutOpen)
					{
						currentState = ExpandCollapseState.Expanded;
					}
				}
				return currentState;
			}
		}

		public void Expand() => GetImpl()?.OpenFlyout();

		public void Collapse() => GetImpl()?.CloseFlyout();

		// IInvokeProvider
		public void Invoke() => GetImpl()?.OnClickPrimary(null, null);
	}
}
