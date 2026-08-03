#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationManager
{
	[TestMethod]
	public void When_Backend_Is_Not_Available_State_Is_Unsupported()
	{
		Assert.AreSame(AppNotificationManager.Default, AppNotificationManager.Default);
		Assert.IsFalse(AppNotificationManager.IsSupported());
		Assert.AreEqual(AppNotificationSetting.Unsupported, AppNotificationManager.Default.Setting);
	}
}
