using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Helpers;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ThemeResource
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestCleanup]
		public async Task Cleanup()
		{
			if (Window.Current?.Content is FrameworkElement root)
			{
				root.RequestedTheme = ElementTheme.Default;
			}

			if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_ApplicationPageBackground()
		{

			var page = new ThemeResource_Themed_Color_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			Assert.AreEqual(Colors.White, (page.Background as SolidColorBrush).Color);

			await SwapSystemTheme();

			Assert.AreEqual(Colors.Black, (page.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public async Task When_Theme_Changed_Default_Style_Overridden()
		{
			var page = new ThemeResource_Themed_Color_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			var button = page.TestButton;

			Assert.AreEqual(Colors.Peru, (button.Foreground as SolidColorBrush).Color);

			await SwapSystemTheme();

			Assert.AreEqual(Colors.Peru, (button.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public async Task When_Themed_Color_Theme_Changed()
		{
			var page = new ThemeResource_Themed_Color_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			Assert.AreEqual(Colors.LightBlue, (page.TestBorder.Background as SolidColorBrush).Color);

			await SwapSystemTheme();

			Assert.AreEqual(Colors.DarkBlue, (page.TestBorder.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Element_Theme_Changed()
		{
			var page = new ThemeResource_Themed_Color_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			Assert.AreEqual(Colors.LightBlue, (page.TestBorder.Background as SolidColorBrush).Color);

			var root = Window.Current.Content as FrameworkElement;

			Assert.IsNotNull(root);

			root.RequestedTheme = ElementTheme.Dark;
			Assert.AreEqual(Colors.DarkBlue, (page.TestBorder.Background as SolidColorBrush).Color);

			root.RequestedTheme = ElementTheme.Light;
			Assert.AreEqual(Colors.LightBlue, (page.TestBorder.Background as SolidColorBrush).Color);

			root.RequestedTheme = ElementTheme.Dark;
			Assert.AreEqual(Colors.DarkBlue, (page.TestBorder.Background as SolidColorBrush).Color);

			root.RequestedTheme = ElementTheme.Default;
			Assert.AreEqual(Colors.LightBlue, (page.TestBorder.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public async Task When_Visual_States_Keyframe_Theme_Changed_Reapplied()
		{
			var page = new ThemeResource_In_Visual_States_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			await WaitForIdle();

			var control = page.VisualStatesTestControl;

			await GoTo("ActiveMidground");

			Assert.AreEqual(Colors.DarkGreen, (control.InnerMyControl.Midground as SolidColorBrush).Color);

			await GoTo("NormalMidground");

			await SwapSystemTheme();

			await GoTo("ActiveMidground");

			Assert.AreEqual(Colors.LightGreen, (control.InnerMyControl.Midground as SolidColorBrush).Color);

			async Task GoTo(string stateName)
			{
				var goToResult = VisualStateManager.GoToState(control, stateName, useTransitions: false);
				Assert.IsTrue(goToResult);
				await WaitForIdle();
			}
		}

		[TestMethod]
		[Ignore("DoubleAnimation not supported by Uno.NET461")]
		public async Task When_Visual_States_DoubleAnimation_Theme_Changed_Reapplied()
		{
			var page = new ThemeResource_In_Visual_States_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			await WaitForIdle();

			var control = page.VisualStatesTestControl;

			await GoTo("HighArduousness");

			Assert.AreEqual(29, control.InnerMyControl.Arduousness);

			await GoTo("NormalArduousness");

			await SwapSystemTheme();

			await GoTo("HighArduousness");

			Assert.AreEqual(47, control.InnerMyControl.Arduousness);

			async Task GoTo(string stateName)
			{
				var goToResult = VisualStateManager.GoToState(control, stateName, useTransitions: false);
				Assert.IsTrue(goToResult);
				await WaitForIdle();
			}
		}

		[TestMethod]
		[Ignore("To support changing an already-set value when system theme changes, we need to resolve the DP pointed to by the BindingPath in Setter.ApplyValue(). (Which can be done, but hasn't.)")]
		public async Task When_ThemeResource_In_Visual_States_Setter_Theme_Changed()
		{
			var page = new ThemeResource_In_Visual_States_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			await WaitForIdle();

			var control = page.VisualStatesTestControl;

			if (page.Parent == null)
			{
				app.HostView.Children.Add(page); // On UWP the control may have been removed by another test after the async pause
			}

			GoTo("HighPilleability");
			await WaitForIdle();
			var lightPilleability = control.InnerMyControl.Pilleability;
			Assert.AreEqual(29, lightPilleability);
			await SwapSystemTheme();
			var darkPilleability = control.InnerMyControl.Pilleability;
			Assert.AreEqual(47, darkPilleability);

			void GoTo(string stateName)
			{
				var goToResult = VisualStateManager.GoToState(control, stateName, useTransitions: false);
				Assert.IsTrue(goToResult);
			}
		}

		[TestMethod]
		public async Task When_ThemeResource_In_Visual_States_Setter_Theme_Changed_Reapplied()
		{
			var page = new ThemeResource_In_Visual_States_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			await WaitForIdle();

			var control = page.VisualStatesTestControl;

			if (page.Parent == null)
			{
				app.HostView.Children.Add(page); // On UWP the control may have been removed by another test after the async pause
			}

			await GoTo("HighPilleability");
			var lightPilleability = control.InnerMyControl.Pilleability;
			Assert.AreEqual(29, lightPilleability);
			await GoTo("NormalPilleability");
			Assert.AreEqual(0, control.InnerMyControl.Pilleability);
			await SwapSystemTheme();
			await GoTo("HighPilleability");
			var darkPilleability = control.InnerMyControl.Pilleability;
			Assert.AreEqual(47, darkPilleability);

			async Task GoTo(string stateName)
			{
				var goToResult = VisualStateManager.GoToState(control, stateName, useTransitions: false);
				Assert.IsTrue(goToResult);
				await WaitForIdle();
			}
		}

#if NETFX_CORE
		private static async Task WaitForIdle() => await Task.Delay(100);
#else
		private static Task WaitForIdle() => Task.CompletedTask;
#endif

		[TestMethod]
		public void When_System_ThemeResource_Light()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Color.FromArgb(0xDE, 0x00, 0x00, 0x00), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public async Task When_System_ThemeResource_Dark()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			try
			{
				await SwapSystemTheme();

				var control = new Test_Control();
				app.HostView.Children.Add(control);

				Assert.AreEqual(Color.FromArgb(0xDE, 0xFF, 0xFF, 0xFF), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
			}
			finally
			{
				//ensure the light theme is reset
				await SwapSystemTheme();
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

		[TestMethod]
		public void When_Inherited_In_Template_Applied()
		{
			var control = new Test_Control();

			control.InlineTemplateControl.ApplyTemplate();
			control.TemplateFromResourceControl.ApplyTemplate();

			control.ApplyTemplate();

			var text2InlineBefore = control.InlineTemplateControl.TextBlock2.Text;
			Assert.AreEqual("LocalVisualTree", text2InlineBefore);
			var text2ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock2.Text;
			Assert.AreEqual("OuterVisualTree", text2ResourceTemplateBefore);

			var text4InlineBefore = control.InlineTemplateControl.TextBlock4.Text;
			Assert.AreEqual("ApplicationLevel", text4InlineBefore);
			var text4ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock4.Text;
			Assert.AreEqual("ApplicationLevel", text4ResourceTemplateBefore);
		}

		[TestMethod]
		public void When_Inherited_In_Template_Applied_XAML_Scope()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			control.InlineTemplateControl.ApplyTemplate();
			control.TemplateFromResourceControl.ApplyTemplate();

			var text2InlineBefore = control.InlineTemplateControl.TextBlock2.Text;
			var text2ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock2.Text;

			var text4InlineBefore = control.InlineTemplateControl.TextBlock4.Text;
			var text4ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock4.Text;

			app.HostView.Children.Add(control);

			var text2InlineAfter = control.InlineTemplateControl.TextBlock2.Text;
			var text2ResourceTemplateAfter = control.TemplateFromResourceControl.TextBlock2.Text;

			Assert.AreEqual("LocalVisualTree", text2InlineBefore);
			Assert.AreEqual("OuterVisualTree", text2ResourceTemplateBefore);

			Assert.AreEqual("ApplicationLevel", text4InlineBefore);
			Assert.AreEqual("ApplicationLevel", text4ResourceTemplateBefore);

			Assert.AreEqual("LocalVisualTree", text2InlineAfter);
			Assert.AreEqual("LocalVisualTree", text2ResourceTemplateAfter);
		}

		[TestMethod]
		public async Task When_Theme_Changed()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;

			Assert.AreEqual("LocalVisualTreeLight", textLightThemeMarkup);

			try
			{
				if (await SwapSystemTheme())
				{
					if (control.Parent == null)
					{
						app.HostView.Children.Add(control); // On UWP the control may have been removed by another test after the async swap
					}
					var textDarkThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;
					Assert.AreEqual("LocalVisualTreeDark", textDarkThemeMarkup); //ThemeResource markup change lookup uses the visual tree (rather than original XAML namescope)
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_Static()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;

			Assert.AreEqual("ApplicationLevelLight", textLightStaticMarkup);

			try
			{
				if (await SwapSystemTheme())
				{
					var textDarkStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;
					Assert.AreEqual("ApplicationLevelLight", textDarkStaticMarkup); //StaticResource markup doesn't change
					;
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_ContentControl()
		{
			var control = new ContentControl() { Content = "Unstyled control" };
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(control);
			AssertEx.AssertHasColor(control.Foreground, Colors.Black);

			try
			{
				if (await SwapSystemTheme())
				{
					if (control.Parent == null)
					{
						app.HostView.Children.Add(control); // On UWP the control may have been removed by another test after the async swap
					}

					AssertEx.AssertHasColor(control.Foreground, Colors.White);
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_From_Setter()
		{
			var button = new Button() { Content = "Bu'on" };
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(button);
			AssertEx.AssertHasColor(button.Foreground, Colors.Black);

			try
			{
				if (await SwapSystemTheme())
				{
					if (button.Parent == null)
					{
						app.HostView.Children.Add(button); // On UWP the control may have been removed by another test after the async swap
					}

					AssertEx.AssertHasColor(button.Foreground, Colors.White);
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_From_Setter_Library()
		{
			var page = new Test_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			var myExtControl = page.MyExtControl;
			var textLightFromThemeResource = myExtControl.MyTagThemed1;
			var textLightFromStaticResource = myExtControl.MyTagThemed2;

			Assert.AreEqual("ExtLight", textLightFromThemeResource);
			Assert.AreEqual("ExtLight", textLightFromStaticResource);

			try
			{
				if (await SwapSystemTheme())
				{
					if (page.Parent == null)
					{
						app.HostView.Children.Add(page); // On UWP the control may have been removed by another test after the async swap
					}

					var textDarkFromThemeResource = myExtControl.MyTagThemed1;
					var textDarkFromStaticResource = myExtControl.MyTagThemed2;

					Assert.AreEqual("ExtDark", textDarkFromThemeResource);
					Assert.AreEqual("ExtLight", textDarkFromStaticResource);
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

#if NETFX_CORE
		private static Task _swapTask;

		private static async Task GetSwapTask()
		{
			await Task.Delay(800);
			var content = new StackPanel();
			content.Children.Add(new TextBlock { Text = "Set default app mode as 'dark' in settings" });
			content.Children.Add(new HyperlinkButton { Content = "Go to settings", NavigateUri = new Uri("ms-settings:personalization-colors") });
			var dialog = new ContentDialog { Content = content, CloseButtonText = "Done" };
			await dialog.ShowAsync();
		}
#endif

		[TestMethod]
		public async Task When_Direct_Assignment_Incompatible()
		{
			var page = new ThemeResource_Direct_Assignment_Incompatible_Page();
			var transform = page.Resources["MyTransform"] as TranslateTransform;

			Assert.AreEqual(115, transform.Y); //Standard double resource
			Assert.AreEqual(490, transform.X); // Resource is actually a Thickness!
		}

		private static async Task<bool> SwapSystemTheme()
		{
			var currentTheme = Application.Current.RequestedTheme;
			var targetTheme = currentTheme == ApplicationTheme.Light ?
				ApplicationTheme.Dark :
				ApplicationTheme.Light;
#if NETFX_CORE
			if (!UnitTestsApp.App.EnableInteractiveTests || targetTheme == ApplicationTheme.Light)
			{
				return false;
			}

			_swapTask = _swapTask ?? GetSwapTask();

			await _swapTask;
#else
			Application.Current.SetExplicitRequestedTheme(targetTheme);
#endif
			Assert.AreEqual(targetTheme, Application.Current.RequestedTheme);
			return true;
		}
	}
}
