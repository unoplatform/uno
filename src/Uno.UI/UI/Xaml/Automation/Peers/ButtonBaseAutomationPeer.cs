#pragma warning disable 108 // new keyword hiding
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ButtonBaseAutomationPeer : FrameworkElementAutomationPeer
	{
		protected ButtonBaseAutomationPeer(ButtonBase buttonBase) : base(buttonBase)
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
