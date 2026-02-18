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
	private WeakReference<FrameworkElement>? _activeOwnerRef;
	private bool _isOpened;
	private bool _isGettingFocus;

	public TextControlFlyout(FlyoutBase flyout)
	{
		_flyoutRef = new WeakReference<FlyoutBase>(flyout);

		flyout.Opened += OnOpened;
		flyout.Closing += OnClosing;
		flyout.Closed += OnClosed;
	}

	public bool IsGettingFocus => _isGettingFocus;
	public bool IsOpened => _isOpened;

	public FrameworkElement? GetActiveOwner()
		=> _activeOwnerRef is not null && _activeOwnerRef.TryGetTarget(out var owner) ? owner : null;

	public FlyoutBase? GetFlyout()
		=> _flyoutRef.TryGetTarget(out var flyout) ? flyout : null;

	public void SetActiveOwner(FrameworkElement owner)
	{
		ClearActiveOwnerEventHandlers();

		_activeOwnerRef = new WeakReference<FrameworkElement>(owner);
		owner.LosingFocus += OnActiveOwnerLosingFocus;
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

	public void ShowAt(Point point, FlyoutShowMode showMode)
	{
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
					ShowMode = showMode
				});
			}
		}
	}

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
			if (activeOwner is TextBox textBox)
			{
				textBox.ForceFocusLoss();
			}
			else if (activeOwner is TextBlock textBlock)
			{
				textBlock.ForceFocusLoss();
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

	public static void ShowAt(FlyoutBase flyout, FrameworkElement owner, Point point, FlyoutShowMode showMode)
	{
		if (!_flyouts.TryGetValue(flyout, out var wrapper))
		{
			wrapper = new TextControlFlyout(flyout);
			_flyouts.AddOrUpdate(flyout, wrapper);
		}

		if (wrapper.GetActiveOwner() != owner)
		{
			wrapper.SetActiveOwner(owner);
		}

		wrapper.ShowAt(point, showMode);
	}
}
#endif
