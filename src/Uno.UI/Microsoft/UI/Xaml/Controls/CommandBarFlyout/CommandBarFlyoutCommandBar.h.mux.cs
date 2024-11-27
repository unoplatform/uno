// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\CommandBarFlyoutCommandBar.h, tag winui3/release/1.6.3, commit 66d24dfff3b2763ab3be096a2c7cbaafc81b31eb

#nullable enable

using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using System.Windows.Input;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;

internal enum CommandBarFlyoutOpenCloseAnimationKind
{
	Opacity,
	Clip
}

partial class CommandBarFlyoutCommandBar
{
	internal CommandBarFlyoutOpenCloseAnimationKind OpenAnimationKind => m_openAnimationKind;

	internal bool m_commandBarFlyoutIsOpening;

	private bool HasVisibleLabel(IAppBarCommand? command)
	{
		return command is not null &&
			!string.IsNullOrEmpty(command.Label) &&
			command.Visibility == Visibility.Visible &&
			command.LabelPosition == CommandBarLabelPosition.Default;
	}

	private FrameworkElement? m_primaryItemsRoot;
	private Popup? m_overflowPopup;
	private FrameworkElement? m_secondaryItemsRoot;
	private ButtonBase? m_moreButton;
	private CommandBarFlyout? m_owningFlyout;
	private readonly SerialDisposable m_overflowPopupActualPlacementChangedRevoker = new();
	private readonly SerialDisposable m_keyDownRevoker = new();
	private readonly SerialDisposable m_secondaryItemsRootPreviewKeyDownRevoker = new();
	private readonly SerialDisposable m_secondaryItemsRootSizeChangedRevoker = new();
	private readonly SerialDisposable m_firstItemLoadedRevoker = new();
	private readonly CompositeDisposable m_itemLoadedRevokerVector = new();
	private readonly CompositeDisposable m_itemSizeChangedRevokerVector = new();

	// We need to manually connect the end element of the primary items to the start element of the secondary items
	// for the purposes of UIA items navigation. To ensure that we only have the current start and end elements registered
	// (e.g., if the app adds a new start element to the secondary commands, we want to unregister the previous start element),
	// we'll save references to those elements.
	private FrameworkElement? m_currentPrimaryItemsEndElement;
	private FrameworkElement? m_currentSecondaryItemsStartElement;

	private CommandBarFlyoutOpenCloseAnimationKind m_openAnimationKind = CommandBarFlyoutOpenCloseAnimationKind.Clip;
	private ManagedWeakReference? m_flyoutPresenter;
	private Storyboard? m_openingStoryboard;
	private Storyboard? m_closingStoryboard;
	private readonly SerialDisposable m_openingStoryboardCompletedRevoker = new();
	private readonly SerialDisposable m_closingStoryboardCompletedCallbackRevoker = new();

	private bool m_secondaryItemsRootSized;

	private readonly SerialDisposable m_expandedUpToCollapsedStoryboardRevoker = new();
	private readonly SerialDisposable m_expandedDownToCollapsedStoryboardRevoker = new();
	private readonly SerialDisposable m_collapsedToExpandedUpStoryboardRevoker = new();
	private readonly SerialDisposable m_collapsedToExpandedDownStoryboardRevoker = new();

	private IList<Control>? m_horizontallyAccessibleControls;
	private IList<Control>? m_verticallyAccessibleControls;

	private FrameworkElement? m_outerOverflowContentRootV2;
	private FrameworkElement? m_primaryItemsSystemBackdropRoot;
	private FrameworkElement? m_overflowPopupSystemBackdropRoot;

	// These ContentExternalBackdropLink objects implement the backdrop behind the CommandBarFlyoutCommandBar. We don't
	// use the one built into Popup because we need to animate this backdrop using Storyboards in the CBFCB's template.
	// The one built into Popup is too high up in the Visual tree to be animated by a custom animation.
#if !HAS_UNO // SystemBackdrop is not yet supported
	private ContentExternalBackdropLink m_backdropLink;
	private ContentExternalBackdropLink m_overflowPopupBackdropLink;

	// A copy of the value in the DependencyProperty. We need to unregister with this SystemBackdrop when this
	// CommandBarFlyoutCommandBar is deleted, but the DP value is already cleared by the time we get to Unloaded or the
	// dtor, so we cache a copy for ourselves to use during cleanup. Another possibility is to do cleanup during Closed,
	// but the app can release and delete this CommandBarFlyoutCommandBar without ever closing it.
	private ManagedWeakReference m_systemBackdrop;

	// Bookkeeping for registering and unregistering with the SystemBackdrop. In order to register, we need to have a
    // XamlRoot available so we can listen for events like theme changed or high contrast changed. It's possible we get
    // a SystemBackdrop object set without being in the tree, in which case there's no XamlRoot so we can't register
    // yet. We'll wait for the Loaded event to register.
    private bool m_registeredWithSystemBackdrop;
#endif

	// Localized string caches. Looking these up from MRTCore is expensive, so we don't want to put the lookups in a
	// loop. Instead, look them up once, cache them, use the cached values, then clear the cache. The values in these
	// caches are only valid after CacheLocalizedStringResources and before ClearLocalizedStringResourceCache.
	private bool m_areLocalizedStringResourcesCached;
	private string? m_localizedCommandBarFlyoutAppBarButtonControlType;
	private string? m_localizedCommandBarFlyoutAppBarToggleButtonControlType;
}
