using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	internal interface ICustomScrollInfo
	{
		/// <summary>
		/// Defines the desired viewport width, if not set, fallbacks to the default algorithm.
		/// This is used in the ScrollViewer measure and in computation of all the ScrollViewer properties.
		/// </summary>
		public double? ViewportWidth { get; }

		/// <summary>
		/// Defines the desired viewport height, if not set, fallbacks to the default algorithm
		/// This is used in the ScrollViewer measure and in computation of all the ScrollViewer properties.
		/// </summary>
		public double? ViewportHeight { get; }
	}

	internal static class CustomScrollInfoExtensions
	{
		public static void ApplyViewport(this ICustomScrollInfo scrollInfo, ref Size size)
		{
			if (scrollInfo is null)
			{
				return;
			}

			if (scrollInfo.ViewportWidth is { } width)
			{
				size.Width = width;
			}
			if (scrollInfo.ViewportHeight is { } height)
			{
				size.Height = height;
			}
		}

		public static void ApplyViewport(this ICustomScrollInfo scrollInfo, ref Rect rect)
		{
			if (scrollInfo is null)
			{
				return;
			}

			if (scrollInfo.ViewportWidth is { } width)
			{
				rect.Width = width;
			}
			if (scrollInfo.ViewportHeight is { } height)
			{
				rect.Height = height;
			}
		}
	}
}
