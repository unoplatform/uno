using Uno.UITest;

namespace SamplesApp.UITests
{
	public static class RectExtensions
	{
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
