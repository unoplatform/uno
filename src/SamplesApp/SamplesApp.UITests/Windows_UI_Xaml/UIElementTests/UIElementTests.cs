using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml
{
	[TestFixture]
	public partial class UIElementTests : SampleControlUITestBase
	{
		// TODO: convert this to RuntimeTests https://github.com/unoplatform/uno/issues/2114#issuecomment-555209397
		[Test]
		[AutoRetry]
		public void When_TransformToVisual_Transform()
		{
			Run("UITests.Shared.Windows_UI_Xaml.UIElementTests.TransformToVisual_Transform", skipInitialScreenshot: false);

			_app.WaitForText("TestsStatus", "SUCCESS");
		}

		// TODO: convert this to RuntimeTests https://github.com/unoplatform/uno/issues/2114#issuecomment-555209397
		[Test]
		[AutoRetry]
		public void When_TransformToVisual_ScrollViewer()
		{
			Run("UITests.Shared.Windows_UI_Xaml.UIElementTests.TransformToVisual_ScrollViewer", skipInitialScreenshot: false);

			_app.WaitForText("TestsStatus", "SUCCESS");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Tests display of Android native view
		public void When_Native_View()
		{
			Run("UITests.Shared.Windows_UI_Xaml.UIElementTests.UIElement_Native_Child");

			_app.WaitForElement("SpacerBorder");
			var spacerRect = _app.GetRect("SpacerBorder");

			using var scrn = TakeScreenshot("Ready", ignoreInSnapshotCompare: true);

			ImageAssert.HasColorAt(scrn, spacerRect.X, spacerRect.Y, Color.Red);
		}
	}
}
