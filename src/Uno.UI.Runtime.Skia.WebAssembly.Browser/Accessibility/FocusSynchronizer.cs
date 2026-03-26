#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Bidirectional bridge between XAML FocusManager and browser document.activeElement.
/// Prevents infinite loops with IsSyncing guard and implements roving tabindex.
/// Resolves focus requests to the nearest semantic DOM element before calling into JS.
/// </summary>
internal sealed partial class FocusSynchronizer
{
	private readonly WebAssemblyAccessibility _accessibility;
	private IntPtr _currentFocusedHandle;
	private IntPtr _previousFocusedHandle;
	private bool _isSyncing;

	/// <summary>
	/// Gets whether a focus synchronization operation is in progress.
	/// Used to prevent infinite XAML-browser focus loops.
	/// </summary>
	internal bool IsSyncing => _isSyncing;

	/// <summary>
	/// Gets the handle of the currently focused semantic element.
	/// </summary>
	internal IntPtr CurrentFocusedHandle => _currentFocusedHandle;

	/// <summary>
	/// Initializes a new FocusSynchronizer with a reference to the accessibility system
	/// for semantic tree resolution.
	/// </summary>
	internal FocusSynchronizer(WebAssemblyAccessibility accessibility)
	{
		_accessibility = accessibility;
	}

	/// <summary>
	/// Initializes the focus synchronizer by subscribing to FocusManager events.
	/// </summary>
	internal void Initialize()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("[A11y] FocusSynchronizer.Initialize: subscribing to FocusManager.GotFocus and LostFocus");
		}
		FocusManager.GotFocus += OnXamlGotFocus;
		FocusManager.LostFocus += OnXamlLostFocus;
	}

	/// <summary>
	/// Handles XAML->Browser focus direction.
	/// Called when FocusManager.GotFocus fires.
	/// Resolves the focused element to its nearest semantic ancestor before syncing to the browser.
	/// </summary>
	private void OnXamlGotFocus(object? sender, FocusManagerGotFocusEventArgs args)
	{
		if (_isSyncing)
		{
			return;
		}

		if (args.NewFocusedElement is not UIElement element)
		{
			return;
		}

		var handle = element.Visual.Handle;
		if (handle == IntPtr.Zero)
		{
			return;
		}

		try
		{
			// Resolve to the nearest element that exists in the semantic DOM tree.
			// Many UIElements (Grid, Border, SplitView, etc.) are pruned from the
			// semantic tree and don't have corresponding DOM nodes.
			var semanticHandle = _accessibility.ResolveToSemanticHandle(element);
			if (semanticHandle == IntPtr.Zero)
			{
				// No semantic element found — fall back to the direct handle.
				// The JS side will gracefully handle "not found" cases.
				// This prevents focus from being silently dropped during page
				// navigation or when focusing non-semantic elements.
				semanticHandle = handle;
			}

			_isSyncing = true;
			try
			{
				_previousFocusedHandle = _currentFocusedHandle;
				_currentFocusedHandle = semanticHandle;

				NativeMethods.FocusSemanticElement(semanticHandle);

				// Track for focus recovery
				TrackFocusedElement(element);
			}
			finally
			{
				_isSyncing = false;
			}
		}
		catch (Exception ex)
		{
			_isSyncing = false;
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] FocusSynchronizer.OnXamlGotFocus failed for {element.GetType().Name}: {ex.Message}", ex);
			}
		}
	}

	private void OnXamlLostFocus(object? sender, FocusManagerLostFocusEventArgs args)
	{
		if (_isSyncing)
		{
			return;
		}

		// Focus is leaving, but new target will be handled by GotFocus.
		// Only need to blur if no new target is coming (e.g., focus leaving app).
	}

	/// <summary>
	/// Handles Browser->XAML focus direction.
	/// Called from the existing OnFocus JSExport when a semantic element receives browser focus.
	/// </summary>
	internal void OnBrowserFocus(IntPtr handle, UIElement? owner)
	{
		if (_isSyncing)
		{
			return;
		}

		if (owner is not Control control || !control.IsFocusable)
		{
			return;
		}

		_isSyncing = true;
		try
		{
			_previousFocusedHandle = _currentFocusedHandle;
			_currentFocusedHandle = handle;
			control.Focus(FocusState.Keyboard);

			// Track for focus recovery
			TrackFocusedElement(owner!);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] FocusSynchronizer.OnBrowserFocus failed for handle={handle}: {ex.Message}", ex);
			}
		}
		finally
		{
			_isSyncing = false;
		}
	}

	/// <summary>
	/// Subscribes to IsEnabled changes and Unloaded events on the currently focused element.
	/// When the focused element becomes disabled or is removed, triggers focus recovery.
	/// </summary>
	internal void TrackFocusedElement(UIElement element)
	{
		// Unsubscribe from previous element
		UntrackFocusedElement();

		_trackedElement = element;

		// Track IsEnabled changes
		if (element is Control control)
		{
			control.IsEnabledChanged += OnTrackedElementIsEnabledChanged;
		}

		// Track removal from visual tree
		if (element is FrameworkElement fe)
		{
			fe.Unloaded += OnTrackedElementUnloaded;
		}
	}

	private void UntrackFocusedElement()
	{
		if (_trackedElement is null)
		{
			return;
		}

		if (_trackedElement is Control control)
		{
			control.IsEnabledChanged -= OnTrackedElementIsEnabledChanged;
		}

		if (_trackedElement is FrameworkElement fe)
		{
			fe.Unloaded -= OnTrackedElementUnloaded;
		}

		_trackedElement = null;
	}

	private UIElement? _trackedElement;

	private void OnTrackedElementIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is false && sender is UIElement element)
		{
			RecoverFocus(element);
		}
	}

	private void OnTrackedElementUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is UIElement element)
		{
			RecoverFocus(element);
		}
	}

	/// <summary>
	/// Finds the next valid focus target when the currently focused element becomes
	/// disabled or is removed from the visual tree.
	/// Strategy: (1) next focusable sibling in tab order, (2) nearest focusable ancestor, (3) body.
	/// </summary>
	private void RecoverFocus(UIElement lostElement)
	{
		UntrackFocusedElement();

		try
		{
			// Try next focusable sibling
			var parent = lostElement.GetParent() as UIElement;
			if (parent is not null)
			{
				var children = parent.GetChildren();
				bool foundLost = false;

				// Look for the next focusable sibling after the lost element
				foreach (var child in children)
				{
					if (child == lostElement)
					{
						foundLost = true;
						continue;
					}

					if (foundLost && child is Control nextControl && nextControl.IsFocusable && nextControl.IsEnabled)
					{
						nextControl.Focus(FocusState.Keyboard);
						return;
					}
				}

				// Try siblings before the lost element
				foreach (var child in children)
				{
					if (child == lostElement)
					{
						break;
					}

					if (child is Control prevControl && prevControl.IsFocusable && prevControl.IsEnabled)
					{
						prevControl.Focus(FocusState.Keyboard);
						return;
					}
				}
			}

			// Try nearest focusable ancestor
			var ancestor = parent;
			while (ancestor is not null)
			{
				if (ancestor is Control ancestorControl && ancestorControl.IsFocusable && ancestorControl.IsEnabled)
				{
					ancestorControl.Focus(FocusState.Keyboard);
					return;
				}

				ancestor = ancestor.GetParent() as UIElement;
			}

			// Fallback: blur the current focus (focus moves to body)
			_currentFocusedHandle = IntPtr.Zero;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] FocusSynchronizer.RecoverFocus failed: {ex.Message}", ex);
			}
			_currentFocusedHandle = IntPtr.Zero;
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.focusSemanticElement")]
		internal static partial void FocusSemanticElement(IntPtr handle);
	}
}
