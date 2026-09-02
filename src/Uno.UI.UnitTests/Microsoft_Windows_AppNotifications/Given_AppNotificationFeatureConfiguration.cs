#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationFeatureConfiguration
{
	[TestMethod]
	public void When_ServiceWorker_Mode_Is_Selected_Then_Configuration_Is_Updated()
	{
		var previous = WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly;
		try
		{
			Assert.IsFalse(previous);

			WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly = true;

			Assert.IsTrue(WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly);
		}
		finally
		{
			WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly = previous;
		}
	}
}