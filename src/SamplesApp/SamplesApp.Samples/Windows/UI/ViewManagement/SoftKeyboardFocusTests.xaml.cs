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
	// These reproduce only on a real touch device (iPad Safari/Chrome), not desktop or the iOS Simulator.
	[Sample("Windows.UI.ViewManagement", Description = "On-device checks for Skia WASM soft-keyboard focus (auto-dismiss, LostFocus, bring-into-view).", IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class SoftKeyboardFocusTests : Page
	{
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
			UpdateFocusState();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing -= OnInputPaneShowing;
			inputPane.Hiding -= OnInputPaneHiding;
		}
	}
}
