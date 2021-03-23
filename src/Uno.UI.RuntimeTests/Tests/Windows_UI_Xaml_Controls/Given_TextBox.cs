using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_TextBox
	{
#if __ANDROID__
		[TestMethod]
		public void When_InputScope_Null_And_ImeOptions()
		{
			var tb = new TextBox();
			tb.InputScope = null;
#if !MONOANDROID80
			tb.ImeOptions = Android.Views.InputMethods.ImeAction.Search;
#endif
		}
#endif


		[TestMethod]
		public async Task When_Fluent_And_Theme_Changed()
		{
			using (StyleHelper.UseFluentStyles())
			{
				var textBox = new TextBox
				{
					PlaceholderText = "Enter..."
				};

				WindowHelper.WindowContent = textBox;
				await WindowHelper.WaitForLoaded(textBox);

				var placeholderTextContentPresenter = textBox.FindFirstChild<TextBlock>(tb => tb.Name == "PlaceholderTextContentPresenter");
				Assert.IsNotNull(placeholderTextContentPresenter);

				var lightThemeForeground = TestsColorHelper.ToColor("#99000000");
				var darkThemeForeground = TestsColorHelper.ToColor("#99FFFFFF");

				Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);

				using (ThemeHelper.UseDarkTheme())
				{
					Assert.AreEqual(darkThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
				}

				Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
			}
		}
	}
}
