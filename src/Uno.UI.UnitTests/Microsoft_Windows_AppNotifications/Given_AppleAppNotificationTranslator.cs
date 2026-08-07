#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleAppNotificationTranslator
{
	[TestMethod]
	public void When_Basic_Notification_Is_Translated_Apple_Fields_Are_Preserved()
	{
		var notification = new AppNotificationBuilder()
			.AddArgument("action", "body")
			.AddText("Title")
			.AddText("Body")
			.MuteAudio()
			.BuildNotification();
		notification.Group = "group";
		notification.Priority = AppNotificationPriority.High;

		var command = AppleAppNotificationTranslator.Translate(CreateEnvelope(17, notification));

		Assert.AreEqual((uint)17, command.Id);
		Assert.AreEqual("uno.appnotifications.17", command.RequestIdentifier);
		Assert.AreEqual(string.Empty, command.CategoryIdentifier);
		Assert.AreEqual("Title", command.Title);
		Assert.AreEqual(string.Empty, command.Subtitle);
		Assert.AreEqual("Body", command.Body);
		Assert.AreEqual("group", command.ThreadIdentifier);
		Assert.AreEqual(string.Empty, command.AttachmentSource);
		Assert.AreEqual("action=body", command.LaunchArgument);
		Assert.IsNull(command.ProtocolUri);
		Assert.IsTrue(command.MuteAudio);
		Assert.IsTrue(command.HighPriority);
	}

	[TestMethod]
	public void When_Three_Text_Lines_Are_Translated_Second_Line_Is_Subtitle()
	{
		var notification = new AppNotificationBuilder()
			.AddText("Title")
			.AddText("Subtitle")
			.AddText("Body")
			.BuildNotification();

		var command = AppleAppNotificationTranslator.Translate(CreateEnvelope(1, notification));

		Assert.AreEqual("Subtitle", command.Subtitle);
		Assert.AreEqual("Body", command.Body);
	}

	[TestMethod]
	public void When_Text_Input_Action_Is_Translated_Input_Metadata_Is_Preserved()
	{
		var notification = new AppNotificationBuilder()
			.AddTextBox("reply", "Type a reply", "Reply")
			.AddButton(new AppNotificationButton("Send").AddArgument("action", "send").SetInputId("reply"))
			.BuildNotification();

		var command = AppleAppNotificationTranslator.Translate(CreateEnvelope(5, notification));

		Assert.AreEqual(1, command.Actions.Length);
		StringAssert.StartsWith(command.Actions[0].Identifier, "uno.appnotifications.action.");
		StringAssert.StartsWith(command.CategoryIdentifier, "uno.appnotifications.category.");
		Assert.AreEqual("action=send", command.Actions[0].Argument);
		Assert.AreEqual("reply", command.Actions[0].InputId);
		Assert.AreEqual("Send", command.Actions[0].InputButtonTitle);
		Assert.AreEqual("Type a reply", command.Actions[0].InputPlaceholder);
	}

	[TestMethod]
	public void When_Protocol_Activation_Is_Used_Uris_Are_Preserved()
	{
		var notification = new AppNotification(
			"<toast launch='https://example.com/body' activationType='protocol'>" +
			"<visual><binding template='ToastGeneric'/></visual>" +
			"<actions><action content='Open' arguments='https://example.com/action' activationType='protocol'/></actions>" +
			"</toast>");

		var command = AppleAppNotificationTranslator.Translate(CreateEnvelope(1, notification));

		Assert.AreEqual("https://example.com/body", command.ProtocolUri);
		Assert.AreEqual("https://example.com/action", command.Actions[0].ProtocolUri);
		Assert.IsNull(command.Actions[0].InputId);
	}

	[TestMethod]
	public void When_Unsupported_Apple_Features_Are_Used_They_Are_Reported()
	{
		var notification = new AppNotificationBuilder()
			.SetHeroImage(new Uri("https://example.com/image.png"))
			.SetAppLogoOverride(new Uri("https://example.com/logo.png"))
			.AddComboBox(new AppNotificationComboBox("selection").AddItem("a", "A"))
			.AddProgressBar(new AppNotificationProgressBar())
			.AddButton(new AppNotificationButton("Action").SetContextMenuPlacement())
			.BuildNotification();
		notification.ExpiresOnReboot = true;

		var command = AppleAppNotificationTranslator.Translate(CreateEnvelope(1, notification));

		CollectionAssert.AreEquivalent(
			new[] { "app-logo overrides", "selection inputs", "progress", "context-menu actions", "expires-on-reboot" },
			command.UnsupportedFeatures);
		Assert.AreEqual("https://example.com/image.png", command.AttachmentSource);
	}

	[TestMethod]
	public void When_Action_Contract_Is_The_Same_Category_Is_Reused()
	{
		var first = new AppNotificationBuilder()
			.AddButton(new AppNotificationButton("Open").AddArgument("action", "open"))
			.BuildNotification();
		var second = new AppNotificationBuilder()
			.AddButton(new AppNotificationButton("Open").AddArgument("action", "different"))
			.BuildNotification();

		var firstCommand = AppleAppNotificationTranslator.Translate(CreateEnvelope(1, first));
		var secondCommand = AppleAppNotificationTranslator.Translate(CreateEnvelope(2, second));

		Assert.AreEqual(firstCommand.CategoryIdentifier, secondCommand.CategoryIdentifier);
		Assert.AreEqual(firstCommand.Actions[0].Identifier, secondCommand.Actions[0].Identifier);
	}

	[TestMethod]
	public void When_Text_Input_Presentation_Differs_Category_Is_Not_Reused()
	{
		var first = new AppNotificationBuilder()
			.AddTextBox("reply", "First placeholder", "Reply")
			.AddButton(new AppNotificationButton("Send").SetInputId("reply"))
			.BuildNotification();
		var second = new AppNotificationBuilder()
			.AddTextBox("reply", "Second placeholder", "Reply")
			.AddButton(new AppNotificationButton("Send").SetInputId("reply"))
			.BuildNotification();

		var firstCommand = AppleAppNotificationTranslator.Translate(CreateEnvelope(1, first));
		var secondCommand = AppleAppNotificationTranslator.Translate(CreateEnvelope(2, second));

		Assert.AreNotEqual(firstCommand.CategoryIdentifier, secondCommand.CategoryIdentifier);
	}

	[TestMethod]
	public void When_Scheduled_Toast_Is_Translated_Separate_Native_Namespace_Is_Used()
	{
		const string payload = "<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>";

		var command = AppleAppNotificationTranslator.TranslateScheduled("schedule", payload, "tag", "group", false);

		Assert.AreEqual("uno.toastschedules.schedule", command.RequestIdentifier);
		Assert.IsTrue(AppleAppNotificationTranslator.TryGetScheduleIdentifier(command.RequestIdentifier, out var identifier));
		Assert.AreEqual("schedule", identifier);
		Assert.IsFalse(AppleAppNotificationTranslator.TryGetNotificationId(command.RequestIdentifier, out _));
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