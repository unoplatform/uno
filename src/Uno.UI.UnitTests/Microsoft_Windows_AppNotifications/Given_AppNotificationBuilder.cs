#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Builder;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationBuilder
{
	private static readonly Uri SampleUri = new("http://www.microsoft.com/");

	[TestMethod]
	public void When_Default_Builder_Is_Used_Payload_Matches_Windows_App_Sdk()
	{
		var notification = new AppNotificationBuilder().BuildNotification();

		Assert.AreEqual("<toast><visual><binding template='ToastGeneric'></binding></visual></toast>", notification.Payload);
	}

	[TestMethod]
	public void When_Arguments_And_Text_Are_Added_Payload_Matches_Windows_App_Sdk()
	{
		var notification = new AppNotificationBuilder()
			.AddArgument("key1", "value1")
			.AddArgument("key2", string.Empty)
			.AddText("content", new AppNotificationTextProperties().SetLanguage("en-US").SetMaxLines(2).SetIncomingCallAlignment())
			.SetTag("tag")
			.SetGroup("group")
			.BuildNotification();

		Assert.AreEqual("<toast scenario='incomingCall' launch='key1=value1;key2'><visual><binding template='ToastGeneric'><text lang='en-US' hint-maxLines='2' hint-callScenarioCenterAlign='true'>content</text></binding></visual></toast>", notification.Payload);
		Assert.AreEqual("tag", notification.Tag);
		Assert.AreEqual("group", notification.Group);
	}

	[TestMethod]
	public void When_Arguments_Are_Added_Fluent_Map_Uses_WinRt_Key_Order()
	{
		var notification = new AppNotificationBuilder()
			.AddArgument("z", "last")
			.AddArgument("a", "first")
			.BuildNotification();

		Assert.AreEqual("<toast launch='a=first;z=last'><visual><binding template='ToastGeneric'></binding></visual></toast>", notification.Payload);
	}

	[TestMethod]
	public void When_Images_Are_Added_Order_Matches_Windows_App_Sdk()
	{
		var notification = new AppNotificationBuilder()
			.SetAppLogoOverride(SampleUri, AppNotificationImageCrop.Circle, "logo")
			.SetHeroImage(SampleUri, "hero")
			.SetInlineImage(SampleUri, AppNotificationImageCrop.Circle, "inline")
			.BuildNotification();

		Assert.AreEqual("<toast><visual><binding template='ToastGeneric'><image src='http://www.microsoft.com/' alt='inline' hint-crop='circle'/><image placement='hero' src='http://www.microsoft.com/' alt='hero'/><image placement='appLogoOverride' src='http://www.microsoft.com/' alt='logo' hint-crop='circle'/></binding></visual></toast>", notification.Payload);
	}

	[TestMethod]
	public void When_Audio_And_Actions_Are_Added_Payload_Matches_Windows_App_Sdk()
	{
		var notification = new AppNotificationBuilder()
			.SetDuration(AppNotificationDuration.Long)
			.SetAudioEvent(AppNotificationSoundEvent.Reminder, AppNotificationAudioLooping.Loop)
			.AddTextBox("input", "Reply", "Title")
			.AddComboBox(new AppNotificationComboBox("choice").AddItem("yes", "Yes"))
			.AddButton(new AppNotificationButton("Send").AddArgument("action", "reply").SetInputId("input").SetButtonStyle(AppNotificationButtonStyle.Success))
			.BuildNotification();

		Assert.AreEqual("<toast duration='long' useButtonStyle='true'><visual><binding template='ToastGeneric'></binding></visual><audio src='ms-winsoundevent:Notification.Reminder' loop='true'/><actions><input id='input' type='text' placeHolderContent='Reply' title='Title'/><input id='choice' type='selection'><selection id='yes' content='Yes'/></input><action content='Send' arguments='action=reply' hint-inputId='input' hint-buttonStyle='Success'/></actions></toast>", notification.Payload);
	}

	[TestMethod]
	public void When_TimeStamp_Is_Set_Payload_Uses_Local_Offset()
	{
		var value = new DateTimeOffset(2026, 8, 3, 10, 20, 30, TimeSpan.Zero);
		var expectedTimestamp = value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);

		var notification = new AppNotificationBuilder().SetTimeStamp(value).BuildNotification();

		Assert.AreEqual($"<toast displayTimestamp='{expectedTimestamp}'><visual><binding template='ToastGeneric'></binding></visual></toast>", notification.Payload);
	}

	[TestMethod]
	public void When_All_Root_Attributes_Are_Set_Order_Matches_Windows_App_Sdk()
	{
		var value = new DateTimeOffset(2026, 8, 3, 10, 20, 30, TimeSpan.Zero);
		var expectedTimestamp = value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);

		var notification = new AppNotificationBuilder()
			.SetTimeStamp(value)
			.SetDuration(AppNotificationDuration.Long)
			.SetScenario(AppNotificationScenario.Alarm)
			.AddArgument("action", "open")
			.AddButton(new AppNotificationButton("Dismiss").SetButtonStyle(AppNotificationButtonStyle.Critical).AddArgument("action", "dismiss"))
			.BuildNotification();

		Assert.AreEqual($"<toast displayTimestamp='{expectedTimestamp}' duration='long' scenario='alarm' launch='action=open' useButtonStyle='true'><visual><binding template='ToastGeneric'></binding></visual><actions><action content='Dismiss' arguments='action=dismiss' hint-buttonStyle='Critical'/></actions></toast>", notification.Payload);
	}

	[TestMethod]
	public void When_Arguments_Contain_Reserved_Characters_They_Are_Encoded_Once()
	{
		var notification = new AppNotificationBuilder()
			.AddArgument("&;\"'=%<>", string.Empty)
			.AddArgument("&\"'<>", ";=%")
			.AddButton(new AppNotificationButton("Open").AddArgument("k%=;", "v%=;"))
			.BuildNotification();

		Assert.AreEqual("<toast launch='&amp;%3B&quot;&apos;%3D%25&lt;&gt;;&amp;&quot;&apos;&lt;&gt;=%3B%3D%25'><visual><binding template='ToastGeneric'></binding></visual><actions><action content='Open' arguments='k%25%3D%3B=v%25%3D%3B'/></actions></toast>", notification.Payload);
	}

	[TestMethod]
	public void When_Limits_Are_Exceeded_Builder_Throws()
	{
		var textBuilder = new AppNotificationBuilder().AddText("1").AddText("2").AddText("3");
		Assert.ThrowsExactly<ArgumentException>(() => textBuilder.AddText("4"));

		var inputBuilder = new AppNotificationBuilder();
		for (var index = 0; index < 5; index++)
		{
			inputBuilder.AddTextBox(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}
		Assert.ThrowsExactly<ArgumentException>(() => inputBuilder.AddTextBox("6"));

		var buttonBuilder = new AppNotificationBuilder();
		for (var index = 0; index < 5; index++)
		{
			buttonBuilder.AddButton(new AppNotificationButton());
		}
		Assert.ThrowsExactly<ArgumentException>(() => buttonBuilder.AddButton(new AppNotificationButton()));
	}

	[TestMethod]
	public void When_Payload_Is_Too_Large_Builder_Throws_EFail()
	{
		var builder = new AppNotificationBuilder().AddText(new string('A', 5120));

		var exception = Assert.ThrowsExactly<COMException>(() => builder.BuildNotification());

		Assert.AreEqual(unchecked((int)0x80004005), exception.HResult);
	}
}
