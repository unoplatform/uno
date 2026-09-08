#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_AppNotificationNativeBackend
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public async Task When_Windows_Backend_Posts_Extended_Xml_Then_History_Preserves_It(bool withProgress)
	{
		if (!OperatingSystem.IsWindows() || !AppNotificationManager.IsSupported())
		{
			Assert.Inconclusive("The native Windows App SDK notification backend is unavailable.");
			return;
		}

		var payload = withProgress
			? "<toast><header id='uno-qa' title='Notification QA' arguments='qa'/><visual><binding template='ToastGeneric'><group><subgroup><text>Native XML QA</text></subgroup></group><progress title='{progressTitle}' value='{progressValue}' valueStringOverride='{progressValueString}' status='{progressStatus}'/></binding></visual></toast>"
			: "<toast><header id='uno-qa' title='Notification QA' arguments='qa'/><visual><binding template='ToastGeneric'><group><subgroup><text>Native XML QA</text></subgroup></group></binding></visual></toast>";
		var manager = AppNotificationManager.Default;
		var registered = false;
		var notification = new AppNotification(payload)
		{
			Tag = Guid.NewGuid().ToString("N"),
			Group = "uno-runtime-qa",
			SuppressDisplay = true,
			Expiration = DateTimeOffset.UtcNow.AddMinutes(2),
			Progress = withProgress ? new AppNotificationProgressData(7) { Status = "Starting", Value = 0.1 } : null,
		};

		try
		{
			manager.Register();
			registered = true;
			if (manager.Setting != AppNotificationSetting.Enabled)
			{
				Assert.Inconclusive("Native app notifications are disabled for this host.");
				return;
			}

			manager.Show(notification);
			Assert.AreNotEqual(0U, notification.Id);

			var records = await manager.GetAllAsync();
			var restored = records.Single(record => record.Id == notification.Id);
			Assert.AreEqual(payload, restored.Payload);
			Assert.AreEqual(notification.Tag, restored.Tag);

			if (withProgress)
			{
				var result = await manager.UpdateAsync(
					new AppNotificationProgressData(8) { Status = "Running", Value = 0.5 },
					notification.Tag,
					notification.Group);
				Assert.AreEqual(AppNotificationProgressResult.Succeeded, result);
				restored = (await manager.GetAllAsync()).Single(record => record.Id == notification.Id);
				Assert.AreEqual(payload, restored.Payload);
				Assert.AreEqual(1U, restored.Progress?.SequenceNumber);
				Assert.AreEqual(0.1, restored.Progress?.Value);
				Assert.AreEqual("Starting", restored.Progress?.Status);
			}

			await manager.RemoveByIdAsync(notification.Id);
			Assert.IsFalse((await manager.GetAllAsync()).Any(record => record.Id == notification.Id));
		}
		finally
		{
			if (notification.Id != 0)
			{
				await manager.RemoveByIdAsync(notification.Id);
			}
			if (registered)
			{
				manager.Unregister();
			}
		}
	}
}
