using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests
{
	[TestClass]
	public class Given_xBind_Functions
	{
		[TestMethod]
		public void When_Initial_Value()
		{
			var SUT = new Functions_Control();

			Assert.AreEqual(string.Empty, SUT._InstanceProperty.Text);
			Assert.AreEqual(string.Empty, SUT._StaticProperty.Text);
			Assert.AreEqual(string.Empty, SUT._StaticPrivateProperty.Text);
			Assert.AreEqual(string.Empty, SUT._StaticPrivateReadonlyField.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceDP.Text);
			Assert.AreEqual(string.Empty, SUT._InnerProperty.Text);
			Assert.AreEqual(string.Empty, SUT._StaticClass_PublicStaticProperty.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_OneParam.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_OneParam_DP_Update_OneTime.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_OneParam_DP_Update_OneWay.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_TwoParam.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_TwoParam_WithConstant.Text);
			Assert.AreEqual(string.Empty, SUT._System_Function.Text);
			Assert.AreEqual(string.Empty, SUT._System_Function_with_Quote.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_Parameterless.Text);
			Assert.AreEqual(string.Empty, SUT._StaticFunction_Parameterless.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_Boolean_False.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_Boolean_True.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("42", SUT._InstanceProperty.Text);
			Assert.AreEqual("43", SUT._StaticProperty.Text);
			Assert.AreEqual("44", SUT._StaticPrivateProperty.Text);
			Assert.AreEqual("45", SUT._StaticPrivateReadonlyField.Text);
			Assert.AreEqual("-1", SUT._InstanceDP.Text);
			Assert.AreEqual("Initial", SUT._InnerProperty.Text);
			Assert.AreEqual("46", SUT._StaticClass_PublicStaticProperty.Text);
			Assert.AreEqual("52", SUT._InstanceFunction_OneParam.Text);
			Assert.AreEqual("9", SUT._InstanceFunction_OneParam_DP_Update_OneWay.Text);
			Assert.AreEqual("9", SUT._InstanceFunction_OneParam_DP_Update_OneTime.Text);
			Assert.AreEqual("85", SUT._InstanceFunction_TwoParam.Text);
			Assert.AreEqual("84.42", SUT._InstanceFunction_TwoParam_WithConstant.Text);
			Assert.AreEqual("42, 43", SUT._System_Function.Text);
			Assert.AreEqual("42, \'43\'", SUT._System_Function_with_Quote.Text);
			Assert.AreEqual("Parameter-less result", SUT._InstanceFunction_Parameterless.Text);
			Assert.AreEqual("Static Parameter-less result", SUT._StaticFunction_Parameterless.Text);
			Assert.AreEqual("Was false", SUT._InstanceFunction_Boolean_False.Text);
			Assert.AreEqual("Was true", SUT._InstanceFunction_Boolean_True.Text);

			Assert.AreEqual(1, SUT.AddDoubleCallCount);
			Assert.AreEqual(2, SUT.AddIntCallCount);
			Assert.AreEqual(3, SUT.OffsetCallCount);
		}

		[TestMethod]
		public void When_Update_DP()
		{
			var SUT = new Functions_Control();

			Assert.AreEqual(string.Empty, SUT._InstanceDP.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("-1", SUT._InstanceDP.Text);

			SUT.InstanceDP = 4242;

			Assert.AreEqual("4242", SUT._InstanceDP.Text);
		}

		[TestMethod]
		public void When_Update_InnerProperty()
		{
			var SUT = new Functions_Control();

			Assert.AreEqual(string.Empty, SUT._InnerProperty.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("Initial", SUT._InnerProperty.Text);

			SUT.MyxBindClassInstance.MyProperty = "Updated value";

			Assert.AreEqual("Updated value", SUT._InnerProperty.Text);
		}

		[TestMethod]
		public void When_Function_OneParam_Update_DP()
		{
			var SUT = new Functions_Control();

			Assert.AreEqual(string.Empty, SUT._InstanceFunction_OneParam_DP_Update_OneTime.Text);
			Assert.AreEqual(string.Empty, SUT._InstanceFunction_OneParam_DP_Update_OneWay.Text);

			Assert.AreEqual(0, SUT.AddDoubleCallCount);
			Assert.AreEqual(0, SUT.AddIntCallCount);
			Assert.AreEqual(0, SUT.OffsetCallCount);

			SUT.ForceLoaded();

			Assert.AreEqual("9", SUT._InstanceFunction_OneParam_DP_Update_OneTime.Text);
			Assert.AreEqual("9", SUT._InstanceFunction_OneParam_DP_Update_OneWay.Text);

			Assert.AreEqual(1, SUT.AddDoubleCallCount);
			Assert.AreEqual(2, SUT.AddIntCallCount);
			Assert.AreEqual(3, SUT.OffsetCallCount);

			SUT.InstanceDP = 42;

			Assert.AreEqual("9", SUT._InstanceFunction_OneParam_DP_Update_OneTime.Text);
			Assert.AreEqual("52", SUT._InstanceFunction_OneParam_DP_Update_OneWay.Text);

			Assert.AreEqual(1, SUT.AddDoubleCallCount);
			Assert.AreEqual(3, SUT.AddIntCallCount);
			Assert.AreEqual(4, SUT.OffsetCallCount);
		}

		[TestMethod]
		public void When_Function_TwoParam_Update_DP()
		{
			var SUT = new Functions_Control();

			Assert.AreEqual(string.Empty, SUT._InstanceFunction_TwoParam_Update.Text);

			Assert.AreEqual(0, SUT.AddDoubleCallCount);
			Assert.AreEqual(0, SUT.AddIntCallCount);
			Assert.AreEqual(0, SUT.OffsetCallCount);

			SUT.ForceLoaded();

			Assert.AreEqual("-4", SUT._InstanceFunction_TwoParam_Update.Text);

			Assert.AreEqual(1, SUT.AddDoubleCallCount);
			Assert.AreEqual(2, SUT.AddIntCallCount);
			Assert.AreEqual(3, SUT.OffsetCallCount);

			SUT.InstanceDP = 10;

			Assert.AreEqual("7", SUT._InstanceFunction_TwoParam_Update.Text);

			Assert.AreEqual(1, SUT.AddDoubleCallCount);
			Assert.AreEqual(3, SUT.AddIntCallCount);
			Assert.AreEqual(4, SUT.OffsetCallCount);

			SUT.MyxBindClassInstance.MyIntProperty = 10;

			Assert.AreEqual("20", SUT._InstanceFunction_TwoParam_Update.Text);

			Assert.AreEqual(1, SUT.AddDoubleCallCount);
			Assert.AreEqual(4, SUT.AddIntCallCount);
			Assert.AreEqual(4, SUT.OffsetCallCount);
		}

		[TestMethod]
		public void When_NullableProperty()
		{
			var SUT = new Binding_Nullable();

			Assert.AreEqual(string.Empty, SUT._NullableBinding.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("False", SUT._NullableBinding.Text);

			SUT.myCheckBox.IsChecked = true;

			Assert.AreEqual("True", SUT._NullableBinding.Text);

			SUT.myCheckBox.IsChecked = false;

			Assert.AreEqual("False", SUT._NullableBinding.Text);

			SUT.myCheckBox.IsChecked = null;

			Assert.AreEqual("False", SUT._NullableBinding.Text);
		}

		[TestMethod]
		public void When_NullableProperty_And_ThreeState()
		{
			var SUT = new Binding_Nullable();
			SUT.myCheckBox.IsThreeState = true;
			SUT.myCheckBox.IsChecked = null;

			Assert.AreEqual(string.Empty, SUT._NullableBinding.Text);

			SUT.ForceLoaded();

			Assert.AreEqual(string.Empty, SUT._NullableBinding.Text);

			SUT.myCheckBox.IsChecked = true;

			Assert.AreEqual("True", SUT._NullableBinding.Text);

			SUT.myCheckBox.IsChecked = false;

			Assert.AreEqual("False", SUT._NullableBinding.Text);

			SUT.myCheckBox.IsChecked = null;

			Assert.AreEqual("False", SUT._NullableBinding.Text);
		}

	}
}
