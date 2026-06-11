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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml.Shapes;

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
		public void Cleanup()
		{
			if (Window.Current?.Content is FrameworkElement root)
			{
				root.RequestedTheme = ElementTheme.Default;
			}
		}

		[TestMethod]
		public void When_Theme_Changed_ApplicationPageBackground()
		{
			var page = new ThemeResource_Themed_Color_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			Assert.AreEqual(Colors.White, (page.Background as SolidColorBrush).Color);

			using var _ = SwapSystemTheme();

			Assert.AreEqual(Colors.Black, (page.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Theme_Changed_ResourceKey()
		{
			using (UseFluentResources())
			{
				var app = UnitTestsApp.App.EnsureApplication();

				var page = new Test_Page_Other();

				app.HostView.Children.Add(page);

				var textBlock = page.ResourceKeyThemedTextBlock;

				// Dark text
				Assert.IsLessThan(100, ((SolidColorBrush)textBlock.Foreground).Color.R);

				using var _ = SwapSystemTheme();

				// Light text
				Assert.IsGreaterThan(200, ((SolidColorBrush)textBlock.Foreground).Color.R);
			}
		}

		[TestMethod]
		public void When_Theme_Changed_ImplicitStyle()
		{
			using (UseFluentResources())
			{
				var app = UnitTestsApp.App.EnsureApplication();

				var button = new Button() { Content = "Dummy" };

				app.HostView.Children.Add(button);

				// Dark text
				Assert.IsLessThan(100, ((SolidColorBrush)button.Foreground).Color.R);

				using var _ = SwapSystemTheme();

				// Light text
				Assert.IsGreaterThan(200, ((SolidColorBrush)button.Foreground).Color.R);
			}
		}

		[TestMethod]
		public void When_Theme_Changed_LocalValue()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var button = new Button() { Content = "Dummy" };

			button.Foreground = new SolidColorBrush(Colors.Pink);

			app.HostView.Children.Add(button);

			Assert.AreEqual(Colors.Pink, (button.Foreground as SolidColorBrush).Color);

			using var _ = SwapSystemTheme();

			Assert.AreEqual(Colors.Pink, (button.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Theme_Changed_Default_Style_Overridden()
		{
			var page = new ThemeResource_Themed_Color_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			var button = page.TestButton;

			Assert.AreEqual(Colors.Peru, (button.Foreground as SolidColorBrush).Color);

			using var _ = SwapSystemTheme();

			Assert.AreEqual(Colors.Peru, (button.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Themed_Color_Theme_Changed()
		{
			var page = new ThemeResource_Themed_Color_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			Assert.AreEqual(Colors.LightBlue, (page.TestBorder.Background as SolidColorBrush).Color);

			using var _ = SwapSystemTheme();

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

			using var _ = SwapSystemTheme();

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

			using var _ = SwapSystemTheme();

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
			using var _ = SwapSystemTheme();
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
			using var _ = SwapSystemTheme();
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
		private static void WaitForIdle() => await Task.Delay(100);
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
		public void When_System_ThemeResource_Dark()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			try
			{
				using var _ = SwapSystemTheme();

				var control = new Test_Control();
				app.HostView.Children.Add(control);

				Assert.AreEqual(Color.FromArgb(0xDE, 0xFF, 0xFF, 0xFF), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
			}
			finally
			{
				//ensure the light theme is reset
				using var _ = SwapSystemTheme();
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
		public void When_Theme_Changed()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;

			Assert.AreEqual("LocalVisualTreeLight", textLightThemeMarkup);

			using var _ = SwapSystemTheme();
			if (control.Parent == null)
			{
				app.HostView.Children.Add(control); // On UWP the control may have been removed by another test after the async swap
			}
			var textDarkThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;
			Assert.AreEqual("LocalVisualTreeDark", textDarkThemeMarkup); //ThemeResource markup change lookup uses the visual tree (rather than original XAML namescope)
		}

		[TestMethod]
		public void When_Theme_Changed_Static()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;

			Assert.AreEqual("ApplicationLevelLight", textLightStaticMarkup);

			using var _ = SwapSystemTheme();
			var textDarkStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;
			Assert.AreEqual("ApplicationLevelLight", textDarkStaticMarkup); //StaticResource markup doesn't change
		}

		[TestMethod]
		public void When_Theme_Changed_ContentControl()
		{
			var control = new ContentControl() { Content = "Unstyled control" };
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(control);
			AssertEx.AssertHasColor(control.Foreground, Colors.Black);

			using var _ = SwapSystemTheme();
			if (control.Parent == null)
			{
				app.HostView.Children.Add(control); // On UWP the control may have been removed by another test after the async swap
			}

			AssertEx.AssertHasColor(control.Foreground, Colors.White);
		}

		[TestMethod]
		public void When_Theme_Changed_From_Setter()
		{
			var button = new Button() { Content = "Bu'on" };
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(button);
			AssertEx.AssertHasColor(button.Foreground, Colors.Black);

			using var _ = SwapSystemTheme();
			if (button.Parent == null)
			{
				app.HostView.Children.Add(button); // On UWP the control may have been removed by another test after the async swap
			}

			AssertEx.AssertHasColor(button.Foreground, Colors.White);
		}

		[TestMethod]
		public void When_Theme_Changed_From_Setter_Library()
		{
			var page = new Test_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			var myExtControl = page.MyExtControl;
			var textLightFromThemeResource = myExtControl.MyTagThemed1;
			var textLightFromStaticResource = myExtControl.MyTagThemed2;

			Assert.AreEqual("ExtLight", textLightFromThemeResource);
			Assert.AreEqual("ExtLight", textLightFromStaticResource);

			using var _ = SwapSystemTheme();
			if (page.Parent == null)
			{
				app.HostView.Children.Add(page); // On UWP the control may have been removed by another test after the async swap
			}

			var textDarkFromThemeResource = myExtControl.MyTagThemed1;
			var textDarkFromStaticResource = myExtControl.MyTagThemed2;

			Assert.AreEqual("ExtDark", textDarkFromThemeResource);
			Assert.AreEqual("ExtLight", textDarkFromStaticResource);
		}

		[TestMethod]
		public void ThemeResource_When_Setter_Override_From_Visual_Parent()
		{
			var SUT = new ThemeResource_When_Setter_Override_From_Visual_Parent();

			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(SUT);

			var tb = (TextBlock)SUT.FindName("MarkTextBlock");
			Assert.IsNotNull(tb);
			Assert.IsInstanceOfType(tb.Foreground, typeof(SolidColorBrush));
			Assert.AreEqual(Colors.Red, ((SolidColorBrush)tb.Foreground).Color);
		}

		[TestMethod]
		public void ThemeResource_When_Setter_Override_State_From_Visual_Parent()
		{
			var SUT = new ThemeResource_When_Setter_Override_State_From_Visual_Parent();

			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(SUT);

			VisualStateManager.GoToState(SUT.SubjectToggleButton, "Checked", false);

			var tb = (TextBlock)SUT.FindName("MarkTextBlock");
			Assert.IsNotNull(tb);
			Assert.IsInstanceOfType(tb.Foreground, typeof(SolidColorBrush));
			Assert.AreEqual(Colors.Orange, ((SolidColorBrush)tb.Foreground).Color);
		}

		[TestMethod]
		public void When_TemplateBinding_And_VisualState_Setter_ClearValue()
		{
			var SUT = new When_TemplateBinding_And_VisualState_Setter_ClearValue();
			SUT.topLevel.Content = "test";
			SUT.topLevel.ApplyTemplate();

			var inner = SUT.FindName("innerContent") as ContentPresenter;
			Assert.IsNotNull(inner);
			Assert.AreEqual("Default Value", inner.Tag);

			VisualStateManager.GoToState(SUT.topLevel, "NewState", true);
			Assert.AreEqual("42", inner.Tag);

			VisualStateManager.GoToState(SUT.topLevel, "DefaultState", true);
			Assert.AreEqual("Default Value", inner.Tag);

			SUT.topLevel.Tag = "Updated value";
			Assert.AreEqual("Updated value", inner.Tag);
		}

		[TestMethod]
		public void When_StaticResource_And_VisualState_Setter_ClearValue()
		{
			var SUT = new When_StaticResource_And_VisualState_Setter_ClearValue();
			SUT.topLevel.Content = "test";
			SUT.topLevel.ApplyTemplate();

			var inner = SUT.FindName("innerContent") as ContentPresenter;
			Assert.IsNotNull(inner);
			Assert.AreEqual("my static resource", inner.Tag);

			VisualStateManager.GoToState(SUT.topLevel, "NewState", true);
			Assert.AreEqual("42", inner.Tag);

			VisualStateManager.GoToState(SUT.topLevel, "DefaultState", true);
			Assert.AreEqual("my static resource", inner.Tag);

			VisualStateManager.GoToState(SUT.topLevel, "NewState", true);
			Assert.AreEqual("42", inner.Tag);

			VisualStateManager.GoToState(SUT.topLevel, "DefaultState", true);
			Assert.AreEqual("my static resource", inner.Tag);
		}

		/// <summary>
		/// Use Fluent styles for the duration of the test.
		/// </summary>
		private IDisposable UseFluentResources()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			var xcr = new Microsoft.UI.Xaml.Controls.XamlControlsResources();
			app.Resources.MergedDictionaries.Insert(0, xcr);
			return new DisposableAction(() => Application.Current.Resources.MergedDictionaries.Remove(xcr));

		}

#if NETFX_CORE
		private static Task _swapTask;

		private static void GetSwapTask()
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
		public void When_Direct_Assignment_Incompatible()
		{
			var page = new ThemeResource_Direct_Assignment_Incompatible_Page();
			var transform = page.Resources["MyTransform"] as TranslateTransform;

			Assert.AreEqual(115, transform.Y); //Standard double resource
			Assert.AreEqual(490, transform.X); // Resource is actually a Thickness!
		}

		[TestMethod]
		public void When_Theme_Bound_Overwritten_By_Local()
		{
			var page = new ThemeResource_When_Theme_Bound_Overwritten_By_Local_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			TextBlock tb = page.SubjectTextBlock;

			AssertEx.AssertHasColor(tb.Foreground, Colors.Black);

			using var _1 = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.White);

			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.Black);

			tb.Foreground = new SolidColorBrush(Colors.PeachPuff);

			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.PeachPuff);

			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.PeachPuff);
		}

		[TestMethod]
		public void When_VisualState_Setter_Value()
		{
			var page = new ThemeResource_When_VisualState_Setter_Value_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			var tb = ViewExtensions.FindFirstChild<TextBlock>(page.SubjectToggleButton);
			AssertEx.AssertHasColor(tb.Foreground, Colors.Black);
			Assert.AreEqual(DependencyPropertyValuePrecedences.Animations, tb.GetCurrentHighestValuePrecedence(TextBlock.ForegroundProperty));

			using var _1 = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.White);
			Assert.AreEqual(DependencyPropertyValuePrecedences.Animations, tb.GetCurrentHighestValuePrecedence(TextBlock.ForegroundProperty));


			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.Black);
			Assert.AreEqual(DependencyPropertyValuePrecedences.Animations, tb.GetCurrentHighestValuePrecedence(TextBlock.ForegroundProperty));

			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.White);
			Assert.AreEqual(DependencyPropertyValuePrecedences.Animations, tb.GetCurrentHighestValuePrecedence(TextBlock.ForegroundProperty));


			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(tb.Foreground, Colors.Black);
			Assert.AreEqual(DependencyPropertyValuePrecedences.Animations, tb.GetCurrentHighestValuePrecedence(TextBlock.ForegroundProperty));
		}

		[TestMethod]
		public void When_VisualState_Setter_Value_Complex_Path()
		{
			var page = new ThemeResource_When_VisualState_Setter_Value_Complex_Path_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			var ellipse = ViewExtensions.FindFirstChild<Ellipse>(page.SubjectToggleButton);

			AssertEx.AssertHasColor(ellipse.Stroke, Colors.DarkGreen);

			using var _1 = SwapSystemTheme();
			AssertEx.AssertHasColor(ellipse.Stroke, Colors.LightGreen);

			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(ellipse.Stroke, Colors.DarkGreen);

			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(ellipse.Stroke, Colors.LightGreen);

			_ = SwapSystemTheme();
			AssertEx.AssertHasColor(ellipse.Stroke, Colors.DarkGreen);
		}

		internal static IDisposable SwapSystemTheme() => ThemeHelper.SwapSystemTheme();
	}
}
