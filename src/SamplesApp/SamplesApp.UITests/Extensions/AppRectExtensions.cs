using Uno.UITest;

namespace SamplesApp.UITests
{
	public static class AppRectExtensions
	{
		public static IAppRect ApplyScale(this IAppRect rect, float scale) =>
			new AppRect(
				x: rect.X * scale,
				y: rect.Y * scale,
				width: rect.Width * scale,
				height: rect.Height * scale
			);

		public static IAppRect UnapplyScale(this IAppRect rect, float scale) => rect.ApplyScale(1f / scale);

		public static IAppRect InflateBy(this IAppRect rect, float thickness) => rect.DeflateBy(-thickness);

		public static IAppRect DeflateBy(this IAppRect rect, float thickness)
		{
			var doubleThickness = thickness * 2f;
			var x = rect.X + thickness;
			var y = rect.Y + thickness;
			var w = rect.Width - doubleThickness;
			var h = rect.Height - doubleThickness;

			if (w <= 0f)
			{
				x = rect.CenterX;
				w = 0f;
			}

			if (h <= 0f)
			{
				y = rect.CenterY;
				h = 0f;
			}

			return new AppRect(x, y, w, h);
		}
	}
}
