using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	internal enum SelectionState
	{
		Selected,
		NotSelected,
		PartiallySelected
	}

	internal partial class SelectionNode
	{
		private SelectionModel m_manager;

		// Note that a node can contain children who are leaf as well as 
		// chlidren containing leaf entries.

		// For inner nodes (any node whose children are data sources)
		private List<SelectionNode> m_childrenNodes = new List<SelectionNode>();
		// Don't take a ref.
		private SelectionNode m_parent = null;

		// For parents of leaf nodes (any node whose children are not data sources)
		private List<IndexRange> m_selected = new List<IndexRange>();

		private object m_source;
		private ItemsSourceView m_dataSource;

		private int m_selectedCount = 0;
		private List<int> m_selectedIndicesCached = new List<int>();
		private bool m_selectedIndicesCacheIsValid = false;
		private int m_realizedChildrenNodeCount = 0;
	}
}
