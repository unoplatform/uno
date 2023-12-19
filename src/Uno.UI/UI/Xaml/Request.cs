#if UNO_HAS_ENHANCED_LIFECYCLE
using System;

namespace Windows.UI.Xaml;

internal struct Request
{
	public DependencyObject Listener;
	public EventHandler Event;
	public DependencyObject Object;
	//public INTERNAL_EVENT_HANDLER m_pfnInternalEventDelegate;
	//public int Token;          // Token value representing this REQUEST for use in Add/Remove events.
	//public bool Fired;
	public bool Added;
	public bool HandledEventsToo;
}
#endif
