namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Specifies the 2D directional navigation behavior when using the keyboard arrow keys.
	/// </summary>
	public enum XYFocusKeyboardNavigationMode
	{
		/// <summary>
		/// Behavior is inherited from the elements ancestors.
		/// If all ancestors have a value of Auto, the fallback behavior is Disabled.
		/// </summary>
		Auto,

		/// <summary>
		/// Arrow keys can be used for 2D directional navigation.
		/// </summary>
		Enabled,

		/// <summary>
		/// Arrow keys cannot be used for 2D directional navigation.
		/// </summary>
		Disabled,
	}
}
