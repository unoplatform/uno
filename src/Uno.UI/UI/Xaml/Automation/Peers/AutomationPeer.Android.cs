using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Views;
using Android.Views.Accessibility;
using Uno.UI;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer
	{
		internal virtual void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo info)
		{
		}

		internal virtual void SendAccessibilityEvent([GeneratedEnum] EventTypes eventType)
		{
		}
	}
}
