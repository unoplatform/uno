#nullable enable

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Windows.AppNotifications.Internal;

internal enum AppleAppNotificationAuthorizationStatus
{
	NotDetermined,
	Denied,
	Authorized,
	Provisional,
	Ephemeral,
}

internal static class AppleAppNotificationSettingEvaluator
{
	public static AppNotificationSetting Evaluate(AppleAppNotificationAuthorizationStatus status)
		=> status switch
		{
			AppleAppNotificationAuthorizationStatus.Authorized or
			AppleAppNotificationAuthorizationStatus.Provisional or
			AppleAppNotificationAuthorizationStatus.Ephemeral => AppNotificationSetting.Enabled,
			AppleAppNotificationAuthorizationStatus.Denied => AppNotificationSetting.DisabledForApplication,
			_ => AppNotificationSetting.DisabledForApplication,
		};
}

internal sealed class AppleAppNotificationSettingCache
{
	private readonly object _gate = new();
	private long _generation;
	private long _completedGeneration;
	private AppleAppNotificationAuthorizationStatus _status;

	public bool HasRefresh
	{
		get
		{
			lock (_gate)
			{
				return _generation > 0;
			}
		}
	}

	public long BeginRefresh()
	{
		lock (_gate)
		{
			_generation++;
			Monitor.PulseAll(_gate);
			return _generation;
		}
	}

	public void CompleteRefresh(long generation, AppleAppNotificationAuthorizationStatus status)
	{
		lock (_gate)
		{
			if (generation == _generation)
			{
				_status = status;
				_completedGeneration = generation;
				Monitor.PulseAll(_gate);
			}
		}
	}

	public bool TryWaitForCurrentRefresh(TimeSpan timeout, out AppleAppNotificationAuthorizationStatus status)
	{
		var elapsed = Stopwatch.StartNew();
		lock (_gate)
		{
			if (_generation == 0)
			{
				status = AppleAppNotificationAuthorizationStatus.NotDetermined;
				return false;
			}
			while (_completedGeneration != _generation)
			{
				var remaining = timeout - elapsed.Elapsed;
				if (remaining <= TimeSpan.Zero || !Monitor.Wait(_gate, remaining))
				{
					status = AppleAppNotificationAuthorizationStatus.NotDetermined;
					return false;
				}
			}
			status = _status;
			return true;
		}
	}
}

internal static class AppleAppNotificationCapabilities
{
	public static bool SupportsProgressUpdates => false;
}