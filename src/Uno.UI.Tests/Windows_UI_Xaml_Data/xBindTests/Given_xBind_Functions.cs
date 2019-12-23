using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests
{
	[TestClass]
	public class Given_xBind_Functions
	{
		[TestMethod]
		public void When_Initial_Value()
		{
			var SUT = new Given_xBind_Functions_Control();

			Assert.IsNull(SUT._InstanceProperty.Text);
			Assert.IsNull(SUT._StaticProperty.Text);
			Assert.IsNull(SUT._StaticPrivateProperty.Text);
			Assert.IsNull(SUT._StaticPrivateReadonlyField.Text);
			Assert.IsNull(SUT._InstanceDP.Text);
			Assert.IsNull(SUT._InnerProperty.Text);
			Assert.IsNull(SUT._StaticClass_PublicStaticProperty.Text);
			Assert.IsNull(SUT._InstanceFunction_OneParam.Text);
			Assert.IsNull(SUT._InstanceFunction_OneParam_DP_Update.Text);
			Assert.IsNull(SUT._InstanceFunction_TwoParam.Text);
			Assert.IsNull(SUT._InstanceFunction_TwoParam_WithConstant.Text);
			Assert.IsNull(SUT._System_Function.Text);
			Assert.IsNull(SUT._System_Function_with_Quote.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("42", SUT._InstanceProperty.Text);
			Assert.AreEqual("43", SUT._StaticProperty.Text);
			Assert.AreEqual("44", SUT._StaticPrivateProperty.Text);
			Assert.AreEqual("45", SUT._StaticPrivateReadonlyField.Text);
			Assert.AreEqual("-1", SUT._InstanceDP.Text);
			Assert.AreEqual("Initial", SUT._InnerProperty.Text);
			Assert.AreEqual("46", SUT._StaticClass_PublicStaticProperty.Text);
			Assert.AreEqual("52", SUT._InstanceFunction_OneParam.Text);
			Assert.AreEqual("9", SUT._InstanceFunction_OneParam_DP_Update.Text);
			Assert.AreEqual("85", SUT._InstanceFunction_TwoParam.Text);
			Assert.AreEqual("84.42", SUT._InstanceFunction_TwoParam_WithConstant.Text);
			Assert.AreEqual("42, 43", SUT._System_Function.Text);
			Assert.AreEqual("42, \'43\'", SUT._System_Function_with_Quote.Text);
		}

		[TestMethod]
		public void When_Update_DP()
		{
			var SUT = new Given_xBind_Functions_Control();

			Assert.IsNull(SUT._InstanceDP.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("-1", SUT._InstanceDP.Text);

			SUT.InstanceDP = 4242;

			Assert.AreEqual("4242", SUT._InstanceDP.Text);
		}

		[TestMethod]
		public void When_Update_InnerProperty()
		{
			var SUT = new Given_xBind_Functions_Control();

			Assert.IsNull(SUT._InnerProperty.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("Initial", SUT._InnerProperty.Text);

			SUT.MyxBindClassInstance.MyProperty = "Updated value";

			Assert.AreEqual("Updated value", SUT._InnerProperty.Text);
		}

		[TestMethod]
		public void When_Function_OneParam_Update_DP()
		{
			var SUT = new Given_xBind_Functions_Control();

			Assert.IsNull(SUT._InstanceFunction_OneParam_DP_Update.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("9", SUT._InstanceFunction_OneParam_DP_Update.Text);

			SUT.InstanceDP = 42;

			Assert.AreEqual("52", SUT._InstanceFunction_OneParam_DP_Update.Text);
		}
	}
}
