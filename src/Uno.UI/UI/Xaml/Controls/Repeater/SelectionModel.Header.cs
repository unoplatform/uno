using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	internal struct SelectedItemInfo
	{
		internal WeakReference<SelectionNode> Node;
		internal IndexPath Path;
	}

	public partial class SelectionModel
	{
		internal bool SelectionInvalidatedDueToCollectionChange()
		{
			return m_selectionInvalidatedDueToCollectionChange;
		}

		internal SelectionNode SharedLeafNode => m_leafNode;

		private SelectionNode m_rootNode = null;
		private bool m_singleSelect = false;

		private bool m_selectionInvalidatedDueToCollectionChange = false;

		private IReadOnlyList<IndexPath> m_selectedIndicesCached = null;
		private IReadOnlyList<object> m_selectedItemsCached = null;

		// Cached Event args to avoid creation cost every time
		private SelectionModelChildrenRequestedEventArgs m_childrenRequestedEventArgs;
		private SelectionModelSelectionChangedEventArgs m_selectionChangedEventArgs;

		// use just one instance of a leaf node to avoid creating a bunch of these.
		private SelectionNode m_leafNode;
	}
}
