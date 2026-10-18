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

		Assert.AreEqual(1, clocks.Count);
		Assert.AreEqual(new Calendar(new[] { CultureInfo.InstalledUICulture.Name }).GetClock(), clocks[0]);
	}
}
