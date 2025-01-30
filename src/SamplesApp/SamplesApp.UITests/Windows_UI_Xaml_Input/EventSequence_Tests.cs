using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public partial class EventSequence_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TestTap()
			=> RunSequence("Tap");

		[Test]
		[AutoRetry]
		public void TestClick()
			=> RunSequence("Click");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Wasm is flaky, failing on Android https://github.com/unoplatform/uno/issues/9080
		public void TestTranslatedTap()
			=> RunSequence("TranslatedTap", TranslateOverElement);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // Wasm is disabled https://github.com/unoplatform/uno/issues/13844
		public void TestTranslatedClick()
			=> RunSequence("TranslatedClick", TranslateOverElement);

		[Test]
		[AutoRetry]
		public void TestHyperlink()
			=> RunSequence("Hyperlink", TapSomewhereInElement);

		[Test]
		[AutoRetry]
		public void TestListView()
			=> RunSequence("ListView", TapSomewhereInElement);

		private readonly Action<QueryEx> TapElement = element => element.FastTap();
		private void TranslateOverElement(QueryEx element)
		{
			var rect = _app.WaitForElement(element).Single().Rect;
			_app.DragCoordinates(rect.X + 2, rect.Y + 2, rect.Right - 2, rect.Bottom - 2);
		}
		private void TapSomewhereInElement(QueryEx element)
		{
			var rect = _app.WaitForElement(element).Single().Rect;
			_app.TapCoordinates(rect.X + 10, rect.Y + 10);
		}

		private void RunSequence(string testName, Action<QueryEx> tap = null)
		{
			tap = tap ?? TapElement;

			Run("UITests.Shared.Windows_UI_Input.PointersTests.EventsSequences", skipInitialScreenshot: true);

			var target = _app.Marked($"Test{testName}Target");
			var reset = _app.Marked($"Test{testName}Reset");
			var validate = _app.Marked($"Test{testName}Validate");
			var result = _app.Marked($"Test{testName}Result");

			_app.WaitForElement(target);
			reset.FastTap();
			tap(target);
			validate.FastTap();

			TakeScreenshot("Result", ignoreInSnapshotCompare: true);

			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}
	}
}
