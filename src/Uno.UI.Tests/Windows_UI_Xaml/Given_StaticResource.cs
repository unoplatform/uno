using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_StaticResource
	{
		[TestMethod]
		public void When_Resource_In_Application_Merged()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			Assert.AreEqual(Colors.MediumSpringGreen, (control.TopGrid.Background as SolidColorBrush).Color); //Resource is resolved before control is in visual tree

			app.HostView.Children.Add(control);

			Assert.AreEqual(Colors.MediumSpringGreen, (control.TopGrid.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Double_Resource_In_Application_Merged()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(256, control.TestBorder.Width);
		}

		[TestMethod]
		public void When_Double_Resource_In_Application_Merged_Non_View()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			var rowDef = control.TestGrid.RowDefinitions.First();
			Assert.AreEqual(256, rowDef.Height.Value);
		}

		[TestMethod]
		public void When_In_Same_Dictionary()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(33, control.StyledButton.FontSize);
		}

		[TestMethod]
		public void When_In_Dictionary_Merged()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(new Thickness(1,2,3,4), control.StyledButton.BorderThickness);
		}

		[TestMethod]
		public void When_Set_On_Non_DependencyObject()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(33, control.TestMyControl.Poco.Bogosity);
		}

		[TestMethod]
		public void When_Setting_Non_DependencyProperty()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(33, control.TestMyControl.Splinitude);
		}
	}
}
