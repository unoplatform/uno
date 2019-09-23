using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls
{
	[TestFixture]
	public class TextBlockTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Visibility_Changed_During_Arrange()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.TextBlock_Visibility_Arrange");

			var textBlock = _app.Marked("SubjectTextBlock");

			_app.WaitForElement(textBlock);

			_app.WaitForDependencyPropertyValue(textBlock, "Text", "It worked!");
		}
	}
}
