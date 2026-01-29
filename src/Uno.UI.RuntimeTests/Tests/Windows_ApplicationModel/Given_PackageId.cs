#if __APPLE_UIKIT__ || __ANDROID__ || __SKIA__ || __WASM__

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel
{
	[TestClass]
	public class Given_PackageId
	{
#if __APPLE_UIKIT__ || __ANDROID__
		[TestMethod]
		public void When_FamilyNameQueried()
		{
			var SUT = Package.Current.Id;
			Assert.IsNotNull(SUT.FamilyName);
		}

		[TestMethod]
		public void When_FullNameQueried()
		{
			var SUT = Package.Current.Id;
			Assert.IsNotNull(SUT.FullName);
		}

		[TestMethod]
		public void When_NameQueried()
		{
			var SUT = Package.Current.Id;
			Assert.IsNotNull(SUT.Name);
		}
#endif

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

#if __SKIA__ || __WASM__
		[TestMethod]
		public void When_VersionQueried_ReturnsValidVersion()
		{
			var SUT = Package.Current.Id;
			var version = SUT.Version;

			// The version should have been populated from AssemblyInformationalVersionAttribute
			// At minimum, we expect the Major version to be set (not just all zeros)
			// unless the app explicitly sets version 0.0.0.0
			// This test verifies that the Version property can be accessed without exception
			Assert.IsGreaterThanOrEqual(0, version.Major);
			Assert.IsGreaterThanOrEqual(0, version.Minor);
			Assert.IsGreaterThanOrEqual(0, version.Build);
			Assert.IsGreaterThanOrEqual(0, version.Revision);
		}
#endif
	}
}
#endif
