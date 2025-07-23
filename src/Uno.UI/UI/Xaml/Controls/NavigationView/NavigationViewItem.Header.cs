// MUX reference NavigationViewItem.h, commit 65718e2813

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class NavigationViewItem
{
	internal SerialDisposable EventRevoker { get; } = new();

	internal ItemsRepeater GetRepeater() => m_repeater;

	private readonly SerialDisposable m_splitViewIsPaneOpenChangedRevoker = new();
	private readonly SerialDisposable m_splitViewDisplayModeChangedRevoker = new();
	private readonly SerialDisposable m_splitViewCompactPaneLengthChangedRevoker = new();

	private readonly SerialDisposable m_presenterPointerPressedRevoker = new();
	private readonly SerialDisposable m_presenterPointerEnteredRevoker = new();
	private readonly SerialDisposable m_presenterPointerMovedRevoker = new();
	private readonly SerialDisposable m_presenterPointerReleasedRevoker = new();
	private readonly SerialDisposable m_presenterPointerExitedRevoker = new();
	private readonly SerialDisposable m_presenterPointerCanceledRevoker = new();
	private readonly SerialDisposable m_presenterPointerCaptureLostRevoker = new();

	private readonly SerialDisposable m_repeaterElementPreparedRevoker = new();
	private readonly SerialDisposable m_repeaterElementClearingRevoker = new();
	private readonly SerialDisposable m_itemsSourceViewCollectionChangedRevoker = new();
	private readonly SerialDisposable m_menuItemsVectorChangedRevoker = new();

	private readonly SerialDisposable m_flyoutClosingRevoker = new();
	private readonly SerialDisposable m_isEnabledChangedRevoker = new();

	private ToolTip m_toolTip;
	private NavigationViewItemHelper<NavigationViewItem> backing_m_helper;
	private NavigationViewItemHelper<NavigationViewItem> m_helper => backing_m_helper ??= new(this);

	private NavigationViewItemPresenter m_navigationViewItemPresenter;
	private object m_suggestedToolTipContent;
	private ItemsRepeater m_repeater;
	private Grid m_flyoutContentGrid;
	private Grid m_rootGrid;

	private bool m_isClosedCompact;

	private bool m_appliedTemplate;
	private bool m_hasKeyboardFocus;

	// Visual state tracking
	private Pointer m_capturedPointer;
	private uint m_trackedPointerId;
	private bool m_isPressed;
	private bool m_isPointerOver;

	private bool m_isRepeaterParentedToFlyout;
	// used to bypass all Chevron visual state logic in order to keep it unloaded 
	private bool m_hasHadChildren;

	// NavigationView needs to force collapse top level items when the pane closes.
	// This bool is used to remember which items need to be restored to expanded state when the pane is opened again.
	private bool m_restoreToExpandedState;
}
