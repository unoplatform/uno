using System.Drawing;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml
{
	[TestFixture]
	public partial class WasmTests : SampleControlUITestBase
	{
		[Test]
		[ActivePlatforms(Platform.Browser)]
		[AutoRetry]
		public void Given_HtmlEvents_On_GenericEvents()
		{
			Run("UITests.Shared.Wasm.Wasm_CustomEvent");

			var control = _app.Marked("genericEvent");
			var rect = _app.GetPhysicalRect(control);

			var result = _app.Marked("tapResult");

			control.FastTap();

			using var screenshot = TakeScreenshot("tap");
			ImageAssert.HasColorAt(screenshot, rect.CenterX, rect.CenterY, Color.Pink, 8);
			result.GetDependencyPropertyValue<string>("Text").Should().Be("Ok");
		}
		[Test]
		[ActivePlatforms(Platform.Browser)]
		[AutoRetry]
		public void Given_HtmlEvents_On_CustomEvents()
		{
			Run("UITests.Shared.Wasm.Wasm_CustomEvent");

			var control = _app.Marked("customEventString");
			var rect = _app.GetPhysicalRect(control);

			var result = _app.Marked("tapResult");

			control.FastTap();

			using var screenshot = TakeScreenshot("tap");
			ImageAssert.HasColorAt(screenshot, rect.CenterX, rect.CenterY, Color.Pink, 8);
			result.GetDependencyPropertyValue<string>("Text").Should().Be("Ok");
		}
		[Test]
		[ActivePlatforms(Platform.Browser)]
		[AutoRetry]
		public void Given_HtmlEvents_On_CustomEventsWithJsonPayload()
		{
			Run("UITests.Shared.Wasm.Wasm_CustomEvent");

			var control = _app.Marked("customEventJson");
			var rect = _app.GetPhysicalRect(control);

			var result = _app.Marked("tapResult");

			control.FastTap();

			using var screenshot = TakeScreenshot("tap");
			ImageAssert.HasColorAt(screenshot, rect.CenterX, rect.CenterY, Color.Pink, 8);
			result.GetDependencyPropertyValue<string>("Text").Should().Be("Ok");
		}
	}
}
