using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ThemeResource
	{
		[TestMethod]
		public void When_System_ThemeResource()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Color.FromArgb(0xDE, 0x00, 0x00, 0x00), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
		}
	}
}
