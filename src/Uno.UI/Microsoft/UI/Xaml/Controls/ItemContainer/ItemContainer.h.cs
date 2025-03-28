// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemContainer.h, tag winui3/release/1.5.0

using Uno.Disposables;

namespace Windows.UI.Xaml.Controls;

partial class ItemContainer
{
#if !MUX_PRERELEASE
	internal ItemContainerUserSelectMode CanUserSelectInternal()
	{
		return m_canUserSelectInternal;
	}

	internal ItemContainerUserInvokeMode CanUserInvokeInternal()
	{
		return m_canUserInvokeInternal;
	}

	internal ItemContainerMultiSelectMode MultiSelectModeInternal()
	{
		return m_multiSelectModeInternal;
	}

	internal void CanUserSelectInternal(ItemContainerUserSelectMode value)
	{
		if (m_canUserSelectInternal != value)
		{
			m_canUserSelectInternal = value;
		}
	}

	internal void CanUserInvokeInternal(ItemContainerUserInvokeMode value)
	{
		if (m_canUserInvokeInternal != value)
		{
			m_canUserInvokeInternal = value;
		}
	}

	internal void MultiSelectModeInternal(ItemContainerMultiSelectMode value)
	{
		if (m_multiSelectModeInternal != value)
		{
			m_multiSelectModeInternal = value;

			// Same code as in ItemContainer::OnPropertyChanged, when MultiSelectMode changed.
			if (m_pointerInfo != null)
			{
				UpdateVisualState(true);
				UpdateMultiSelectState(true);
			}
		}
	}
#endif

	// Uno docs: We use IsEnabledChanged override instead.
	//SerialDisposable m_isEnabledChangedRevoker = new();
	SerialDisposable m_checked_revoker = new();
	SerialDisposable m_unchecked_revoker = new();
	PointerInfo<ItemContainer> m_pointerInfo;

	CheckBox m_selectionCheckbox;
	Panel m_rootPanel;

#if !MUX_PRERELEASE
	ItemContainerUserSelectMode m_canUserSelectInternal = ItemContainerUserSelectMode.Auto;
	ItemContainerUserInvokeMode m_canUserInvokeInternal = ItemContainerUserInvokeMode.Auto;
	ItemContainerMultiSelectMode m_multiSelectModeInternal = ItemContainerMultiSelectMode.Auto;
#endif

	private const string s_disabledStateName = "Disabled";
	private const string s_enabledStateName = "Enabled";
	private const string s_selectedPressedStateName = "SelectedPressed";
	private const string s_unselectedPressedStateName = "UnselectedPressed";
	private const string s_selectedPointerOverStateName = "SelectedPointerOver";
	private const string s_unselectedPointerOverStateName = "UnselectedPointerOver";
	private const string s_selectedNormalStateName = "SelectedNormal";
	private const string s_unselectedNormalStateName = "UnselectedNormal";
	private const string s_multipleStateName = "Multiple";
	private const string s_singleStateName = "Single";
	private const string s_selectionCheckboxName = "PART_SelectionCheckbox";
	private const string s_containerRootName = "PART_ContainerRoot";
};
