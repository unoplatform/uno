

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// An Uno Platform specific <see cref="Microsoft.UI.Xaml.Window"/> helper.
	/// </summary>
	public static class WindowHelper
	{

		/// <summary>
		/// Sets the Window background
		/// </summary>
		public static void SetBackground(this Window window, Brush background)
		{
			window.Background = background;
		}
	}
}
