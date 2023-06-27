#nullable enable

using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal enum CommandBarFlyoutOpenCloseAnimationKind
{
	Opacity,
    Clip
}

partial class CommandBarFlyoutCommandBar
{
	internal CommandBarFlyoutOpenCloseAnimationKind OpenAnimationKind => m_openAnimationKind;

	internal bool m_commandBarFlyoutIsOpening;

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
}
