#nullable enable

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Hosting;

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
/// fan-out calls via the <c>Route*</c> methods on <see cref="SkiaAccessibilityBase"/>.
/// </remarks>
internal static class AccessibilityRouter
{
	private static IAccessibilityOwner? _activeOwner;
	private static bool _initialized;
	private static readonly object _gate = new();

	/// <summary>
	/// Claims the framework's single-slot accessibility registrations and
	/// points them at this router. Idempotent; subsequent calls are no-ops.
	/// Called from host startup (Win32Host / MacSkiaHost).
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
			if (typeof(AccessibilityRouter).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AccessibilityRouter).Log().Debug(
					$"[A11y] AccessibilityRouter.Resolve: could not resolve owner UIElement for peer {peer?.GetType().Name ?? "null"}");
			}
			return null;
		}

		return Resolve(element);
	}

	/// <summary>Resolves a UIElement to its owning window's instance, or null.</summary>
	public static SkiaAccessibilityBase? Resolve(UIElement element)
	{
		if (element.XamlRoot is not { } xamlRoot)
		{
			if (typeof(AccessibilityRouter).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AccessibilityRouter).Log().Debug(
					$"[A11y] AccessibilityRouter.Resolve: element {element.GetType().Name} has no XamlRoot; dropping callback");
			}
			return null;
		}

		var host = XamlRootMap.GetHostForRoot(xamlRoot);
		return (host as IAccessibilityOwner)?.Accessibility;
	}

	// ────────────────────────────────────────────────────────────────
	//  Active-window fallback path
	// ────────────────────────────────────────────────────────────────

	/// <summary>Returns the active instance (sticky), or null.</summary>
	public static SkiaAccessibilityBase? TryGetActive()
		=> _activeOwner?.Accessibility;

	internal static IAccessibilityOwner? FindAnyLiveOwner()
	{
		foreach (var pair in XamlRootMap.Enumerate())
		{
			if (pair.Value is IAccessibilityOwner { Accessibility: { } accessibility } owner &&
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

	private static void OnVisualOffsetOrSizeChanged(Visual visual)
	{
		if (visual is ContainerVisual { Owner.Target: UIElement owner })
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
			foreach (var pair in XamlRootMap.Enumerate())
			{
				if (pair.Value is IAccessibilityOwner { Accessibility: { IsAccessibilityEnabled: true } })
				{
					return true;
				}
			}
			return false;
		}

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
			=> NotifyAutomationEvent(peer, eventId);
	}

	private sealed class RouterAnnouncerShim : IUnoAccessibility
	{
		public bool IsAccessibilityEnabled
			=> _activeOwner?.Accessibility?.IsAccessibilityEnabled == true;

		public void AnnouncePolite(string text)
		{
			if (TryGetActive() is { } active)
			{
				active.AnnouncePolite(text);
				return;
			}

			if (typeof(AccessibilityRouter).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AccessibilityRouter).Log().Debug(
					$"[A11y] Source-less polite announcement dropped — no active accessibility owner (FR-008). Text=\"{text}\"");
			}
		}

		public void AnnounceAssertive(string text)
		{
			if (TryGetActive() is { } active)
			{
				active.AnnounceAssertive(text);
				return;
			}

			if (typeof(AccessibilityRouter).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AccessibilityRouter).Log().Debug(
					$"[A11y] Source-less assertive announcement dropped — no active accessibility owner (FR-008). Text=\"{text}\"");
			}
		}
	}
}
