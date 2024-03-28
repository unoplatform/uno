namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines constants that specify the sound played by the ElementSoundPlayer.Play method.
	/// </summary>
	public enum ElementSoundKind
	{
		/// <summary>
		/// The sound to play when an element gets focus.
		/// </summary>
		Focus,

		/// <summary>
		/// The sound to play when an element is invoked.
		/// </summary>
		Invoke,

		/// <summary>
		/// The sound to play when a flyout, dialog, or command bar is opened.
		/// </summary>
		Show,

		/// <summary>
		/// The sound to play when a flyout, dialog, or command bar is closed.
		/// </summary>
		Hide,

		/// <summary>
		/// The sound to play when a user navigates to the previous panel or view within a page.
		/// </summary>
		MovePrevious,

		/// <summary>
		/// The sound to play when a user navigates to the next panel or view within a page.
		/// </summary>
		MoveNext,

		/// <summary>
		/// The sound to play when a user navigates back.
		/// </summary>
		GoBack,
	}
}
