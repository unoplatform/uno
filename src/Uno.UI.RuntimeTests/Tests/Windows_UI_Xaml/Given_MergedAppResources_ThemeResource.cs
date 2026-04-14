using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_MergedAppResources_ThemeResource
{
	[TestMethod]
	public async Task When_MergedAppResource_ThemeResource_On_Direct_Property_And_Setter()
	{
		using (StyleHelper.UseAppLevelResources(new MergedAppResources_ThemeResource_Test_AppBrushes()))
		using (StyleHelper.UseAppLevelResources(new MergedAppResources_ThemeResource_Test_ButtonStyles()))
		{
			var view = new MergedAppResources_ThemeResource_Test_View
			{
				RequestedTheme = ElementTheme.Dark
			};

			WindowHelper.WindowContent = view;
			await WindowHelper.WaitForLoaded(view);
			await WindowHelper.WaitForIdle();

			var directBorder = (Border)view.FindName("DirectBorder");
			var styledButton = (Button)view.FindName("StyledButton");
			var crossDictButton = (Button)view.FindName("CrossDictStyledButton");

			Assert.IsNotNull(directBorder.Background);
			Assert.IsNotNull(directBorder.BorderBrush);
			Assert.IsInstanceOfType(directBorder.BorderBrush, typeof(LinearGradientBrush));

			Assert.IsNotNull(styledButton.Background);
			Assert.IsNotNull(styledButton.BorderBrush);
			Assert.IsInstanceOfType(styledButton.BorderBrush, typeof(LinearGradientBrush));

			Assert.IsNotNull(crossDictButton.Background);
			Assert.IsNotNull(crossDictButton.BorderBrush);
			Assert.IsInstanceOfType(crossDictButton.BorderBrush, typeof(LinearGradientBrush));
		}
	}

	[TestMethod]
	public async Task When_MergedAppResource_ThemeResource_Tracks_Theme_Change()
	{
		using (StyleHelper.UseAppLevelResources(new MergedAppResources_ThemeResource_Test_AppBrushes()))
		{
			var view = new MergedAppResources_ThemeResource_Test_View
			{
				RequestedTheme = ElementTheme.Light
			};

			WindowHelper.WindowContent = view;
			await WindowHelper.WaitForLoaded(view);
			await WindowHelper.WaitForIdle();

			var directBorder = (Border)view.FindName("DirectBorder");

			var lightBrush = directBorder.Background as SolidColorBrush;
			Assert.IsNotNull(lightBrush);
			Assert.AreEqual(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), lightBrush.Color);

			view.RequestedTheme = ElementTheme.Dark;
			await WindowHelper.WaitForIdle();

			var darkBrush = directBorder.Background as SolidColorBrush;
			Assert.IsNotNull(darkBrush);
			Assert.AreEqual(Color.FromArgb(0xFF, 0x16, 0x18, 0x1A), darkBrush.Color);
		}
	}
}
