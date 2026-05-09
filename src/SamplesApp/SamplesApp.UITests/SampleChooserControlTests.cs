using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	[TestFixture]
	public partial class SampleChooserControlTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Sample_Description_Is_Long_It_Is_Scrollable()
		{
			Run("UITests.Windows_UI_Xaml_Controls.WebView.WebView_AnchorNavigation", skipInitialScreenshot: true);

			_app.WaitForElement("DescriptionScrollViewer");
			_app.WaitForElement("CurrentSampleDescription");

			var scrollViewerRect = _app.GetPhysicalRect("DescriptionScrollViewer");
			var descriptionRect = _app.GetPhysicalRect("CurrentSampleDescription");

			Assert.That(scrollViewerRect.Height, Is.GreaterThan(0));
			Assert.That(descriptionRect.Height, Is.GreaterThan(scrollViewerRect.Height));

			TakeScreenshot("Long sample description is scrollable", ignoreInSnapshotCompare: true);
		}
	}
}
