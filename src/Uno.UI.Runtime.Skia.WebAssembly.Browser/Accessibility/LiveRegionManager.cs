#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Coordinates live region announcements with two-tier rate limiting.
/// Tier 1: 100ms debounce collapses rapid bursts to final content.
/// Tier 2: Sustained throttle caps polite at 500ms, assertive at 200ms.
/// </summary>
internal sealed partial class LiveRegionManager
{
	private const int DebounceMs = 100;
	private const int PoliteThrottleMs = 500;
	private const int AssertiveThrottleMs = 200;

	private string? _pendingPoliteContent;
	private string? _pendingAssertiveContent;
	private Timer? _politeDebounceTimer;
	private Timer? _assertiveDebounceTimer;
	private readonly object _announcementGate = new();
	private int _politeGeneration;
	private int _assertiveGeneration;
	private long _politeThrottleTimestamp;
	private long _assertiveThrottleTimestamp;
	/// <summary>
	/// When set, only announcements from elements inside the modal are allowed through.
	/// Background live region changes are suppressed while a modal is active.
	/// </summary>
	internal IntPtr ActiveModalHandle { get; set; }

	/// <summary>
	/// Handles a LiveRegionChanged automation event from an AutomationPeer.
	/// </summary>
	internal void HandleLiveRegionChanged(AutomationPeer peer)
	{
		var liveSetting = peer.GetLiveSetting();
		var content = peer.GetName();

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"HandleLiveRegionChanged peer={peer.GetType().Name} liveSetting={liveSetting} content='{content}'");
		}

		if (string.IsNullOrEmpty(content))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("HandleLiveRegionChanged skipped (empty content)");
			}
			return;
		}

		// Modal-aware filtering: when a modal dialog is active, suppress live region
		// changes from background elements. Only announcements from elements within
		// the modal (or assertive system announcements with no owner) pass through.
		if (ActiveModalHandle != IntPtr.Zero)
		{
			if (peer is FrameworkElementAutomationPeer { Owner: { } liveOwner })
			{
				if (!IsDescendantOfModal(liveOwner, ActiveModalHandle))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("HandleLiveRegionChanged suppressed (background element during modal)");
					}
					return;
				}
			}
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"LiveRegionChanged: liveSetting={liveSetting}, content={content}");
		}

		switch (liveSetting)
		{
			case AutomationLiveSetting.Off:
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("HandleLiveRegionChanged liveSetting=Off, no-op");
				}
				break;
			case AutomationLiveSetting.Polite:
				EnqueuePolite(content);
				break;
			case AutomationLiveSetting.Assertive:
				EnqueueAssertive(content);
				break;
		}
	}

	/// <summary>
	/// Checks whether the given element is a descendant of the modal dialog.
	/// </summary>
	private static bool IsDescendantOfModal(UIElement element, IntPtr modalHandle)
	{
		var current = element as DependencyObject;
		while (current is not null)
		{
			if (current is UIElement uiElement && uiElement.Visual.Handle == modalHandle)
			{
				return true;
			}
			current = (current as FrameworkElement)?.Parent;
		}
		return false;
	}

	private void EnqueuePolite(string content)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"EnqueuePolite content='{content}' debounce={DebounceMs}ms");
		}
		lock (_announcementGate)
		{
			_pendingPoliteContent = content;
			if (_politeDebounceTimer is null)
			{
				SchedulePoliteLocked(DebounceMs);
			}
		}
	}

	private void EnqueueAssertive(string content)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"EnqueueAssertive content='{content}' debounce={DebounceMs}ms");
		}
		lock (_announcementGate)
		{
			_pendingAssertiveContent = content;
			if (_assertiveDebounceTimer is null)
			{
				ScheduleAssertiveLocked(DebounceMs);
			}
		}
	}

	private void SchedulePoliteLocked(int delay)
	{
		var generation = ++_politeGeneration;
		_politeDebounceTimer = new Timer(_ => FlushPolite(generation), null, delay, Timeout.Infinite);
	}

	private void ScheduleAssertiveLocked(int delay)
	{
		var generation = ++_assertiveGeneration;
		_assertiveDebounceTimer = new Timer(_ => FlushAssertive(generation), null, delay, Timeout.Infinite);
	}

	private void FlushPolite(int generation)
	{
		lock (_announcementGate)
		{
			if (generation != _politeGeneration)
			{
				return;
			}

			_politeDebounceTimer?.Dispose();
			_politeDebounceTimer = null;
			var content = _pendingPoliteContent;
			_pendingPoliteContent = null;
			if (string.IsNullOrEmpty(content))
			{
				return;
			}

			var now = Environment.TickCount64;
			if (now - _politeThrottleTimestamp < PoliteThrottleMs)
			{
				_pendingPoliteContent = content;
				SchedulePoliteLocked(PoliteThrottleMs - (int)(now - _politeThrottleTimestamp));
				return;
			}

			_politeThrottleTimestamp = now;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"FlushPolite announcing content='{content}'");
			}
			NativeMethods.UpdateLiveRegionContent(IntPtr.Zero, content, 1);
		}
	}

	private void FlushAssertive(int generation)
	{
		lock (_announcementGate)
		{
			if (generation != _assertiveGeneration)
			{
				return;
			}

			_assertiveDebounceTimer?.Dispose();
			_assertiveDebounceTimer = null;
			var content = _pendingAssertiveContent;
			_pendingAssertiveContent = null;
			if (string.IsNullOrEmpty(content))
			{
				return;
			}

			var now = Environment.TickCount64;
			if (now - _assertiveThrottleTimestamp < AssertiveThrottleMs)
			{
				_pendingAssertiveContent = content;
				ScheduleAssertiveLocked(AssertiveThrottleMs - (int)(now - _assertiveThrottleTimestamp));
				return;
			}

			_assertiveThrottleTimestamp = now;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"FlushAssertive announcing content='{content}'");
			}
			NativeMethods.UpdateLiveRegionContent(IntPtr.Zero, content, 2);
		}
	}

	/// <summary>
	/// Clears all pending announcements. Called on accessibility disable or page unload.
	/// </summary>
	internal void ClearPending()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("ClearPending — clearing all pending announcements");
		}
		lock (_announcementGate)
		{
			_politeGeneration++;
			_assertiveGeneration++;
			_politeDebounceTimer?.Dispose();
			_politeDebounceTimer = null;
			_assertiveDebounceTimer?.Dispose();
			_assertiveDebounceTimer = null;
			_pendingPoliteContent = null;
			_pendingAssertiveContent = null;
			NativeMethods.ClearPendingAnnouncements();
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.LiveRegion.updateLiveRegionContent")]
		internal static partial void UpdateLiveRegionContent(IntPtr handle, string content, int liveSetting);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.LiveRegion.clearPendingAnnouncements")]
		internal static partial void ClearPendingAnnouncements();
	}
}
