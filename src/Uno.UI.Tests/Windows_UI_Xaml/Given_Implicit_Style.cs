using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Implicit_Style
	{
		[TestMethod]
		public void When_Implicit_Style_In_Application_Merged()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Colors.LightGoldenrodYellow, (control.TestCheckBox.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Implicit_Style_In_Visual_Tree_Local_Type()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			var strBefore = control.InlineTemplateControl.MyStringDP;

			app.HostView.Children.Add(control);

			var strAfter = control.InlineTemplateControl.MyStringDP;

			Assert.AreEqual("AppLevelImplicit", strBefore);
			Assert.AreEqual("InnerTreeImplicit", strAfter);
		}

		[TestMethod]
		public void When_Implicit_Style_In_Visual_Tree_Framework_Type()
		{

			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			var tagBefore = control.TestRadioButton.Tag;

			app.HostView.Children.Add(control);

			var tagAfter = control.TestRadioButton.Tag;

			Assert.AreEqual("AppLevelImplicit", tagBefore);
			Assert.AreEqual("InnerTreeImplicit", tagAfter);
		}
	}
}
