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
		// NOTE (UITest -> runtime-test migration): XBind_Functions was migrated to
		// Uno.UI.RuntimeTests Tests/Windows_UI_Xaml_Data/Given_XBind_UITest.cs.
		// XBind_Validation was intentionally NOT migrated: its sample (XBindControl01) marks the
		// target TextBlocks with x:Uid rather than x:Name, so the runtime-test query shim
		// (QueryEx.Marked matches FrameworkElement.Name only) cannot resolve them; and its
		// assertions read the aggregated Text of Run-composed TextBlocks whose exact inter-Run
		// whitespace is platform-sensitive (the sample itself skips textBlock1_Counter/textBlock5
		// for the same reason). Left here for manual review.
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
	}
}
