using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ButtonAutomationPeer : ButtonBaseAutomationPeer, IInvokeProvider
	{
		public ButtonAutomationPeer(Button owner) : base(owner)
		{
		}
		
		protected override string GetClassNameCore()
		{
			return "Button";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Button;
		}

		public void Invoke()
		{
			if (IsEnabled())
			{
				(Owner as Button).AutomationPeerClick();
			}
		}
	}
}
