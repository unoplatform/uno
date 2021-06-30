using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Private.Infrastructure
{
	partial class TestServices
	{
		public static class InputHelper
		{
			public static void DynamicPressCenter(UIElement element, double x, double y, PointerFinger finger)
			{
				throw new System.NotImplementedException();
			}
			public static void DynamicRelease(PointerFinger finger)
			{
				throw new System.NotImplementedException();
			}

			public static void MoveMouse(Point position)
			{
				throw new System.NotImplementedException();
			}

			public static void MoveMouse(UIElement element)
			{
				throw new System.NotImplementedException();
			}

			public static void Tap(UIElement element)
			{
#if !NETFX_CORE
				var args = new TappedEventArgs(PointerDeviceType.Touch, default, 1);
				element.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs(element, args));
#endif
			}

			public static void ScrollMouseWheel(CalendarView cv, int i)
			{
				throw new System.NotImplementedException();
			}

			public static void LeftMouseClick(UIElement element) => Tap(element);
		}
	}
}
