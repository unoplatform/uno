using System;

namespace Windows.UI.Xaml
{
	public static class UIElementExtensions
	{
		/// <summary>
		/// Will invoke `addEventListener(<paramref name="eventName"/>)` on the corresponding HTML element.
		/// </summary>
		public static void RegisterHtmlEventHandler(this UIElement element, string eventName, EventHandler handler)
		{
			element.RegisterEventHandler(eventName, handler);
		}

		/// <summary>
		/// Unregister previously registered event with RegisterHtmlEventHandler.
		/// </summary>
		public static void UnregisterHtmlEventHandler(this UIElement element, string eventName, EventHandler handler)
		{
			element.UnregisterEventHandler(eventName, handler);
		}
	}
}
