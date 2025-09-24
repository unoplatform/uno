using Uno;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer
	{
		private bool HasKeyboardFocusHelper()
		{
			xref_ptr<CAutomationPeer> ap;
			XINT32 isKeyboardFocusable = FALSE;

			*pRetVal = FALSE;

			IFC_RETURN(IsKeyboardFocusable(&isKeyboardFocusable));
			if (isKeyboardFocusable)
			{
				if (CFocusManager * focusManager = GetFocusManagerNoRef())
				{
					ap = focusManager->GetFocusedAutomationPeer();

					if (this == static_cast<CAutomationPeer*>(ap.get()))
					{
						*pRetVal = TRUE;
					}
				}
			}

			return S_OK;
		}


		internal static bool ListenerExistsHelper(AutomationEvents eventId)
#if __SKIA__
			=> AutomationPeerListener?.ListenerExistsHelper(eventId) == true;
#else
			=> true;
#endif

		// Removes the leading and trailing spaces in the provided string and returns the trimmed version
		// or an empty string when no characters are left.
		// Because it is recommended to set an AppBarButton, AppBarToggleButton, MenuFlyoutItem or ToggleMenuFlyoutItem's
		// KeyboardAcceleratorTextOverride to a single space to hide their keyboard accelerator UI, this trimming method
		// prevents automation tools like Narrator from emitting a space when navigating to such an element.
		internal static string GetTrimmedKeyboardAcceleratorTextOverride(string keyboardAcceleratorTextOverride) => keyboardAcceleratorTextOverride.Trim();
	}
}
