#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	private Lazy<IInputPaneExtension?>? _inputPaneExtension;
	private bool _subscribedToFocusChanges;

	partial void InitializePlatform()
	{
		_inputPaneExtension = new(() =>
		{
			ApiExtensibility.CreateInstance<IInputPaneExtension>(this, out var extension);
			return extension;
		});
	}

	private bool TryShowPlatform() => _inputPaneExtension?.Value?.TryShow() ?? false;

	private bool TryHidePlatform() => _inputPaneExtension?.Value?.TryHide() ?? false;

	partial void EnsureFocusedElementInViewPartial()
	{
		// Use the per-XamlRoot association instead of Window.InitialWindow
		var xamlRoot = _xamlRoot;
		if (xamlRoot is null)
		{
			return;
		}

		var rsv = xamlRoot.VisualTree?.RootScrollViewer;

		if (Visible)
		{
			// Keyboard is showing - shrink RSV and enable scrolling
			if (rsv is not null)
			{
				// Shrink RSV to visible area above keyboard
				rsv.Height = OccludedRect.Y;
				rsv.NotifyInputPaneStateChange(true, OccludedRect);
			}

			// Subscribe to focus changes to re-BringIntoView when user taps a different text field
			SubscribeToFocusChanges();

			if (FocusManager.GetFocusedElement(xamlRoot) is UIElement focusedElement)
			{
				// TODO: WinUI's CBringIntoViewHandler adjusts for caret position (75% CaretAlignmentThreshold),
				// adds 20px padding (ExtraPixelsForBringIntoView), and accounts for AppBar height.
				// See: BringIntoViewHandler.cpp

				// Dispatch BringIntoView after the next layout pass so the RootScrollViewer's
				// height change has taken effect.
				_ = UI.Core.CoreDispatcher.Main.RunAsync(
					UI.Core.CoreDispatcherPriority.Normal, () => focusedElement.StartBringIntoView()
				);
			}
		}
		else
		{
			// Keyboard is hiding - restore RSV
			UnsubscribeFromFocusChanges();

			if (rsv is not null)
			{
				rsv.Height = double.NaN; // Stretch to fill
				rsv.NotifyInputPaneStateChange(false, Rect.Empty);
			}
		}
	}

	private void SubscribeToFocusChanges()
	{
		if (_subscribedToFocusChanges)
		{
			return;
		}

		// TODO: WinUI uses InputPaneProcessor::NotifyFocusChanged() which checks
		// SIP showing + text-editable focused = forces bringIntoView=true.
		// Our GotFocus approach is simpler but may behave differently for
		// non-keyboard focus changes. See InputPaneProcessor.cpp lines 222-234.
		FocusManager.GotFocus += OnFocusManagerGotFocus;
		_subscribedToFocusChanges = true;
	}

	private void UnsubscribeFromFocusChanges()
	{
		if (_subscribedToFocusChanges)
		{
			FocusManager.GotFocus -= OnFocusManagerGotFocus;
			_subscribedToFocusChanges = false;
		}
	}

	private void OnFocusManagerGotFocus(object? sender, FocusManagerGotFocusEventArgs e)
	{
		if (Visible && e.NewFocusedElement is UIElement newFocused)
		{
			// RSV height is already shrunk - just BringIntoView for the new element
			_ = UI.Core.CoreDispatcher.Main.RunAsync(
				UI.Core.CoreDispatcherPriority.Normal,
				() => newFocused.StartBringIntoView());
		}
	}
}
