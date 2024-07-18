using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Navigation;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace DirectUI;

partial class NavigationHistory
{
	private bool m_isNavigationPending;

	private bool m_isSetNavigationStatePending;

	// This can be NULL for cases where we skip navigating to current, when NULL it won't be added to the BackStack or ForwardStack.
	PageStackEntry m_tpCurrentPageStackEntry;
	PageStackEntry m_tpPendingPageStackEntry;

	PageStackEntryTrackerCollection m_tpForwardStack;
	TrackerPtr<PageStackEntryTrackerCollection> m_tpBackStack;

	xaml_controls::IFrame* m_pIFrame;

	xaml::Navigation::NavigationMode m_navigationMode;

}
