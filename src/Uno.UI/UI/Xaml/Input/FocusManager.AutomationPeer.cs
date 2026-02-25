#nullable enable
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Input
{
	public partial class FocusManager
	{
		private static AutomationPeer? _s_focusedAutomationPeer;

		/// <summary>
		/// Get the currently focused automation peer (static helper used by AutomationPeer).
		/// </summary>
		public static AutomationPeer? GetFocusedAutomationPeer()
			=> _s_focusedAutomationPeer;

		/// <summary>
		/// Set the currently focused automation peer on the instance FocusManager (called from peers).
		/// </summary>
		public void SetFocusedAutomationPeer(AutomationPeer? peer)
		{
			_s_focusedAutomationPeer = peer;
		}
	}
}
