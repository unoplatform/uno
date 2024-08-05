namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	private bool AreCustomValuesAllowed() => m_customValuesAllowed;

	// Allows the insertion of custom values by not reverting values outside the item source.
	private bool m_customValuesAllowed;

	protected bool m_skipFocusSuggestion;

	// Can be negative. (-1) means nothing focused.
	private int m_focusedIndex;

	// GetFocusedIndex and SetFocusedIndex are consistently used instead of 
	// m_focusedIndex to make it easier to track when this field is read & written.
	private protected int GetFocusedIndex() => m_focusedIndex;

	void SetFocusedIndex(int focusedIndex)
	{
		if (m_focusedIndex != focusedIndex)
		{
			m_focusedIndex = focusedIndex;
		}
	}
}
