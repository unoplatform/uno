#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleAppNotificationSettingEvaluator
{
	[DataTestMethod]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.NotDetermined, AppNotificationSetting.DisabledForApplication)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Denied, AppNotificationSetting.DisabledForUser)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Authorized, AppNotificationSetting.Enabled)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Provisional, AppNotificationSetting.Enabled)]
	[DataRow((int)AppleAppNotificationAuthorizationStatus.Ephemeral, AppNotificationSetting.Enabled)]
	public void When_Authorization_Is_Evaluated_Setting_Is_Mapped(
		int status,
		AppNotificationSetting expected)
		=> Assert.AreEqual(expected, AppleAppNotificationSettingEvaluator.Evaluate((AppleAppNotificationAuthorizationStatus)status));
}