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
	public class Given_ThemeResource
	{
		[TestMethod]
		public void When_System_ThemeResource_Light()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Color.FromArgb(0xDE, 0x00, 0x00, 0x00), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_System_ThemeResource_Dark()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			try
			{
				app.ForceSetRequestedTheme(Windows.UI.Xaml.ApplicationTheme.Dark);

				var control = new Test_Control();
				app.HostView.Children.Add(control);

				Assert.AreEqual(Color.FromArgb(0xDE, 0xFF, 0xFF, 0xFF), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
			}
			finally
			{
				//ensure the light theme is reset
				app.ForceSetRequestedTheme(Windows.UI.Xaml.ApplicationTheme.Light);
			}
		}

		[TestMethod]
		public void When_App_ThemeResource_Default()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Colors.LemonChiffon, (control.TestBorder.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_App_ThemeResource_Light()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(ApplicationTheme.Light, app.RequestedTheme);
			Assert.AreEqual(Colors.RosyBrown, (control.TestBorder.BorderBrush as SolidColorBrush).Color);
		}
	}
}
