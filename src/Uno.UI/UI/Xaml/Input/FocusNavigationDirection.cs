namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Specifies the direction that focus moves from element to element within the app UI.
	/// </summary>
	public enum FocusNavigationDirection
	{
		/// <summary>
		/// The next element in the tab order.
		/// </summary>
		Next,

		/// <summary>
		/// The previous element in the tab order.
		/// </summary>
		Previous,

		/// <summary>
		/// An element above the element with focus.
		/// </summary>
		Up,

		/// <summary>
		/// An element below the element with focus.
		/// </summary>
		Down,

		/// <summary>
		/// An element to the left of the element with focus.
		/// </summary>
		Left,

		/// <summary>
		/// An element to the right of the element with focus.
		/// </summary>
		Right,

		/// <summary>
		/// No change in focus.
		/// </summary>
		None
	}
}
