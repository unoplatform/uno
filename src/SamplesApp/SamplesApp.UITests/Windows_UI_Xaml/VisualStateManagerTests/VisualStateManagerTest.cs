using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.VisualStateManagerTests
{
	[TestFixture]
	public class VisualStateManagerTest : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Testing_ComplexSetters()
		{
			Run("UITests.Shared.Windows_UI_Xaml.VisualStateTests.VisualState_ComplexSetters_Automated");

			var changeState = _app.Marked("changeState");
			_app.WaitForElement(changeState);

			var border01_bound = _app.Marked("border01_bound");
			var border02 = _app.Marked("border02");
			var border03 = _app.Marked("border03");

			_app.FastTap(changeState);

			_app.WaitForDependencyPropertyValue(border01_bound, "Background", "[SolidColorBrush [Color: 000000FF;000000FF;00000000;00000000]]");
			_app.WaitForDependencyPropertyValue(border02, "Background", "[SolidColorBrush [Color: 000000FF;00000080;00000000;00000080]]");
			_app.WaitForDependencyPropertyValue(border03, "Background", "[SolidColorBrush [Color: 000000FF;000000FF;000000A5;00000000]]");
		}

		[Test]
		[AutoRetry]
		public void When_Value_Overriden_Locally()
		{
			Run("UITests.Shared.Windows_UI_Xaml.VisualStateTests.VisualState_LocalOverride");
			
			var rootGrid = _app.Marked("RootGrid");
			_app.WaitForElement(rootGrid);

			// Initially, the background should be red.
			_app.WaitForDependencyPropertyValue(rootGrid, "Background", "[SolidColorBrush [Color: 000000FF;000000FF;00000000;00000000]]");

			var setColorButton = _app.Marked("SetColorButton");
			_app.FastTap(setColorButton);

			// The button should override the value set by VisualState's Setter.
			_app.WaitForDependencyPropertyValue(rootGrid, "Background", "[SolidColorBrush [Color: 000000FF;00000000;00000000;000000FF]]");
		}
	}
}
