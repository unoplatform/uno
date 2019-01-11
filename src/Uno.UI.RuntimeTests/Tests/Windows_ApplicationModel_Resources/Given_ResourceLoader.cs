using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_ResourceLoader
	{
		[TestMethod]
		public void When_SimpleString()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual("SamplesApp", SUT.GetString("ApplicationName"));
			Assert.AreEqual("My Simple String (en-US)", SUT.GetString("Given_ResourceLoader/When_SimpleString"));
		}

		[TestMethod]
		public void When_UnnamedLoader()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual(@"This is en-US\Resources.resw", SUT.GetString("Given_ResourceLoader/When_NamedLoader"));
		}

		[TestMethod]
		public void When_NamedLoader_Resources()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Resources");
			Assert.AreEqual(@"This is en-US\Resources.resw", SUT.GetString("Given_ResourceLoader/When_NamedLoader"));
		}

		[TestMethod]
		public void When_NamedLoader_Test01()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Test01");
			Assert.AreEqual(@"This is en-US\Test01.resw", SUT.GetString("Given_ResourceLoader/When_NamedLoader"));
		}

		[TestMethod]
		public void When_UnnamedLoader_Test01_Only()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual("", SUT.GetString("Given_ResourceLoader/When_NamedLoader_Test01_Only"));
		}

		[TestMethod]
		public void When_NamedLoader_Test01_Only()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Test01");
			Assert.AreEqual(@"This is en-US\Test01.resw only", SUT.GetString("Given_ResourceLoader/When_NamedLoader_Test01_Only"));
		}

		[TestMethod]
		public void When_UnnamedLoader_UnknownResource()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse();
			Assert.AreEqual(@"", SUT.GetString("Given_ResourceLoader/INVALID_RESOURCE_NAME"));
		}

		[TestMethod]
		public void When_NnamedLoader_UnknownResource()
		{
			var SUT = ResourceLoader.GetForViewIndependentUse("Test01");
			Assert.AreEqual(@"", SUT.GetString("Given_ResourceLoader/INVALID_RESOURCE_NAME"));
		}
	}
}
