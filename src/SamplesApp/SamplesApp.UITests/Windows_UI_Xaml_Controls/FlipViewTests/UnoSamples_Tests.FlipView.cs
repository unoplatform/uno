using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FlipViewTests
{
    [TestFixture]
    public partial class UnoSamples_Tests : SampleControlUITestBase
    {
		[Test]
		[AutoRetry]
		public void FlipView_WithButtons_FlipForward()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			_app.WaitForElement("Button1");

			_app.WaitForElement("NextButtonHorizontal");
			_app.FastTap("NextButtonHorizontal");

			_app.WaitForElement("Button2");
		}

		[Test]
		[AutoRetry]
		public void FlipView_WithButtons_FlipBackward()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			_app.WaitForElement("NextButtonHorizontal");
			_app.FastTap("NextButtonHorizontal");

			_app.WaitForElement("Button2");

			_app.WaitForElement("PreviousButtonHorizontal");
			_app.FastTap("PreviousButtonHorizontal");

			_app.WaitForElement("Button1");
		}
	}
}
