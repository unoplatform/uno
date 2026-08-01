#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

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
	private int _lifecycleGeneration;
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
			this.Log().Debug($"HandleLiveRegionChanged peer={peer.GetType().Name} liveSetting={liveSetting} contentLength={content?.Length ?? 0}");
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
			this.Log().Trace($"LiveRegionChanged: liveSetting={liveSetting}, contentLength={content.Length}");
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
			this.Log().Debug($"EnqueuePolite contentLength={content.Length} debounce={DebounceMs}ms");
		}
		var lifecycleGeneration = Volatile.Read(ref _lifecycleGeneration);
		RunOnDispatcher(() =>
		{
			if (lifecycleGeneration != Volatile.Read(ref _lifecycleGeneration))
			{
				return;
			}

			_pendingPoliteContent = content;
			if (_politeDebounceTimer is null)
			{
				SchedulePolite(DebounceMs);
			}
		});
	}

	private void EnqueueAssertive(string content)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"EnqueueAssertive contentLength={content.Length} debounce={DebounceMs}ms");
		}
		var lifecycleGeneration = Volatile.Read(ref _lifecycleGeneration);
		RunOnDispatcher(() =>
		{
			if (lifecycleGeneration != Volatile.Read(ref _lifecycleGeneration))
			{
				return;
			}

			_pendingAssertiveContent = content;
			if (_assertiveDebounceTimer is null)
			{
				ScheduleAssertive(DebounceMs);
			}
		});
	}

	private void SchedulePolite(int delay)
	{
		var generation = ++_politeGeneration;
		_politeDebounceTimer?.Dispose();
		_politeDebounceTimer = new Timer(
			_ => NativeDispatcher.Main.Enqueue(() => FlushPolite(generation)),
			null,
			delay,
			Timeout.Infinite);
	}

	private void ScheduleAssertive(int delay)
	{
		var generation = ++_assertiveGeneration;
		_assertiveDebounceTimer?.Dispose();
		_assertiveDebounceTimer = new Timer(
			_ => NativeDispatcher.Main.Enqueue(() => FlushAssertive(generation)),
			null,
			delay,
			Timeout.Infinite);
	}

	private void FlushPolite(int generation)
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
			SchedulePolite(PoliteThrottleMs - (int)(now - _politeThrottleTimestamp));
			return;
		}

		_politeThrottleTimestamp = now;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"FlushPolite announcing contentLength={content.Length}");
		}
		NativeMethods.UpdateLiveRegionContent(IntPtr.Zero, content, 1);
	}

	private void FlushAssertive(int generation)
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
			ScheduleAssertive(AssertiveThrottleMs - (int)(now - _assertiveThrottleTimestamp));
			return;
		}

		_assertiveThrottleTimestamp = now;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"FlushAssertive announcing contentLength={content.Length}");
		}
		NativeMethods.UpdateLiveRegionContent(IntPtr.Zero, content, 2);
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
		Interlocked.Increment(ref _lifecycleGeneration);
		RunOnDispatcher(() =>
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
		});
	}

	private static void RunOnDispatcher(Action action)
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			action();
		}
		else
		{
			NativeDispatcher.Main.Enqueue(action);
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
