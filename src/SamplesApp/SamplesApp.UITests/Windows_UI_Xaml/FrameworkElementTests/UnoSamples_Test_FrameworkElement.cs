using System.Globalization;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FrameworkElementTests
{
	public class UnoSamples_Test_FrameworkElement : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Loaded_Unloaded_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml.FrameworkElementTests.LoadEvents");

			var loadedResult = _app.Marked("loadedResult");
			var unloadedResult = _app.Marked("unloadedResult");

			_app.WaitForElement(loadedResult);

			_app.WaitForDependencyPropertyValue(loadedResult, "Text", "[OK] Loaded event received");
			_app.WaitForDependencyPropertyValue(unloadedResult, "Text", "[OK] Unloaded event received");
		}

		[Test]
		[AutoRetry]
		public void ItemsControl_LoadCount()
		{
			Run("UITests.Shared.Windows_UI_Xaml.FrameworkElementTests.ItemsControl_Loaded");

			var result = _app.Marked("result");

			_app.WaitForElement(result);

			_app.WaitForDependencyPropertyValue(result, "Text", "Loaded: 1");
		}
	}
}
