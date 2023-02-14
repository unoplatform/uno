using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

		[TestMethod]
		public void When_Get_ResourceFromResw()
		{
			var sut = ResourceHelper.ResourcesService;
			var value = sut.Get("Given_ResourcesService.When_Get_ResourceFromResw");

			Assert.AreEqual($"Value for {nameof(When_Get_ResourceFromResw)} (en)", value);
		}
	}
#endif
}
