#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleAppNotificationSettingEvaluator
{
	[TestMethod]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.NotDetermined, AppNotificationSetting.DisabledForApplication)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Denied, AppNotificationSetting.DisabledForApplication)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Authorized, AppNotificationSetting.Enabled)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Provisional, AppNotificationSetting.Enabled)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Ephemeral, AppNotificationSetting.Enabled)]
	public void When_Authorization_Is_Evaluated_Setting_Is_Mapped(
		int status,
		AppNotificationSetting expected)
		=> Assert.AreEqual(expected, AppleAppNotificationSettingEvaluator.Evaluate((AppleAppNotificationAuthorizationStatus)status));

	[TestMethod]
	public void When_Initial_Settings_Query_Is_Pending_Readiness_Is_Not_Reported()
	{
		var cache = new AppleAppNotificationSettingCache();
		var generation = cache.BeginRefresh();

		Assert.IsFalse(cache.TryWaitForCurrentRefresh(TimeSpan.Zero, out _));

		cache.CompleteRefresh(generation, AppleAppNotificationAuthorizationStatus.Authorized);
		Assert.IsTrue(cache.TryWaitForCurrentRefresh(TimeSpan.Zero, out var status));
		Assert.AreEqual(AppleAppNotificationAuthorizationStatus.Authorized, status);
	}

	[TestMethod]
	public void When_Foreground_Refresh_Completes_Previous_Setting_Is_Replaced()
	{
		var cache = new AppleAppNotificationSettingCache();
		var initialGeneration = cache.BeginRefresh();
		cache.CompleteRefresh(initialGeneration, AppleAppNotificationAuthorizationStatus.Denied);
		var foregroundGeneration = cache.BeginRefresh();

		Assert.IsFalse(cache.TryWaitForCurrentRefresh(TimeSpan.Zero, out _));

		cache.CompleteRefresh(foregroundGeneration, AppleAppNotificationAuthorizationStatus.Authorized);
		Assert.IsTrue(cache.TryWaitForCurrentRefresh(TimeSpan.Zero, out var status));
		Assert.AreEqual(AppleAppNotificationAuthorizationStatus.Authorized, status);
	}

	[TestMethod]
	public void When_Stale_Settings_Callback_Completes_Current_Refresh_Remains_Pending()
	{
		var cache = new AppleAppNotificationSettingCache();
		var initialGeneration = cache.BeginRefresh();
		var foregroundGeneration = cache.BeginRefresh();

		cache.CompleteRefresh(initialGeneration, AppleAppNotificationAuthorizationStatus.Authorized);

		Assert.IsFalse(cache.TryWaitForCurrentRefresh(TimeSpan.Zero, out _));
		cache.CompleteRefresh(foregroundGeneration, AppleAppNotificationAuthorizationStatus.Denied);
		Assert.IsTrue(cache.TryWaitForCurrentRefresh(TimeSpan.Zero, out var status));
		Assert.AreEqual(AppleAppNotificationAuthorizationStatus.Denied, status);
	}

	[TestMethod]
	public async Task When_Waiting_Settings_Query_Is_Superseded_It_Waits_For_Foreground_Refresh()
	{
		var cache = new AppleAppNotificationSettingCache();
		cache.BeginRefresh();
		using var waitStarted = new ManualResetEventSlim();
		var waiting = Task.Run(() =>
		{
			waitStarted.Set();
			var succeeded = cache.TryWaitForCurrentRefresh(TimeSpan.FromSeconds(2), out var status);
			return (succeeded, status);
		});
		Assert.IsTrue(waitStarted.Wait(TimeSpan.FromSeconds(1)));
		Thread.Sleep(50);

		var foregroundGeneration = cache.BeginRefresh();
		cache.CompleteRefresh(foregroundGeneration, AppleAppNotificationAuthorizationStatus.Authorized);

		var result = await waiting.WaitAsync(TimeSpan.FromSeconds(1));
		Assert.IsTrue(result.succeeded);
		Assert.AreEqual(AppleAppNotificationAuthorizationStatus.Authorized, result.status);
	}

	[TestMethod]
	public void When_Progress_Capability_Is_Queried_Apple_Reports_Unsupported()
		=> Assert.IsFalse(AppleAppNotificationCapabilities.SupportsProgressUpdates);
}