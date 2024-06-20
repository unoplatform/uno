// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyout_Partial.h, tag winui3/release/1.5.4, commit 98a60c8

using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyout
{
	private MenuFlyoutItemBaseCollection m_tpItems;

	// In Threshold, MenuFlyout uses the MenuPopupThemeTransition.
	// private Transition m_tpMenuPopupThemeTransition;

	private InputDeviceType m_inputDeviceTypeUsedToOpen;

	private ManagedWeakReference m_wrParentMenu;

	private bool m_openWindowed; // TODO Uno specific: Always false in Uno for now.

	private bool m_itemsSourceRefreshPending;
}
