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
	public partial class Given_Style
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
	}

	// Repro tests for https://github.com/unoplatform/uno/issues/4066
	public partial class Given_Style
	{
		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4066")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_TargetType_Style_Not_Applied_To_NonTarget_Elements()
		{
			// Issue: A Style with TargetType="Border" in a container's Resources is applied
			// to non-Border elements (like ScrollViewer), overriding their explicitly set properties.
			// Expected: A typed implicit style should ONLY be applied to elements of that type.

			var root = (FrameworkElement)XamlReader.Load(
				@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					   Width='400' Height='300'>
					<Grid.Resources>
						<!-- This Border style should NOT affect the ScrollViewer -->
						<Style TargetType='Border'>
							<Setter Property='Width' Value='350' />
							<Setter Property='Height' Value='200' />
							<Setter Property='Padding' Value='20' />
							<Setter Property='VerticalAlignment' Value='Top' />
						</Style>
					</Grid.Resources>
					<ScrollViewer x:Name='TestScrollViewer'
						Width='400'
						Height='300'
						VerticalAlignment='Stretch'
						Padding='0' />
				</Grid>");

			await UITestHelper.Load(root);
			await UITestHelper.WaitForIdle();

			var scrollViewer = root.FindName("TestScrollViewer") as ScrollViewer;
			Assert.IsNotNull(scrollViewer);

			// ScrollViewer's VerticalAlignment should be Stretch (explicitly set)
			// NOT Top (from Border style)
			Assert.AreEqual(VerticalAlignment.Stretch, scrollViewer.VerticalAlignment,
				$"Expected ScrollViewer.VerticalAlignment to be Stretch (explicitly set), " +
				$"but got {scrollViewer.VerticalAlignment}. " +
				$"The Border-targeted style is leaking into the ScrollViewer.");

			// ScrollViewer's Padding should be 0 (explicitly set), not 20 (from Border style)
			Assert.AreEqual(new Thickness(0), scrollViewer.Padding,
				$"Expected ScrollViewer.Padding to be 0 (explicitly set), " +
				$"but got {scrollViewer.Padding}. " +
				$"The Border-targeted style is leaking into the ScrollViewer.");

			// ScrollViewer's Width should be 400 (explicitly set), not 350 (from Border style)
			Assert.AreEqual(400d, scrollViewer.Width, 1d,
				$"Expected ScrollViewer.Width to be 400 (explicitly set), " +
				$"but got {scrollViewer.Width}. " +
				$"The Border-targeted style is leaking into the ScrollViewer.");
		}
	}
}
