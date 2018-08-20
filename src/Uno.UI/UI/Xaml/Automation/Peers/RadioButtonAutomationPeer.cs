using Uno;

namespace Windows.UI.Xaml.Automation.Peers
{
	public  partial class RadioButtonAutomationPeer : ToggleButtonAutomationPeer, Provider.ISelectionItemProvider
	{
		public RadioButtonAutomationPeer(Controls.RadioButton owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "RadioButton";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.RadioButton;
		}

		[NotImplemented]
		public  bool IsSelected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RadioButtonAutomationPeer.IsSelected is not implemented in Uno.");
			}
		}

		[NotImplemented]
		public Provider.IRawElementProviderSimple SelectionContainer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRawElementProviderSimple RadioButtonAutomationPeer.SelectionContainer is not implemented in Uno.");
			}
		}
				
		[NotImplemented]
		public  void AddToSelection()
		{
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer", "void RadioButtonAutomationPeer.AddToSelection()");
		}

		[NotImplemented]
		public void RemoveFromSelection()
		{
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer", "void RadioButtonAutomationPeer.RemoveFromSelection()");
		}

		[NotImplemented]
		public void Select()
		{
			Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer", "void RadioButtonAutomationPeer.Select()");
		}
	}
}
