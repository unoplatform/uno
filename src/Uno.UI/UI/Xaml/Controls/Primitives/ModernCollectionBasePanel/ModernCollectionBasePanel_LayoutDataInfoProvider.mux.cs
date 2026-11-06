// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_LayoutDataInfoProvider.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal partial class LayoutDataInfoProvider
	{
		public LayoutDataInfoProvider()
		{
			_pMCBP = null;
		}

		internal void Initialize(ModernCollectionBasePanel pPanel) => _pMCBP = pPanel;

		internal void Uninitialize() => _pMCBP = null;

		public int GetTotalItemCount() => GetPanelRef()!.GetTotalItemCount();

		public int GetTotalGroupCount() => GetPanelRef()!.GetTotalGroupCount();

		public void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)
			=> GetPanelRef()!.GetGroupInformationFromItemIndexImpl(itemIndex, out indexOfGroup, out indexInsideGroup, out itemCountInGroup);

		public void GetGroupInformationFromGroupIndex(int groupIndex, out int startItemIndex, out int itemCountInGroup)
			=> GetPanelRef()!.GetGroupInformationFromGroupIndexImpl(groupIndex, out startItemIndex, out itemCountInGroup);

		private ModernCollectionBasePanel? GetPanelRef()
		{
			if (_pMCBP is null)
			{
				throw new InvalidOperationException();
			}

			return _pMCBP;
		}
	}

	//
	// Actual implementation of ILayoutDataInfoProvider.
	//

	private int GetTotalItemCount() => m_cacheManager.GetTotalItemCount();

	internal int GetTotalGroupCount() => m_cacheManager.GetTotalLayoutGroupCount();

	private void GetGroupInformationFromItemIndexImpl(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)
	{
		m_cacheManager.GetGroupInformationFromItemIndex(itemIndex, out indexOfGroup, out indexInsideGroup, out itemCountInGroup);

		indexOfGroup = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, indexOfGroup);
	}

	private void GetGroupInformationFromGroupIndexImpl(int groupIndex, out int startItemIndex, out int itemCountInGroup)
	{
		groupIndex = m_cacheManager.LayoutIndexToDataIndex(ElementType.GroupHeader, groupIndex);
		m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out startItemIndex, out itemCountInGroup);
	}
}
