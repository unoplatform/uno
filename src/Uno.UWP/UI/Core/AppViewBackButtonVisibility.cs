namespace Windows.UI.Core
{
	/// <summary>
	/// Defines constants that specify whether the back button is shown in the system UI.
	/// </summary>
	public enum AppViewBackButtonVisibility
	{
		/// <summary>
		/// The back button is shown.
		/// </summary>
		Visible = 0,
		/// <summary>
		/// The back button is not shown and space is not reserved for it in the layout.
		/// </summary>
		Collapsed = 1,
		/// <summary>
		/// The back button is shown, but not enabled.
		/// </summary>
		Disabled = 2
	}
}
