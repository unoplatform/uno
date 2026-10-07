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
		Assert.AreNotEqual("IV", region);
		if (region != "ZZ")
		{
			Assert.AreEqual(region, new RegionInfo(region).TwoLetterISORegionName);
		}
	}

	[TestMethod]
	public void When_HomeGeographicRegion_InvariantCulture_Then_Not_Invariant_Region()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

			// RegionInfo.CurrentRegion is cached until the culture data is cleared.
			CultureInfo.CurrentCulture.ClearCachedData();

			// Windows never reports the invariant "IV" region; an unknown home location is "ZZ".
			Assert.AreNotEqual("IV", GlobalizationPreferences.HomeGeographicRegion);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentCulture.ClearCachedData();
		}
	}
}
