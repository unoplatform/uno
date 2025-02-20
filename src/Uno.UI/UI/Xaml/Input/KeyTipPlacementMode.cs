namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Defines constants that specify where a Key Tip is placed in relation to a UIElement.
	/// </summary>
	public enum KeyTipPlacementMode
	{
		/// <summary>
		/// The placement of the Key Tip is determined by the system.
		/// </summary>
		Auto,

		/// <summary>
		/// The Key Tip is placed below the element.
		/// </summary>
		Bottom,

		/// <summary>
		/// he Key Tip is placed above the element.
		/// </summary>
		Top,

		/// <summary>
		/// The Key Tip is placed left of the element.
		/// </summary>
		Left,

		/// <summary>
		/// The Key Tip is placed right of the element.
		/// </summary>
		Right,

		/// <summary>
		/// The Key Tip is centered on the element.
		/// </summary>
		Center,

		/// <summary>
		/// The Key Tip is not shown.
		/// </summary>
		Hidden,
	}
}
