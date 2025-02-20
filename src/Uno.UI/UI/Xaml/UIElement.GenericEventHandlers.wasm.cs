using System;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		/// <summary>
		/// Generic handlers for DOM mapped events
		/// </summary>
		internal static class GenericEventHandlers
		{
			internal static object RaiseEventHandler(Delegate d, object sender, object args)
			{
				if (d is EventHandler handler)
				{
					handler(sender, args as EventArgs);
					return null;
				}

				throw new InvalidOperationException($"The parameters for invoking GenericEventHandlers.RaiseEventHandler with {d} are incorrect");
			}

			internal static object RaiseRawEventHandler(Delegate d, object sender, object args)
			{
				if (d is RawEventHandler handler)
				{
					return handler(sender as UIElement, args as string);
				}

				throw new InvalidOperationException($"The parameters for invoking GenericEventHandlers.RaiseEventHandler with {d} are incorrect");
			}

			internal static object RaiseRoutedEventHandler(Delegate d, object sender, object args)
			{
				if (d is RoutedEventHandler handler)
				{
					handler(sender, args as RoutedEventArgs);
					return null;
				}

				throw new InvalidOperationException($"The parameters for invoking GenericEventHandlers.RaiseEventHandler with {d} are incorrect");
			}

			internal static object RaiseExceptionRoutedEventHandler(Delegate d, object sender, object args)
			{
				if (d is ExceptionRoutedEventHandler handler)
				{
					handler(sender, args as ExceptionRoutedEventArgs);
					return null;
				}
				return null;
			}

			internal static object RaiseRoutedEventHandlerWithHandled(Delegate d, object sender, object args)
			{
				if (d is RoutedEventHandlerWithHandled handler)
				{
					return handler(sender, args as RoutedEventArgs);
				}
				return null;
			}
		}
	}
}
