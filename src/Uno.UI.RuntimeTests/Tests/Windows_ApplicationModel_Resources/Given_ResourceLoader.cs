using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Resources;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_ResourceLoader
	{
		[TestMethod]
		public void When_SimpleString()
		{
			var SUT = ResourceLoader.GetForCurrentView();
			Assert.AreEqual("My Simple String (en-US)", SUT.GetString("Given_ResourceLoader.When_SimpleString"));
		}
	}
}
