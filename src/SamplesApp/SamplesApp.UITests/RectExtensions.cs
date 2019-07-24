using Uno.UITest;

namespace SamplesApp.UITests
{
	public static class RectExtensions
	{
		public static (float x, float y) GetCenter(this IAppRect rect)
		{
			return (x: rect.Width / 2 + rect.X, y: rect.Height / 2 + rect.Y);
		}

		public static float GetRight(this IAppRect rect)
		{
			return rect.X + rect.Width;
		}

		public static float GetBottom(this IAppRect rect)
		{
			return rect.Y + rect.Height;
		}
	}
}
