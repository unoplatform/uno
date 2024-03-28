namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Defines values used to specify how items can be selected.
	/// </summary>
	public enum SelectionMode
	{
		/// <summary>
		/// Only a single item can be selected.
		/// </summary>
		Single,

		/// <summary>
		/// Multiple items can be selected (no special mode required).
		/// </summary>
		Multiple,

		/// <summary>
		/// Multiple items can be selected within a special mode (such as with a modifier key).
		/// </summary>
		Extended
	}
}
