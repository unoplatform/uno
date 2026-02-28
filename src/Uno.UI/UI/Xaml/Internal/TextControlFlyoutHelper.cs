// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Ported from: TextControlFlyoutHelper.h, TextControlFlyoutHelper.cpp

#if __SKIA__
#nullable enable

using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Internal;

/// <summary>
/// Wraps a <see cref="FlyoutBase"/> used by text controls, tracking focus
/// transitions between the owner and the flyout so the owner can keep its
/// focused visual state while the flyout is open.
/// </summary>
/// <remarks>
/// Port of DirectUI::TextControlFlyout (TextControlFlyoutHelper.cpp).
/// </remarks>
internal sealed class TextControlFlyout
{
	private readonly WeakReference<FlyoutBase> _flyoutRef;
	private readonly bool _isProofingFlyout;
	private WeakReference<FrameworkElement>? _activeOwnerRef;
	private bool _isOpened;
	private bool _isTransient;
	private bool _isGettingFocus;

	// WinUI header declares OnActiveOwnerGettingFocus and OnActiveOwnerGotFocus
	// but the .cpp does NOT implement them. Noted here for completeness.

	public TextControlFlyout(FlyoutBase flyout, bool isProofingFlyout = false)
	{
		_flyoutRef = new WeakReference<FlyoutBase>(flyout);
		_isProofingFlyout = isProofingFlyout;

		_isTransient =
			flyout.ShowMode == FlyoutShowMode.Transient ||
			flyout.ShowMode == FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

		flyout.Opened += OnOpened;
		flyout.Closing += OnClosing;
		flyout.Closed += OnClosed;
	}

	public bool IsGettingFocus => _isGettingFocus;
	public bool IsOpened => _isOpened;
	public bool IsTransient => _isTransient;
	public bool IsProofingFlyout => _isProofingFlyout;

	public FrameworkElement? GetActiveOwner()
		=> _activeOwnerRef is not null && _activeOwnerRef.TryGetTarget(out var owner) ? owner : null;

	public FlyoutBase? GetFlyout()
		=> _flyoutRef.TryGetTarget(out var flyout) ? flyout : null;

	public void SetActiveOwner(FrameworkElement owner)
	{
		ClearActiveOwnerEventHandlers();

		_activeOwnerRef = new WeakReference<FrameworkElement>(owner);
		owner.LosingFocus += OnActiveOwnerLosingFocus;

		// Propagate XamlRoot for multi-window support.
		// WinUI equivalent: flyout->SetVisualTree(VisualTree::GetForElementNoRef(owner))
		if (_flyoutRef.TryGetTarget(out var flyout))
		{
			flyout.XamlRoot = owner.XamlRoot;
		}
	}

	public bool IsOpen()
	{
		if (_flyoutRef.TryGetTarget(out var flyout))
		{
			return flyout.IsOpen;
		}
		return false;
	}

	public void CloseIfOpen()
	{
		if (IsOpen() && _flyoutRef.TryGetTarget(out var flyout))
		{
			flyout.Hide();
		}
	}

	public void ShowAt(Point point, Rect exclusionRect, FlyoutShowMode showMode)
	{
		_isTransient =
			showMode == FlyoutShowMode.Transient ||
			showMode == FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

		// If the flyout is open, close it so it refreshes the buttons
		CloseIfOpen();

		if (_flyoutRef.TryGetTarget(out var flyout))
		{
			var owner = GetActiveOwner();
			if (owner is not null)
			{
				flyout.ShowAt(owner, new FlyoutShowOptions
				{
					Position = point,
					ShowMode = showMode,
					// TODO Uno: FlyoutShowOptions.ExclusionRect is [NotImplemented] in Uno.
					// When implemented, set: ExclusionRect = exclusionRect
				});
			}
		}
	}

	/// <summary>Backward-compatible overload without exclusion rect.</summary>
	public void ShowAt(Point point, FlyoutShowMode showMode)
		=> ShowAt(point, default, showMode);

	private void OnOpened(object? sender, object e)
	{
		_isOpened = true;
	}

	private void OnClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
	{
		_isGettingFocus = false;
	}

	private void OnClosed(object? sender, object e)
	{
		// For some reason, the Closed event is sometimes being fired when the flyout is still open.
		// Verify the flyout is closed and close it if it isn't.
		CloseIfOpen();

		_isOpened = false;
		ClearActiveOwnerEventHandlers();

		var activeOwner = GetActiveOwner();

		if (activeOwner is not null && !activeOwner.IsFocused)
		{
			// WinUI gates ForceFocusLoss on !IsProofingFlyout() so that closing
			// a proofing flyout does not tear down the selection highlight.
			if (!_isProofingFlyout)
			{
				if (activeOwner is TextBox textBox)
				{
					textBox.ForceFocusLoss();
				}
				else if (activeOwner is TextBlock textBlock)
				{
					textBlock.ForceFocusLoss();
				}
				// TODO Uno: WinUI also handles RichTextBlock and RichTextBlockOverflow here
				// via their SelectionManager.ForceFocusLoss(). These are [NotImplemented] on Skia.
				// Original C++:
				//   if (auto richTextBlock = do_pointer_cast<CRichTextBlock>(activeOwner))
				//       selectionManager = richTextBlock->GetSelectionManager();
				//   else if (auto richTextBlockOverflow = do_pointer_cast<CRichTextBlockOverflow>(activeOwner))
				//       selectionManager = richTextBlockOverflow->GetMaster()->GetSelectionManager();
				//   if (selectionManager) selectionManager->ForceFocusLoss();
			}
		}

		_activeOwnerRef = null;
	}

	private void OnActiveOwnerLosingFocus(object sender, LosingFocusEventArgs e)
	{
		if (e.NewFocusedElement is UIElement newFocusedElement)
		{
			var flyoutGettingFocus = Popup.GetClosestFlyoutAncestor(newFocusedElement);

			if (flyoutGettingFocus is not null && flyoutGettingFocus == GetFlyout())
			{
				_isGettingFocus = true;
			}
		}
	}

	private void ClearActiveOwnerEventHandlers()
	{
		var activeOwner = GetActiveOwner();
		if (activeOwner is not null)
		{
			activeOwner.LosingFocus -= OnActiveOwnerLosingFocus;
		}
	}
}

/// <summary>
/// Registry and helpers for <see cref="TextControlFlyout"/> wrappers.
/// Equivalent to WinUI's TextControlFlyoutHelper namespace which uses
/// DXamlCore.GetTextControlFlyout / SetTextControlFlyout.
/// </summary>
internal static class TextControlFlyoutHelper
{
	private static readonly ConditionalWeakTable<FlyoutBase, TextControlFlyout> _flyouts = new();

	public static bool IsGettingFocus(FlyoutBase? flyout, FrameworkElement owner)
	{
		if (flyout is null)
		{
			return false;
		}

		if (!_flyouts.TryGetValue(flyout, out var wrapper))
		{
			return false;
		}

		if (!wrapper.IsGettingFocus)
		{
			return false;
		}

		return wrapper.GetActiveOwner() == owner;
	}

	public static bool IsOpen(FlyoutBase? flyout)
	{
		if (flyout is null)
		{
			return false;
		}

		if (_flyouts.TryGetValue(flyout, out var wrapper))
		{
			return wrapper.IsOpen();
		}

		return false;
	}

	public static bool IsElementChildOfOpenedFlyout(UIElement? element)
	{
		if (element is null)
		{
			return false;
		}

		var flyout = Popup.GetClosestFlyoutAncestor(element);
		if (flyout is null)
		{
			return false;
		}

		return _flyouts.TryGetValue(flyout, out var wrapper) && wrapper.IsOpened;
	}

	public static bool IsElementChildOfTransientOpenedFlyout(UIElement? element)
	{
		if (element is null)
		{
			return false;
		}

		var flyout = Popup.GetClosestFlyoutAncestor(element);
		if (flyout is null)
		{
			return false;
		}

		return _flyouts.TryGetValue(flyout, out var wrapper) && wrapper.IsOpened && wrapper.IsTransient;
	}

	public static bool IsElementChildOfProofingFlyout(UIElement? element)
	{
		if (element is null)
		{
			return false;
		}

		var flyout = Popup.GetClosestFlyoutAncestor(element);
		if (flyout is null)
		{
			return false;
		}

		return _flyouts.TryGetValue(flyout, out var wrapper) && wrapper.IsProofingFlyout;
	}

	public static void DismissAllFlyoutsForOwner(UIElement? element)
	{
		if (element is null)
		{
			return;
		}

		var flyout = Popup.GetClosestFlyoutAncestor(element);
		if (flyout is null)
		{
			return;
		}

		if (!_flyouts.TryGetValue(flyout, out var wrapper))
		{
			return;
		}

		var owner = wrapper.GetActiveOwner();
		if (owner is null)
		{
			return;
		}

		// Delegate to owner-specific DismissAllFlyouts which closes all tracked flyouts.
		if (owner is Controls.TextBox textBox)
		{
			textBox.DismissAllFlyouts();
		}
		else
		{
			// Close both ContextFlyout and SelectionFlyout on the owner.
			CloseIfOpen(owner.ContextFlyout);
			if (owner is Controls.TextBlock textBlock)
			{
				CloseIfOpen(textBlock.SelectionFlyout);
			}
		}
	}

	public static void CloseIfOpen(FlyoutBase? flyout)
	{
		if (flyout is null)
		{
			return;
		}

		if (_flyouts.TryGetValue(flyout, out var wrapper))
		{
			wrapper.CloseIfOpen();
		}
	}

	public static void AddProofingFlyout(FlyoutBase flyout, FrameworkElement owner)
	{
		if (!_flyouts.TryGetValue(flyout, out var wrapper))
		{
			wrapper = new TextControlFlyout(flyout, isProofingFlyout: true);
			_flyouts.AddOrUpdate(flyout, wrapper);
		}

		if (wrapper.GetActiveOwner() != owner)
		{
			wrapper.SetActiveOwner(owner);
		}
	}

	public static void ShowAt(FlyoutBase flyout, FrameworkElement owner, Point point, Rect exclusionRect, FlyoutShowMode showMode)
	{
		// Ported from TextControlFlyoutHelper.cpp:153-181
		// For backward compatibility, fire ContextMenuOpening event for every
		// floatie invocation. If the app handles it, do not show the flyout.
		if (owner is Controls.TextBox textBox)
		{
			// WinUI: CTextBoxBase covers TextBox, PasswordBox, RichEditBox
			// In Uno, PasswordBox extends TextBox so this covers both.
			if (textBox.FireContextMenuOpeningEventSynchronously(point))
			{
				return;
			}
		}
		else if (owner is Controls.TextBlock textBlock)
		{
			if (textBlock.FireContextMenuOpeningEventSynchronously(point))
			{
				return;
			}
		}
		// TODO Uno: WinUI also fires ContextMenuOpening for RichTextBlock
		// (CRichTextBlock::FireContextMenuOpeningEventSynchronously, TextBlock.cpp:3607).

		if (!_flyouts.TryGetValue(flyout, out var wrapper))
		{
			wrapper = new TextControlFlyout(flyout);
			_flyouts.AddOrUpdate(flyout, wrapper);
		}

		if (wrapper.GetActiveOwner() != owner)
		{
			wrapper.SetActiveOwner(owner);
		}

		wrapper.ShowAt(point, exclusionRect, showMode);
	}

	/// <summary>Backward-compatible overload without exclusion rect.</summary>
	public static void ShowAt(FlyoutBase flyout, FrameworkElement owner, Point point, FlyoutShowMode showMode)
		=> ShowAt(flyout, owner, point, default, showMode);
}
#endif
