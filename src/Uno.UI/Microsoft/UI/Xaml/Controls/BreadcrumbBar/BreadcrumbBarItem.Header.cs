// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBarItem.h, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using Uno.Disposables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.DataBinding;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class BreadcrumbBarItem : ContentControl
{
	// Only used for bug workaround in BreadcrumbElementFactory::RecycleElementCore.
	internal bool IsEllipsisDropDownItem()
	{
		return m_isEllipsisDropDownItem;
	}

	// Common item fields

	// Contains the 1-indexed assigned to the element
	private int m_index;
	private bool m_isEllipsisDropDownItem;

	// Inline item fields

	private bool m_isEllipsisItem;
	private bool m_isLastItem;

	// BreadcrumbBarItem visual representation
	private Button? m_button = null;
	// Parent BreadcrumbBarItem to ask for hidden elements
	private ManagedWeakReference? m_parentBreadcrumb = null;

	// Flyout content for ellipsis item
	private Flyout? m_ellipsisFlyout = null;
	private ItemsRepeater? m_ellipsisItemsRepeater = null;
	private IElementFactory? m_ellipsisDropDownItemDataTemplate = null;
	private BreadcrumbElementFactory? m_ellipsisElementFactory = null;

	// Ellipsis dropdown item fields

	// BreadcrumbBarItem that owns the flyout
	private BreadcrumbBarItem? m_ellipsisItem = null;

	// Visual State tracking
	private uint m_trackedPointerId = 0;
	private bool m_isPressed = false;
	private bool m_isPointerOver = false;

	// Common item token & revoker
	private readonly SerialDisposable m_childPreviewKeyDownToken = new SerialDisposable();
	private readonly SerialDisposable m_keyDownRevoker = new SerialDisposable();

	// Inline item token & revokers
	private long? m_flowDirectionChangedToken = null;
	private readonly SerialDisposable m_buttonLoadedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_buttonClickRevoker = new SerialDisposable();
	private readonly SerialDisposable m_ellipsisRepeaterElementPreparedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_ellipsisRepeaterElementIndexChangedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_isPressedButtonRevoker = new SerialDisposable();
	private readonly SerialDisposable m_isPointerOverButtonRevoker = new SerialDisposable();
	private readonly SerialDisposable m_isEnabledButtonRevoker = new SerialDisposable();

	// Ellipsis dropdown item revoker
	private readonly SerialDisposable m_isEnabledChangedRevoker = new SerialDisposable();

	// Common Visual States
	private const string s_normalStateName = "Normal";
	private const string s_currentStateName = "Current";
	private const string s_pointerOverStateName = "PointerOver";
	private const string s_pressedStateName = "Pressed";
	private const string s_disabledStateName = "Disabled";

	// Inline Item Type Visual States
	private const string s_ellipsisStateName = "Ellipsis";
	private const string s_ellipsisRTLStateName = "EllipsisRTL";
	private const string s_lastItemStateName = "LastItem";
	private const string s_defaultStateName = "Default";
	private const string s_defaultRTLStateName = "DefaultRTL";

	// Item Type Visual States
	private const string s_inlineStateName = "Inline";
	private const string s_ellipsisDropDownStateName = "EllipsisDropDown";

	// Template Parts
	private const string s_ellipsisItemsRepeaterPartName = "PART_EllipsisItemsRepeater";
	private const string s_itemButtonPartName = "PART_ItemButton";
	private const string s_itemEllipsisFlyoutPartName = "PART_EllipsisFlyout";

	// Automation Names
	private const string s_ellipsisFlyoutAutomationName = "EllipsisFlyout";
	private const string s_ellipsisItemsRepeaterAutomationName = "EllipsisItemsRepeater";
}
