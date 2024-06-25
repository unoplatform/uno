// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutPresenter_Partial.h, tag winui3/release/1.5.4, commit 98a60c8

using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.DataBinding;
using static Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutPresenter
{
	// Can be negative. (-1) means nothing focused.
	internal int m_iFocusedIndex;

	// Weak reference to the menu that ultimately owns this MenuFlyoutPresenter.
	private ManagedWeakReference m_wrOwningMenu;

	// Weak reference to the parent MenuFlyout.
	private ManagedWeakReference m_wrParentMenuFlyout;

	// Weak reference to the owner of this menu.
	// Only populated if this is the presenter for an ISubMenuOwner.
	private ManagedWeakReference m_wrOwner;

	// Weak reference to the sub-presenter that was created by a child menu owner.
	private ManagedWeakReference m_wrSubPresenter;

	// Whether ItemsSource contains at least one ToggleMenuFlyoutItem.
	private bool m_containsToggleItems;

	// Whether ItemsSource contains at least one MenuFlyoutItem with an Icon.
	private bool m_containsIconItems;

	// Whether ItemsSource contains at least one MenuFlyoutItem or ToggleMenuFlyoutItem with KeyboardAcceleratorText.
	private bool m_containsItemsWithKeyboardAcceleratorText;

	// private bool m_animationInProgress; TODO Animation not supported in Uno yet

	private bool m_isSubPresenter;

	private int m_depth;

	private MajorPlacementMode m_mostRecentPlacement;

#if false // Unused in WinUI
	// References the panels in the template.
	private UIElement m_tpOuterBorder;
#endif

#if false // TODO Uno: Unused for now
	private Timeline m_tpTopPortraitTimeline;
	private Timeline m_tpBottomPortraitTimeline;
	private Timeline m_tpLeftLandscapeTimeline;
	private Timeline m_tpRightLandscapeTimeline;
#endif

	private ScrollViewer m_tpScrollViewer;

	internal bool IsSubPresenter
	{
		get => m_isSubPresenter;
		set => m_isSubPresenter = value;
	}

	/// <summary>
	/// Returns true if the ItemsSource contains at least one ToggleMenuFlyoutItem; false otherwise.
	/// </summary>
	internal bool GetContainsToggleItems() => m_containsToggleItems;

	/// <summary>
	/// Returns true if the ItemsSource contains at least one MenuFlyoutItem with an Icon; false otherwise.
	/// </summary>
	internal bool GetContainsIconItems() => m_containsIconItems;

	/// <summary>
	/// Returns true if the ItemsSource contains at least one MenuFlyoutItem with an keyboard accelerator; false otherwise.
	/// </summary>
	internal bool GetContainsItemsWithKeyboardAcceleratorText() => m_containsItemsWithKeyboardAcceleratorText;
}
