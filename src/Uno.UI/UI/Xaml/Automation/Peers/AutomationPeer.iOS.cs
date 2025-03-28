using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer
	{
		internal virtual bool AccessibilityActivate()
		{
			return false;
		}

		internal virtual bool UpdateAccessibilityElement()
		{
			return false;
		}
	}
}
