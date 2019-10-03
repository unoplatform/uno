using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Explicit_Style
	{
#if NETFX_CORE
		internal static readonly Thickness DefaultButtonPadding = new Thickness(8, 4, 8, 5);
#else
		// TODO: adjust this when Uno retargets newer version of the SDK
		internal static readonly Thickness DefaultButtonPadding = new Thickness(8, 4, 8, 4);
#endif
		[TestMethod]
		public void When_Custom_Type()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			Assert.AreEqual("TopImplicitStyleValue", control.StylesTestControl.Rugosity);
			Assert.AreEqual("FromDefault", control.StylesTestControlExplicit.Rugosity); // Explicit styles are not additive with implicit style

			Assert.AreEqual(Colors.DarkCyan, (control.StylesTestControlExplicit.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Framework_Type()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			var button = control.StyledButton;
			Assert.IsNotNull(button.Template);
			Assert.AreEqual(DefaultButtonPadding, button.Padding); // Explicit styles are additive with built-in framework style
		}

		[TestMethod]
		public void When_Custom_From_Framework_Type()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			Assert.AreEqual(DefaultButtonPadding, control.StylesTestButton.Padding);
			Assert.AreEqual(typeof(Button), control.StylesTestButton.ExposedKey);
		}

		[TestMethod]
		public void When_Custom_Type_And_Custom_Key()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			Assert.AreEqual(new Thickness(), control.StylesTestButtonCustomKey.Padding); //DefaultStyleKey is modified so base framework type defaults are not applied
		}

		[TestMethod]
		public void When_Explicit_Style_Unset()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			Assert.IsNotNull(control.TestRadioButtonExplicit.Style);
			Assert.IsNull(control.TestRadioButtonExplicit.Tag); //Active Style is explicit

			var oldStyle = control.TestRadioButtonExplicit.Style;

			control.TestRadioButtonExplicit.Style = null;
			Assert.IsNull((object)control.TestRadioButtonExplicit.Tag); //Active Style is null

			{ // This isn't necessary on Windows, but it's needed for this test on Uno because going from local null => local UnsetValue isn't recognized as a change in the value
				control.TestRadioButtonExplicit.Style = oldStyle;
			}

			control.TestRadioButtonExplicit.SetValue(FrameworkElement.StyleProperty, DependencyProperty.UnsetValue);

			Assert.AreEqual("InnerTreeImplicit", control.TestRadioButtonExplicit.Tag); //Active Style is implicit
		}

		[TestMethod]
		public void When_Custom_Key_Local_And_Explicit()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			var c1 = Colors.GreenYellow;
			var c2 = Colors.YellowGreen;

			var b1 = control.StylesTestButtonCustomKey.Background;
			var b2 = control.StylesTestButtonCustomKeyExplicit.Background;

			var f2 = control.StylesTestButtonCustomKeyExplicit.Foreground;
		}

		[TestMethod]
		public void When_Multiple_Styles_With_Nested_Templates()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var button1 = new Button { Style = app.Resources["ButtonStyleWithTemplates1"] as Style };

			app.HostView.Children.Add(button1);

			button1.Measure(new Size(1000, 1000));

			var text1 = button1.FindFirstChild<TextBlock>();
			Assert.IsNotNull(text1);
			Assert.AreEqual("RadioButtonText1", text1.Text);

			var button2 = new Button { Style = app.Resources["ButtonStyleWithTemplates2"] as Style };

			app.HostView.Children.Add(button2);

			button2.Measure(new Size(1000, 1000));

			var text2 = button2.FindFirstChild<TextBlock>();
			Assert.IsNotNull(text2);
			Assert.AreEqual("RadioButtonText2", text2.Text);

			var button3 = new Button { Style = app.Resources["ButtonStyleWithTemplates3"] as Style };

			app.HostView.Children.Add(button3);

			button3.Measure(new Size(1000, 1000));

			var text3 = button3.FindFirstChild<TextBlock>();
			Assert.IsNotNull(text3);
			Assert.AreEqual("RadioButtonText3", text3.Text);
		}
	}
}
