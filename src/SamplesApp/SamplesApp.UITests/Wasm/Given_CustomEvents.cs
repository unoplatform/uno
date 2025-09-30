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
