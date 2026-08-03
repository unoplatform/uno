#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AndroidAppNotificationTranslator
{
	[TestMethod]
	public void When_Envelope_Is_Translated_Basic_Fields_Are_Preserved()
	{
		var now = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);
		var notification = new AppNotificationBuilder()
			.AddText("Title")
			.AddText("Body")
			.SetAttributionText("Contoso")
			.SetTimeStamp(now.AddMinutes(-5))
			.MuteAudio()
			.BuildNotification();
		notification.Tag = "tag";
		notification.Group = "group";
		notification.Expiration = now.AddMinutes(10);
		notification.Priority = AppNotificationPriority.High;

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(17, notification), now);

		Assert.AreEqual(17, command.NativeId);
		Assert.AreEqual("uno.appnotifications", command.NativeTag);
		Assert.AreEqual("Title", command.Title);
		Assert.AreEqual("Body", command.Body);
		Assert.AreEqual("Contoso", command.Attribution);
		Assert.AreEqual("group", command.Group);
		Assert.AreEqual(now.AddMinutes(-5).ToUnixTimeMilliseconds(), command.DisplayTimestampMilliseconds);
		Assert.AreEqual(600_000L, command.TimeoutMilliseconds);
		Assert.IsTrue(command.MuteAudio);
		Assert.IsFalse(command.SuppressDisplay);
		Assert.IsTrue(command.HighPriority);
	}

	[TestMethod]
	public void When_Images_Are_Translated_AppLogo_And_Hero_Take_Precedence()
	{
		var notification = new AppNotificationBuilder()
			.SetInlineImage(new Uri("ms-appx:///inline.png"), AppNotificationImageCrop.Default, "inline")
			.SetHeroImage(new Uri("ms-appx:///hero.png"), "hero")
			.SetAppLogoOverride(new Uri("ms-appx:///logo.png"))
			.BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual("ms-appx:///logo.png", command.LargeIconSource);
		Assert.AreEqual("ms-appx:///hero.png", command.BigPictureSource);
		Assert.AreEqual("hero", command.BigPictureAlternateText);
	}

	[TestMethod]
	public void When_Tag_Is_Default_Private_Native_Namespace_Is_Still_Used()
	{
		var notification = new AppNotificationBuilder().BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual("uno.appnotifications", command.NativeTag);
		Assert.IsNull(command.TimeoutMilliseconds);
		Assert.IsFalse(command.HighPriority);
	}

	[TestMethod]
	public void When_Expiration_Is_In_The_Past_Timeout_Is_Immediate()
	{
		var now = DateTimeOffset.UtcNow;
		var notification = new AppNotificationBuilder().BuildNotification();
		notification.Expiration = now.AddMinutes(-1);

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), now);

		Assert.AreEqual(0L, command.TimeoutMilliseconds);
	}

	[TestMethod]
	public void When_Uno_Id_Uses_Unsigned_Range_Bits_Are_Preserved()
	{
		var notification = new AppNotificationBuilder().BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(uint.MaxValue, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual(-1, command.NativeId);
	}

	[TestMethod]
	public void When_Notification_Uses_Unsupported_Android_Features_They_Are_Reported()
	{
		var notification = new AppNotificationBuilder()
			.AddText("Title")
			.AddText("Body")
			.AddText("Additional")
			.AddButton(new AppNotificationButton("Action").AddArgument("action", "open").SetContextMenuPlacement())
			.AddTextBox("reply")
			.AddProgressBar(new AppNotificationProgressBar())
			.BuildNotification();
		notification.ExpiresOnReboot = true;

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		CollectionAssert.AreEquivalent(new[] { "additional text", "context-menu actions", "progress", "expires-on-reboot" }, command.UnsupportedFeatures);
	}

	[TestMethod]
	public void When_SuppressDisplay_Is_Enabled_It_Is_Distinct_From_MuteAudio()
	{
		var notification = new AppNotificationBuilder().BuildNotification();
		notification.SuppressDisplay = true;

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.IsFalse(command.MuteAudio);
		Assert.IsTrue(command.SuppressDisplay);
	}

	[TestMethod]
	public void When_Body_And_Actions_Are_Translated_Activation_Descriptors_Are_Preserved()
	{
		var notification = new AppNotificationBuilder()
			.AddArgument("action", "body")
			.AddTextBox("reply", "Type a reply", "Reply")
			.AddButton(new AppNotificationButton("Send").AddArgument("action", "send").SetInputId("reply"))
			.AddButton(new AppNotificationButton("Open web").SetInvokeUri(new Uri("https://example.com/open")))
			.BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual("action=body", command.BodyActivation.Argument);
		Assert.IsNull(command.BodyActivation.ProtocolUri);
		Assert.AreEqual(2, command.Actions.Length);
		Assert.AreEqual("action=send", command.Actions[0].Argument);
		Assert.AreEqual("reply", command.Actions[0].InputId);
		Assert.AreEqual("Reply", command.Actions[0].InputLabel);
		Assert.AreEqual("https://example.com/open", command.Actions[1].ProtocolUri);
	}

	[TestMethod]
	public void When_More_Than_Three_Actions_Are_Provided_Android_Uses_First_Three()
	{
		var notification = new AppNotificationBuilder()
			.AddButton(new AppNotificationButton("1").AddArgument("action", "1"))
			.AddButton(new AppNotificationButton("2").AddArgument("action", "2"))
			.AddButton(new AppNotificationButton("3").AddArgument("action", "3"))
			.AddButton(new AppNotificationButton("4").AddArgument("action", "4"))
			.BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual(3, command.Actions.Length);
		Assert.AreEqual("action=3", command.Actions[2].Argument);
		CollectionAssert.Contains(command.UnsupportedFeatures, "more than three actions");
	}

	[TestMethod]
	public void When_Action_Content_Is_Empty_Tooltip_Is_Used_As_Android_Label()
	{
		var notification = new AppNotificationBuilder()
			.AddButton(new AppNotificationButton().AddArgument("action", "open").SetToolTip("Open"))
			.BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual("Open", command.Actions[0].Content);
	}

	[TestMethod]
	public void When_Protocol_Action_References_Input_Android_Drops_Input()
	{
		var button = new AppNotificationButton("Open")
		{
			InputId = "reply",
			InvokeUri = new Uri("https://example.com/open"),
		};
		var notification = new AppNotificationBuilder()
			.AddTextBox("reply", "Type a reply", string.Empty)
			.AddButton(button)
			.BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual("https://example.com/open", command.Actions[0].ProtocolUri);
		Assert.IsNull(command.Actions[0].InputId);
		Assert.IsNull(command.Actions[0].InputLabel);
		CollectionAssert.Contains(command.UnsupportedFeatures, "protocol action inputs");
	}

	[TestMethod]
	public void When_Reply_Title_Is_Empty_Placeholder_Is_Used_As_Label()
	{
		var notification = new AppNotificationBuilder()
			.AddTextBox("reply", "Type a reply", string.Empty)
			.AddButton(new AppNotificationButton("Send").AddArgument("action", "send").SetInputId("reply"))
			.BuildNotification();

		var command = AndroidAppNotificationTranslator.Translate(CreateEnvelope(1, notification), DateTimeOffset.UtcNow);

		Assert.AreEqual("Type a reply", command.Actions[0].InputLabel);
	}

	[TestMethod]
	public void When_Progress_Is_Translated_Static_Fields_And_Value_Are_Mapped()
	{
		var notification = new AppNotificationBuilder().BuildNotification();
		var envelope = CreateEnvelope(1, notification) with
		{
			Progress = new AppNotificationProgressSnapshot(2, "Download", 0.456, "46%", "Running"),
		};

		var command = AndroidAppNotificationTranslator.Translate(envelope, DateTimeOffset.UtcNow);

		Assert.AreEqual("Download", command.ProgressTitle);
		Assert.AreEqual("Running", command.ProgressStatus);
		Assert.AreEqual("46%", command.ProgressValueString);
		Assert.AreEqual(456, command.ProgressValue);
	}

	[TestMethod]
	public void When_Progress_Value_Is_Outside_Range_It_Is_Clamped()
	{
		var notification = new AppNotificationBuilder().BuildNotification();
		var envelope = CreateEnvelope(1, notification) with
		{
			Progress = new AppNotificationProgressSnapshot(1, string.Empty, 1.5, string.Empty, string.Empty),
		};

		var command = AndroidAppNotificationTranslator.Translate(envelope, DateTimeOffset.UtcNow);

		Assert.AreEqual(1000, command.ProgressValue);
	}

	[TestMethod]
	public void When_Progress_Value_Is_NotFinite_Native_Progress_Is_Omitted()
	{
		var notification = new AppNotificationBuilder().BuildNotification();
		var envelope = CreateEnvelope(1, notification) with
		{
			Progress = new AppNotificationProgressSnapshot(1, string.Empty, double.NaN, string.Empty, string.Empty),
		};

		var command = AndroidAppNotificationTranslator.Translate(envelope, DateTimeOffset.UtcNow);

		Assert.IsNull(command.ProgressValue);
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
