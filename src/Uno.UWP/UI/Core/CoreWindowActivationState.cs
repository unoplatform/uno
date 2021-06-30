namespace Windows.UI.Core
{
	/// <summary>
	/// Specifies the set of reasons that a CoreWindow's Activated event was raised.
	/// </summary>
	public enum CoreWindowActivationState
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
		PointerActivated
	}
}
