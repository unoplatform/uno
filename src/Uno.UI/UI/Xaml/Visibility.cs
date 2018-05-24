namespace Windows.UI.Xaml
{
	/// <summary>
	/// Specifies the display state of an element.
	/// </summary>
	public enum Visibility
	{
		/// <summary>
		/// Display the element.
		/// </summary>
		Visible,
		/// <summary>
		/// Do not display the element, and do not reserve space for it in layout.
		/// </summary>
		Collapsed,
	}

	public static class VisiblityExtensions
	{
		/// <summary>
		/// Determines if the specified visibility is hidden as per UIKit conventions
		/// </summary>
		/// <param name="visibility"></param>
		/// <returns></returns>
		public static bool IsHidden(this Visibility visibility)
		{
			return visibility == Visibility.Collapsed;
		}
	}
}

