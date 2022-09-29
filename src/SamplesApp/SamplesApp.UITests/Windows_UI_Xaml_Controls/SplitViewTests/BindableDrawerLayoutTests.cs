#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.SplitViewTests
{
	[TestFixture]
	public partial class BindableDrawerLayoutTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Pane_IsReset()
		{
			Run("UITests.Windows_UI_Xaml_Controls.SplitView.BindableDrawerLayout_ChangePane");
			_app.WaitForElement("RootSplitView");

			// validate BindableDrawerLayout pane can be set to null and restore later

			_app.Tap("ManualOpenPaneButton");
			Assert.IsTrue(_app.Marked("DrawerContent").HasResults());

			_app.Tap("SetPaneToNullButton");
			Assert.IsFalse(_app.Marked("DrawerContent").HasResults());

			_app.Tap("SetPaneToSkyBlueGridButton");
			Assert.IsTrue(_app.Marked("DrawerContent").HasResults());
		}
	}
}
