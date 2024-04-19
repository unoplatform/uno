using Uno;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer
	{
		internal static bool ListenerExistsHelper(AutomationEvents eventId)
#if __SKIA__
			=> AutomationPeerListener?.ListenerExistsHelper(eventId) == true;
#else
			=> false;
#endif
	}
}
