using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace Uno.UI
{
	public static class ViewHelper
	{
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
	}
}
