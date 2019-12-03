using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ControlTests
{
	[TestFixture]
	public class Control_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_IsEnabled_Initially_False_And_Inherited()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ControlTests.Control_IsEnabled_Inheritance");

			_app.WaitForText("CounterTextBlock", "0");

			_app.Tap("ButtonUnderTest");

			_app.WaitForText("CounterTextBlock", "0");

			_app.Tap("ToggleEnabledButton");

			_app.WaitForText("IsEnabledTextBlock", "True");

			_app.Tap("ButtonUnderTest");

			_app.WaitForText("CounterTextBlock", "1");
		}
	}
}
