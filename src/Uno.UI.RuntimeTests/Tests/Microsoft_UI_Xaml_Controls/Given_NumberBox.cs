using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
internal class Given_NumberBox
{
	[TestMethod]
	public async Task When_Fluent_And_Theme_Changed()
	{
		using (StyleHelper.UseFluentStyles())
		{
			var textBox = new NumberBox
			{
				PlaceholderText = "Enter..."
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			var placeholderTextContentPresenter = textBox.FindFirstChild<TextBlock>(tb => tb.Name == "PlaceholderTextContentPresenter");
			Assert.IsNotNull(placeholderTextContentPresenter);

			var lightThemeForeground = TestsColorHelper.ToColor("#9E000000");
			var darkThemeForeground = TestsColorHelper.ToColor("#C5FFFFFF");

			Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);

			using (ThemeHelper.UseDarkTheme())
			{
				Assert.AreEqual(darkThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
			}

			Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
		}
	}
}
