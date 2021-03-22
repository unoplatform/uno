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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
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
				var textBox = new MyTextBox
				{
					PlaceholderText = "Enter..."
				};

				WindowHelper.WindowContent = textBox;
				await WindowHelper.WaitForLoaded(textBox);

				Assert.AreEqual(Colors.Black, (textBox.PlaceholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);

				using (ThemeHelper.UseDarkTheme())
				{
					Assert.AreEqual(Colors.White, (textBox.PlaceholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
				}

				Assert.AreEqual(Colors.Black, (textBox.PlaceholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
			}
		}
	}
}
