#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Windows.AppNotifications;
using Private.Infrastructure;
using Windows.Data.Xml.Dom;

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

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public async Task When_Legacy_Third_Line_Is_Shown_Once_And_History_Restores_It()
	{
		if (!OperatingSystem.IsWindows() || !AppNotificationManager.IsSupported())
		{
			Assert.Inconclusive("The native Windows App SDK notification backend is unavailable.");
			return;
		}

		const string payload = "<toast><visual><binding template='ToastText04'><text id='1'>Legacy QA</text><text id='2'>Second line</text><text id='3' xml:lang='en-US'>Third line</text></binding></visual></toast>";
		var content = new XmlDocument();
		content.LoadXml(payload);
		var notification = new Windows.UI.Notifications.ToastNotification(content)
		{
			Tag = Guid.NewGuid().ToString("N")[..16],
			Group = "uno-runtime-qa",
			SuppressPopup = true,
			ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(2),
		};
		var manager = AppNotificationManager.Default;
		var registered = false;
		try
		{
			manager.Register();
			registered = true;
			if (manager.Setting != AppNotificationSetting.Enabled)
			{
				Assert.Inconclusive("Native app notifications are disabled for this host.");
				return;
			}

			Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(notification);
			var shown = (await manager.GetAllAsync()).Single(record => record.Tag == notification.Tag && record.Group == notification.Group);
			var texts = XDocument.Parse(shown.Payload).Root!.Element("visual")!.Element("binding")!.Elements("text").ToArray();
			Assert.AreEqual(2, texts.Length);
			Assert.AreEqual("Second line\nThird line", texts[1].Value);

			var restored = Windows.UI.Notifications.ToastNotificationManager.History.GetHistory()
				.Single(record => record.Tag == notification.Tag && record.Group == notification.Group);
			Assert.IsTrue(XNode.DeepEquals(XDocument.Parse(payload), XDocument.Parse(restored.Content.GetXml())));
		}
		finally
		{
			if (registered)
			{
				try
				{
					await manager.RemoveByTagAndGroupAsync(notification.Tag, notification.Group);
				}
				finally
				{
					manager.Unregister();
				}
			}
		}
	}
}
