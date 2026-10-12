#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System.Profile;

namespace Uno.UI.Tests.Windows_System.Profile;

[TestClass]
public class Given_AnalyticsVersionInfo
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25259")]
	public void When_Windows_DeviceFamily_Is_Initialized_With_Non_Windows_Platform()
	{
		var deviceFamily = AnalyticsVersionInfo.GetDeviceFamily(
			isWindows: true,
			platform: PlatformID.Unix,
			deviceForm: "Desktop");

		Assert.AreEqual("Windows.Desktop", deviceFamily);
	}
}
