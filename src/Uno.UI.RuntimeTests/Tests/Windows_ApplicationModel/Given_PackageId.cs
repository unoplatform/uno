#if __IOS__ || __ANDROID__

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel
{
	[TestClass]
	public class Given_PackageId
	{
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
	}
}
#endif
