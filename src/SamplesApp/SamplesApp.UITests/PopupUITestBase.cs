using NUnit.Framework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests
{
	public class PopupUITestBase : SampleControlUITestBase
	{
		[SetUp]
		public void AndroidFullScreenSetup()
		{
			if (AppInitializer.GetLocalPlatform() == Platform.Android)
			{
				// workaround for #2747: force the app into fullscreen
				// to prevent status bar from reappearing when popup are shown.
				_app.InvokeGeneric("browser:SampleRunner|SetFullScreenMode", true);
			}
		}

		[TearDown]
		public void AndroidFullScreenTearDown()
		{
			if (AppInitializer.GetLocalPlatform() == Platform.Android)
			{
				_app.InvokeGeneric("browser:SampleRunner|SetFullScreenMode", false);
			}
		}
	}
}
