using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests
{
	[TestClass]
	public class Given_xBind_Binding_Poco
	{
		[TestMethod]
		public void When_Basic_PocoBinding()
		{
			var SUT = new Binding_OneWay_PocoObject();

			Assert.AreEqual(3, SUT.myControl.ClassCollection.Count);
			Assert.AreEqual(null, SUT.myControl.ClassCollection[0].SampleString);
			Assert.AreEqual(null, SUT.myControl.ClassCollection[1].SampleString);
			Assert.AreEqual("Test03", SUT.myControl.ClassCollection[2].SampleString);

			SUT.ForceLoaded();

			Assert.AreEqual(3, SUT.myControl.ClassCollection.Count);
			Assert.AreEqual("Test01", SUT.myControl.ClassCollection[0].SampleString);
			Assert.AreEqual("Test02", SUT.myControl.ClassCollection[1].SampleString);
			Assert.AreEqual("Test03", SUT.myControl.ClassCollection[2].SampleString);

			SUT.MyDependencyProperty = "Test42";

			Assert.AreEqual(3, SUT.myControl.ClassCollection.Count);
			Assert.AreEqual("Test01", SUT.myControl.ClassCollection[0].SampleString);
			Assert.AreEqual("Test42", SUT.myControl.ClassCollection[1].SampleString);
			Assert.AreEqual("Test03", SUT.myControl.ClassCollection[2].SampleString);
		}

		[TestMethod]
		public void When_Static_PocoBinding()
		{
			var SUT = new Binding_OneTime_PocoObject_Static();

			Assert.AreEqual(3, SUT.myControl.ClassCollection.Count);
			Assert.AreEqual(null, SUT.myControl.ClassCollection[0].SampleString);
			Assert.AreEqual(null, SUT.myControl.ClassCollection[1].SampleString);
			Assert.AreEqual("Test03", SUT.myControl.ClassCollection[2].SampleString);

			SUT.ForceLoaded();

			Assert.AreEqual(3, SUT.myControl.ClassCollection.Count);
			Assert.AreEqual("Test01", SUT.myControl.ClassCollection[0].SampleString);
			Assert.AreEqual("Test02", SUT.myControl.ClassCollection[1].SampleString);
			Assert.AreEqual("Test03", SUT.myControl.ClassCollection[2].SampleString);
		}
	}
}
