using System;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_ViewManagement
{
	// Manual, on-device sample for the Skia WASM iPad soft-keyboard focus fixes:
	//  1. Tapping a TextBox must keep the keyboard open (no self-dismiss).
	//  2. LostFocus must fire when the keyboard is dismissed / focus leaves the field.
	//  3. The focused field near the bottom must scroll above the on-screen keyboard.
	//  4. Moving focus between fields of a side panel must keep the keyboard open and must not let Safari
	//     pan the page (the panel's ScrollViewer is the only thing allowed to move).
	//  5. Dragging inside the side panel scrolls the panel, not the whole page.
	// 1-3 reproduce only on a real touch device (iPad Safari/Chrome). 4-5 also reproduce in the iOS Simulator:
	// with the field focused, toggle I/O > Keyboard > Connect Hardware Keyboard on and off to force a keyboard
	// frame change.
	[Sample("Windows.UI.ViewManagement", Description = "On-device checks for Skia WASM soft-keyboard focus (auto-dismiss, LostFocus, bring-into-view).", IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class SoftKeyboardFocusTests : Page
	{
		private string _focusedFieldName = "(none)";
		private int _gotFocusCount;
		private int _lostFocusCount;

		public SoftKeyboardFocusTests()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
			this.Unloaded += OnUnloaded;
		}

		private void OnFocusTextBoxGotFocus(object sender, RoutedEventArgs e)
		{
			_gotFocusCount++;
			UpdateFocusState();
		}

		private void OnFocusTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			_lostFocusCount++;
			UpdateFocusState();
		}

		private void OnPanelFieldGotFocus(object sender, RoutedEventArgs e)
		{
			var name = (sender as FrameworkElement)?.Name;
			_focusedFieldName = string.IsNullOrEmpty(name) ? sender.GetType().Name : name;
			UpdatePanelState();
		}

		// The offset has to follow the panel, not just the focus: checks 4 and 5 are about which element moves,
		// so the readout is only useful if it keeps up during the bring-into-view and the drag.
		private void OnSidePanelViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => UpdatePanelState();

		private void UpdatePanelState()
			=> PanelFocusTextBlock.Text = $"Focused field: {_focusedFieldName}   Panel offset: {SidePanelScrollViewer.VerticalOffset:0}";

		private void UpdateFocusState()
		{
			FocusStateTextBlock.Text =
				$"GotFocus: {_gotFocusCount}   LostFocus: {_lostFocusCount}   IsFocused: {FocusTextBox.FocusState != FocusState.Unfocused}";
		}

		private void OnInputPaneShowing(InputPane sender, InputPaneVisibilityEventArgs args)
			=> UpdateOccludedRect(sender.OccludedRect, "Showing");

		private void OnInputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs args)
			=> UpdateOccludedRect(sender.OccludedRect, "Hiding");

		private void UpdateOccludedRect(Rect occludedRect, string eventType)
			=> OccludedRectTextBlock.Text = $"Last event: {eventType}   OccludedRect: {occludedRect}";

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing += OnInputPaneShowing;
			inputPane.Hiding += OnInputPaneHiding;
			SidePanelScrollViewer.ViewChanged += OnSidePanelViewChanged;
			UpdateFocusState();
			UpdatePanelState();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing -= OnInputPaneShowing;
			inputPane.Hiding -= OnInputPaneHiding;
			SidePanelScrollViewer.ViewChanged -= OnSidePanelViewChanged;
		}
	}
}
