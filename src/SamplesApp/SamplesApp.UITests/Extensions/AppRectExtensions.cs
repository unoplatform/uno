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
	}
}
