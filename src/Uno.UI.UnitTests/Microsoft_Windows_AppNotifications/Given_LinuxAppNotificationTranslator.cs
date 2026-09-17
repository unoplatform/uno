#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_LinuxAppNotificationTranslator
{
	[TestMethod]
	public void When_Basic_Notification_Is_Translated_Freedesktop_Fields_Are_Preserved()
	{
		var now = new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);
		var notification = new AppNotificationBuilder()
			.AddArgument("action", "body")
			.AddText("Title")
			.AddText("Body")
			.SetAppLogoOverride(new Uri("file:///tmp/icon.png"))
			.SetScenario(AppNotificationScenario.Reminder)
			.BuildNotification();
		notification.Priority = AppNotificationPriority.High;
		notification.Expiration = now.AddMinutes(1);

		var command = LinuxAppNotificationTranslator.Translate(CreateEnvelope(7, notification), now);

		Assert.AreEqual((uint)7, command.Id);
		Assert.AreEqual("Title", command.Summary);
		Assert.AreEqual("Body", command.Body);
		Assert.AreEqual("file:///tmp/icon.png", command.AppIcon);
		Assert.AreEqual("appointment", command.Category);
		Assert.AreEqual((byte)2, command.Urgency);
		Assert.AreEqual(60_000, command.ExpireTimeoutMilliseconds);
		Assert.AreEqual("default", command.BodyActionKey);
		Assert.AreEqual("action=body", command.LaunchArgument);
	}

	[TestMethod]
	[DataRow("ms-appx:///Assets/My%20Icon%23%25%E2%9C%93.png")]
	[DataRow("file:///tmp/My%20Icon%23%25%E2%9C%93.png")]
	public void When_Escaped_App_Logo_Uri_Is_Translated_Escaping_Is_Preserved(string source)
	{
		var notification = new AppNotificationBuilder()
			.SetAppLogoOverride(new Uri(source))
			.BuildNotification();

		var command = LinuxAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual(source, command.AppIcon);
	}

	[TestMethod]
	public void When_Actions_Are_Translated_Stable_Keys_And_Arguments_Are_Preserved()
	{
		var notification = new AppNotificationBuilder()
			.AddButton(new AppNotificationButton("Open").AddArgument("action", "open"))
			.AddButton(new AppNotificationButton("Website").SetInvokeUri(new Uri("https://example.com")))
			.BuildNotification();

		var command = LinuxAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual(2, command.Actions.Length);
		Assert.AreEqual("uno.appnotifications.action.0", command.Actions[0].Key);
		Assert.AreEqual("action=open", command.Actions[0].Argument);
		Assert.AreEqual("https://example.com/", command.Actions[1].ProtocolUri);
	}

	[TestMethod]
	public void When_Progress_Is_Translated_Value_Is_Clamped_And_Rounded()
	{
		var notification = new AppNotificationBuilder().BuildNotification();
		var envelope = CreateEnvelope(1, notification) with
		{
			Progress = new AppNotificationProgressSnapshot(1, "Download", 0.456, "46%", "Running"),
		};

		var command = LinuxAppNotificationTranslator.Translate(envelope, DateTimeOffset.UtcNow);

		Assert.AreEqual(46, command.ProgressPercentage);
	}

	[TestMethod]
	public void When_No_Expiration_Is_Set_Server_Default_Is_Used()
	{
		var notification = new AppNotificationBuilder().BuildNotification();

		var command = LinuxAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual(-1, command.ExpireTimeoutMilliseconds);
	}

	[TestMethod]
	public void When_Unsupported_Linux_Features_Are_Used_They_Are_Reported()
	{
		var notification = new AppNotificationBuilder()
			.AddTextBox("reply")
			.SetHeroImage(new Uri("file:///tmp/hero.png"))
			.AddButton(new AppNotificationButton("Menu").SetContextMenuPlacement())
			.BuildNotification();
		notification.ExpiresOnReboot = true;

		var command = LinuxAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		CollectionAssert.AreEquivalent(
			new[] { "inputs", "hero images", "context-menu actions", "expires-on-reboot" },
			command.UnsupportedFeatures);
	}

	private static AppNotificationEnvelope CreateEnvelope(uint id, AppNotification notification)
		=> new(
			id,
			AppNotificationPayloadParser.Parse(notification.Payload),
			notification.Tag,
			notification.Group,
			notification.Expiration,
			notification.ExpiresOnReboot,
			notification.SuppressDisplay,
			notification.Priority);
}