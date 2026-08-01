namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Specifies the set of reasons that a window's Activated event was raised.
	/// </summary>
	public enum WindowActivationState
	{
		/// <summary>
		/// The window was activated by a call to Activate.
		/// </summary>
		CodeActivated,

		/// <summary>
		/// The window was deactivated.
		/// </summary>
		Deactivated,

		/// <summary>
		/// The window was activated by pointer interaction.
		/// </summary>
		PointerActivated,
	}
}
