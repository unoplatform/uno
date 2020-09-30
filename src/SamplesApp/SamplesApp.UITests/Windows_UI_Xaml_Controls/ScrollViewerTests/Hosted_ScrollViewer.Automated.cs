using System;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[TestFixture]
	public partial class Hosted_ScrollViewer : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Content_NotTemplateBound()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests.Hosted_ScrollViewer");

			var result = _app.Marked("SUT_1").GetDependencyPropertyValue("Content");

			Assert.AreEqual("OuterContentPresenter Tag", result);
		}

		[Test]
		[AutoRetry]
		public void When_Content_TemplateBound()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests.Hosted_ScrollViewer");

			var result = _app.Marked("SUT_2").GetDependencyPropertyValue("Content");

			Assert.AreEqual("OuterContentPresenter Tag", result);
		}
	}
}
