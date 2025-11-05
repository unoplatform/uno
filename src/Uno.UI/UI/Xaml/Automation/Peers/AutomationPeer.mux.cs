using Microsoft.UI.Xaml.Input;
using Uno;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using static Uno.UI.FeatureConfiguration;

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

		//------------------------------------------------------------------------
		//
		//  Method:   SetFocusCore
		//
		//  Synopsis:
		//      If this is Focusable and not the current focused element, then we should be able to treat it as focused.
		//      It basically supports the cases for Application Devs while they create Custom AutomationPeer for non-UIElement controls.
		//      It also enables showing Soft keyboard during IHM scenarios in such cases as needed.
		//
		//------------------------------------------------------------------------
		private void SetFocusHelper()
		{
			bool isKeyboardFocusable = IsKeyboardFocusable();
			if (isKeyboardFocusable)
			{
				if (GetFocusManagerNoRef() is FocusManager focusManager)
				{
					AutomationPeer ap = focusManager.GetFocusedAutomationPeer();

					if (this != ap)
					{
						if (S_OK == GetContext()->UIAClientsAreListening(UIAXcp::AEAutomationFocusChanged))
						{
							ap = this;

							if (this->GetAPEventsSource())
							{
								ap = this->GetAPEventsSource();
							}

							focusManager.SetFocusedAutomationPeer(this);
							ap.RaiseAutomationEvent(UIAXcp::AEAutomationFocusChanged);
						}
					}
				}
			}
		}

		//------------------------------------------------------------------------
		//
		//  Method:   SetAutomationFocusHelper
		//
		//  Synopsis:
		//     We want this method to set automation focus, but not keyboard focus, to the element that this is an automation peer of.
		//     AutomationPeer's override of SetFocusHelper() happens to do exactly this, so we'll just use its implementation.
		//     (We can't just call SetFocusHelper(), because would likely be overridden by CFrameworkElementAutomationPeer.)
		//     The motivation behind this is, for example, Pivot, where keyboard focus is given to the header panel to enable
		//     keyboarding scenarios, but we want to act as though individual PivotItems have focus for the purposes of what we
		//     report to UIA clients, since they require automation focus to be given to elements before they can read them
		//     and report their contents.
		//
		//------------------------------------------------------------------------
		private void SetAutomationFocusHelper() => SetFocusHelper();

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

		private FocusManager GetFocusManagerNoRef()
		{
			if (GetContext())
			{
				if (GetRootNoRef() is DependencyObject root)
				{
					return VisualTree.GetFocusManagerForElement(root);
				}
			}

			return null;
		}
	}
}
