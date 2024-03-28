using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ThemeResource
	{
		[TestMethod]
#if WINAPPSDK
		[Ignore("Fails on UWP with 'The parameter is incorrect.'")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_ThemeResource_Binding_In_Template()
		{
			using (StyleHelper.UseAppLevelResources(new App_Level_Resources()))
			{
				var userControl = new When_ThemeResource_Binding_In_Template_UserControl();

				WindowHelper.WindowContent = userControl;
				await WindowHelper.WaitForLoaded(userControl);

				Assert.AreEqual(Colors.Red, GetInnerBackgroundColor(userControl.Inner_ThemeResource_Control_No_Override));
				Assert.AreEqual(Colors.Blue, GetInnerBackgroundColor(userControl.Inner_ThemeResource_Control_With_Override));

				Color GetInnerBackgroundColor(Inner_ThemeResource_Control control)
				{
					var brush = control.ThemeBoundBorder?.Background as SolidColorBrush;
					Assert.IsNotNull(brush);
					return brush.Color;
				}
			}
		}

		[TestMethod]
		public async Task When_Parent_Resource_Override_On_Loaded()
		{
			using (StyleHelper.UseAppLevelResources(new App_Level_Resources()))
			{
				var userControl = new When_Parent_Resource_Override_On_Loaded();

				WindowHelper.WindowContent = userControl;
				await WindowHelper.WaitForLoaded(userControl);

				Assert.AreEqual(Colors.Yellow, (userControl.innerBorder.Background as SolidColorBrush).Color);
			}
		}

#if HAS_UNO // On UWP/WinUI, the Samples app is always in Fluent theme
		[TestMethod]
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_DefaultForeground_Non_Fluent()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_DefaultForeground(Colors.Black, Colors.White);
		}
#endif

		[TestMethod]
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_DefaultForeground_Fluent()
		{
			await When_DefaultForeground(Color.FromArgb(228, 0, 0, 0), Colors.White);
		}

		[TestMethod]
		public async Task When_AppLevel_Resource_CheckBox_Override()
		{
			// Use fluent styles to rely on known Theme Resources
			var SUT = new When_AppLevel_Resource_CheckBox_Override();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var normalRectangle = SUT.cb01.FindName("NormalRectangle") as Rectangle;

			Assert.IsNotNull(normalRectangle);

			// Validation for early ThemeResource resolution in Storyboard timeline
			Assert.AreEqual(Colors.Yellow, normalRectangle.Fill.GetValue(SolidColorBrush.ColorProperty));

			SUT.cb01.IsChecked = true;

			// Validation for late (OnLoaded) ThemeResource resolution in Storyboard timeline
			Assert.AreEqual(Colors.Red, normalRectangle.Fill.GetValue(SolidColorBrush.ColorProperty));
		}

		[TestMethod]
		public async Task When_AppLevel_Resource_SplitButton_Override()
		{
			// Use fluent styles to rely on known Theme Resources
			var SUT = new When_AppLevel_Resource_SplitButton_Override();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var contentPresenter = SUT.sb01.FindName("ContentPresenter") as ContentPresenter;

			Assert.IsNotNull(contentPresenter);

			var color = contentPresenter.Foreground.GetValue(SolidColorBrush.ColorProperty);

			SUT.sb01.IsEnabled = false;

			// Validation for late (OnLoaded) ThemeResource resolution in Setter value
			Assert.AreEqual(Colors.Yellow, contentPresenter.Foreground.GetValue(SolidColorBrush.ColorProperty));
			Assert.AreNotEqual(color, contentPresenter.Foreground.GetValue(SolidColorBrush.ColorProperty));
		}

		[TestMethod]
		public async Task When_ThemeResource_Style_Switch()
		{
			var SUT = new When_ThemeResource_Style_Switch_Page();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var color = ((SolidColorBrush)SUT.TestButton.Background).Color;

			Assert.AreEqual(Colors.Blue, color);

			SUT.TestButton.Style = (Style)SUT.Resources["SecondButtonStyle"];

			await WindowHelper.WaitForIdle();

			color = ((SolidColorBrush)SUT.TestButton.Background).Color;

			Assert.AreEqual(Colors.Red, color);
		}

		[TestMethod]
		public async Task When_Theme_Changed()
		{
			var control = new ThemeResource_Theme_Changing_Override();
			WindowHelper.WindowContent = control;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(Colors.Red, (control.button01.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Red, (control.button02.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Red, (control.button03.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Red, (control.button04.Background as SolidColorBrush)?.Color);

			Assert.AreEqual(Colors.Green, (control.button01_override.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Green, (control.button02_override.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Green, (control.button03_override.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Green, (control.button04_override.Background as SolidColorBrush)?.Color);

			using (ThemeHelper.UseDarkTheme())
			{
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(Colors.DarkRed, (control.button01.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkRed, (control.button02.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkRed, (control.button03.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkRed, (control.button04.Background as SolidColorBrush)?.Color);

				Assert.AreEqual(Colors.DarkGreen, (control.button01_override.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkGreen, (control.button02_override.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkGreen, (control.button03_override.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkGreen, (control.button04_override.Background as SolidColorBrush)?.Color);
			}
		}

		private async Task When_DefaultForeground(Color lightThemeColor, Color darkThemeColor)
		{
			var run = new Run()
			{
				Text = "Hello"
			};

			var textBlock = new TextBlock()
			{
				Inlines = {
					run,
				}
			};

			var button = new Button() { Content = "Test" };
			var bitmapIcon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/Icons/search.png") };
			var contentPresenter = new ContentPresenter() { Content = "Hi" };
			var stackPanel = new StackPanel()
			{
				Children =
				{
					textBlock,
					button,
					bitmapIcon,
					contentPresenter,
				}
			};

			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(stackPanel);

#if !HAS_UNO
			// Due to a bug in WinUI, RequestedTheme needs to be set explicitly here to force the correct
			// foreground color - https://github.com/microsoft/microsoft-ui-xaml/issues/8392.
			stackPanel.RequestedTheme = ElementTheme.Light;
#endif
			// Light theme
			var runForegroundBrush = (SolidColorBrush)run.Foreground;
			var textBlockForegroundBrush = (SolidColorBrush)textBlock.Foreground;
			var buttonForegroundBrush = (SolidColorBrush)button.Foreground;
			var bitmapIconForegroundBrush = (SolidColorBrush)bitmapIcon.Foreground;
			var contentPresenterForegroundBrush = (SolidColorBrush)contentPresenter.Foreground;
			Assert.AreEqual(lightThemeColor, runForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, textBlockForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, buttonForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, bitmapIconForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, contentPresenterForegroundBrush.Color);

			using (ThemeHelper.UseDarkTheme())
			{
#if !HAS_UNO
				// Due to a bug in WinUI, RequestedTheme needs to be set explicitly here to force the correct
				// foreground color - https://github.com/microsoft/microsoft-ui-xaml/issues/8392.
				stackPanel.RequestedTheme = ElementTheme.Dark;
#endif
				runForegroundBrush = (SolidColorBrush)run.Foreground;
				textBlockForegroundBrush = (SolidColorBrush)textBlock.Foreground;
				buttonForegroundBrush = (SolidColorBrush)button.Foreground;
				bitmapIconForegroundBrush = (SolidColorBrush)bitmapIcon.Foreground;
				contentPresenterForegroundBrush = (SolidColorBrush)contentPresenter.Foreground;
				Assert.AreEqual(darkThemeColor, runForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, textBlockForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, buttonForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, bitmapIconForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, contentPresenterForegroundBrush.Color);
			}
		}
	}
}
