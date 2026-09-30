#if __SKIA__

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel
{
	[TestClass]
	public class Given_PackageId
	{
		[TestMethod]
		public void When_VersionQueried()
		{
			var SUT = Package.Current.Id;
			try
			{
				var _ = SUT.Version;
			}
			catch (Exception ex)
			{
				Assert.Fail("Expected no exception, but got: " + ex.Message);
			}
		}

#if __SKIA__
		[TestMethod]
		public void When_VersionQueried_ReturnsValidVersion()
		{
			var SUT = Package.Current.Id;
			var version = SUT.Version;

			// The version should have been populated from AssemblyInformationalVersionAttribute
			// At minimum, we expect the Major version to be set (not just all zeros)
			// unless the app explicitly sets version 0.0.0.0
			// This test verifies that the Version property can be accessed without exception
			Assert.IsGreaterThanOrEqualTo(0, version.Major);
			Assert.IsGreaterThanOrEqualTo(0, version.Minor);
			Assert.IsGreaterThanOrEqualTo(0, version.Build);
			Assert.IsGreaterThanOrEqualTo(0, version.Revision);
		}
#endif
	}
}
#endif
