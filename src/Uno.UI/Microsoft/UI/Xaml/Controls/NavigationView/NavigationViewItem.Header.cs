using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NavigationViewItem
	{
		private ToolTip m_toolTip = null;
		private NavigationViewItemHelper<NavigationViewItem> m_helper = null;
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
