using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Views;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Helpers;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_StaticResource
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_Resource_In_Application_Merged()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			Assert.AreEqual(Colors.MediumSpringGreen, (control.TopGrid.Background as SolidColorBrush).Color); //Resource is resolved before control is in visual tree

			app.HostView.Children.Add(control);

			Assert.AreEqual(Windows.UI.Colors.MediumSpringGreen, (control.TopGrid.Background as SolidColorBrush).Color);
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

			var rowDef = control.TestGrid.RowDefinitions.First<RowDefinition>();
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

			Assert.AreEqual(new Thickness(1, 2, 3, 4), control.StyledButton.BorderThickness);
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

		[TestMethod]
		public void When_Inherited_In_Template()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var text1InlineAfter = control.InlineTemplateControl.TextBlock1.Text;

			Assert.AreEqual("LocalVisualTree", text1InlineAfter);
		}

		[TestMethod]
		public void When_Inherited_In_Template_XAML_Scope()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var text1ResourceTemplateAfter = control.TemplateFromResourceControl.TextBlock1.Text;

			Assert.AreEqual("OuterVisualTree", text1ResourceTemplateAfter);
		}

		[TestMethod]
		public void When_Inherited_In_Template_Applied()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			control.InlineTemplateControl.ApplyTemplate();
			control.TemplateFromResourceControl.ApplyTemplate();

			var text1InlineBefore = control.InlineTemplateControl.TextBlock1.Text;
			var text1ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock1.Text;

			var text3InlineBefore = control.InlineTemplateControl.TextBlock3.Text;
			var text3ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock3.Text;

			app.HostView.Children.Add(control);

			var text1InlineAfter = control.InlineTemplateControl.TextBlock1.Text;
			var text1ResourceTemplateAfter = control.TemplateFromResourceControl.TextBlock1.Text;

			Assert.AreEqual("LocalVisualTree", text1InlineBefore);
			Assert.AreEqual("OuterVisualTree", text1ResourceTemplateBefore);

			Assert.AreEqual("ApplicationLevel", text3InlineBefore);
			Assert.AreEqual("ApplicationLevel", text3ResourceTemplateBefore);

			Assert.AreEqual("LocalVisualTree", text1InlineAfter);
			Assert.AreEqual("OuterVisualTree", text1ResourceTemplateAfter);
		}

		[TestMethod]
		public void When_Multiple_References_Framework_DependencyObject()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var foreground = control.InlineTemplateControl.TextBlock1.Foreground as SolidColorBrush;
			var background = control.TemplateFromResourceControl.Background as SolidColorBrush;
			var inResources = control.Resources["MutatedColorBrush"] as SolidColorBrush;

			Assert.AreEqual(Colors.Chartreuse, foreground.Color);
			Assert.AreEqual(Colors.Chartreuse, background.Color);
			Assert.AreEqual(Colors.Chartreuse, inResources.Color);

			Assert.IsTrue(ReferenceEquals(foreground, background));
			Assert.IsTrue(ReferenceEquals(inResources, foreground));

			inResources.Color = Colors.GhostWhite;
			Assert.AreEqual(Colors.GhostWhite, foreground.Color);
			Assert.AreEqual(Colors.GhostWhite, background.Color);

			var control2 = new Test_Control();
			app.HostView.Children.Add(control2);
			control2.Measure(new Size(1000, 1000));


			var background2 = control2.TemplateFromResourceControl.Background as SolidColorBrush;
			var areRefEqual = ReferenceEquals(background, background2);
			Assert.IsFalse(areRefEqual);
			Assert.AreEqual(Colors.Chartreuse, background2.Color);
		}

		[TestMethod]
		public void When_Multiple_References_Poco()
		{
			var control = new Test_Control();

			var poco1 = control.InlineTemplateControl.Poco;
			var poco2 = control.TemplateFromResourceControl.Poco;
			Assert.IsTrue(ReferenceEquals(poco1, poco2));

			Assert.AreEqual(101, poco1.Bogosity);
			Assert.AreEqual(101, poco2.Bogosity);

			poco1.Bogosity = 107;
			Assert.AreEqual(107, poco2.Bogosity);

			var control2 = new Test_Control();
			var poco3 = control2.InlineTemplateControl.Poco;
			Assert.IsFalse(ReferenceEquals(poco1, poco3));
			Assert.AreEqual(101, poco3.Bogosity);
		}

		[TestMethod]
		public void When_Multiple_References_Custom_DependencyObject()
		{
			var control = new Test_Control();

			var do1_1 = control.InlineTemplateControl.DObjDP;
			var do1_2 = control.InlineTemplateControl.PlainDO;
			var do1_3 = control.TemplateFromResourceControl.DObjDP;
			var areRefEqual1 = ReferenceEquals(do1_1, do1_2);
			var areRefEqual2 = ReferenceEquals(do1_1, do1_3);
			Assert.IsTrue(areRefEqual1);
			Assert.IsTrue(areRefEqual2);

			var control2 = new Test_Control();
			var do2_1 = control2.InlineTemplateControl.DObjDP;
			var areRefEqual3 = ReferenceEquals(do1_1, do2_1);
			Assert.IsFalse(areRefEqual3);
		}

		[TestMethod]
		public void When_System_Resource()
		{
			var control = new Test_Control();

			Assert.AreEqual(Color.FromArgb(0xB3, 0xB6, 0xB6, 0xB6), (control.TopGrid.BorderBrush as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Attached_Property_And_App_Resource()
		{
			var control = new Test_Control();

			var bulbousness = MyBehavior.GetBulbousness(control.TestTextBlock);
			Assert.AreEqual(256, bulbousness);
		}

		[TestMethod]
		public void When_Attached_Property_And_Local_Resource()
		{
			var control = new Test_Control();

			var bulbousness = MyBehavior.GetBulbousness(control.TestBorder);
			Assert.AreEqual(105.5, bulbousness);
		}

		[TestMethod]
		public void When_Attached_Property_And_Top_Level()
		{
			var page = new Test_Page();

			var bulbousness = MyBehavior.GetBulbousness(page);
			Assert.AreEqual(256, bulbousness);
		}

		[TestMethod]
		public void When_Attached_Property_No_DP()
		{
			var control = new Test_Control();

			var noDP = MyBehavior.GetNoDPProperty(control.TestTextBlock);
			Assert.AreEqual(256, noDP);
		}

		[TestMethod]
		public void When_Non_View_And_Local_Brush()
		{
			var control = new Test_Control();

			Assert.AreEqual(Colors.MintCream, (control.TemplateFromResourceControl.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Non_View_And_Local_Resource()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			var rowDef = control.TestGrid.RowDefinitions[1];
			Assert.AreEqual(333, rowDef.Height.Value);
		}

		[TestMethod]
		public void When_ResourceKey_And_System()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var res = app.Resources["ImAStaticResourceInADictColorBrush"] as SolidColorBrush;
			Assert.AreEqual(Colors.LightGray, res.Color);
		}

		[TestMethod]
		public void When_ResourceKey_And_Local()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var res = app.Resources["ResourceKeyLocalReference"] as SolidColorBrush;
			Assert.AreEqual(Colors.MediumSpringGreen, res.Color);
		}

		[TestMethod]
		public void When_ResourceKey_And_Assigned()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var template = app.Resources["TemplateUsingStaticResourceAlias"] as ControlTemplate;
			var button = new Button { Template = template };
			button.Measure(new Size(1000, 1000));
			var tb = button.FindFirstChild<TextBlock>();
			Assert.AreEqual(Colors.MediumSpringGreen, (tb.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Converter()
		{
			var page = new Test_Page();

			page.DataContext = new
			{
				Boolean1 = true,
				Boolean2 = false
			};

			AssertEx.AssertHasColor(page.TestBorder.Background, Colors.Plum);
		}

		[TestMethod]
		public void When_Converter_In_Template()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var page = new Test_Page();
			page.DataContext = new
			{
				Boolean1 = true,
				Boolean2 = false
			};

			app.HostView.Children.Add(page);
			page.Measure(new Size(1000, 1000));

			var tb = page.TestContentControl.FindFirstChild<TextBlock>();
			Assert.AreEqual("Inner text", tb.Text);
			AssertEx.AssertHasColor(tb.Foreground, Colors.Tomato);
		}

		[TestMethod]
		public void When_Converter_In_Template_Separate_Xaml()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var page = new Test_Page();

			page.SpiffyItemsControl.ItemsSource = Enumerable.Range(0, 3).Select(i => true).ToArray();

			app.HostView.Children.Add(page);
			page.Measure(new Size(1000, 1000));

			var rb = page.SpiffyItemsControl.FindFirstChild<RadioButton>();
			AssertEx.AssertHasColor(rb.Foreground, Colors.Plum);
		}

		[TestMethod]
		public void When_Implicit_Conversion()
		{
			var page = new Test_Page();

			var splineFrame = page.Resources["FineSpline"] as SplineDoubleKeyFrame;
			var spline = splineFrame.KeySpline;
			Assert.AreEqual(new Point(0.6, 0), spline.ControlPoint1);
			Assert.AreEqual(new Point(0, 0.7), spline.ControlPoint2);
		}

		[TestMethod]
		public void When_System_Resource_From_Template()
		{
			const double System_AppBarThemeCompactHeight =
#if NETFX_CORE
				48 //This changed from RS4 to RS5
#else
				40
#endif
				;
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 2000));

			Assert.AreEqual(System_AppBarThemeCompactHeight, control.TestCommandBar.TemplateSettings.CompactVerticalDelta);
			Assert.AreEqual(System_AppBarThemeCompactHeight, control.TestCommandBar2.TemplateSettings.CompactVerticalDelta);
		}

		[TestMethod]
		public void When_System_Resource_From_Template_Overridden()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			try
			{
				app.Resources["AppBarThemeCompactHeight"] = 17d;

				var control = new Test_Control();

				app.HostView.Children.Add(control);

				control.Measure(new Size(1000, 2000));

				Assert.AreEqual(17, control.TestCommandBar.TemplateSettings.CompactVerticalDelta);
				Assert.AreEqual(17, control.TestCommandBar2.TemplateSettings.CompactVerticalDelta);
			}
			finally
			{
				app.Resources.Remove("AppBarThemeCompactHeight");
			}
		}

		[TestMethod]
		public void When_TimeSpan_Intra_Dictionary()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var radioButton = new RadioButton();

			radioButton.Style = app.Resources["ExplicitRadioButtonStyle2"] as Style;
			radioButton.ApplyTemplate();

			var ctrl = radioButton.FindFirstChild<MyControl>();

			Assert.AreEqual(TimeSpan.FromMilliseconds(400), ctrl.MyInterval);
		}

		[TestMethod]
		public void When_Ext_Library_Same_Library()
		{
			var page = new Test_Page();
			var tx = page.myExtTextBox;
			AssertEx.AssertHasColor(tx.Foreground, (Color)Colors.Honeydew);
			AssertEx.AssertHasColor(tx.Background, (Color)Colors.AntiqueWhite);
		}

		[TestMethod]
		public void When_Explicit_And_TargetProperty()
		{
			var page = new Test_SetterTarget();
			Assert.AreEqual(page.myButton.Width, 42.0);
			Assert.AreEqual(page.myButton.Height, 42.0);
		}
	}
}
