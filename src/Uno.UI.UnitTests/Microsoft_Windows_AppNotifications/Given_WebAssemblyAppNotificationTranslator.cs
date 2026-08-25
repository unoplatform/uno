#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_WebAssemblyAppNotificationTranslator
{
	[TestMethod]
	public void When_Basic_Notification_Is_Translated_Browser_Fields_Are_Preserved()
	{
		var timestamp = new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);
		var notification = new AppNotificationBuilder()
			.AddText("Title", new AppNotificationTextProperties().SetLanguage("ar-SA"))
			.AddText("Body")
			.SetTimeStamp(timestamp)
			.SetAppLogoOverride(new Uri("https://example.com/icon.png"))
			.SetHeroImage(new Uri("https://example.com/image.png"))
			.MuteAudio()
			.BuildNotification();

		var command = WebAssemblyAppNotificationTranslator.Translate(CreateEnvelope(17, notification), supportsActions: false);

		Assert.AreEqual((uint)17, command.Id);
		Assert.AreEqual("uno.appnotifications.17", command.NativeTag);
		Assert.AreEqual("Title", command.Title);
		Assert.AreEqual("Body", command.Body);
		Assert.AreEqual("ar-SA", command.Language);
		Assert.AreEqual("rtl", command.Direction);
		Assert.AreEqual("https://example.com/icon.png", command.Icon);
		Assert.AreEqual("https://example.com/image.png", command.Image);
		Assert.AreEqual(timestamp.ToUnixTimeMilliseconds(), command.Timestamp);
		Assert.IsNull(command.ExpirationTimestamp);
		Assert.IsTrue(command.Silent);
	}

	[TestMethod]
	public void When_Actions_Are_Translated_Stable_Identifiers_And_Arguments_Are_Preserved()
	{
		var notification = new AppNotificationBuilder()
			.AddArgument("action", "body")
			.AddButton(new AppNotificationButton("Open").AddArgument("action", "open"))
			.AddButton(new AppNotificationButton().SetToolTip("Dismiss").AddArgument("action", "dismiss"))
			.BuildNotification();

		var command = WebAssemblyAppNotificationTranslator.Translate(CreateEnvelope(1, notification), supportsActions: false);

		Assert.AreEqual("action=body", command.LaunchArgument);
		Assert.AreEqual(2, command.Actions.Length);
		Assert.AreEqual("action-0", command.Actions[0].Id);
		Assert.AreEqual("Open", command.Actions[0].Title);
		Assert.AreEqual("action=open", command.Actions[0].Argument);
		Assert.IsNull(command.Actions[0].ProtocolUri);
		Assert.AreEqual("Dismiss", command.Actions[1].Title);
		Assert.AreEqual("action=dismiss", command.Actions[1].Argument);
		CollectionAssert.Contains(command.UnsupportedFeatures, "actions");
	}

	[TestMethod]
	public void When_Service_Worker_Renders_Actions_They_Are_Not_Reported_As_Unsupported()
	{
		var notification = new AppNotificationBuilder()
			.AddButton(new AppNotificationButton("Open").AddArgument("action", "open"))
			.AddButton(new AppNotificationButton("Menu").AddArgument("action", "menu").SetContextMenuPlacement())
			.AddTextBox("reply")
			.BuildNotification();

		var command = WebAssemblyAppNotificationTranslator.Translate(CreateEnvelope(1, notification), supportsActions: true);

		Assert.AreEqual(1, command.Actions.Length);
		Assert.AreEqual("Open", command.Actions[0].Title);
		CollectionAssert.DoesNotContain(command.UnsupportedFeatures, "actions");
		CollectionAssert.Contains(command.UnsupportedFeatures, "context-menu actions");
		CollectionAssert.Contains(command.UnsupportedFeatures, "inputs");
	}

	[TestMethod]
	public void When_Unsupported_Browser_Features_Are_Used_They_Are_Reported()
	{
		var notification = new AppNotificationBuilder()
			.AddText("Title")
			.AddText("Body")
			.AddText("Additional")
			.SetAttributionText("Attribution")
			.AddTextBox("reply")
			.AddButton(new AppNotificationButton("Action").SetContextMenuPlacement())
			.AddProgressBar(new AppNotificationProgressBar())
			.BuildNotification();
		notification.ExpiresOnReboot = true;

		var command = WebAssemblyAppNotificationTranslator.Translate(CreateEnvelope(1, notification), supportsActions: false);

		CollectionAssert.AreEquivalent(
			new[] { "additional text", "attribution text", "inputs", "progress", "context-menu actions", "expires-on-reboot" },
			command.UnsupportedFeatures);
	}

	[TestMethod]
	public void When_High_Priority_Is_Used_Browser_Interaction_Is_Requested()
	{
		var notification = new AppNotificationBuilder().BuildNotification();
		notification.Priority = AppNotificationPriority.High;

		var command = WebAssemblyAppNotificationTranslator.Translate(CreateEnvelope(1, notification), supportsActions: false);

		Assert.IsTrue(command.RequireInteraction);
		Assert.AreEqual("auto", command.Direction);
	}

	[TestMethod]
	public void When_Protocol_Activation_Is_Used_Uri_Is_Preserved()
	{
		var notification = new AppNotification(
			"<toast launch='https://example.com/body' activationType='protocol'>" +
			"<visual><binding template='ToastGeneric'/></visual>" +
			"<actions><action content='Open' arguments='https://example.com/action' activationType='protocol'/></actions>" +
			"</toast>");

		var command = WebAssemblyAppNotificationTranslator.Translate(CreateEnvelope(1, notification), supportsActions: false);

		Assert.AreEqual("https://example.com/body", command.ProtocolUri);
		Assert.AreEqual("https://example.com/action", command.Actions[0].ProtocolUri);
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