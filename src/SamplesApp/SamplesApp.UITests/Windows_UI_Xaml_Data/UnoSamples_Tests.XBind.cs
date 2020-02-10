using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	[TestFixture]
	public partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void XBind_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.XBind.XBind_Simple");

			{
                // Wait for the first textblock value, the rest of the values are set 
                // synchronously in the test
				var tb = _app.Marked("textBlock1");
                _app.WaitFor(() => tb.GetDependencyPropertyValue("Text")?.ToString() != null);

				Assert.AreEqual("42", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
			{
				// Discrepancies between Wasm and iOS/Android
				// var tb = _app.Marked("textBlock1_Counter");
				// Assert.AreEqual("Should be 1:  1", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
			{
				var tb = _app.Marked("textBlock2");
				Assert.AreEqual("Should be 42:  42", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
			{
				var tb = _app.Marked("textBlock3");
				Assert.AreEqual("Should be 43:  43", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
			{
				var tb = _app.Marked("textBlock4");
				Assert.AreEqual("Should be 44:  44", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
			{
				// Discrepancies between Wasm and iOS/Android
				// var tb = _app.Marked("textBlock5");
				// Assert.AreEqual("Should be 2:  2", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
			{
				var tb = _app.Marked("textBlock6");
				Assert.AreEqual("Should be 43:  43", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
			{
				var tb = _app.Marked("textBlock7");
				Assert.AreEqual("Should be 44:  44", tb.GetDependencyPropertyValue("Text")?.ToString());
			}
		}

		[Test]
		[AutoRetry]
		public void XBind_Functions()
		{
			Run("UITests.Shared.Windows_UI_Xaml.xBindTests.xBind_Functions");

			var _StaticPropertyIntValue = _app.Marked("_StaticPropertyIntValue");
			Assert.AreEqual("42", _StaticPropertyIntValue.GetText());

			var _StaticPropertyStringValue = _app.Marked("_StaticPropertyStringValue");
			Assert.AreEqual("value 43", _StaticPropertyStringValue.GetText());

			var _StaticPropertyStringValueFunction = _app.Marked("_StaticPropertyStringValueFunction");
			Assert.AreEqual("VALUE 43", _StaticPropertyStringValueFunction.GetText());

			var _MemberFunctionMultiple_Bind_OneWay = _app.Marked("_MemberFunctionMultiple_Bind_OneWay");
			Assert.AreEqual("440", _MemberFunctionMultiple_Bind_OneWay.GetText());

			var _SaticFunctionMultiple_Bind_OneWay = _app.Marked("_SaticFunctionMultiple_Bind_OneWay");
			Assert.AreEqual("42", _SaticFunctionMultiple_Bind_OneWay.GetText());

			var _SystemFunction_Bind_OneWay = _app.Marked("_SystemFunction_Bind_OneWay");
			Assert.AreEqual("slider1: 20, slider2:22", _SystemFunction_Bind_OneWay.GetText());

			var updateTemplateButton = _app.Marked("updateTemplateButton");

			var _DataTemplate_MyProperty = _app.Marked("_DataTemplate_MyProperty");
			Assert.AreEqual("Initial", _DataTemplate_MyProperty.GetText());

			var _DataTemplate_MyProperty_Function = _app.Marked("_DataTemplate_MyProperty_Function");
			Assert.AreEqual("INITIAL", _DataTemplate_MyProperty_Function.GetText());

			var _DataTemplate_MyProperty_Formatted = _app.Marked("_DataTemplate_MyProperty_Formatted");
			Assert.AreEqual("Formatted Initial", _DataTemplate_MyProperty_Formatted.GetText());

			var _DataTemplate_MyProperty_Function_OneWay = _app.Marked("_DataTemplate_MyProperty_Function_OneWay");
			Assert.AreEqual("INITIAL", _DataTemplate_MyProperty_Function_OneWay.GetText());

			var _DataTemplate_MyProperty_Formatted_OneWay = _app.Marked("_DataTemplate_MyProperty_Formatted_OneWay");
			Assert.AreEqual("Formatted Initial", _DataTemplate_MyProperty_Formatted_OneWay.GetText());

			updateTemplateButton.FastTap();

			Assert.AreEqual("NEW UPDATE", _DataTemplate_MyProperty_Function_OneWay.GetText());
			Assert.AreEqual("Formatted new update", _DataTemplate_MyProperty_Formatted_OneWay.GetText());
		}
	}
}
