using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Uno.UI.Xaml.Core;

internal partial class RootScrollViewer
{


	// RootScrollViewer prevent to show the root ScrollViewer automation peer.
	protected override AutomationPeer OnCreateAutomationPeer() => null;
}
