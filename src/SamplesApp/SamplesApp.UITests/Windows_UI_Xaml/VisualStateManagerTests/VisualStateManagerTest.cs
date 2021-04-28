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
	public partial class VisualStateManagerTest : SampleControlUITestBase
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

			_app.WaitForDependencyPropertyValue(border01_bound, "Background", "[SolidColorBrush [#FFFF0000]]");
			_app.WaitForDependencyPropertyValue(border02, "Background", "[SolidColorBrush [#FF800080]]");
			_app.WaitForDependencyPropertyValue(border03, "Background", "[SolidColorBrush [#FFFFA500]]");
		}
	}
}
