namespace Windows.UI.Core
{
	/// <summary>
	/// Defines constants that specify the activation state of a window.
	/// </summary>
	public enum CoreWindowActivationMode
	{
		/// <summary>
		/// No state is specified.
		/// </summary>
		None,

		/// <summary>
		/// The window is deactivated.
		/// </summary>
		Deactivated,

		/// <summary>
		/// The window is activated, but not in the foreground.
		/// </summary>
		ActivatedNotForeground,

		/// <summary>
		/// The window is activated and in the foreground.
		/// </summary>
		ActivatedInForeground,
	}
}
