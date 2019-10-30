using System;
using Windows.UI.Xaml;

namespace Uno.Extensions
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

		/// <summary>
		/// Will invoke `addEventListener(<paramref name="eventName"/>)` on the corresponding HTML element.
		/// </summary>
		/// <remarks>
		/// For use with CustomEvent("name", {detail:{detail here}}).
		/// </remarks>
		/// <param name="isDetailJson">
		/// True will JSON.stringify the content. False will treat the content as a string.
		/// </param>
		public static void RegisterHtmlCustomEventHandler(this UIElement element, string eventName, EventHandler<HtmlCustomEventArgs> handler, bool isDetailJson = false)
		{
			var extractor = isDetailJson
				? UIElement.HtmlEventExtractor.CustomEventDetailJsonExtractor
				: UIElement.HtmlEventExtractor.CustomEventDetailStringExtractor;

			element.RegisterEventHandler(
				eventName,
				handler,
				eventExtractor: extractor,
				payloadConverter: (_, s) => new HtmlCustomEventArgs(s));
		}

		/// <summary>
		/// Unregister previously registered event with RegisterHtmlCustomEventHandler.
		/// </summary>
		public static void UnregisterHtmlCustomEventHandler(this UIElement element, string eventName, EventHandler<HtmlCustomEventArgs> handler)
		{
			element.UnregisterEventHandler(eventName, handler);
		}
		
		/// <summary>
		/// Get the parent view in the visual tree.
		/// </summary>
		public static UIElement GetVisualTreeParent(this UIElement element) => element?.GetParent() as UIElement;
	}
}
