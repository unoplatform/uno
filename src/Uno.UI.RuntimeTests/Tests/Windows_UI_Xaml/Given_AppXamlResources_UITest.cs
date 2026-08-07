using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_AppXamlResources_UITest
{
	// Mirrors SamplesApp's App.xaml GlobalThemeResource_Test01 / GlobalStaticResource_Test01: a
	// ThemeResource with no local override, and a flat StaticResource, both declared only at the
	// Application level and consumed from a freshly-loaded subtree.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Bound_To_GlobalThemedResources()
	{
		using var _ = ThemeHelper.UseApplicationLightTheme();

		var appResources = new ResourceDictionary();
		appResources.ThemeDictionaries["Light"] = new ResourceDictionary { ["AppXamlResourcesTest_ThemeBrush"] = new SolidColorBrush(Colors.Yellow) };
		appResources.ThemeDictionaries["Dark"] = new ResourceDictionary { ["AppXamlResourcesTest_ThemeBrush"] = new SolidColorBrush(Colors.Blue) };
		appResources["AppXamlResourcesTest_StaticBrush"] = new SolidColorBrush(Colors.Purple);

		using (StyleHelper.UseAppLevelResources(appResources))
		{
			var root = (StackPanel)XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
							HorizontalAlignment="Left">
					<Border x:Name="ThemedBorder" Width="100" Height="30" Background="{ThemeResource AppXamlResourcesTest_ThemeBrush}" />
					<Border x:Name="StaticBorder" Width="100" Height="30" Background="{StaticResource AppXamlResourcesTest_StaticBrush}" />
				</StackPanel>
				""");

			try
			{
				await UITestHelper.Load(root);

				var themedBorder = (Border)root.FindName("ThemedBorder");
				var staticBorder = (Border)root.FindName("StaticBorder");

				Assert.AreEqual(Colors.Yellow, ((SolidColorBrush)themedBorder.Background).Color);
				Assert.AreEqual(Colors.Purple, ((SolidColorBrush)staticBorder.Background).Color);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
