#nullable enable

namespace Windows.UI.Xaml.Controls
{
	public static class ScrollViewerExtensions
	{
		/// <summary>
		/// Sets the padding on the scrollviewer.
		/// </summary>
		/// <param name="sv">The scrollviewer</param>
		/// <param name="padding">The padding thickness</param>
		private static ScrollViewer Padding(this ScrollViewer sv, Thickness padding)
		{

#if !IS_UNIT_TESTS
			sv.Padding = padding;
#endif

			return sv;
		}

		/// <summary>
		/// Sets the padding on the scrollviewer.
		/// </summary>
		/// <param name="sv">The scrollviewer</param>
		/// <param name="left">The left padding</param>
		/// <param name="top">The top padding</param>
		/// <param name="right">The right padding</param>
		/// <param name="bottom">The bottom padding</param>
		public static ScrollViewer Padding(this ScrollViewer sv, float left, float top, float right, float bottom)
		{
			return sv.Padding(new Thickness(left, top, right, bottom));
		}
	}
}
