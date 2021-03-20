using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public partial class RoutedEventIsEnabled_Tests : SampleControlUITestBase
	{
		private const string XamlTestPage = "UITests.Windows_UI_Xaml_Input.RoutedEvents.RoutedEvent_IsEnabled";

		[Test]
		[AutoRetry]
		public void When_Basic()
		{
			Run(XamlTestPage);

			var button = _app.Marked("DisabledButton");
			button.Tap();

			var result = _app.Marked("EventInfoTextBlock");

			Assert.IsEmpty(result.GetText());
		}
	}
}
