using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class TextBlockAutomationPeer : FrameworkElementAutomationPeer
	{
		public TextBlockAutomationPeer(TextBlock owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return base.GetClassNameCore();
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Text;
		}
	}
}
