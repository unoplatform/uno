// MUX reference NavigationViewItem.h, commit fd22d7f

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NavigationViewItem
	{
		internal SerialDisposable EventRevoker { get; } = new SerialDisposable();

		internal ItemsRepeater GetRepeater() => m_repeater;

		private readonly SerialDisposable m_splitViewIsPaneOpenChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewDisplayModeChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewCompactPaneLengthChangedRevoker = new SerialDisposable();

		private readonly SerialDisposable m_presenterPointerPressedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_presenterPointerEnteredRevoker = new SerialDisposable();
		private readonly SerialDisposable m_presenterPointerMovedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_presenterPointerReleasedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_presenterPointerExitedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_presenterPointerCanceledRevoker = new SerialDisposable();
		private readonly SerialDisposable m_presenterPointerCaptureLostRevoker = new SerialDisposable();

		private readonly SerialDisposable m_repeaterElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_repeaterElementClearingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_itemsSourceViewCollectionChangedRevoker = new SerialDisposable();

		private readonly SerialDisposable m_flyoutClosingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_isEnabledChangedRevoker = new SerialDisposable();

		private ToolTip m_toolTip = null;
		private NavigationViewItemHelper<NavigationViewItem> backing_m_helper = null;
		private NavigationViewItemHelper<NavigationViewItem> m_helper => backing_m_helper ??= new NavigationViewItemHelper<NavigationViewItem>(this);

		private NavigationViewItemPresenter m_navigationViewItemPresenter = null;
		private object m_suggestedToolTipContent = null;
		private ItemsRepeater m_repeater = null;
		private Grid m_flyoutContentGrid = null;
		private Grid m_rootGrid = null;

		private bool m_isClosedCompact = false;

		private bool m_appliedTemplate = false;
		private bool m_hasKeyboardFocus = false;

		// Visual state tracking
		private Pointer m_capturedPointer = null;
		private uint m_trackedPointerId = 0;
		private bool m_isPressed = false;
		private bool m_isPointerOver = false;

		private bool m_isRepeaterParentedToFlyout = false;
	}
}
