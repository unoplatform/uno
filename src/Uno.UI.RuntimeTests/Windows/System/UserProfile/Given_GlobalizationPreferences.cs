using System;
using CultureInfo = System.Globalization.CultureInfo;
using Windows.Globalization;
using Windows.System.UserProfile;

namespace Uno.UI.RuntimeTests.Tests.Windows_System_UserProfile;

[TestClass]
public class Given_GlobalizationPreferences
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	public void When_Clocks_Then_Clock_Of_Regional_Format()
	{
		// As in WinRT, the user's clock is the one a calendar in the regional format's own language takes, which
		// carries the user's 12/24-hour setting.
		var clocks = GlobalizationPreferences.Clocks;
		var culture = CultureInfo.InstalledUICulture;

		Assert.AreEqual(1, clocks.Count);
#if HAS_UNO
		// On Android, iOS and macOS, the setting comes from the device and reaches only a calendar in the device's
		// locale, which the runtime's culture may not be (LANG on macOS, an activity in another language on Android).
		if ((OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacOS())
			&& global::Windows.Globalization.DeviceClock.GetClock(culture) is null)
		{
			Assert.Inconclusive($"The runtime's culture, {culture.Name}, isn't the device's locale.");
		}
#endif
		Assert.AreEqual(new Calendar(new[] { culture.Name }).GetClock(), clocks[0]);
	}
}
