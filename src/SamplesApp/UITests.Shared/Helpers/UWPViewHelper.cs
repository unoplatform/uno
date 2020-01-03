using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;

namespace Uno.UI
{
	public static class UWPViewHelper
	{
#if !XAMARIN && !__WASM__
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size PhysicalToLogicalPixels(this Size size)
			=> size;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size LogicalToPhysicalPixels(this Size size)
			=> size;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect LogicalToPhysicalPixels(this Rect rect)
			=> rect;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect PhysicalToLogicalPixels(this Rect rect)
			=> rect;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point PhysicalToLogicalPixels(this Point point)
			=> point;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point LogicalToPhysicalPixels(this Point point)
			=> point;
#endif
	}
}
