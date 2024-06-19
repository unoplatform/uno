using Uno;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer
	{
		internal static bool ListenerExistsHelper(AutomationEvents eventId)
#if __SKIA__
			=> AutomationPeerListener?.ListenerExistsHelper(eventId) == true;
#else
			=> true;
#endif
		/// <summary>
		/// Removes the leading and trailing spaces in the provided string and returns the trimmed version
		/// or an empty string when no characters are left.
		/// Because it is recommended to set an AppBarButton, AppBarToggleButton, MenuFlyoutItem or ToggleMenuFlyoutItem's
		/// KeyboardAcceleratorTextOverride to a single space to hide their keyboard accelerator UI, this trimming method
		/// prevents automation tools like Narrator from emitting a space when navigating to such an element.
		/// </summary>
		private protected static string GetTrimmedKeyboardAcceleratorTextOverride(string keyboardAcceleratorTextOverride)
		{
			// Return an empty string when the provided keyboardAcceleratorTextOverride is already empty.
			if (!string.IsNullOrEmpty(keyboardAcceleratorTextOverride))
			{
				var trimmedKeyboardAcceleratorTextOverride = keyboardAcceleratorTextOverride.TrimStart(' ');

				// Return an empty string when the remaining string is empty.
				if (!string.IsNullOrEmpty(trimmedKeyboardAcceleratorTextOverride))
				{
					// Trim the trailing spaces as well.
					trimmedKeyboardAcceleratorTextOverride = trimmedKeyboardAcceleratorTextOverride.TrimEnd(' ');
					return trimmedKeyboardAcceleratorTextOverride;
				}
			}

			// Return an empty string
			return "";
		}
	}
}
