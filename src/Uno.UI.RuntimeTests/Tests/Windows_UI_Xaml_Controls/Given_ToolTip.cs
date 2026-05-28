using System;
using Windows.UI;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Toolkit.DevTools.Input;
using Uno.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ToolTip
	{
		[TestMethod]
		public async Task When_DataContext_Set_On_ToolTip_Owner()
		{
			try
			{
				var textBlock = new TextBlock();
				var SUT = new ToolTip();
				ToolTipService.SetToolTip(textBlock, SUT);
				var stackPanel = new StackPanel
				{
					Children =
					{
						textBlock,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", textBlock.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				SUT.IsOpen = true;

				stackPanel.DataContext = "DataContext2";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", textBlock.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		public async Task When_ToggleButton_DataContext_Set_On_ToolTip_Owner_After()
		{
			try
			{
				var toggleButton = new ToggleButton();

				var textBlock = new TextBlock();
				textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new(".") });

				var SUT = new ToolTip() { Content = textBlock };
				ToolTipService.SetToolTip(toggleButton, SUT);

				var stackPanel = new StackPanel
				{
					Children =
					{
						toggleButton,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", toggleButton.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				SUT.IsOpen = true;

				stackPanel.DataContext = "DataContext2";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", toggleButton.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
				Assert.AreEqual("DataContext2", textBlock.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		public async Task When_ToggleButton_DataContext_Set_On_ToolTip_Owner_Before()
		{
			try
			{
				var toggleButton = new ToggleButton();

				var textBlock = new TextBlock();
				textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new(".") });

				var SUT = new ToolTip() { Content = textBlock };
				ToolTipService.SetToolTip(toggleButton, SUT);

				var stackPanel = new StackPanel
				{
					Children =
					{
						toggleButton,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", toggleButton.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				// Set the datacontext before opening
				stackPanel.DataContext = "DataContext2";

				SUT.IsOpen = true;

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", toggleButton.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
				Assert.AreEqual("DataContext2", textBlock.DataContext);

				stackPanel.DataContext = "DataContext3";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext3", toggleButton.DataContext);
				Assert.AreEqual("DataContext3", SUT.DataContext);
				Assert.AreEqual("DataContext3", textBlock.DataContext);

				SUT.IsOpen = false;

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext4";

				SUT.IsOpen = true;

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext4", toggleButton.DataContext);
				Assert.AreEqual("DataContext4", SUT.DataContext);
				Assert.AreEqual("DataContext4", textBlock.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		public async Task When_ToggleButton_DataContext_Set_On_ToolTip_Owner_Nested()
		{
			try
			{
				var toggleButton = new ToggleButton();

				var textBlock = new TextBlock();
				textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new(".") });

				var innerStackPanel = new StackPanel();
				innerStackPanel.Children.Add(textBlock);

				var SUT = new ToolTip() { Content = innerStackPanel };
				ToolTipService.SetToolTip(toggleButton, SUT);

				var stackPanel = new StackPanel
				{
					Children =
					{
						toggleButton,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				SUT.DataContextChanged += (s, e) =>
				{
				};

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", toggleButton.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				SUT.IsOpen = true;

				stackPanel.DataContext = "DataContext2";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", toggleButton.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
				Assert.AreEqual("DataContext2", textBlock.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

#if !__APPLE_UIKIT__ // Disabled due to #10791
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public Task When_Switch_Theme_Fluent() => When_Switch_Theme_Inner(brush => (brush as AcrylicBrush).TintColor);

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Switch_Theme_Uwp()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_Switch_Theme_Inner(brush => (brush as SolidColorBrush).Color);
		}

		private async Task When_Switch_Theme_Inner(Func<Brush, Color> backgroundColorGetter)
		{
			try
			{
				var textBlock = new TextBlock() { Text = "Test" };
				var SUT = new ToolTip()
				{
					Content = "I'm a ToolTip!"
				};
				ToolTipService.SetToolTip(textBlock, SUT);
				var stackPanel = new StackPanel
				{
					Children =
					{
						textBlock,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				await Task.Delay(1000);
				await TestServices.WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(textBlock.XamlRoot);
				var popup = popups[0];
				var toolTipChild = popup.Child as ToolTip;
				Assert.AreEqual(SUT, toolTipChild);
				var color = backgroundColorGetter(toolTipChild.Background);
				Assert.IsTrue(color.R > 100 && color.G > 100 && color.B > 100);

				using var _ = ThemeHelper.UseDarkTheme();

				await TestServices.WindowHelper.WaitForIdle();
				color = backgroundColorGetter(toolTipChild.Background);
				Assert.IsTrue(color.R < 100 && color.G < 100 && color.B < 100);
			}
			finally
			{
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ToolTip_Dismissed_On_PointerCanceled()
		{
			var border = new Border() { Margin = new Thickness(30), Width = 30, Height = 30, Background = new SolidColorBrush(Colors.Red) };
			var sv = new ScrollViewer
			{
				Content = new StackPanel
				{
					Height = 1000,
					Children =
					{
						border.Apply(tb => ToolTipService.SetToolTip(tb, new ToolTip { Content = "Simple ToolTip 1" }))
					}
				}
			};

			await UITestHelper.Load(sv);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(border.GetAbsoluteBoundsRect().GetCenter());
			await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 300));
			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(border.XamlRoot));

			finger.MoveBy(0, -50);
			await UITestHelper.WaitForIdle();
			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(border.XamlRoot));
		}
#endif

		[TestMethod]
		public async Task When_ToolTip_Popup_XamlRoot()
		{
			var toolTip = new ToolTip();
			try
			{
				var host = new Button() { Content = "Asd" };
				toolTip.Content = new Button() { Content = "Test" };

				TestServices.WindowHelper.WindowContent = host;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForLoaded(host);

				ToolTipService.SetToolTip(host, toolTip);
				toolTip.IsOpen = true;

				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(host.XamlRoot, toolTip.XamlRoot);
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].Child.XamlRoot);
			}
			finally
			{
				toolTip.IsOpen = false;
			}
		}
#if HAS_UNO
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		public async Task When_Programmatic_Focus_Does_Not_Show_ToolTip_At_Pointer_Position()
		{
			var button = new Button
			{
				Content = "Button with tooltip",
				Width = 200,
				Height = 50,
			};
			var tooltip = new ToolTip
			{
				Content = "ToolTip content",
			};
			ToolTipService.SetToolTip(button, tooltip);

			// A separate element to tap on, positioned far from the button.
			var tapTarget = new Border
			{
				Width = 200,
				Height = 200,
				Background = new SolidColorBrush(Colors.Transparent),
			};

			var stackPanel = new StackPanel
			{
				Spacing = 100,
				Children = { button, tapTarget },
			};

			await UITestHelper.Load(stackPanel);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// Tap the far-away target to set the last pointer position away from the button.
			var tapTargetCenter = tapTarget.GetAbsoluteBoundsRect().GetCenter();
			mouse.Press(tapTargetCenter);
			await UITestHelper.WaitForIdle();
			mouse.Release();
			await UITestHelper.WaitForIdle();

			// Programmatically focus the button - this should NOT open the tooltip.
			button.Focus(FocusState.Programmatic);
			await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 500));
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(tooltip.IsOpen, "ToolTip should not open on programmatic focus.");
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		public async Task When_Keyboard_Focus_Shows_ToolTip_Above_Button()
		{
			try
			{
				var button = new Button
				{
					Content = "Button with tooltip",
					Width = 200,
					Height = 50,
				};
				var tooltip = new ToolTip
				{
					Content = "ToolTip content",
				};
				ToolTipService.SetToolTip(button, tooltip);

				// A separate focusable element placed far below the button.
				var tapTarget = new Button
				{
					Content = "Tap here first",
					Width = 200,
					Height = 50,
				};

				var stackPanel = new StackPanel
				{
					Spacing = 300,
					Children = { button, tapTarget },
				};

				await UITestHelper.Load(stackPanel);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				// Click the far-away target to set the last pointer position away from the button.
				var tapTargetCenter = tapTarget.GetAbsoluteBoundsRect().GetCenter();
				mouse.Press(tapTargetCenter);
				await UITestHelper.WaitForIdle();
				mouse.Release();
				await UITestHelper.WaitForIdle();

				// Tab from tapTarget to the button (Shift+Tab since button is before tapTarget).
				await TestServices.KeyboardHelper.ShiftTab();
				await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 500));
				await UITestHelper.WaitForIdle();

				Assert.IsTrue(tooltip.IsOpen, "ToolTip should open on keyboard focus.");

				var buttonBounds = button.GetAbsoluteBoundsRect();
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
				Assert.IsTrue(popups.Count > 0, "Expected at least one open popup.");

				var tooltipPopup = popups[0];

				// The tooltip should be positioned near the button (above it), not near the tap target.
				// With keyboard input mode, placement falls back to Top, so the popup's vertical
				// offset should be near or above the button's top edge, not near tapTargetCenter.Y.
				Assert.IsTrue(
					tooltipPopup.VerticalOffset < tapTargetCenter.Y - 50,
					$"ToolTip popup should be positioned above the button (near Y={buttonBounds.Top}), " +
					$"not near the tap position (Y={tapTargetCenter.Y}). Actual VerticalOffset={tooltipPopup.VerticalOffset}");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
			}
		}
#endif

#if HAS_UNO
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Currently fails on Android and iOS")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		public async Task When_ToolTip_Owner_Clicked()
		{
			Button button = new Button()
			{
				Content = "Click when tooltip is visible",
			};
			ToolTip tooltip = new ToolTip
			{
				Content = "Tooltip should disappear when button is clicked!",
			};
			ToolTipService.SetToolTip(button, tooltip);
			await UITestHelper.Load(button);
			tooltip.IsOpen = true;
			await UITestHelper.WaitForIdle();
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			mouse.Press(button.GetAbsoluteBoundsRect().GetCenter());
			await UITestHelper.WaitForIdle();
			mouse.Release();
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(tooltip.IsOpen);
		}
#endif

		// ---------------------------------------------------------------------------
		// {ThemeResource} inheritance for popup-hosted (ToolTip) content — kahua #484.
		//
		// Verifies that a {ThemeResource} used inside ToolTip (popup-hosted) content resolves
		// against the content's own inherited ActualTheme — not the ambient/application theme.
		//
		// A label's foreground is a {ThemeResource} whose theme-keyed brush is declared in the
		// surrounding view's themed ResourceDictionary (Light/Dark sub-dicts). When the label is
		// shown in a ToolTip from a Light subtree under a Dark application, the label's ActualTheme
		// is correctly Light, but the {ThemeResource} can wrongly resolve the Dark sub-dictionary
		// value — so the label renders with the wrong theme's color (e.g. a light-on-light
		// foreground that is invisible).
		//
		// The OS-vs-app mismatch is reproduced on Uno by pinning the application theme to Dark
		// (ThemeHelper.UseApplicationDarkTheme, #if HAS_UNO — it relies on the Uno-internal
		// SetExplicitRequestedTheme) and placing a host that pins RequestedTheme=Light. On native WinUI
		// the app-theme pin is unavailable, so the test runs as a Light-host baseline confirming the
		// WinUI-correct value (Green); WinUI never exhibits the regression, so it still validates the
		// behavior the Uno fix must match. The host's ToolTip presents a
		// TextBlock whose Foreground is {ThemeResource LabelForegroundBrush}, declared inline in the
		// same XAML so it parses inside the host's resource scope (a standalone XamlReader.Load of a
		// {ThemeResource} fragment throws on WinUI). ToolTip content is hosted in the PopupRoot,
		// reparented out of the owner's scope.
		//
		// WinUI-correct behavior: the ToolTip label's {ThemeResource} resolves against the owner's
		// inherited theme (Light), so the brush evaluates to the Light sentinel (Green) — even though
		// the application theme is Dark. Uno regression: the popup-presented label resolves the
		// {ThemeResource} against the global/application active theme (Dark), evaluating to the Dark
		// sentinel (Red), despite the label's ActualTheme correctly being Light.
		//
		// The assertion is on a single robust scenario (Light host, app pinned Dark on Uno) where the
		// element's ActualTheme is verifiably Light but the resolved brush value reveals which theme
		// the {ThemeResource} was resolved against. The expected value (Green) is identical on Skia
		// Desktop and native WinUI; only the app-level mismatch differs (forced Dark on Uno, default on
		// WinUI, as noted above).
		// ---------------------------------------------------------------------------

		// One XAML document: a RequestedTheme=Light host whose Resources declare a theme-keyed sentinel
		// brush (Light=Green, Dark=Red), an owner Border carrying a ToolTip, and the ToolTip's label using
		// the brush via {ThemeResource}. Mirrors a view declaring its own label brushes in a themed
		// ResourceDictionary.
		private static Border CreateThemeResourceLightHost()
			=> (Border)XamlReader.Load(
				"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
					<Border.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="LabelForegroundBrush" Color="Green" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="LabelForegroundBrush" Color="Red" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
						</ResourceDictionary>
					</Border.Resources>
					<Border x:Name="Owner" Width="120" Height="40">
						<ToolTipService.ToolTip>
							<ToolTip>
								<TextBlock Text="Label"
										Foreground="{ThemeResource LabelForegroundBrush}" />
							</ToolTip>
						</ToolTipService.ToolTip>
					</Border>
				</Border>
				""");

		private static Color? GetThemeResourceForegroundColor(TextBlock textBlock)
			=> (textBlock.Foreground as SolidColorBrush)?.Color;

		// A Light host under a Dark application (the literal OS-Dark / app-Light mismatch). The ToolTip
		// label's ActualTheme is Light, so its {ThemeResource} must resolve the host's Light sentinel
		// (Green). On Uno the popup-presented label resolves the {ThemeResource} against the
		// application/global Dark theme, producing the Dark sentinel (Red).
		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/484")]
		public async Task When_ToolTip_Label_Uses_Owner_Subtree_Theme_Light_Under_Dark_App()
		{
#if HAS_UNO
			using var darkApp = ThemeHelper.UseApplicationDarkTheme();
			await TestServices.WindowHelper.WaitForIdle();
#endif

			var host = CreateThemeResourceLightHost();
			var owner = (Border)host.FindName("Owner");
			var tooltip = (ToolTip)ToolTipService.GetToolTip(owner);
			var label = (TextBlock)tooltip.Content;

			var root = new Border { Child = host };

			try
			{
				TestServices.WindowHelper.WindowContent = root;
				await TestServices.WindowHelper.WaitForLoaded(root);

				tooltip.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree.");
				Assert.AreEqual(ElementTheme.Light, label.ActualTheme, "ToolTip label should inherit the owner's Light theme.");

				var foreground = GetThemeResourceForegroundColor(label);
				Assert.IsNotNull(foreground, "ToolTip label should have resolved a SolidColorBrush foreground.");

				Assert.AreEqual(Colors.Green, foreground.Value,
					$"ToolTip label {{ThemeResource}} should resolve against the label's inherited Light theme " +
					$"(Light sentinel Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the " +
					$"popup-presented label resolved the {{ThemeResource}} against the application/global Dark " +
					$"theme instead of its own inherited ActualTheme.");
			}
			finally
			{
				tooltip.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}
	}
}
