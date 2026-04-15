#nullable enable

// CONTRACT — Phase 1 design artifact.
// This file is not compiled in-place; the shipped implementation lives at
// src/Uno.UI.Runtime.Skia/Accessibility/AccessibilityRouter.cs.
//
// Feature: 001-multi-window-a11y

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Process-wide router that owns the framework's single-slot accessibility
/// registration points and dispatches each incoming signal to the correct
/// per-window <see cref="SkiaAccessibilityBase"/> instance, resolved via
/// the element's XamlRoot and the platform's XamlRootMap.
/// </summary>
/// <remarks>
/// Registration slots claimed by this router:
///   * AutomationPeer.AutomationPeerListener
///   * AccessibilityAnnouncer.AccessibilityImpl
///   * UIElementAccessibilityHelper.ExternalOnChildAdded / ExternalOnChildRemoved
///   * VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged
///
/// Per-window instances MUST NOT write to these slots directly; they receive
/// fan-out calls via the abstract protected methods on SkiaAccessibilityBase.
/// </remarks>
internal static class AccessibilityRouter
{
	private static IAccessibilityOwner? _activeOwner;
	private static bool _initialized;
	private static readonly object _gate = new();

	/// <summary>
	/// Claims the framework's single-slot accessibility registrations and
	/// points them at this router. Idempotent; subsequent calls are no-ops.
	/// Called from <c>Win32Host</c> / <c>MacSkiaHost</c> static construction.
	/// </summary>
	public static void EnsureInitialized()
	{
		if (_initialized)
		{
			return;
		}

		lock (_gate)
		{
			if (_initialized)
			{
				return;
			}

			AccessibilityAnnouncer.AccessibilityImpl = new RouterAnnouncerShim();
			UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
			UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;
			VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnVisualOffsetOrSizeChanged;
			AutomationPeer.AutomationPeerListener = new RouterAutomationPeerListener();

			_initialized = true;
		}
	}

	/// <summary>Updates the sticky active-owner reference.</summary>
	/// <remarks>
	/// Called by wrappers on platform activation signals:
	///   * Win32: WM_ACTIVATE with WA_ACTIVE or WA_CLICKACTIVE.
	///   * macOS: NSWindowDidBecomeMainNotification.
	/// Never called on deactivation; the last-active owner is retained so
	/// source-less announcements that arrive while the app is inactive
	/// still have a target when the user returns.
	/// </remarks>
	public static void SetActive(IAccessibilityOwner owner)
	{
		_activeOwner = owner;
	}

	/// <summary>
	/// Called by wrappers when their per-window accessibility instance is
	/// being disposed. If the disposed owner was the active one, picks any
	/// other live owner as a best-effort fallback (FR-008).
	/// </summary>
	public static void NotifyDisposed(IAccessibilityOwner owner)
	{
		if (ReferenceEquals(_activeOwner, owner))
		{
			_activeOwner = FindAnyLiveOwner();
		}
	}

	// ────────────────────────────────────────────────────────────────
	//  Resolution
	// ────────────────────────────────────────────────────────────────

	/// <summary>Resolves an automation peer to its owning window's instance, or null.</summary>
	public static SkiaAccessibilityBase? Resolve(AutomationPeer peer)
	{
		if (!SkiaAccessibilityBase.TryGetPeerOwner(peer, out var element))
		{
			return null;
		}

		return Resolve(element);
	}

	/// <summary>Resolves a UIElement to its owning window's instance, or null.</summary>
	public static SkiaAccessibilityBase? Resolve(UIElement element)
	{
		if (element.XamlRoot is not { } xamlRoot)
		{
			return null;
		}

		var host = XamlRootMap.GetHostForRoot(xamlRoot);
		return (host as IAccessibilityOwner)?.Accessibility;
	}

	// ────────────────────────────────────────────────────────────────
	//  Active-window fallback path
	// ────────────────────────────────────────────────────────────────

	/// <summary>Returns the active instance, or any live fallback, or null.</summary>
	public static SkiaAccessibilityBase? TryGetActive()
		=> _activeOwner?.Accessibility;

	private static IAccessibilityOwner? FindAnyLiveOwner()
	{
		// Iterate XamlRootMap for any IAccessibilityOwner with a non-disposed instance.
		// Implementation detail left to the consuming host; XamlRootMap exposes enumeration.
		foreach (var (_, host) in XamlRootMap.Enumerate())
		{
			if (host is IAccessibilityOwner { Accessibility: { } accessibility } owner &&
				accessibility.IsAccessibilityEnabled)
			{
				return owner;
			}
		}

		return null;
	}

	// ────────────────────────────────────────────────────────────────
	//  Fan-out — tree mutations & visual changes
	// ────────────────────────────────────────────────────────────────

	private static void OnChildAdded(UIElement parent, UIElement child, int? index)
		=> Resolve(parent)?.RouteChildAdded(parent, child, index);

	private static void OnChildRemoved(UIElement parent, UIElement child)
		=> Resolve(parent)?.RouteChildRemoved(parent, child);

	private static void OnVisualOffsetOrSizeChanged(Microsoft.UI.Composition.Visual visual)
	{
		// Visual → owning UIElement → instance.
		if (visual is Microsoft.UI.Composition.ContainerVisual { Owner.Target: UIElement owner })
		{
			Resolve(owner)?.RouteVisualOffsetOrSizeChanged(visual);
		}
	}

	// ────────────────────────────────────────────────────────────────
	//  Fan-out shims — automation peer listener / announcer
	// ────────────────────────────────────────────────────────────────

	private sealed class RouterAutomationPeerListener : IAutomationPeerListener
	{
		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty property, object oldValue, object newValue)
			=> Resolve(peer)?.NotifyPropertyChangedEvent(peer, property, oldValue, newValue);

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
			=> Resolve(peer)?.NotifyAutomationEvent(peer, eventId);

		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind kind, AutomationNotificationProcessing processing, string displayString, string activityId)
			=> Resolve(peer)?.NotifyNotificationEvent(peer, kind, processing, displayString, activityId);

		public bool ListenerExistsHelper(AutomationEvents eventId)
		{
			// Any live instance with accessibility enabled counts as a listener.
			foreach (var (_, host) in XamlRootMap.Enumerate())
			{
				if (host is IAccessibilityOwner { Accessibility.IsAccessibilityEnabled: true })
				{
					return true;
				}
			}
			return false;
		}

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
			=> NotifyAutomationEvent(peer, eventId);

		public void OnAdviseEventAdded(int eventId, int[]? propertyIds) { }
		public void OnAdviseEventRemoved(int eventId, int[]? propertyIds) { }
	}

	private sealed class RouterAnnouncerShim : IUnoAccessibility
	{
		public bool IsAccessibilityEnabled
			=> _activeOwner?.Accessibility?.IsAccessibilityEnabled == true;

		public void AnnouncePolite(string text)
		{
			// Source-less path (FR-007, FR-008): route to active owner; drop if none.
			if (TryGetActive() is { } active)
			{
				active.AnnouncePolite(text);
			}
			// else: drop with trace (FR-008). Trace omitted here for contract brevity.
		}

		public void AnnounceAssertive(string text)
		{
			if (TryGetActive() is { } active)
			{
				active.AnnounceAssertive(text);
			}
		}
	}
}
