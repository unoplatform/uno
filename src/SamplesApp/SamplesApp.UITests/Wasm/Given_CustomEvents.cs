// [UITest -> runtime-test migration] NOT migrated to a runtime test:
// WASM-DOM specific: the sample (Wasm_CustomEvent, #if __WASM__-only) uses WebAssemblyRuntime.InvokeJS to dispatch raw/custom DOM Events and RegisterHtmlEventHandler/RegisterHtmlCustomEventHandler to bind to them by HtmlId — a browser-DOM-only mechanism with no Skia equivalent; not translatable to a Skia runtime test.
using System.Threading;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Wasm
{
	[TestFixture]
	public partial class Given_CustomEvents : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void Check_CustomEvent()
		{
			Run("UITests.Shared.Wasm.Wasm_CustomEvent");

			_app.FastTap("genericEvent");

			_app.Marked("tapResult").GetText().Should().Be("Ok");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void Check_CustomEvent_WithPayload()
		{
			Run("UITests.Shared.Wasm.Wasm_CustomEvent");

			_app.FastTap("customEventString");

			_app.Marked("tapResult").GetText().Should().Be("Ok");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void Check_CustomEvent_WithJsonPayload()
		{
			Run("UITests.Shared.Wasm.Wasm_CustomEvent");

			_app.FastTap("customEventJson");

			_app.Marked("tapResult").GetText().Should().Be("Ok");
		}
	}
}
