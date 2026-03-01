#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
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
	private long _politeThrottleTimestamp;
	private long _assertiveThrottleTimestamp;

	/// <summary>
	/// Handles a LiveRegionChanged automation event from an AutomationPeer.
	/// </summary>
	internal void HandleLiveRegionChanged(AutomationPeer peer)
	{
		var liveSetting = peer.GetLiveSetting();
		var content = peer.GetName();

		Console.WriteLine($"[A11y] LIVE REGION: HandleLiveRegionChanged peer={peer.GetType().Name} liveSetting={liveSetting} content='{content}'");

		if (string.IsNullOrEmpty(content))
		{
			Console.WriteLine("[A11y] LIVE REGION: SKIPPED (empty content)");
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"LiveRegionChanged: liveSetting={liveSetting}, content={content}");
		}

		switch (liveSetting)
		{
			case AutomationLiveSetting.Off:
				Console.WriteLine("[A11y] LIVE REGION: liveSetting=Off — no-op");
				break;
			case AutomationLiveSetting.Polite:
				EnqueuePolite(content);
				break;
			case AutomationLiveSetting.Assertive:
				EnqueueAssertive(content);
				break;
		}
	}

	private void EnqueuePolite(string content)
	{
		Console.WriteLine($"[A11y] LIVE REGION: EnqueuePolite content='{content}' debounce={DebounceMs}ms");
		_pendingPoliteContent = content;
		_politeDebounceTimer?.Dispose();
		_politeDebounceTimer = new Timer(_ => FlushPolite(), null, DebounceMs, Timeout.Infinite);
	}

	private void EnqueueAssertive(string content)
	{
		Console.WriteLine($"[A11y] LIVE REGION: EnqueueAssertive content='{content}' debounce={DebounceMs}ms");
		_pendingAssertiveContent = content;
		_assertiveDebounceTimer?.Dispose();
		_assertiveDebounceTimer = new Timer(_ => FlushAssertive(), null, DebounceMs, Timeout.Infinite);
	}

	private void FlushPolite()
	{
		var content = _pendingPoliteContent;
		_pendingPoliteContent = null;
		_politeDebounceTimer?.Dispose();
		_politeDebounceTimer = null;

		if (string.IsNullOrEmpty(content))
		{
			return;
		}

		var now = Environment.TickCount64;
		if (now - _politeThrottleTimestamp < PoliteThrottleMs)
		{
			// Throttled — re-enqueue with remaining throttle time
			var remaining = PoliteThrottleMs - (int)(now - _politeThrottleTimestamp);
			_pendingPoliteContent = content;
			_politeDebounceTimer = new Timer(_ => FlushPolite(), null, remaining, Timeout.Infinite);
			return;
		}

		_politeThrottleTimestamp = now;
		Console.WriteLine($"[A11y] LIVE REGION: FlushPolite ANNOUNCING content='{content}'");
		NativeMethods.UpdateLiveRegionContent(IntPtr.Zero, content, 1);
	}

	private void FlushAssertive()
	{
		var content = _pendingAssertiveContent;
		_pendingAssertiveContent = null;
		_assertiveDebounceTimer?.Dispose();
		_assertiveDebounceTimer = null;

		if (string.IsNullOrEmpty(content))
		{
			return;
		}

		var now = Environment.TickCount64;
		if (now - _assertiveThrottleTimestamp < AssertiveThrottleMs)
		{
			// Throttled — re-enqueue with remaining throttle time
			var remaining = AssertiveThrottleMs - (int)(now - _assertiveThrottleTimestamp);
			_pendingAssertiveContent = content;
			_assertiveDebounceTimer = new Timer(_ => FlushAssertive(), null, remaining, Timeout.Infinite);
			return;
		}

		_assertiveThrottleTimestamp = now;
		Console.WriteLine($"[A11y] LIVE REGION: FlushAssertive ANNOUNCING content='{content}'");
		NativeMethods.UpdateLiveRegionContent(IntPtr.Zero, content, 2);
	}

	/// <summary>
	/// Clears all pending announcements. Called on accessibility disable or page unload.
	/// </summary>
	internal void ClearPending()
	{
		Console.WriteLine("[A11y] LIVE REGION: ClearPending — clearing all pending announcements");
		_politeDebounceTimer?.Dispose();
		_politeDebounceTimer = null;
		_assertiveDebounceTimer?.Dispose();
		_assertiveDebounceTimer = null;
		_pendingPoliteContent = null;
		_pendingAssertiveContent = null;
		NativeMethods.ClearPendingAnnouncements();
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.LiveRegion.updateLiveRegionContent")]
		internal static partial void UpdateLiveRegionContent(IntPtr handle, string content, int liveSetting);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.LiveRegion.clearPendingAnnouncements")]
		internal static partial void ClearPendingAnnouncements();
	}
}
