using System;
using System.Drawing;
using System.Linq;
using Uno.UITest;

namespace SamplesApp.UITests.Extensions
{
	internal static class AppScalingExtensions
	{
		public static IAppRect LogicalToPhysicalPixels(this IAppRect rect, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return new AppRect(
				x: rect.X * scale,
				y: rect.Y * scale,
				width: rect.Width * scale,
				height: rect.Height * scale
			);
		}

		public static IAppRect PhysicalToLogicalPixels(this IAppRect rect, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return new AppRect(
				x: rect.X / scale,
				y: rect.Y / scale,
				width: rect.Width / scale,
				height: rect.Height / scale
			);
		}

		public static Point LogicalToPhysicalPixels(this Point point, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return new Point(
				(int) (point.X * scale),
				(int) (point.Y * scale)
			);
		}

		public static Point PhysicalToLogicalPixels(this Point point, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return new Point(
				(int)(point.X / scale),
				(int)(point.Y / scale)
			);
		}

		public static Size LogicalToPhysicalPixels(this Size size, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return new Size(
				(int)(size.Width * scale),
				(int)(size.Height * scale)
			);
		}

		public static Size PhysicalToLogicalPixels(this Size size, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return new Size(
				(int)(size.Width / scale),
				(int)(size.Height / scale)
			);
		}

		public static double LogicalToPhysicalPixels(this double pixels, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return pixels * scale;
		}

		public static double PhysicalToLogicalPixels(this double pixels, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return pixels / scale;
		}

		public static int LogicalToPhysicalPixels(this int pixels, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return (int)(pixels * scale);
		}

		public static int PhysicalToLogicalPixels(this int pixels, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return (int)(pixels / scale);
		}

		public static float LogicalToPhysicalPixels(this float pixels, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return pixels * scale;
		}

		public static float PhysicalToLogicalPixels(this float pixels, IApp app)
		{
			var scale = app.GetDisplayScreenScaling();

			return pixels / scale;
		}
	}
}
