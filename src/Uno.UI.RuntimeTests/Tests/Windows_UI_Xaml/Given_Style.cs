using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24159")]
		public async Task When_Explicit_Style_Uses_Fluent_Default_Value()
		{
			// WinUI resolves a control's built-in style from the framework's generic.xaml, and its
			// {ThemeResource} setters let Application.Resources (i.e. XamlControlsResources) override
			// the resolved value. A ToggleButton carrying an explicit style that doesn't set
			// Background must therefore surface the Fluent ToggleButtonBackground, not the legacy one.
			var expected = Application.Current.Resources["ToggleButtonBackground"] as SolidColorBrush;
			Assert.IsNotNull(expected);

			var button = new ToggleButton
			{
				Content = "Explicit style",
				Style = new Style(typeof(ToggleButton)),
			};

			try
			{
				await UITestHelper.Load(button);

				var actual = button.Background as SolidColorBrush;
				Assert.IsNotNull(actual);
				Assert.AreEqual(expected.Color, actual.Color);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24159")]
		public async Task When_Explicit_Style_Uses_Uwp_Default_Value()
		{
			// Counterpart of When_Explicit_Style_Uses_Fluent_Default_Value: without
			// XamlControlsResources the built-in style must still resolve to the legacy value.
			using var uwpStyles = StyleHelper.UseUwpStyles();

			var expected = Application.Current.Resources["SystemControlBackgroundBaseLowBrush"] as SolidColorBrush;
			Assert.IsNotNull(expected);

			var button = new ToggleButton
			{
				Content = "Explicit style",
				Style = new Style(typeof(ToggleButton)),
			};

			try
			{
				await UITestHelper.Load(button);

				var actual = button.Background as SolidColorBrush;
				Assert.IsNotNull(actual);
				Assert.AreEqual(expected.Color, actual.Color);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24159")]
		public async Task When_Uwp_ToggleButton_Uses_State_Resources()
		{
			using var uwpStyles = StyleHelper.UseUwpStyles();
			var resources = Application.Current.Resources;
			var stateResources = new Dictionary<string, SolidColorBrush>
			{
				["ToggleButtonBackgroundCheckedPressed"] = new(Colors.Red),
				["ToggleButtonForegroundCheckedPressed"] = new(Colors.Green),
				["ToggleButtonBorderBrushCheckedPressed"] = new(Colors.Blue),
				["ToggleButtonBackgroundIndeterminate"] = new(Colors.Yellow),
				["ToggleButtonForegroundIndeterminate"] = new(Colors.Magenta),
				["ToggleButtonBorderBrushIndeterminate"] = new(Colors.Cyan),
			};
			var originalResources = new Dictionary<string, object>();

			foreach (var (key, brush) in stateResources)
			{
				originalResources[key] = resources[key];
				resources[key] = brush;
			}

			var button = new ToggleButton
			{
				Content = "State resources",
				IsThreeState = true,
			};

			try
			{
				await UITestHelper.Load(button);

				var rootGrid = button.GetTemplateChild("RootGrid") as Grid;
				var contentPresenter = button.GetTemplateChild("ContentPresenter") as ContentPresenter;
				Assert.IsNotNull(rootGrid);
				Assert.IsNotNull(contentPresenter);

				Assert.IsTrue(VisualStateManager.GoToState(button, "CheckedPressed", useTransitions: false));
				await TestServices.WindowHelper.WaitForIdle();
				AssertBrush(stateResources["ToggleButtonBackgroundCheckedPressed"], rootGrid.Background);
				AssertBrush(stateResources["ToggleButtonForegroundCheckedPressed"], contentPresenter.Foreground);
				AssertBrush(stateResources["ToggleButtonBorderBrushCheckedPressed"], contentPresenter.BorderBrush);

				Assert.IsTrue(VisualStateManager.GoToState(button, "Indeterminate", useTransitions: false));
				await TestServices.WindowHelper.WaitForIdle();
				AssertBrush(stateResources["ToggleButtonBackgroundIndeterminate"], rootGrid.Background);
				AssertBrush(stateResources["ToggleButtonForegroundIndeterminate"], contentPresenter.Foreground);
				AssertBrush(stateResources["ToggleButtonBorderBrushIndeterminate"], contentPresenter.BorderBrush);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
				foreach (var (key, value) in originalResources)
				{
					resources[key] = value;
				}
			}

			static void AssertBrush(SolidColorBrush expected, Brush actual)
			{
				Assert.IsInstanceOfType<SolidColorBrush>(actual);
				Assert.AreEqual(expected.Color, ((SolidColorBrush)actual).Color);
			}
		}
#endif

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
}
