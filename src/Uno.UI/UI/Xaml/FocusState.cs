namespace Windows.UI.Xaml
{
	/// <summary>
	/// Describes how an element obtained focus.
	/// </summary>
	public enum FocusState
	{
		// Element is not currently focused.
		Unfocused = 0,

		// Element obtained focus through a pointer action.
		Pointer = 1,

		// Element obtained focus through a keyboard action, such as tab sequence traversal.
		Keyboard = 2,

		// Element obtained focus through a deliberate call to Focus or a related API. 
		Programmatic = 3
	}
}
