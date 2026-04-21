#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A special ScrollViewer that wraps the root content of a XamlIslandRoot.
/// Matches WinUI's RootScrollViewer (RootScrollViewer_Partial.h/.cpp).
/// Scrolling is disabled by default and only enabled when the soft input
/// panel (SIP/keyboard) is showing, to allow the user to scroll to see
/// content that would otherwise be occluded by the keyboard.
/// </summary>
internal partial class RootScrollViewer : ScrollViewer
{
	private bool _isInputPaneShow;
	private double _preInputPaneOffsetX;
	private double _preInputPaneOffsetY;

	internal RootScrollViewer()
	{
		// Match WinUI: all scrolling disabled, hidden bars, not focusable
		VerticalScrollMode = ScrollMode.Disabled;
		HorizontalScrollMode = ScrollMode.Disabled;
		VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
		HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		ZoomMode = ZoomMode.Disabled;
		IsTabStop = false;
	}

	/// <summary>
	/// Returns true. Used by ScrollViewer base to suppress behaviors
	/// (pointer, keyboard, focus) when the SIP is not showing.
	/// </summary>
	internal override bool IsRootScrollViewer => true;

	/// <summary>
	/// Whether the input pane (soft keyboard) is currently showing.
	/// When true, scrolling is enabled on this RootScrollViewer.
	/// </summary>
	internal bool IsInputPaneShow => _isInputPaneShow;

	/// <summary>
	/// Called when the InputPane state changes (showing/hiding).
	/// Enables/disables scrolling and saves/restores scroll offsets.
	/// </summary>
	// TODO: WinUI notifies ApplicationBarService.OnBoundsChanged and
	// FlyoutBase.NotifyInputPaneStateChange for open flyouts.
	internal void NotifyInputPaneStateChange(bool isShowing, Rect inputPaneBounds)
	{
		if (isShowing && !_isInputPaneShow)
		{
			// Save pre-SIP scroll offsets
			_preInputPaneOffsetX = HorizontalOffset;
			_preInputPaneOffsetY = VerticalOffset;

			// Enable scrolling while keyboard is showing
			VerticalScrollMode = ScrollMode.Enabled;
			HorizontalScrollMode = ScrollMode.Enabled;
		}
		else if (!isShowing && _isInputPaneShow)
		{
			// Disable scrolling
			VerticalScrollMode = ScrollMode.Disabled;
			HorizontalScrollMode = ScrollMode.Disabled;

			// Restore pre-SIP scroll offsets
			ChangeView(_preInputPaneOffsetX, _preInputPaneOffsetY, null, disableAnimation: true);
		}

		_isInputPaneShow = isShowing;
	}

	// Strip template matching WinUI: CScrollContentControl::ApplyTemplate releases m_pTemplate.
	// The RSV has no visual template. ScrollContentPresenter is created internally by the
	// ScrollViewer's default template, but we strip all other template parts.
	// TODO: WinUI has ApplyInputPaneTransition() for smooth SIP show/hide animations.

	// Suppress automation peer (invisible to automation, matching WinUI)
	protected override AutomationPeer OnCreateAutomationPeer() => null!;
}
