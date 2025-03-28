namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines constants that specify a control's preference for whether sounds are played.
	/// </summary>
	public enum ElementSoundMode
	{
		/// <summary>
		/// Sound is played based on the ElementSoundPlayer.State setting.
		/// </summary>
		Default,

		/// <summary>
		/// Sound is played only when focus on the control changes.
		/// </summary>
		FocusOnly,

		/// <summary>
		/// No sounds are played.
		/// </summary>
		Off,
	}
}
