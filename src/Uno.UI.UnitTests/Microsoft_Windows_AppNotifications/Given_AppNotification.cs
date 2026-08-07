#nullable enable

using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotification
{
	[TestMethod]
	public void When_Created_Defaults_Match_Windows_App_Sdk()
	{
		const string payload = "<toast><visual><binding template='ToastGeneric'/></visual></toast>";
		var notification = new AppNotification(payload);

		Assert.AreEqual(payload, notification.Payload);
		Assert.AreEqual(string.Empty, notification.Tag);
		Assert.AreEqual(string.Empty, notification.Group);
		Assert.AreEqual(0u, notification.Id);
		Assert.IsNull(notification.Progress);
		Assert.AreEqual(DateTimeOffset.FromFileTime(0).ToLocalTime(), notification.Expiration);
		Assert.IsFalse(notification.ExpiresOnReboot);
		Assert.AreEqual(AppNotificationPriority.Default, notification.Priority);
		Assert.IsFalse(notification.SuppressDisplay);
	}

	[TestMethod]
	public void When_Payload_Is_Not_Xml_It_Throws()
	{
		Assert.ThrowsExactly<XmlException>(() => new AppNotification("not xml"));
	}

	[TestMethod]
	public void When_Payload_Is_Null_It_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new AppNotification(null!));
	}

	[TestMethod]
	public void When_Payload_Contains_A_Dtd_It_Throws()
	{
		const string payload = "<!DOCTYPE toast [<!ENTITY content 'expanded'>]><toast>&content;</toast>";

		Assert.ThrowsExactly<XmlException>(() => new AppNotification(payload));
	}

	[TestMethod]
	public void When_Payload_Exceeds_Size_Limit_It_Throws()
	{
		var payload = $"<toast>{new string('A', 5121)}</toast>";

		Assert.ThrowsExactly<FormatException>(() => new AppNotification(payload));
	}

	[TestMethod]
	public void When_Properties_Are_Changed_Values_Round_Trip()
	{
		var notification = new AppNotification("<toast/>");
		var progress = new AppNotificationProgressData(1);
		var expiration = DateTimeOffset.UtcNow.AddMinutes(5);

		notification.Tag = "tag";
		notification.Group = "group";
		notification.Progress = progress;
		notification.Expiration = expiration;
		notification.ExpiresOnReboot = true;
		notification.Priority = AppNotificationPriority.High;
		notification.SuppressDisplay = true;

		Assert.AreEqual("tag", notification.Tag);
		Assert.AreEqual("group", notification.Group);
		Assert.AreSame(progress, notification.Progress);
		Assert.AreEqual(expiration.ToLocalTime(), notification.Expiration);
		Assert.IsTrue(notification.ExpiresOnReboot);
		Assert.AreEqual(AppNotificationPriority.High, notification.Priority);
		Assert.IsTrue(notification.SuppressDisplay);
	}
}
