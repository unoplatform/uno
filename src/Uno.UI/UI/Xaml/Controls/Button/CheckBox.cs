using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
    public partial class CheckBox : ToggleButton
    {
		public CheckBox()
		{
			InitializeVisualStates();
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new CheckBoxAutomationPeer(this);
		}
	}
}