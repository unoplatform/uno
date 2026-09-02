#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AndroidAppNotificationSettingEvaluator
{
	[TestMethod]
	public void When_Api_Level_Is_Below_23_Backend_Is_Unsupported()
	{
		Assert.IsFalse(AndroidAppNotificationSettingEvaluator.IsSupported(22));
		Assert.IsTrue(AndroidAppNotificationSettingEvaluator.IsSupported(23));
	}

	[TestMethod]
	public void When_Runtime_Permission_Is_Not_Declared_Setting_Is_DisabledByManifest()
	{
		var setting = AndroidAppNotificationSettingEvaluator.Evaluate(true, false, false, false);

		Assert.AreEqual(AppNotificationSetting.DisabledByManifest, setting);
	}

	[TestMethod]
	public void When_Runtime_Permission_Is_Denied_Setting_Is_DisabledForApplication()
	{
		var setting = AndroidAppNotificationSettingEvaluator.Evaluate(true, true, false, true);

		Assert.AreEqual(AppNotificationSetting.DisabledForApplication, setting);
	}

	[TestMethod]
	public void When_Notifications_Are_Disabled_Setting_Is_DisabledForApplication()
	{
		var setting = AndroidAppNotificationSettingEvaluator.Evaluate(false, false, false, false);

		Assert.AreEqual(AppNotificationSetting.DisabledForApplication, setting);
	}

	[TestMethod]
	public void When_Permission_And_Notifications_Are_Enabled_Setting_Is_Enabled()
	{
		var setting = AndroidAppNotificationSettingEvaluator.Evaluate(true, true, true, true);

		Assert.AreEqual(AppNotificationSetting.Enabled, setting);
	}
}
