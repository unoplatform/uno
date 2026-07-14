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
	private bool _isInputPaneShown;
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
	internal bool IsInputPaneShown => _isInputPaneShown;

	/// <summary>
	/// Called when the InputPane state changes (showing/hiding).
	/// Enables/disables scrolling and saves/restores scroll offsets.
	/// </summary>
	// TODO: WinUI notifies ApplicationBarService.OnBoundsChanged and
	// FlyoutBase.NotifyInputPaneStateChange for open flyouts.
	internal void NotifyInputPaneStateChange(bool isShowing, Rect inputPaneBounds)
	{
		if (isShowing && !_isInputPaneShown)
		{
			// Save pre-SIP scroll offsets
			_preInputPaneOffsetX = HorizontalOffset;
			_preInputPaneOffsetY = VerticalOffset;

			// Enable scrolling while keyboard is showing
			VerticalScrollMode = ScrollMode.Enabled;
			HorizontalScrollMode = ScrollMode.Enabled;
		}
		else if (!isShowing && _isInputPaneShown)
		{
			// Disable scrolling
			VerticalScrollMode = ScrollMode.Disabled;
			HorizontalScrollMode = ScrollMode.Disabled;

			// Restore pre-SIP scroll offsets
			ChangeView(_preInputPaneOffsetX, _preInputPaneOffsetY, null, disableAnimation: true);
		}

		_isInputPaneShown = isShowing;
	}

	// Unlike WinUI (CScrollContentControl::ApplyTemplate releases the template), the RSV keeps the default
	// ScrollViewer template: its ScrollContentPresenter is what hosts the content. The chrome is instead
	// neutralized from the constructor (scroll bars hidden, scrolling and zoom disabled, not a tab stop).
	// TODO: WinUI has ApplyInputPaneTransition() for smooth SIP show/hide animations.

	// Suppress the automation peer (invisible to automation, matching WinUI).
	// The base ScrollViewer override is declared non-nullable, so `null!` is required here.
	protected override AutomationPeer OnCreateAutomationPeer() => null!;
}
