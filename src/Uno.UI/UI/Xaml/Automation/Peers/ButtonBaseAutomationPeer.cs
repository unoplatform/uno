#pragma warning disable 108 // new keyword hiding
using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class ButtonBaseAutomationPeer : FrameworkElementAutomationPeer
	{
		protected ButtonBaseAutomationPeer(ButtonBase owner) : base(owner)
		{
		}

		protected ButtonBaseAutomationPeer(ButtonBaseAutomationPeer buttonBase) : base(buttonBase)
		{
		}

		protected override bool IsControlElementCore()
		{
			return true;
		}
	}
}
