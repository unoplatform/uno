#nullable enable

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime;

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
///   * UIElementAccessibilityHelper.ExternalOnChildAdded / ExternalOnChildRemoved / ExternalOnTextControlStateChanged
///   * VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged
///     (these once a bridge is enabled, see <see cref="EnsureTreeNotifications"/>)
///
/// Per-window instances MUST NOT write to these slots directly; they receive
/// fan-out calls via the <c>Route*</c> methods on <see cref="SkiaAccessibilityBase"/>.
/// </remarks>
internal static class AccessibilityRouter
{
	private static volatile IAccessibilityOwner? _activeOwner;
	private static volatile bool _initialized;
	private static volatile bool _anyBridgeEnabled;
	private static readonly object _gate = new();

	/// <summary>
	/// Whether any window's bridge has been enabled since startup. Sticky: until then no bridge can consume a
	/// signal, so the router drops it before resolving the owning window.
	/// </summary>
	internal static bool IsAnyBridgeEnabled => _anyBridgeEnabled;

	/// <summary>
	/// Claims the framework's single-slot accessibility registrations and
	/// points them at this router. Idempotent; subsequent calls are no-ops.
	/// Called from host startup (Win32Host / MacOSHost).
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
			AutomationPeer.AutomationPeerListener = new RouterAutomationPeerListener();

			_initialized = true;
		}
	}

	/// <summary>
	/// Starts routing visual tree changes, which costs every layout pass a lookup. A bridge calls this once it is
	/// enabled: it builds its tree from scratch then, so the changes it missed don't matter.
	/// </summary>
	public static void EnsureTreeNotifications()
	{
		_anyBridgeEnabled = true;
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;
		UIElementAccessibilityHelper.ExternalOnTextControlStateChanged = OnTextControlStateChanged;
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnVisualOffsetOrSizeChanged;
	}

	/// <summary>Updates the sticky active-owner reference.</summary>
	/// <remarks>
	/// Called by wrappers on platform activation signals:
	///   * Win32: WM_ACTIVATE with WA_ACTIVE or WA_CLICKACTIVE.
	///   * macOS: NSWindowDidBecomeMainNotification.
	///   * Android: Activity resume/window-focus activation.
	///   * iOS: Window/controller activation.
	/// Never called on deactivation; the last-active owner is retained so
	/// source-less announcements that arrive while the app is inactive
	/// still have a target when the user returns.
	/// </remarks>
	public static void SetActive(IAccessibilityOwner owner)
	{
		lock (_gate)
		{
			_activeOwner = owner;
		}
	}

	/// <summary>
	/// Called by wrappers when their per-window accessibility instance is
	/// being disposed. If the disposed owner was the active one, picks any
	/// other live owner as a best-effort fallback (FR-008).
	/// </summary>
	public static void NotifyDisposed(IAccessibilityOwner owner)
	{
		lock (_gate)
		{
			if (ReferenceEquals(_activeOwner, owner))
			{
				_activeOwner = FindAnyLiveOwner(owner);
			}
		}
	}

	// ────────────────────────────────────────────────────────────────
	//  Resolution
	// ────────────────────────────────────────────────────────────────

	/// <summary>Resolves an automation peer to its owning window's instance, or null.</summary>
	public static SkiaAccessibilityBase? Resolve(AutomationPeer peer)
	{
		var providerPeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		if (!SkiaAccessibilityBase.TryGetPeerOwner(providerPeer, peer, out var element) &&
			(providerPeer is ItemAutomationPeer ||
				!providerPeer.TryGetProviderOwner(out element)))
		{
			if (typeof(AccessibilityRouter).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(AccessibilityRouter).Log().Trace(
					$"[A11y] AccessibilityRouter.Resolve: could not resolve owner UIElement for peer {peer?.GetType().Name ?? "null"}");
			}
			return null;
		}

		return Resolve(element);
	}

	/// <summary>Resolves a UIElement to its owning window's instance, or null.</summary>
	public static SkiaAccessibilityBase? Resolve(UIElement element)
	{
		var current = element;
		var xamlRoot = current.XamlRoot;
		while (xamlRoot is null &&
			current.GetUIElementAdjustedParentInternal() is { } adjustedParent)
		{
			current = adjustedParent;
			xamlRoot = current.XamlRoot;
		}

		if (xamlRoot is null)
		{
			if (typeof(AccessibilityRouter).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(AccessibilityRouter).Log().Trace(
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

	internal static IAccessibilityOwner? FindAnyLiveOwner(IAccessibilityOwner? excludedOwner = null)
	{
		foreach (var pair in XamlRootMap.Enumerate())
		{
			if (pair.Value is IAccessibilityOwner { Accessibility: { } accessibility } owner &&
				!ReferenceEquals(owner, excludedOwner) &&
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

	private static void OnTextControlStateChanged(UIElement element)
		=> Resolve(element)?.RouteTextControlStateChanged(element);

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

	// Every bridge ignores a signal while disabled and rebuilds its tree when enabled, so a signal is dropped before
	// the owner lookup until one is.
	private sealed class RouterAutomationPeerListener : IAutomationPeerListener
	{
		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty property, object oldValue, object newValue)
		{
			if (_anyBridgeEnabled)
			{
				Resolve(peer)?.NotifyPropertyChangedEvent(peer, property, oldValue, newValue);
			}
		}

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		{
			if (_anyBridgeEnabled)
			{
				Resolve(peer)?.NotifyAutomationEvent(peer, eventId);
			}
		}

		public void NotifyAccessibilityViewChanged(
			UIElement element,
			AccessibilityView oldValue,
			AccessibilityView newValue)
		{
			if (_anyBridgeEnabled)
			{
				Resolve(element)?.NotifyAccessibilityViewChanged(element, oldValue, newValue);
			}
		}

		public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child)
		{
			if (_anyBridgeEnabled)
			{
				Resolve(peer)?.NotifyStructureChangedEvent(peer, structureChangeType, child);
			}
		}

		public void NotifyInvalidatePeer(AutomationPeer peer)
		{
			if (_anyBridgeEnabled)
			{
				Resolve(peer)?.NotifyInvalidatePeer(peer);
			}
		}

		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind kind, AutomationNotificationProcessing processing, string displayString, string activityId)
		{
			if (_anyBridgeEnabled)
			{
				Resolve(peer)?.NotifyNotificationEvent(peer, kind, processing, displayString, activityId);
			}
		}

		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType changeType, System.Collections.Generic.IReadOnlyList<string> changedData)
		{
			if (_anyBridgeEnabled)
			{
				Resolve(peer)?.NotifyTextEditTextChangedEvent(peer, changeType, changedData);
			}
		}

		// Asked on every TextBlock text change, so it must not allocate.
		public bool ListenerExistsHelper(AutomationEvents eventId)
		{
			if (!_anyBridgeEnabled)
			{
				return false;
			}

			foreach (var host in XamlRootMap.Hosts)
			{
				if (host is IAccessibilityOwner { Accessibility: { } accessibility }
					&& accessibility.ListenerExistsHelper(eventId))
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
					"[A11y] Source-less polite announcement dropped because no accessibility owner is active.");
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
					"[A11y] Source-less assertive announcement dropped because no accessibility owner is active.");
			}
		}
	}
}
