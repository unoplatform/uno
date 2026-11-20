#nullable enable

using Windows.Globalization;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_DeviceClock
{
#if HAS_UNO
	// The platform's own patterns, which CurrentCulture takes on a device whose 12/24-hour setting isn't its locale's
	// clock: en-US's for each clock, with the hour of that clock.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMobile)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow(true)]
	[DataRow(false)]
	public void When_Platform_Gives_Time_Patterns_Then_They_Have_The_Clock(bool is24Hour)
	{
		var device = DeviceClock.TestDevice;
		try
		{
			DeviceClock.TestDevice = null;
			string? shortPattern = null;
			string? longPattern = null;

			DeviceClock.ReadTimePatterns("en-US", is24Hour, ref shortPattern, ref longPattern);

			DeviceClock.IsTwentyFourHourPattern(shortPattern).Should().Be(is24Hour, shortPattern);
			DeviceClock.IsTwentyFourHourPattern(longPattern).Should().Be(is24Hour, longPattern);
			longPattern.Should().Contain("ss");
			DeviceClock.ToNetTimePattern(shortPattern, ":", "/").Should().NotBeNull();
			DeviceClock.ToNetTimePattern(longPattern, ":", "/").Should().NotBeNull();
		}
		finally
		{
			DeviceClock.TestDevice = device;
		}
	}
#endif
}
