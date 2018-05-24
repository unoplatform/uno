using Android.Views;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Extensions
{
	internal static class MotionEventExtensions
	{
		internal static Pointer GetPointer(this MotionEvent ev, int pointerIndex)
		{
			var properties = new MotionEvent.PointerProperties();
			ev.GetPointerProperties(pointerIndex, properties);

			return new Pointer(properties);
		}
	}
}
