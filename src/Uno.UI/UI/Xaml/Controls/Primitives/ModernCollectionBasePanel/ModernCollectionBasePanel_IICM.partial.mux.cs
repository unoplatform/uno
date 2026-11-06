// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_IICM_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using DirectUI;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal IItemContainerMapping GetItemContainerMapping() => this;

	internal IGroupHeaderMapping GetGroupHeaderMapping() => this;

	internal object? ItemFromContainer(DependencyObject container)
		=> m_containerManager.IICM_ItemFromContainer(container);

	internal DependencyObject? ContainerFromItem(object? item)
		=> m_containerManager.IICM_ContainerFromItem(item);

	internal int IndexFromContainer(DependencyObject container)
		=> m_containerManager.IICM_IndexFromContainer(container);

	internal DependencyObject? ContainerFromIndex(int index)
		=> m_containerManager.IICM_ContainerFromIndex(index);

	internal DependencyObject? GroupHeaderContainerFromItemContainer(DependencyObject pItemContainer)
		=> m_containerManager.IICM_GroupHeaderContainerFromItemContainerImpl(pItemContainer);

	internal object? GroupFromHeader(DependencyObject header) => m_containerManager.IGHM_GroupFromHeader(header);

	internal DependencyObject? HeaderFromGroup(object? group) => m_containerManager.IGHM_HeaderFromGroup(group);

	internal int IndexFromHeader(DependencyObject header, bool excludeHiddenEmptyGroups) => m_containerManager.IGHM_IndexFromHeader(header, excludeHiddenEmptyGroups);

	internal DependencyObject? HeaderFromIndex(int index) => m_containerManager.IGHM_HeaderFromIndex(index);
}
