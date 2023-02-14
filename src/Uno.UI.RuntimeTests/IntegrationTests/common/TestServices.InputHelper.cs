using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

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

			public static void Hold(UIElement element)
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
			public static void Tap(Point point)
			{
				throw new System.NotImplementedException();
			}

			public static void ScrollMouseWheel(CalendarView cv, int i)
			{
				throw new System.NotImplementedException();
			}

			public static void LeftMouseClick(UIElement element) => Tap(element);

			public static void PenBarrelTap(FrameworkElement pElement)
			{
				throw new System.NotImplementedException();
			}

			public static void ClickMouseButton(MouseButton button, Point position)
			{
				throw new System.NotImplementedException();
			}
		}
	}
}
