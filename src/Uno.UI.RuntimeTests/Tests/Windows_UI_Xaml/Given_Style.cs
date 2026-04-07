using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using static Uno.UI.Extensions.ViewExtensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	// The attribute is required when running WinUI. See:
	// https://github.com/microsoft/microsoft-ui-xaml/issues/4723#issuecomment-812753123
	[Bindable]
	public sealed partial class ThrowingElement : FrameworkElement
	{
		public ThrowingElement() => throw new Exception("Inner exception");
	}

	[TestClass]
	public class Given_Style
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_StyleFailsToApply()
		{
			var controlTemplate = (ControlTemplate)XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
								 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
								 xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml">
					<local:ThrowingElement />
				</ControlTemplate>
				""");

			var style = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.TemplateProperty, controlTemplate)
				}
			};

			// This shouldn't throw.
			_ = new ContentControl() { Style = style };
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15460")]
#if __ANDROID__
		[Ignore("Doesn't pass in CI on Android")]
#endif
		public async Task When_ImplicitStyle()
		{
			var implicitStyle = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
				},
				TargetType = typeof(ContentControl),
			};

			var explicitStyle = new Style()
			{
				TargetType = typeof(ContentControl),
			};

			var cc = new ContentControl() { Width = 100, Height = 100 };

			// On Android and iOS, ContentControl fails to load if it doesn't have content.
			cc.Content = new Border() { Width = 100, Height = 100 };

			Assert.AreEqual(HorizontalAlignment.Center, cc.HorizontalContentAlignment);

			cc.Resources.Add(typeof(ContentControl), implicitStyle);
			await UITestHelper.Load(cc);

			Assert.AreEqual(HorizontalAlignment.Stretch, cc.HorizontalContentAlignment);

			cc.Style = explicitStyle;

			Assert.AreEqual(HorizontalAlignment.Left, cc.HorizontalContentAlignment);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Style_Flows_To_Popup()
		{
			var page = new StyleFlowToPopup();
			TestServices.WindowHelper.WindowContent = page;
			await UITestHelper.Load(page);

			var foreground = (SolidColorBrush)page.GridTextBlock.Foreground;
			Assert.AreEqual(Microsoft.UI.Colors.Red, foreground.Color);

			page.ShowPopup();

			await TestServices.WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count > 0);

			var popupForeground = (SolidColorBrush)page.PopupTextBlock.Foreground;
			Assert.AreEqual(Microsoft.UI.Colors.Red, popupForeground.Color);
		}

		// Repro tests for https://github.com/unoplatform/uno/issues/3443
		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3443")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_TextBlock_ImplicitStyle_Not_Applied_Inside_DataTemplate()
		{
			// Issue: Implicit styles for TextBlock are incorrectly applied inside DataTemplates.
			// Expected: Only elements inheriting from Control should have implicit styles applied
			// inside a DataTemplate. TextBlock (inherits from UIElement) should NOT get them.
			// On WinUI: TextBlock inside DataTemplate is black (no implicit style), outside is green.
			// Bug: On Uno, both are green (implicit style leaks into DataTemplate).

			var greenBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
			var defaultColor = new TextBlock().Foreground is SolidColorBrush tb ? tb.Color : Microsoft.UI.Colors.Black;

			// Create a container with an implicit TextBlock style (green foreground)
			var root = (FrameworkElement)XamlReader.Load(
				@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					   Width='300' Height='100'>
					<Grid.Resources>
						<Style TargetType='TextBlock'>
							<Setter Property='Foreground' Value='Green' />
						</Style>
					</Grid.Resources>
					<StackPanel Orientation='Horizontal'>
						<!-- TextBlock outside DataTemplate — should get implicit style (green) -->
						<TextBlock x:Name='OutsideTextBlock' Text='Outside' />
						<!-- ContentControl using DataTemplate — TextBlock inside should NOT get implicit style -->
						<ContentControl>
							<ContentControl.ContentTemplate>
								<DataTemplate>
									<TextBlock x:Name='InsideTextBlock' Text='Inside' />
								</DataTemplate>
							</ContentControl.ContentTemplate>
							<ContentControl.Content>
								<x:String>test</x:String>
							</ContentControl.Content>
						</ContentControl>
					</StackPanel>
				</Grid>");

			await UITestHelper.Load(root);
			await UITestHelper.WaitForIdle();

			var outsideTextBlock = root.FindName("OutsideTextBlock") as TextBlock;
			Assert.IsNotNull(outsideTextBlock, "OutsideTextBlock should be found.");

			// Outside TextBlock SHOULD get the implicit style (green)
			var outsideForeground = outsideTextBlock.Foreground as SolidColorBrush;
			Assert.IsNotNull(outsideForeground);
			Assert.AreEqual(Microsoft.UI.Colors.Green, outsideForeground.Color,
				$"Outside TextBlock should have green foreground from implicit style, but got {outsideForeground.Color}.");

			// Find the inside TextBlock via visual tree
			var contentControl = root.FindFirstDescendant<ContentControl>();
			Assert.IsNotNull(contentControl, "ContentControl should be found.");

			var insideTextBlock = contentControl.FindFirstDescendant<TextBlock>();
			Assert.IsNotNull(insideTextBlock, "InsideTextBlock should be found inside DataTemplate.");

			// Inside TextBlock should NOT get the implicit style (should be default, not green)
			var insideForeground = insideTextBlock.Foreground as SolidColorBrush;
			Assert.IsNotNull(insideForeground);
			Assert.AreNotEqual(Microsoft.UI.Colors.Green, insideForeground.Color,
				$"Inside DataTemplate TextBlock should NOT have green foreground (implicit style should not apply inside DataTemplate), " +
				$"but got {insideForeground.Color}. This confirms implicit styles leak into DataTemplates.");
		}
	}
}
