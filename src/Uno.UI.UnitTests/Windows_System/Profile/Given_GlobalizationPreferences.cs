#nullable enable

using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System.UserProfile;

namespace Uno.UI.Tests.Windows_System.Profile;

[TestClass]
public class Given_GlobalizationPreferences
{
	[TestMethod]
	public void When_HomeGeographicRegion_Then_Is_Valid_Region()
	{
		// Windows derives it from the user's home location (GeoId), not the current culture.
		var region = GlobalizationPreferences.HomeGeographicRegion;

		Assert.IsFalse(string.IsNullOrEmpty(region));
		Assert.AreEqual(region, new RegionInfo(region).TwoLetterISORegionName);
	}
}
