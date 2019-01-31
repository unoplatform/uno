using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Services
{
#if __IOS__ || __ANDROID__
	[TestClass]
	public class Given_ResourceService
	{
		[TestMethod]
		public void When_Valid_NativeResource()
		{
			Assert.AreEqual("SamplesApp", ResourceHelper.ResourcesService.Get("ApplicationName"));
			Assert.AreEqual("ValidResource (en)", ResourceHelper.ResourcesService.Get("Given_ResourceService.ValidResource"));
		}
	}
#endif
}
