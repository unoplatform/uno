using System;
using System.Linq;
using Gdk;
using Gtk;

namespace Uno.UI.Runtime.Skia.Gtk;

internal class UnoEventBox : EventBox
{
	// On some windowing platforms, multitouch devices perform pointer emulation,
	// this works by granting a “pointer emulating” hint to one of the currently interacting touch sequences,
	// which will be reported on every GdkEventTouch event from that sequence.
	// By default, if a widget didn’t request touch events by setting GDK_TOUCH_MASK on its event mask and didn’t override GtkWidget::touch-event,
	// GTK will transform these “pointer emulating” events into semantically similar GdkEventButton and GdkEventMotion events.
	//
	// If the widget sets GDK_TOUCH_MASK on its event mask and doesn’t chain up on GtkWidget::touch-event,
	// only touch events will be received, and no pointer emulation will be performed.
	//
	// https://docs.gtk.org/gtk3/input-handling.html#touch-events

	/// <inheritdoc />
	protected override bool OnTouchEvent(EventTouch evnt)
	{
		Touched?.Invoke(this, evnt);
		return true;
	}

	public event EventHandler<EventTouch> Touched;

	/// <inheritdoc />
	protected override bool OnMotionNotifyEvent(EventMotion evnt)
		=> base.OnMotionNotifyEvent(evnt);
}
