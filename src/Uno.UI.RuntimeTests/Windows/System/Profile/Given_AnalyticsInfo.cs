#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
using System;
using System.Text.RegularExpressions;
using Windows.System.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_System.Profile
{
	[TestClass]
	public class Given_AnalyticsInfo
	{
		[TestMethod]
		public void When_DeviceForm_Is_Queried()
		{
			try
			{
				var _ = AnalyticsInfo.DeviceForm;
			}
			catch (Exception ex)
			{
				Assert.Fail("AnalyticsInfo.DeviceForm threw an exception: " + ex.Message);
			}
		}

		[TestMethod]
		public void When_DeviceFamily_Has_Format()
		{
			var deviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;
			var split = deviceFamily.Split('.');
			Assert.HasCount(2, split);
		}

		[TestMethod]
		public void When_DeviceFamily_Contains_DeviceForm()
		{
			var deviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;
			Assert.IsTrue(deviceFamily.EndsWith(
				AnalyticsInfo.DeviceForm,
				StringComparison.OrdinalIgnoreCase));
		}

		[TestMethod]
		public void When_DeviceFamilyVersion_Is_Queried()
		{
			try
			{
				var _ = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
			}
			catch (Exception ex)
			{
				Assert.Fail("AnalyticsInfo.VersionInfo.DeviceFamilyVersion threw an exception: " + ex.Message);
			}
		}
	}
}
#endif
