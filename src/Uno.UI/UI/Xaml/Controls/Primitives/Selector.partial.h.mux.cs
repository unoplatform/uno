namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	private bool AreCustomValuesAllowed() => m_customValuesAllowed;

	// Allows the insertion of custom values by not reverting values outside the item source.
	private bool m_customValuesAllowed;

	protected bool m_skipFocusSuggestion;
	private bool m_inCollectionChange;

	// Can be negative. (-1) means nothing focused.
	private int m_focusedIndex = -1;

	// Holds the last focused index just before focusing out of the selector.
	private int m_lastFocusedIndex;

	// GetFocusedIndex and SetFocusedIndex are consistently used instead of 
	// m_focusedIndex to make it easier to track when this field is read & written.
	private protected int GetFocusedIndex() => m_focusedIndex;

	private protected int GetLastFocusedIndex() => m_lastFocusedIndex;

	// Called to detect whether we can scroll to the View or not.
	private protected bool CanScrollIntoView()
	{
		var itemsHost = ItemsPanelRoot;
		var isItemsHostInvalid = false;
		var isInLiveTree = false;

		if (itemsHost is not null)
		{
			isItemsHostInvalid = IsItemsHostInvalid;
			if (!isItemsHostInvalid)
			{
				isInLiveTree = IsInLiveTree;
			}
		}

		return !isItemsHostInvalid && isInLiveTree && !m_skipScrollIntoView && !m_inCollectionChange;
	}

	internal void SetFocusedIndex(int focusedIndex)
	{
		if (m_focusedIndex != focusedIndex)
		{
			m_focusedIndex = focusedIndex;
		}
	}

	private protected void SetLastFocusedIndex(int lastFocusedIndex)
	{
		if (m_lastFocusedIndex != lastFocusedIndex)
		{
			m_lastFocusedIndex = lastFocusedIndex;
		}
	}
}
