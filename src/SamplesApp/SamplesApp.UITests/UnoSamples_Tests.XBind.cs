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
				// x:Bind is evaluated too many times
				// var tb = _app.Marked("textBlock5");
				// Assert.AreEqual("Should be 1:  1", tb.GetDependencyPropertyValue("Text")?.ToString());
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
	}
}
