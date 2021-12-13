using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBoxTests
{
	[TestFixture]
	public partial class AutoSuggestBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void PasswordBox_With_Description()
		{
			Run("UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests.AutoSuggestBox_Description", skipInitialScreenshot: true);
			var autoSuggestBox = _app.WaitForElement("DescriptionAutoSuggestBox")[0];
			using var screenshot = TakeScreenshot("AutoSuggestBox Description", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			ImageAssert.HasColorAt(screenshot, autoSuggestBox.Rect.X + autoSuggestBox.Rect.Width / 2, autoSuggestBox.Rect.Y + autoSuggestBox.Rect.Height - 150, Color.Red);
		}
	}
}
