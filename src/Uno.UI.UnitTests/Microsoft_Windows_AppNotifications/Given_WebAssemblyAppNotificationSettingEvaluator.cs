#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_WebAssemblyAppNotificationSettingEvaluator
{
	[TestMethod]
	public void When_Context_Is_Insecure_Notifications_Are_Unsupported()
	{
		Assert.IsFalse(WebAssemblyAppNotificationSettingEvaluator.IsSupported(false, true));
		Assert.AreEqual(AppNotificationSetting.Unsupported, WebAssemblyAppNotificationSettingEvaluator.Evaluate(false, "granted"));
	}

	[TestMethod]
	public void When_Api_Is_Missing_Notifications_Are_Unsupported()
		=> Assert.IsFalse(WebAssemblyAppNotificationSettingEvaluator.IsSupported(true, false));

	[TestMethod]
	[DataRow("granted", AppNotificationSetting.Enabled)]
	[DataRow("denied", AppNotificationSetting.DisabledForApplication)]
	[DataRow("default", AppNotificationSetting.DisabledForApplication)]
	[DataRow("unknown", AppNotificationSetting.DisabledForApplication)]
	public void When_Permission_Is_Evaluated_Setting_Is_Mapped(string permission, AppNotificationSetting expected)
		=> Assert.AreEqual(expected, WebAssemblyAppNotificationSettingEvaluator.Evaluate(true, permission));
}