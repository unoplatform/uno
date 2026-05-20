// MUX Reference SplitViewPaneAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Automation peer for the SplitView pane element.
/// Implements IWindowProvider to represent the pane as a window-like
/// overlay when the SplitView can be light-dismissed.
/// </summary>
internal partial class SplitViewPaneAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
	public SplitViewPaneAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Window && IsWindowContextEnabled())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => "SplitViewPane";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Window;

	#region IWindowProvider

	/// <summary>
	/// Gets whether the pane is modal. Always returns true since
	/// the pane overlays and captures input.
	/// </summary>
	public bool IsModal => true;

	/// <summary>
	/// Gets whether the pane is topmost. Always returns true.
	/// </summary>
	public bool IsTopmost => true;

	/// <summary>
	/// Gets whether the pane is maximizable. Always returns false.
	/// </summary>
	public bool Maximizable => false;

	/// <summary>
	/// Gets whether the pane is minimizable. Always returns false.
	/// </summary>
	public bool Minimizable => false;

	/// <summary>
	/// Gets the interaction state. Always returns Running.
	/// </summary>
	public WindowInteractionState InteractionState
		=> WindowInteractionState.Running;

	/// <summary>
	/// Gets the visual state. Always returns Normal.
	/// </summary>
	public WindowVisualState VisualState
		=> WindowVisualState.Normal;

	/// <summary>
	/// Closes the pane. No-op in current implementation.
	/// </summary>
	public void Close()
	{
		// TODO Uno: Should close the SplitView pane when
		// SplitView pane element is fully wired.
	}

	/// <summary>
	/// Sets the visual state. No-op since the pane has a fixed visual state.
	/// </summary>
	public void SetVisualState(WindowVisualState state)
	{
	}

	/// <summary>
	/// Waits for input idle. Returns false (no wait implemented).
	/// </summary>
	public bool WaitForInputIdle(int milliseconds)
	{
		return false;
	}

	#endregion

	private bool IsWindowContextEnabled()
	{
		if (Owner is FrameworkElement fe)
		{
			var splitView = fe.TemplatedParent as SplitView;
			if (splitView is not null)
			{
				// CanLightDismiss: the pane acts as a window overlay in Overlay and CompactOverlay modes
				return splitView.DisplayMode == SplitViewDisplayMode.Overlay ||
					   splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay;
			}
		}

		return false;
	}
}
