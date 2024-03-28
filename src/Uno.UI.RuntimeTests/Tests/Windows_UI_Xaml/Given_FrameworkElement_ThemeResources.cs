using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
#if WINAPPSDK
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FrameworkElement_ThemeResources
	{
		[TestMethod]
		public async Task When_Detached_From_Window_While_Theme_Changed()
		{
			var SUT = new Button { Content = "Ye button" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(Color.FromArgb(0xE4, 0, 0, 0), (SUT.Foreground as SolidColorBrush)?.Color);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			using (ThemeHelper.UseDarkTheme())
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(Colors.White, (SUT.Foreground as SolidColorBrush)?.Color);
			}
		}

		[TestMethod]
		public async Task When_Styled_And_Not_Loaded_While_Theme_Changed()
		{
			var buttonUserControl = new ButtonUserControl();

			using (ThemeHelper.UseDarkTheme())
			{
				WindowHelper.WindowContent = buttonUserControl;
				await WindowHelper.WaitForLoaded(buttonUserControl);

				Assert.AreEqual(Colors.White, (buttonUserControl.MyButton.Foreground as SolidColorBrush)?.Color);
			}
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_ComboBox_Theme_Changed()
		{
			var comboBox = new ComboBox() { PlaceholderText = "combo" };
			WindowHelper.WindowContent = comboBox;
			await WindowHelper.WaitForLoaded(comboBox);

			comboBox.ItemsSource = "abcdef".ToArray();

			using (ThemeHelper.UseDarkTheme())
			{
				await OpenAndCheckComboBox(comboBox, Colors.White, Color.FromArgb(255, 44, 44, 44));

			}
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_ComboBox_Theme_Changed_After_First_Open()
		{
			var comboBox = new ComboBox() { PlaceholderText = "combo" };
			WindowHelper.WindowContent = comboBox;
			await WindowHelper.WaitForLoaded(comboBox);

			comboBox.ItemsSource = "abcdef".ToArray();

			await OpenAndCheckComboBox(comboBox, Color.FromArgb(228, 0, 0, 0), Color.FromArgb(255, 249, 249, 249));

			using (ThemeHelper.UseDarkTheme())
			{
				await OpenAndCheckComboBox(comboBox, Colors.White, Color.FromArgb(255, 44, 44, 44));

			}
		}

		private static async Task OpenAndCheckComboBox(ComboBox comboBox, Color expectedForeground, Color expectedBackground)
		{
			try
			{
				comboBox.IsDropDownOpen = true;
				var firstItem = await WindowHelper.WaitForNonNull(() => comboBox.ContainerFromIndex(1) as ComboBoxItem);
				await WindowHelper.WaitForLoaded(firstItem);

				Assert.AreEqual(expectedForeground, (firstItem.Foreground as SolidColorBrush)?.Color);

				var popup = comboBox.FindFirstChild<Popup>();
				var popupBorder = popup.Child as Border;
				Assert.IsNotNull(popupBorder);
				Assert.AreEqual(expectedBackground, (popupBorder.Background as AcrylicBrush)?.FallbackColorWithOpacity);
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				await WindowHelper.WaitForIdle();
			}
		}
	}
}
