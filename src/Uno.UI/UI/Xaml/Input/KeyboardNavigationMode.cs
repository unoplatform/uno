namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Specifies the tabbing behavior across tab stops for a tabbing sequence within a container.
	/// </summary>
	public enum KeyboardNavigationMode
	{
		/// <summary>
		/// Tab indexes are considered on the local subtree only inside this container.
		/// </summary>
		Local,

		/// <summary>
		/// Focus returns to the first or the last keyboard navigation stop inside
		/// of a container when the first or last keyboard navigation stop is reached.
		/// </summary>
		Cycle,

		/// <summary>
		/// The container and all of its child elements as a whole receive focus only once.
		/// </summary>
		Once,
	}
}
