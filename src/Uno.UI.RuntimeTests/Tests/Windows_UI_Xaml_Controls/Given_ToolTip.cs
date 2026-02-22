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
		public Task When_Switch_Theme_Fluent() => When_Switch_Theme_Inner(brush => (brush as AcrylicBrush).TintColor);

		[TestMethod]
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
			TextBox tb;
			var sv = new ScrollViewer
			{
				Content = new StackPanel
				{
					Height = 1000,
					Children =
					{
						(tb = new TextBox().Apply(tb => ToolTipService.SetToolTip(tb, new ToolTip { Content = "Simple ToolTip 1" })))
					}
				}
			};

			await UITestHelper.Load(sv);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(tb.GetAbsoluteBoundsRect().GetCenter());
			await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 300));
			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(tb.XamlRoot));

			finger.MoveBy(0, -50);
			await UITestHelper.WaitForIdle();
			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(tb.XamlRoot));
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
	}
}
