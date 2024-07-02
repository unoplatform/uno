#pragma warning disable 168 // for cleanup imported member

#if HAS_UNO
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests;
using Windows.Foundation.Collections;
using ButtonBase = Microsoft.UI.Xaml.Controls.Primitives.ButtonBase;
using Uno.UI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Controls;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

namespace Windows.UI.Tests.Enterprise
{
	[TestClass]
	[RequiresFullWindow]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public class CommandBarIntegrationTests : BaseDxamlTestClass
	{
		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[ClassCleanup]
		public static void TestCleanup()
		{
			TestServices.WindowHelper.ShutdownXaml();
			TestServices.WindowHelper.VerifyTestCleanup();
		}

		//
		// Test Cases
		//
		//void CommandBarIntegrationTests::CanInstantiate()
		//{
		//	Generic::DependencyObjectTests<xaml_controls::CommandBar>::CanInstantiate();
		//}

		[TestMethod]

		[Description("Validates that we can successfully add/remove a CommandBar from the live tree.")]
		[Ignore("TopAppBar not implemented.")]
		public async Task CanEnterAndLeaveLiveTree()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			Page page = null;

			var hasLoadedEvent = new Event();
			var hasUnloadedEvent = new Event();

			var loadedRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("Loaded");
			var unloadedRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("Unloaded");

			await RunOnUIThread(() =>
			{
				page = WindowHelper.SetupSimulatedAppPage();

				cmdBar = new CommandBar();
				loadedRegistration.Attach(cmdBar, (s, e) =>
				{
					hasLoadedEvent.Set();
				});

				unloadedRegistration.Attach(cmdBar, (s, e) =>
				{
					hasUnloadedEvent.Set();
				});
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cmdBar.IsOpen = true;
				//TODO: BottomAppBar not implemented
				//page.BottomAppBar = cmdBar;
			});
			await hasLoadedEvent.WaitForDefault();

			await RunOnUIThread(() => page.BottomAppBar = null);
			await hasUnloadedEvent.WaitForDefault();
		}

		[TestMethod]

		[Description("Validates that we can successfully reapply a template after the first time we apply it.")]
		[Ignore("TopAppBar not implemented.")]
		public async Task CanReapplyTemplate()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			Page page = null;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				loadedRegistration.Attach(cmdBar, (s, e) => loadedEvent.Set());

				page = WindowHelper.SetupSimulatedAppPage();
				//TODO: BottomAppBar not implemented
				//page.BottomAppBar = cmdBar;
			});
			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var cmdBarTemplate = (ControlTemplate)XamlReader.Load(@"
					<ControlTemplate
                        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                        xmlns:muxc=""http://schemas.microsoft.com/winfx/2006/xaml""
                        TargetType=""CommandBar"">
                        <Grid x:Name=""LayoutRoot"">
                            <Grid.Clip>
                                <RectangleGeometry Rect=""{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.ClipRect}"">
                                    <RectangleGeometry.Transform>
                                        <TranslateTransform x:Name=""ClipGeometryTransform"" Y=""{Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.CompactVerticalDelta}""/>
                                    </RectangleGeometry.Transform>
                                </RectangleGeometry>
                            </Grid.Clip>
                            <Grid x:Name=""ContentRoot"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""Auto""/>
                                </Grid.ColumnDefinitions>
                                <Grid.RenderTransform>
                                    <TranslateTransform x:Name=""ContentTransform""/>
                                </Grid.RenderTransform>
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width=""*""/>
                                        <ColumnDefinition Width=""Auto""/>
                                    </Grid.ColumnDefinitions>
                                    <ContentControl x:Name=""ContentContro""
                                        Content=""{TemplateBinding Content}""
                                        ContentTemplate=""{TemplateBinding ContentTemplate}""/>
                                    <ItemsControl x:Name=""PrimaryItemsContro"">
                                        <ItemsControl.ItemsPanel>
                                            <ItemsPanelTemplate>
                                                <StackPanel Orientation=""Horizonta"" />
                                            </ItemsPanelTemplate>
                                        </ItemsControl.ItemsPanel>
                                    </ItemsControl>
                                </Grid>
                                <Button x:Name=""MoreButton"" />
                                <Popup x:Name=""OverflowPopup"">
                                    <Popup.RenderTransform>
                                        <TranslateTransform x:Name=""OverflowPopupOffsetTransform""/>
                                    </Popup.RenderTransform>
                                    <Grid x:Name=""OverflowContentRoot"">
                                        <Grid.Clip>
                                            <RectangleGeometry x:Name=""OverflowContentRootClip""/>
                                        </Grid.Clip>
                                        <Grid.RenderTransform>
                                            <TranslateTransform x:Name=""OverflowContentRootTransform""
                                                X=""{Binding RelativeSource={RelativeSource TemplatedParent}, Path=CommandBarTemplateSettings.OverflowContentHorizontalOffset}""/>
                                        </Grid.RenderTransform>
                                        <CommandBarOverflowPresenter x:Name=""SecondaryItemsContro""
                                            Style=""{TemplateBinding CommandBarOverflowPresenterStyle}""
                                            IsEnabled=""False""
                                            IsTabStop=""False"">
                                            <CommandBarOverflowPresenter.RenderTransform>
                                                <TranslateTransform x:Name=""OverflowContentTransform""/>
                                            </CommandBarOverflowPresenter.RenderTransform>
                                            <CommandBarOverflowPresenter.ItemContainerStyle>
                                                <Style TargetType=""FrameworkElement"">
                                                    <Setter Property=""HorizontalAlignment"" Value=""Stretch""/>
                                                    <Setter Property=""Width"" Value=""NaN""/>
                                                </Style>
                                            </CommandBarOverflowPresenter.ItemContainerStyle>
                                        </CommandBarOverflowPresenter>
                                    </Grid>
                                </Popup>
                                <Rectangle x:Name=""HighContrastBorder"" x:DeferLoadStrategy=""Lazy"" Grid.ColumnSpan=""2""  Visibility=""Collapsed"" VerticalAlignment=""Stretch"" Stroke=""{ThemeResource SystemControlForegroundTransparentBrush}"" StrokeThickness=""1""/>
                            </Grid>
                        </Grid>
                    </ControlTemplate>
				");

				cmdBar.Template = cmdBarTemplate;
				cmdBar.ApplyTemplate();
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]

		[Description("Validates that CommandBar opens and closes, with appropriate events firing, using IsOpen property.")]
		public async Task CanOpenAndCloseUsingAPI()
		{
			TestCleanupWrapper cleanup;

			Func<CommandBar, Task> openFunc = async (cmdBar) => await RunOnUIThread(() => cmdBar.IsOpen = true);
			Func<CommandBar, Task> closeFunc = async (cmdBar) => await RunOnUIThread(() => cmdBar.IsOpen = false);

			await ValidateOpenAndCloseWorker(openFunc, closeFunc);
		}

		[TestMethod]

		[Description("Validates that CommandBar opens and closes, with appropriate events firing, using taps on More Button.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		public async Task CanOpenAndCloseUsingMoreButton()
		{
			TestCleanupWrapper cleanup;

			Func<CommandBar, Task> openAndCloseFunc = async (cmdBar) =>
			{
				var moreButton = await GetMoreButton(cmdBar);
				TestServices.InputHelper.Tap(moreButton);
			};

			await ValidateOpenAndCloseWorker(openAndCloseFunc, openAndCloseFunc);
		}

		[TestMethod]

		[Description("Validates that CommandBar closes when the device's Back button is pressed.")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("InjectBackButtonPress not implemented.")]
		public async Task CanCloseUsingBackButton()
		{
			TestCleanupWrapper cleanup;

			Func<CommandBar, Task> openFunc = async (cmdBar) => await RunOnUIThread(() => cmdBar.IsOpen = true);
			Func<CommandBar, Task> closeFunc = (cmdBar) =>
			{
				bool backButtonPressHandled = false;
				TestServices.Utilities.InjectBackButtonPress(ref backButtonPressHandled);
				VERIFY_IS_TRUE(backButtonPressHandled);
				return Task.CompletedTask;
			};

			await ValidateOpenAndCloseWorker(openFunc, closeFunc);
		}

		[TestMethod]

		[Description("Validates that CommandBar can close when a primary command is selected from the overflow.")]
		public async Task DoesCloseOnPrimaryCommandSelection()
		{
			TestCleanupWrapper cleanup;

			Func<CommandBar, Task> openFunc = async (cmdBar) => await RunOnUIThread(() => cmdBar.IsOpen = true);
			Func<CommandBar, Task> closeFunc = async (cmdBar) =>
			{
				FrameworkElement tapTarget = null;

				await RunOnUIThread(() => tapTarget = (FrameworkElement)cmdBar.PrimaryCommands[0]);

				TestServices.InputHelper.Tap(tapTarget);
			};

			await ValidateOpenAndCloseWorker(openFunc, closeFunc);
		}

		[TestMethod]

		[Description("Validates that CommandBar can close when a secondary command is selected from the overflow.")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		public async Task DoesCloseOnSecondaryCommandSelection()
		{
			TestCleanupWrapper cleanup;

			Func<CommandBar, Task> openFunc = async (cmdBar) => await RunOnUIThread(() => cmdBar.IsOpen = true);
			Func<CommandBar, Task> closeFunc = async (cmdBar) =>
			{
				FrameworkElement tapTarget = null;

				await RunOnUIThread(() => tapTarget = (FrameworkElement)cmdBar.SecondaryCommands[0]);

				TestServices.InputHelper.Tap(tapTarget);
			};

			await ValidateOpenAndCloseWorker(openFunc, closeFunc);
		}

		[TestMethod]

		[Description("Validates that items can be added to the CommandBar's collection properties.")]

		public async Task CanAddToAndRemoveFromCommandCollections()
		{
			TestCleanupWrapper cleanup;

			// Make sure we can add/remove items to/from our command collections.
			await RunOnUIThread(() =>
			{
				CommandBar cmdBar = new CommandBar();

				VERIFY_IS_TRUE(cmdBar.PrimaryCommands.Count == 0);
				VERIFY_IS_TRUE(cmdBar.SecondaryCommands.Count == 0);

				var btn1 = new AppBarButton();
				cmdBar.PrimaryCommands.Append(btn1);
				VERIFY_IS_TRUE(btn1 == (AppBarButton)cmdBar.PrimaryCommands[0]);

				var btn2 = new AppBarToggleButton();
				cmdBar.PrimaryCommands.Append(btn2);
				VERIFY_IS_TRUE(btn2 == cmdBar.PrimaryCommands[1]);

				cmdBar.PrimaryCommands.RemoveAt(1);
				VERIFY_IS_TRUE(cmdBar.PrimaryCommands.Count == 1);

				cmdBar.PrimaryCommands.RemoveAt(0);
				VERIFY_IS_TRUE(cmdBar.PrimaryCommands.Count == 0);

				var btn3 = new AppBarButton();
				cmdBar.SecondaryCommands.Append(btn3);
				VERIFY_IS_TRUE(btn3 == cmdBar.SecondaryCommands[0]);

				var btn4 = new AppBarToggleButton();
				cmdBar.SecondaryCommands.Append(btn4);
				VERIFY_IS_TRUE(btn4 == cmdBar.SecondaryCommands[1]);

				cmdBar.SecondaryCommands.RemoveAt(1);
				VERIFY_IS_TRUE(cmdBar.SecondaryCommands.Count == 1);

				cmdBar.SecondaryCommands.RemoveAt(0);
				VERIFY_IS_TRUE(cmdBar.SecondaryCommands.Count == 0);

				cmdBar.SecondaryCommands.Append(btn3);
				VERIFY_IS_TRUE(btn3 == cmdBar.SecondaryCommands.GetAt(0));

				cmdBar.SecondaryCommands.SetAt(0, btn4);
				VERIFY_IS_TRUE(btn4 == cmdBar.SecondaryCommands.GetAt(0));

				cmdBar.SecondaryCommands.RemoveAt(cmdBar.SecondaryCommands.Count - 1);
				VERIFY_IS_TRUE(cmdBar.SecondaryCommands.Count == 0);

				cmdBar.PrimaryCommands.Append(btn1);
				VERIFY_IS_TRUE(btn1 == cmdBar.PrimaryCommands.GetAt(0));

				cmdBar.PrimaryCommands.Append(btn2);
				VERIFY_IS_TRUE(btn2 == cmdBar.PrimaryCommands.GetAt(1));

				cmdBar.SecondaryCommands.Append(btn3);
				VERIFY_IS_TRUE(btn3 == cmdBar.SecondaryCommands.GetAt(0));

				cmdBar.SecondaryCommands.Append(btn4);
				VERIFY_IS_TRUE(btn4 == cmdBar.SecondaryCommands.GetAt(1));

				cmdBar.PrimaryCommands.Clear();
				VERIFY_IS_TRUE(cmdBar.PrimaryCommands.Count == 0);

				cmdBar.SecondaryCommands.Clear();
				VERIFY_IS_TRUE(cmdBar.SecondaryCommands.Count == 0);
			});

			// Make sure we can add items to our command collections via the parser.
			await RunOnUIThread(() =>
			{
				CommandBar cmdBar = (CommandBar)XamlReader.Load(@"
				<CommandBar xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                    <AppBarToggleButton Label=""btn1""/>
                    <AppBarButton Label=""btn2""/>
                    <CommandBar.SecondaryCommands>
                        <AppBarToggleButton  Label=""btn3""/>
                        <AppBarButton  Label=""btn4""/>
                    </CommandBar.SecondaryCommands>
                </CommandBar>
			");

				VERIFY_IS_NOT_NULL(cmdBar);
				VERIFY_IS_TRUE(cmdBar.PrimaryCommands.Count == 2);
				VERIFY_IS_TRUE(cmdBar.SecondaryCommands.Count == 2);

				var btn1 = cmdBar.PrimaryCommands.GetAt(0) as AppBarToggleButton;
				VERIFY_IS_NOT_NULL(btn1);
				VERIFY_IS_TRUE(btn1.Label == "btn1");

				var btn2 = cmdBar.PrimaryCommands.GetAt(1) as AppBarButton;
				VERIFY_IS_NOT_NULL(btn2);
				VERIFY_IS_TRUE(btn2.Label == "btn2");

				var btn3 = cmdBar.SecondaryCommands.GetAt(0) as AppBarToggleButton;
				VERIFY_IS_NOT_NULL(btn3);
				VERIFY_IS_TRUE(btn3.Label == "btn3");

				var btn4 = cmdBar.SecondaryCommands.GetAt(1) as AppBarButton;
				VERIFY_IS_NOT_NULL(btn4);
				VERIFY_IS_TRUE(btn4.Label == "btn4");
			});
		}

		[TestMethod]
		[Description("Validates that the overflow's open direction and alignment.")]
#if __ANDROID__
		[Ignore("Disabled for failing assertion: https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task ValidateOverflowPlacement()
		{
			TestCleanupWrapper cleanup;

			LOG_OUTPUT("ValidateOverflowPosition: Opened Up, Aligned Right, FlowDirection=LTR");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Up, OverflowAlignment.Right, false /*isRTL*/);

			LOG_OUTPUT("ValidateOverflowPosition: Opened Up, Aligned Left, FlowDirection=LTR");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Up, OverflowAlignment.Left, false /*isRTL*/);

			LOG_OUTPUT("ValidateOverflowPosition: Opened Down, Aligned Right, FlowDirection=LTR");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Down, OverflowAlignment.Right, false /*isRTL*/);

			LOG_OUTPUT("ValidateOverflowPosition: Opened Down, Aligned Left, FlowDirection=LTR");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Down, OverflowAlignment.Left, false /*isRTL*/);

#if !HAS_UNO // TODO: Fix these scenarios.
			// Validate the same scenarios, except with FlowDirection=RTL
			LOG_OUTPUT("ValidateOverflowPosition: Opened Up, Aligned Right, FlowDirection=RT");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Up, OverflowAlignment.Right, true /*isRTL*/);

			LOG_OUTPUT("ValidateOverflowPosition: Opened Up, Aligned Left, FlowDirection=RT");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Up, OverflowAlignment.Left, true /*isRTL*/);

			LOG_OUTPUT("ValidateOverflowPosition: Opened Down, Aligned Right, FlowDirection=RT");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Down, OverflowAlignment.Right, true /*isRTL*/);

			LOG_OUTPUT("ValidateOverflowPosition: Opened Down, Aligned Left, FlowDirection=RT");
			await ValidateOverflowPlacementWorker(OverflowOpenDirection.Down, OverflowAlignment.Left, true /*isRTL*/);
#endif
		}

		[TestMethod]

		[Description("Validates that the overflow snaps to the window width when it's less than 480.")]
		[Ignore("SetWindowSizeOverride not implemented.")]
		public async Task ValidateOverflowSnapsToWindowWidth()
		{
			TestCleanupWrapper cleanup;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<CommandBar, RoutedEventHandler>("Loaded");

			double expectedWidth = 400;

			// Override the window size to be < 480 to simulate the conditions
			// under which the overflow menu will snap.
			WindowHelper.SetWindowSizeOverride(new Size(expectedWidth, 600));

			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.IsOpen = true;

				var button = new AppBarButton();
				button.Label = "menu item";

				cmdBar.SecondaryCommands.Append(button);

				loadedRegistration.Attach(cmdBar, (s, e) =>
				{
					LOG_OUTPUT("CommandBar.Loaded raised.");
					loadedEvent.Set();
				});

				SetWindowContent(cmdBar);
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);
				VERIFY_IS_NOT_NULL(overflowContentRoot);

				VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, expectedWidth);
				VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, expectedWidth);
			});
		}

		[TestMethod]

		[Description("Validates that the overflow's max height is 50% of the window height.")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("SetWindowSizeOverride not implemented")]
		public void ValidateOverflowMaxHeight()
		{
			//	TestCleanupWrapper cleanup;

			//	const double overflowHeight = 300;
			//	// 40 for WindowedPopupPadding
			//	double expectedHeight = overflowHeight + 40;
			//	TestServices::WindowHelper->SetWindowSizeOverride(wf::Size(500, static_cast<float>(overflowHeight * 2)));

			//	// We add a rectangle to give us extra space in which to do translate transforms
			//	// when using windowed popups, so we add that to the max height and need to account for it.
			//	if (PopupHelper::AreWindowedPopupsEnabled())
			//	{
			//		expectedHeight += 64;
			//	}

			//	auto loadedEvent = std::make_shared<Event>();
			//	auto loadedRegistration = CreateSafeEventRegistration(xaml_controls::CommandBar, Loaded);

			//	xaml_controls::CommandBar ^ cmdBar = nullptr;

			//	RunOnUIThread([&]()

			//{
			//		cmdBar = ref new xaml_controls::CommandBar();
			//		cmdBar->IsOpen = true;

			//		for (size_t i = 0; i < 50; ++i)
			//		{
			//			auto button = ref new xaml_controls::AppBarButton();
			//			button->Label = "menu item";

			//			cmdBar->SecondaryCommands->Append(button);
			//		}

			//		loadedRegistration.Attach(cmdBar, [&]()

			//	{
			//			LOG_OUTPUT("CommandBar.Loaded raised.");
			//			loadedEvent->Set();
			//		});

			//		TestServices::WindowHelper->WindowContent = cmdBar;
			//	});

			//	loadedEvent->WaitForDefault();
			//	TestServices::WindowHelper->WaitForIdle();

			//	RunOnUIThread([&]()

			//{
			//		auto overflowContentRoot = safe_cast < xaml::FrameworkElement ^> (TreeHelper::GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar));
			//		VERIFY_IS_NOT_NULL(overflowContentRoot);

			//		VERIFY_ARE_EQUAL(overflowContentRoot->MaxHeight, expectedHeight);
			//		VERIFY_ARE_EQUAL(overflowContentRoot->ActualHeight, expectedHeight);
			//	});
		}

		[TestMethod]

		[Description("Validates that resizing the AppBar after opening and closing causes its width to properly get updated.")]
		[TestProperty("Hosting:Mode", "UAP")]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task CanResizeCommandBarAfterOpeningAndClosing()
		{
			TestCleanupWrapper cleanup;

			Page page = null;
			CommandBar cmdBar = null;
			Button moreButton = null;
			var originalMoreButtonOffset = new Point();

			await RunOnUIThread(() =>
			{
				cmdBar = (CommandBar)XamlReader.Load(@"
				<CommandBar xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Margin=""0,0,100,0"">
                    <AppBarButton Label=""btn1""/>
                    <AppBarButton Label=""btn2""/>
                    <AppBarButton Label=""btn3""/>
                    <AppBarButton Label=""btn4""/>
                    <AppBarButton Label=""btn5""/>
                    <CommandBar.SecondaryCommands>
                        <AppBarButton Label=""btn1""/>
                        <AppBarButton Label=""btn2""/>
                        <AppBarButton Label=""btn3""/>
                    </CommandBar.SecondaryCommands>
                </CommandBar>
			");

				page = WindowHelper.SetupSimulatedAppPage();

				//Uno TODO: Use Page.BottomAppBar instead of Page.Content
				SetPageContent(cmdBar, page);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				moreButton = (Button)TreeHelper.GetVisualChildByName(cmdBar, "MoreButton");
				cmdBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				originalMoreButtonOffset = moreButton.TransformToVisual(null).TransformPoint(new Point(0, 0));
				cmdBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cmdBar.Margin = ThicknessHelper.FromLengths(0, 0, 0, 0);
				cmdBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var newMoreButtonPosition = moreButton.TransformToVisual(null).TransformPoint(new Point(0, 0));
				VERIFY_IS_GREATER_THAN(newMoreButtonPosition.X, originalMoreButtonOffset.X);

				cmdBar.IsOpen = false;
			});
		}

		[TestMethod]

		[Description("Validates that a CommandBar can use an AppBarButton taller than the app window.")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("Missing implementations: BottomAppBar, SetWindowSizeOverride, KeyboardHelper, InputHelper")]
		public async Task CanUseLargeAppBarButton()
		{
			TestCleanupWrapper cleanup;
			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			Page page = null;
			CommandBar cmdBar = null;
			AppBarButton appBarButton = null;

			await RunOnUIThread(() =>
			{
				cmdBar = (CommandBar)XamlReader.Load(@"
					<CommandBar   >
                      <CommandBar.SecondaryCommands>
                        <AppBarButton  Label=""AppBarButton"" Height=""1000"">
                          <AppBarButton.Flyout>
                            <MenuFlyout>
                              <MenuFlyoutItem>MenuFlyoutItem</MenuFlyoutItem>
                            </MenuFlyout>
                          </AppBarButton.Flyout>
                        </AppBarButton>
                      </CommandBar.SecondaryCommands>
                    </CommandBar>
				");
				VERIFY_IS_NOT_NULL(cmdBar);

				appBarButton = (AppBarButton)cmdBar.SecondaryCommands[0];
				VERIFY_IS_NOT_NULL(appBarButton);

				page = WindowHelper.SetupSimulatedAppPage();
				VERIFY_IS_NOT_NULL(page);

				//TODO: BottomAppBar not implemented
				//page.BottomAppBar = cmdBar;
			});
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Tabbing into the BottomAppBar's SecondaryCommands.");
			KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Opening the BottomAppBar's SecondaryCommands.");
			KeyboardHelper.Enter();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Tabbing into the tall AppBarButton.");
			KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Scrolling down within the AppBarButton.");
			KeyboardHelper.PageDown();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over the AppBarButton.");
			TestServices.InputHelper.MoveMouse(appBarButton);
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Holding the AppBarButton.");
			TestServices.InputHelper.Hold(appBarButton);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Closing the BottomAppBar.");
				cmdBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]

		[Description("When the CommandBar is Disabled, the more button should be greyed out.")]
		[Ignore("ResourceDictionary retrieval is incorrect #17271")]
		public async Task ValidateMoreButtonVisualInDisabledState()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			FontIcon ellipsisIcon = null;
			Brush expectedBrushEnabled = null;
			Brush expectedBrushDisabled = null;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				SetWindowContent(cmdBar);

				var gridResources = cmdBar.Resources;
				gridResources.TryGetValue("AppBarButtonForeground", out var oExpectedBrushEnabled);
				expectedBrushEnabled = oExpectedBrushEnabled as Brush;
				gridResources.TryGetValue("AppBarButtonForegroundDisabled", out var oExpectedBrushDisabled);
				expectedBrushDisabled = oExpectedBrushDisabled as Brush;

			});
			await WindowHelper.WaitForIdle();

			// Verify that the ellipsis is the correct color in the Enabled CommandBar:
			await RunOnUIThread(() =>
			{
				ellipsisIcon = (FontIcon)TreeHelper.GetVisualChildByName(cmdBar, "EllipsisIcon");

				VERIFY_ARE_EQUAL(ellipsisIcon.Foreground, expectedBrushEnabled);

				cmdBar.IsEnabled = false;
			});
			await WindowHelper.WaitForIdle();

			// Verify that the ellipsis is the correct color in the Disabled CommandBar:
			await RunOnUIThread(() => VERIFY_ARE_EQUAL(ellipsisIcon.Foreground, expectedBrushDisabled));
		}

		[TestMethod]

		[Description("Validates that AppBarButtons have invisible labels when IsOpen is false.")]
		public async Task ValidateAppBarButtonsHaveInvisibleLabelsWhenClosed()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			AppBarButton button = null;
			TextBlock buttonLabel = null;
			AppBarToggleButton toggleButton = null;
			TextBlock toggleButtonLabel = null;
			AppBarButton buttonSecondary = null;
			TextBlock buttonSecondaryLabel = null;
			AppBarToggleButton toggleButtonSecondary = null;
			TextBlock toggleButtonSecondaryLabel = null;

			// Setup our environment.
			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();

				button = new AppBarButton();
				button.Label = "First button";
				cmdBar.PrimaryCommands.Append(button);

				toggleButton = new AppBarToggleButton();
				toggleButton.Label = "Second button";
				cmdBar.PrimaryCommands.Append(toggleButton);

				buttonSecondary = new AppBarButton();
				buttonSecondary.Label = "First button";
				cmdBar.SecondaryCommands.Append(buttonSecondary);

				toggleButtonSecondary = new AppBarToggleButton();
				toggleButtonSecondary.Label = "Second button";
				cmdBar.SecondaryCommands.Append(toggleButtonSecondary);

				SetWindowContent(cmdBar);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				buttonLabel = (TextBlock)TreeHelper.GetVisualChildByName(button, "TextLabel");
				toggleButtonLabel = (TextBlock)TreeHelper.GetVisualChildByName(toggleButton, "TextLabel");

				VERIFY_ARE_EQUAL(buttonLabel.Visibility, Visibility.Collapsed);
				VERIFY_ARE_EQUAL(toggleButtonLabel.Visibility, Visibility.Collapsed);

				cmdBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// We have to wait for the overflow popup to be open to query these.
				buttonSecondaryLabel = (TextBlock)TreeHelper.GetVisualChildByName(buttonSecondary, "OverflowTextLabel");
				toggleButtonSecondaryLabel = (TextBlock)TreeHelper.GetVisualChildByName(toggleButtonSecondary, "OverflowTextLabel");

				VERIFY_ARE_EQUAL(buttonLabel.Visibility, Visibility.Visible);
				VERIFY_ARE_EQUAL(toggleButtonLabel.Visibility, Visibility.Visible);
				VERIFY_ARE_EQUAL(buttonSecondaryLabel.Visibility, Visibility.Visible);
				VERIFY_ARE_EQUAL(toggleButtonSecondaryLabel.Visibility, Visibility.Visible);

				cmdBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(buttonLabel.Visibility, Visibility.Collapsed);
				VERIFY_ARE_EQUAL(toggleButtonLabel.Visibility, Visibility.Collapsed);

				// Secondary buttons' label visibilities are unaffected, since the buttons aren't touched when IsOpen is false.
				VERIFY_ARE_EQUAL(buttonSecondaryLabel.Visibility, Visibility.Visible);
				VERIFY_ARE_EQUAL(toggleButtonSecondaryLabel.Visibility, Visibility.Visible);
			});

		}

		[TestMethod]
		[Description("Validates that AppBarButtons' text labels are offset to the right when there also are AppBarToggleButtons in the same secondary commands list.")]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task ValidateAppBarButtonsAreOffsetWithAppBarToggleButtons()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			AppBarButton button = null;
			TextBlock buttonLabel = null;
			AppBarToggleButton toggleButton = null;

			var buttonLoadedEvent = new Event();
			var buttonUnloadedEvent = new Event();
			var toggleButtonLoadedEvent = new Event();
			var toggleButtonUnloadedEvent = new Event();
			var buttonLoadedRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("Loaded");
			var buttonUnloadedRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("Unloaded");
			var toggleButtonLoadedRegistration = CreateSafeEventRegistration<AppBarToggleButton, RoutedEventHandler>("Loaded");
			var toggleButtonUnloadedRegistration = CreateSafeEventRegistration<AppBarToggleButton, RoutedEventHandler>("Unloaded");

			double originalAppBarButtonMargin = 0;

			// Setup our environment.
			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();

				button = new AppBarButton();
				button.Label = "First button";
				cmdBar.SecondaryCommands.Append(button);

				toggleButton = new AppBarToggleButton();
				toggleButton.Label = "Second button";

				buttonLoadedRegistration.Attach(button, (s, e) =>
				{
					LOG_OUTPUT("AppBarButton loaded.");
					buttonLoadedEvent.Set();
				});

				buttonUnloadedRegistration.Attach(button, (s, e) =>
				{
					LOG_OUTPUT("AppBarButton unloaded.");
					buttonUnloadedEvent.Set();
				});

				toggleButtonLoadedRegistration.Attach(toggleButton, (s, e) =>
				{
					LOG_OUTPUT("AppBarToggleButton loaded.");
					toggleButtonLoadedEvent.Set();
				});

				toggleButtonUnloadedRegistration.Attach(toggleButton, (s, e) =>
				{
					LOG_OUTPUT("AppBarToggleButton unloaded.");
					toggleButtonUnloadedEvent.Set();
				});

				SetWindowContent(cmdBar);
			});
			await buttonLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);

			await RunOnUIThread(() =>
			{
				buttonLabel = (TextBlock)TreeHelper.GetVisualChildByName(button, "OverflowTextLabel");
				originalAppBarButtonMargin = buttonLabel.Margin.Left;
				cmdBar.IsOpen = false;
			});

			await buttonUnloadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cmdBar.SecondaryCommands.Append(toggleButton);
				cmdBar.IsOpen = true;
			});

			await buttonLoadedEvent.WaitForDefault();
			await toggleButtonLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cmdBar.SecondaryCommands.RemoveAt(1);
				cmdBar.IsOpen = true;
			});

			await buttonLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				buttonLabel = (TextBlock)TreeHelper.GetVisualChildByName(button, "OverflowTextLabel");
				VERIFY_ARE_EQUAL(buttonLabel.Margin.Left, originalAppBarButtonMargin);
			});
		}

		[TestMethod]

		[Description("Validates that CommandBars can be placed inline are light-dismissible.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		public async Task ValidateInlineCommandBarLightDismissBehavior()
		{
			TestCleanupWrapper cleanup;

			Button tapTarget = null;
			CommandBar cmdBar = null;

			var clickEvent = new Event();
			var openedEvent = new Event();
			var closedEvent = new Event();

			var clickRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("Click");
			var openedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Closed");

			await RunOnUIThread(() =>
			{
				tapTarget = new Button();
				tapTarget.Content = "Click Me!";

				// Add a top margin to push the button out from under the statusbar on phone
				// and a bottom margin to make sure the CommandBar doesn't open over the button.
				tapTarget.Margin = ThicknessHelper.FromLengths(0, 32, 0, 32);

				cmdBar = new CommandBar();
				clickRegistration.Attach(tapTarget, (s, e) => clickEvent.Set());
				openedRegistration.Attach(cmdBar, (s, e) => openedEvent.Set());
				closedRegistration.Attach(cmdBar, (s, e) => closedEvent.Set());

				var root = new StackPanel();
				root.Children.Append(tapTarget);
				root.Children.Append(cmdBar);
				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cmdBar.IsOpen = true;
			});
			await openedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			// Click outside of the CommandBar to close it.
			TestServices.InputHelper.Tap(tapTarget);
			await closedEvent.WaitForDefault();

			// Validate that sticky CommandBars are not light-dismissible.
			await RunOnUIThread(() =>
			{
				cmdBar.IsOpen = true;
				cmdBar.IsSticky = true;
			});
			await openedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			// Click outside of the CommandBar.
			TestServices.InputHelper.Tap(tapTarget);
			await WindowHelper.WaitForIdle();

			// Since the CommandBar shouldn't be light-dismissible, the button should
			// have received the input and invoked its click handler.
			await clickEvent.WaitForDefault();
		}

		[TestMethod]

		[Description("Validates that non-hidden CommandBars do not open on right-tap, while hidden ones do.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("MouseHelper not implemented.")] // CoreWindow pointer events no longering being raised from lifted Xaml
		public void ValidateRightClickBehavior()
		{
			LOG_OUTPUT("Validating CommandBarRightClickBehavior with ClosedDisplayMode=Hidden.");
			ValidateRightClickBehaviorWorker(AppBarClosedDisplayMode.Hidden);

			LOG_OUTPUT("Validating CommandBarRightClickBehavior with ClosedDisplayMode=Minimal.");
			ValidateRightClickBehaviorWorker(AppBarClosedDisplayMode.Minimal);

			LOG_OUTPUT("Validating CommandBarRightClickBehavior with ClosedDisplayMode=Compact.");
			ValidateRightClickBehaviorWorker(AppBarClosedDisplayMode.Compact);
		}

		[TestMethod]

		[Description("Validates the CommandBar behavior for arrow key presses.")]
		[TestProperty("Hosting:Mode", "UAP")]
#if __ANDROID__ || __IOS__
		[Ignore("Keyboard nav not supported")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task ValidateArrowKeys()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			Page page = null;
			Button moreButton = null;
			UIElement secondaryItemsPresenter = null;
			var focusSequence = "";
			var expectedFocusSequence = "[M][P2][P1][P2][M][S1][S2][S4][M][S4][S2][S1][M]";

			List<SafeEventRegistration<AppBarButton, RoutedEventHandler>> buttonGotFocusRegistrations = new List<SafeEventRegistration<AppBarButton, RoutedEventHandler>>();
			var separatorGotFocusRegistration = CreateSafeEventRegistration<AppBarSeparator, RoutedEventHandler>("GotFocus");
			var toggleButtonGotFocusRegistration = CreateSafeEventRegistration<AppBarToggleButton, RoutedEventHandler>("GotFocus");
			var moreButtonGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");

			var rightKeySequence = "#$d$_right#$u$_right";
			var leftKeySequence = "#$d$_left#$u$_left";
			//var returnKeySequence = "#$d$_return#$u$_return";

			int primaryCount = 0;
			int secondaryCount = 0;

			RoutedEventHandler gotFocusHandler = null;

			await RunOnUIThread(() =>
			{
				page = WindowHelper.SetupSimulatedAppPage();

				gotFocusHandler = (s, e) => focusSequence += $"[{((FrameworkElement)s).Tag}]";

				cmdBar = new CommandBar();
				//UNO TODO: Fix inital IsOpen load
				//cmdBar.IsOpen = true;

				// Add a couple of AppBarButtons to primary
				for (int i = 1; i <= 2; i++)
				{
					var appBarButton = new AppBarButton();
					appBarButton.Tag = "P" + i;
					cmdBar.PrimaryCommands.Append(appBarButton);

					var gotFocusRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("GotFocus");
					gotFocusRegistration.Attach(appBarButton, gotFocusHandler);
					buttonGotFocusRegistrations.Append(gotFocusRegistration);
				}

				// Add a couple of AppBarButtons to secondary
				for (int i = 1; i <= 2; i++)
				{
					var appBarButton = new AppBarButton();
					appBarButton.Tag = "S" + i;
					cmdBar.SecondaryCommands.Append(appBarButton);

					var gotFocusRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("GotFocus");
					gotFocusRegistration.Attach(appBarButton, gotFocusHandler);
					buttonGotFocusRegistrations.Append(gotFocusRegistration);
				}

				// Add an AppBarSeparator to secondary
				{
					var appBarSeparator = new AppBarSeparator();
					appBarSeparator.Tag = "S3";
					cmdBar.SecondaryCommands.Append(appBarSeparator);
					separatorGotFocusRegistration.Attach(appBarSeparator, gotFocusHandler);
				}

				// Add an AppBarToggleButton to secondary
				{
					var appBarToggleButton = new AppBarToggleButton();
					appBarToggleButton.Tag = "S4";
					cmdBar.SecondaryCommands.Append(appBarToggleButton);
					toggleButtonGotFocusRegistration.Attach(appBarToggleButton, gotFocusHandler);
				}

				SetPageContent(cmdBar, page);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await Task.Delay(2000);
			await RunOnUIThread(() =>
			{
				secondaryItemsPresenter = GetSecondaryItemsPresenter(cmdBar);
			});

			await RunOnUIThread(() =>
			{
				moreButton = (Button)TreeHelper.GetVisualChildByName(cmdBar, "MoreButton");
				VERIFY_IS_NOT_NULL(moreButton);
				moreButton.Tag = "M";
				moreButtonGotFocusRegistration.Attach(moreButton, gotFocusHandler);

				primaryCount = cmdBar.PrimaryCommands.Count;
				secondaryCount = cmdBar.SecondaryCommands.Count;
			});
			await WindowHelper.WaitForIdle();

			focusSequence = "";

			// Start focus with more button
			await RunOnUIThread(() => moreButton.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			// Press left arrow key (number of primary commands + 1) times
			for (int i = 0; i <= primaryCount; i++)
			{
				KeyboardHelper.PressKeySequence(leftKeySequence, cmdBar);
				await WindowHelper.WaitForIdle();
			}

			// Press right arrow key (number of primary commands + 1) times
			for (int i = 0; i <= primaryCount; i++)
			{
				KeyboardHelper.PressKeySequence(rightKeySequence, cmdBar);
				await WindowHelper.WaitForIdle();
			}

			// Press down arrow key (number of secondary commands - 1) times
			for (int i = 0; i < secondaryCount; i++)
			{
				KeyboardHelper.Down(i == 0 ? cmdBar : secondaryItemsPresenter);
				await WindowHelper.WaitForIdle();
			}

			// Press up arrow key (number of secondary commands - 1) times
			for (int i = 0; i < secondaryCount; i++)
			{
				KeyboardHelper.Up(i == 0 ? cmdBar : secondaryItemsPresenter);
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT($"Expected focus sequence: {expectedFocusSequence}");
			LOG_OUTPUT($"Actual focus sequence:   {focusSequence}");

			VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);

			await RunOnUIThread(() =>
			{
				//page.BottomAppBar = null;
			});
		}

		[TestMethod]

		[Ignore("ControlHelper.ValidateUIElementTree not implemented.")]
		public void ValidateUIElementTreeBoth()
		{
			//TestCleanupWrapper cleanup;

			//LOG_OUTPUT("Validating CommandBars with both Primary & Secondary commands.");
			//ControlHelper::ValidateUIElementTree(
			//	ValidateTreeParams(
			//		PopupHelper::AreWindowedPopupsEnabled() ? "Windowed" : "Unwindowed",
			//		wf::Size(500, 800),
			//		1.f,

			//		[]()

			//	{
			//	return ValidateUIElementTestSetup(true /*addPrimary*/, true /*addSecondary*/);
			//},
			//             GetUIElementTreeValidationRules())
			//         );
		}

		[TestMethod]

		[Ignore("ControlHelper.ValidateUIElementTree not implemented.")]
		public void ValidateUIElementTreePrimaryOnly()
		{
			//TestCleanupWrapper cleanup;

			//LOG_OUTPUT("Validating CommandBars with only Primary commands.");
			//ControlHelper::ValidateUIElementTree(
			//	ValidateTreeParams(
			//		PopupHelper::AreWindowedPopupsEnabled() ? "Windowed" : "Unwindowed",
			//		wf::Size(500, 800),
			//		1.f,

			//		[]()

			//	{
			//	return ValidateUIElementTestSetup(true /*addPrimary*/, false /*addSecondary*/);
			//},
			//             GetUIElementTreeValidationRules())
			//         );
		}

		[TestMethod]

		[Ignore("ControlHelper.ValidateUIElementTree not implemented.")]
		public void ValidateUIElementTreeSecondaryOnly()
		{
			//TestCleanupWrapper cleanup;

			//LOG_OUTPUT("Validating CommandBars with only Secondary commands.");
			//ControlHelper::ValidateUIElementTree(
			//		ValidateTreeParams(
			//		PopupHelper::AreWindowedPopupsEnabled() ? "Windowed" : "Unwindowed",
			//		wf::Size(500, 800),
			//		1.f,

			//		[]()

			//	{
			//	return ValidateUIElementTestSetup(false /*addPrimary*/, true /*addSecondary*/);
			//},
			//             GetUIElementTreeValidationRules())
			//             );
		}

		[TestMethod]

		[Description("Validates a fix for a bug where primary command items would disappear unexpectedly.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		//[Ignore] Lifted Xaml Test: Fix and re-enable tests that were disabled due to being unreliable in Helix test pass.
		public async Task PrimaryCommandItemsDoNotDisappear()
		{
			TestCleanupWrapper cleanup;
			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				var rootGrid = new Grid();

				cmdBar = new CommandBar();
				cmdBar.VerticalAlignment = VerticalAlignment.Center; // Center it to get it out from under the statusbar.

				var appBarButton = new AppBarButton();
				appBarButton.Label = "button";
				cmdBar.PrimaryCommands.Append(appBarButton);

				rootGrid.Children.Append(cmdBar);

				SetWindowContent(rootGrid);
			});
			await WindowHelper.WaitForIdle();

			// Change the ClosedDisplayMode to Hidden.
			await RunOnUIThread(() => cmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden);
			await WindowHelper.WaitForIdle();

			// Open the menu.
			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await WindowHelper.WaitForIdle();

			// Change the ClosedDisplayMode back to Compact.
			await RunOnUIThread(() => cmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact);
			await WindowHelper.WaitForIdle();

			// Attempt to tap the more button.
			Button moreButton = null;
			var clickEvent = new Event();
			var clickRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("Click");

			// Find the MoreButton and attempt to tap it.
			await RunOnUIThread(() =>
			{
				moreButton = (Button)TreeHelper.GetVisualChildByName(cmdBar, "MoreButton");
				clickRegistration.Attach(moreButton, (s, e) => clickEvent.Set());
			});

			TestServices.InputHelper.Tap(moreButton);
			await clickEvent.WaitForDefault();
		}

		[TestMethod]

		[Description("Validates that the overflow menu's scrollviewer does not scroll with arrow keys.")]
		public async Task ValidateOverflowScrollViewerDoesNotScrollWithArrowKeys()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			var firstItemOriginalPosition = new Point();
			UIElement secondaryItemsPresenter = null;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				//UNO TODO: Fix inital IsOpen load
				//cmdBar.IsOpen = true;

				for (int i = 0; i < 50; ++i)
				{
					var button = new AppBarButton();
					button.Label = "menu item";

					cmdBar.SecondaryCommands.Append(button);
				}

				SetWindowContent(cmdBar);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await Task.Delay(2000);

			await RunOnUIThread(() =>
			{
				// Focus the first element in the overflow menu.
				var item = (AppBarButton)cmdBar.SecondaryCommands[0];
				item.Focus(FocusState.Keyboard);

				// Save off it's original position.
				var transform = item.TransformToVisual(null);
				firstItemOriginalPosition = transform.TransformPoint(new Point(0, 0));
			});
			await WindowHelper.WaitForIdle();

			// Press down arrow key
			await RunOnUIThread(() => secondaryItemsPresenter = GetSecondaryItemsPresenter(cmdBar));
			KeyboardHelper.Down(secondaryItemsPresenter);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var item = (AppBarButton)cmdBar.SecondaryCommands[0];
				var transform = item.TransformToVisual(null);
				var firstItemNewPosition = transform.TransformPoint(new Point(0, 0));

				VERIFY_ARE_EQUAL(firstItemNewPosition, firstItemOriginalPosition);
			});
		}

		[TestMethod]

		[Description("Validates focus returns to the more button when it was previously in the overflow menu when closing.")]
		[Ignore("Popup Focus not implemented")] // TODO Focus: Popups
		public async Task DoesFocusReturnToMoreButtonFromOverflowMenuWhenClosed()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				//UNO TODO: Fix inital IsOpen load
				//cmdBar.IsOpen = true;

				var button = new AppBarButton();
				cmdBar.SecondaryCommands.Append(button);

				SetWindowContent(cmdBar);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await Task.Delay(2000);

			await RunOnUIThread(() =>
			{
				// Focus the first element in the overflow menu.
				var item = (AppBarButton)cmdBar.SecondaryCommands[0];
				item.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => cmdBar.IsOpen = false);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var focusedElement = FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
				var moreButton = TreeHelper.GetVisualChildByName(cmdBar, "MoreButton");

				VERIFY_IS_TRUE(focusedElement.Equals(moreButton));
			});
		}

		[TestMethod]

		[Description("Validates that closing a CommandBar does not result in focus being transferred to the first focusable element in the page.")]
		public async Task ValidateFirstElementIsNotFocusedWhenClosingCommandBar()
		{
			TestCleanupWrapper cleanup;

			TextBox textBox = null;
			Button button = null;
			CommandBar cmdBar = null;
			bool textBoxWasFocused = false;
			var textBoxGotFocusRegistration = CreateSafeEventRegistration<TextBox, RoutedEventHandler>("GotFocus");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.PrimaryCommands.Append(new AppBarButton());

				var page = WindowHelper.SetupSimulatedAppPage();
				//page.BottomAppBar = cmdBar;

				// Add a TextBox and a Button to the page with the TextBox being the First Focusable Element.
				var stackPanel = new StackPanel();
				textBox = new TextBox();
				stackPanel.Children.Append(textBox);
				button = new Button();
				button.Content = "Button";
				stackPanel.Children.Append(button);
				stackPanel.Children.Append(cmdBar);
				SetPageContent(stackPanel, page);

				textBoxGotFocusRegistration.Attach(textBox, (s, e) => textBoxWasFocused = true);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// Move focus to Button so it becomes the Previously Focused Element.
				button.Focus(FocusState.Programmatic);
				cmdBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();
			await Task.Delay(2000);

			textBoxWasFocused = false;

			await RunOnUIThread(() => cmdBar.IsOpen = false);
			await WindowHelper.WaitForIdle();

			// Verify that the focus did not move to the First Focusable Element.
			VERIFY_IS_FALSE(textBoxWasFocused);
		}

		[TestMethod]

		[Description("Validates that focused command bar elements stay focused after a collection or size change.")]
		[Ignore("Popup Focus is buggy.")]
		public async Task CanMaintainFocusAfterCollectionOrSizeChange()
		{
			TestCleanupWrapper cleanup;

			CommandBar commandBar = null;

			Action<string, IObservableVector<ICommandBarElement>> addButton = (label, commands) =>
			{
				var button = new AppBarButton();
				button.Label = label;
				commands.Append(button);
			};

			await RunOnUIThread(() =>
			{
				commandBar = new CommandBar();
				commandBar.IsDynamicOverflowEnabled = false;
				//commandBar.IsOpen = true;
				commandBar.IsSticky = true;
				commandBar.Width = 500;

				for (int i = 0; i < 3; ++i)
				{
					addButton("Primary Item #" + i.ToString(), commandBar.PrimaryCommands);
					addButton("Secondary Item #" + i.ToString(), commandBar.SecondaryCommands);
				}

				SetWindowContent(commandBar);
			});
			await WindowHelper.WaitForIdle();

			//UNO TOOD: Fix IsOpen initial load
			await RunOnUIThread(() => commandBar.IsOpen = true);
			await Task.Delay(2000);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Focus the second primary command.");
				((Control)commandBar.PrimaryCommands.GetAt(1)).Focus(FocusState.Keyboard);

				LOG_OUTPUT("Add new primary command.");
				addButton("Added Primary Item", commandBar.PrimaryCommands);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Validate the second primary command still has focus.");
				VERIFY_ARE_EQUAL(commandBar.PrimaryCommands.GetAt(1), (ICommandBarElement)FocusManager.GetFocusedElement(WindowHelper.XamlRoot));

				LOG_OUTPUT("Focus the second secondary command.");
				((Control)commandBar.SecondaryCommands.GetAt(1)).Focus(FocusState.Keyboard);

				LOG_OUTPUT("Add new secondary command.");
				addButton("Added Secondary Item", commandBar.SecondaryCommands);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Validate the second secondary command still has focus.");
				VERIFY_ARE_EQUAL(commandBar.SecondaryCommands.GetAt(1), (ICommandBarElement)(FocusManager.GetFocusedElement(WindowHelper.XamlRoot)));

				LOG_OUTPUT("Clearing all secondary commands. Focus is expected to go to the more button.");
				commandBar.SecondaryCommands.Clear();
			});
			await WindowHelper.WaitForIdle();
			await Task.Delay(2000);
			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Validate the more button has focus.");
				var moreButton = (Button)(TreeHelper.GetVisualChildByName(commandBar, "MoreButton"));
				var focused = (Button)FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
				VERIFY_ARE_EQUAL(moreButton, focused);

				LOG_OUTPUT("Focus fourth primary command, enable dynamic overflow and resize command bar.");
				((Control)commandBar.PrimaryCommands.GetAt(3)).Focus(FocusState.Keyboard);
				commandBar.IsDynamicOverflowEnabled = true;
				commandBar.Width = 250;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Fourth primary command is now in the secondary ItemsControl. Validate it still has focus.");
				VERIFY_ARE_EQUAL(commandBar.PrimaryCommands.GetAt(3), (ICommandBarElement)(FocusManager.GetFocusedElement(WindowHelper.XamlRoot)));

				LOG_OUTPUT("Resize command bar back to its original size.");
				commandBar.Width = 500;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Fourth primary command is back in the primary ItemsControl. Validate it still has focus.");
				VERIFY_ARE_EQUAL(commandBar.PrimaryCommands.GetAt(3), (ICommandBarElement)(FocusManager.GetFocusedElement(WindowHelper.XamlRoot)));
			});
		}

		[TestMethod]

		[Description("Validates that setting CommandBar.IsOpen = true in Closed does not permanently hide labels and the overflow popup as though the CommandBar were still closed.")]
		[TestProperty("Hosting:Mode", "UAP")]
		public async Task CanReopenInClosedHandler()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			AppBarButton appBarButton = null;
			var commandBarClosedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Closed");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				appBarButton = new AppBarButton();
				cmdBar.PrimaryCommands.Append(appBarButton);
				commandBarClosedRegistration.Attach(cmdBar, (s, e) => ((CommandBar)s).IsOpen = true);

				var page = WindowHelper.SetupSimulatedAppPage();
				SetPageContent(cmdBar, page);
				//page.BottomAppar = cmdBar;
			});

			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => cmdBar.IsOpen = false);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(cmdBar.IsOpen);
				VERIFY_IS_FALSE(appBarButton.IsCompact);

				var overflowPopup = (Popup)TreeHelper.GetVisualChildByName(cmdBar, "OverflowPopup");

				VERIFY_IS_TRUE(overflowPopup.IsOpen);

				// We need to close the popup at the end of this test,
				// and without detaching our event handler, closing it
				// is just going to cause it to re-open again.
				// So we need to detach the event handler here.
				commandBarClosedRegistration.Detach();

				cmdBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]

		[Description("Validates that tabbing will not focus the CommandBar when it's ClosedDisplayMode=Hidden and it's closed.")]
		[Ignore("Popup focus is buggy")]
		public async Task CanNotTabIntoWhenClosedAndHidden()
		{
			TestCleanupWrapper cleanup;

			Button button = null;
			CommandBar cmdBar = null;

			var cmdBarGotFocusRegistration = CreateSafeEventRegistration<CommandBar, RoutedEventHandler>("GotFocus");
			bool didCmdBarGetFocus = false;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;

				// Add some content to make sure it doesn't get focus either.
				cmdBar.PrimaryCommands.Append(new AppBarButton());

				cmdBarGotFocusRegistration.Attach(cmdBar, (s, e) => didCmdBarGetFocus = true);

				button = new Button();
				button.Content = "button";

				var root = new Grid();
				root.Children.Append(button);
				root.Children.Append(cmdBar);

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				button.Focus(FocusState.Programmatic);
			});
			await WindowHelper.WaitForIdle();

			await SimulateTabAsync(WindowHelper.WindowContent);
			await WindowHelper.WaitForIdle();

			VERIFY_IS_FALSE(didCmdBarGetFocus);
		}

		[TestMethod]

		[Description("Validates that the overflow menu is not shown when all the items are Collapsed.")]
#if __ANDROID__
		[Ignore("Disabled for failing assertion: https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task DoesNotShowMenuIfSecondaryElementsAreCollapsed()
		{
			TestCleanupWrapper cleanup;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<CommandBar, RoutedEventHandler>("Loaded");

			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				var collapsedAppBarButton = new AppBarButton();
				collapsedAppBarButton.Visibility = Visibility.Collapsed;

				var collapsedAppBarSeparator = new AppBarSeparator();
				collapsedAppBarSeparator.Visibility = Visibility.Collapsed;

				var collapsedAppBarToggleButton = new AppBarToggleButton();
				collapsedAppBarToggleButton.Visibility = Visibility.Collapsed;

				cmdBar = new CommandBar();
				//cmdBar.IsOpen = true;
				cmdBar.SecondaryCommands.Append(collapsedAppBarButton);
				cmdBar.SecondaryCommands.Append(collapsedAppBarSeparator);
				cmdBar.SecondaryCommands.Append(collapsedAppBarToggleButton);

				loadedRegistration.Attach(cmdBar, (s, e) =>
				{
					LOG_OUTPUT("CommandBar.Loaded raised.");
					loadedEvent.Set();
				});

				SetWindowContent(cmdBar);
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			//UNO TODO: IsOpen on load
			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await Task.Delay(2000);

			await RunOnUIThread(() =>
			{
				var overflowContentRoot = (UIElement)TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);
				VERIFY_IS_NOT_NULL(overflowContentRoot);

				VERIFY_ARE_EQUAL(overflowContentRoot.Visibility, Visibility.Collapsed);
			});
		}

		[TestMethod]
		[Description("Validates that the CommandBar opens down/up based on the available space inside the layout bounds.")]
#if __ANDROID__
		[Ignore("CommandBar popup measure glitch with fullscreen")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task ValidateCommandBarOpensInsideLayoutBounds()
		{
			TestCleanupWrapper cleanup;

			await ValidateInlineCommandBarOpenDirection(VerticalAlignment.Top);
			await ValidateInlineCommandBarOpenDirection(VerticalAlignment.Center);
			await ValidateInlineCommandBarOpenDirection(VerticalAlignment.Bottom);
		}

		private async Task ValidateInlineCommandBarOpenDirection(VerticalAlignment alignment)
		{
			CommandBar commandBar = null;
			Page page = null;

			await RunOnUIThread(() =>
			{
				page = WindowHelper.SetupSimulatedAppPage();
				commandBar = new CommandBar();
				commandBar.VerticalAlignment = alignment;
				commandBar.PrimaryCommands.Append(new AppBarButton());
				commandBar.SecondaryCommands.Append(new AppBarButton());
				commandBar.SecondaryCommands.Append(new AppBarButton());
				Grid grid = new Grid();
				grid.Children.Append(commandBar);
				SetPageContent(grid, page);
			});
			await WindowHelper.WaitForIdle();

			await OpenCommandBar(commandBar, OpenMethod.Programmatic);

			UIElement overflowContentRoot = null;

			await RunOnUIThread(() => overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", commandBar));
			await WindowHelper.WaitForIdle();

			Rect commandBarBounds = await ControlHelper.GetBounds(commandBar);
			Rect commandBarOverflowBounds = await ControlHelper.GetBounds((FrameworkElement)overflowContentRoot);

			LOG_OUTPUT($"commandBarBounds:         ({commandBarBounds.X}, {commandBarBounds.Y}, {commandBarBounds.Width}, {commandBarBounds.Height})");
			LOG_OUTPUT($"commandBarOverflowBounds: ({commandBarOverflowBounds.X}, {commandBarOverflowBounds.Y}, {commandBarOverflowBounds.Width}, {commandBarOverflowBounds.Height})");

			//Test that the CommandBarOverflow is positioned correctly
			switch (alignment)
			{
				case VerticalAlignment.Top:
					VERIFY_IS_TRUE(commandBarOverflowBounds.Y >= commandBarBounds.Y);
					break;
				case VerticalAlignment.Center:
					VERIFY_IS_TRUE(commandBarOverflowBounds.Y < commandBarBounds.Y);
					break;
				case VerticalAlignment.Bottom:
					VERIFY_IS_TRUE(commandBarOverflowBounds.Y < commandBarBounds.Y);
					break;
			}
		}

		[TestMethod]

		[Description("Validates that after clicking on an AppBarButton, the CommandBar closes before the button's click handlers execute.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		public void DoesCloseBeforeButtonClickHandlersExecute()
		{

		}

		[TestMethod]

		[Description("Validates CommandBar cycles focus when open.")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("Focus bug with Shift-Tab in Popups.")]
		public async Task DoesCycleFocusWhenOpen()
		{
			var expectedFocusSequence = "[S1][P1][P2][P3][M][P3][P2][P1][S1][M]";
			await DoesCycleFocusWhenOpenWorker(Location.Inline, 5, expectedFocusSequence);
			//	await DoesCycleFocusWhenOpenWorker(Location.Top, 5, expectedFocusSequence);
			//	await DoesCycleFocusWhenOpenWorker(Location.Bottom, 5, expectedFocusSequence);
		}

		private async Task DoesCycleFocusWhenOpenWorker(Location location, int numTabs, string expectedFocusSequence)
		{
			TestCleanupWrapper cleanup;

			Page page = null;
			StackPanel panel = null;
			CommandBar cmdBar = null;
			UIElement secondaryItemsPresenter = null;

			var pageLoadedEvent = new Event();
			var moreButtonGotFocusEvent = new Event();
			var pageLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
			var buttonGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");
			var moreButtonGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");
			var commandBarGotFocusRegistration = CreateSafeEventRegistration<CommandBar, RoutedEventHandler>("GotFocus");
			var overflowGotFocusRegistration = CreateSafeEventRegistration<UIElement, RoutedEventHandler>("GotFocus");

			var focusSequence = "";
			RoutedEventHandler gotFocusHandler = null;

			await RunOnUIThread(() =>
			{
				gotFocusHandler = (s, e) =>
				{
					focusSequence += $"[{((FrameworkElement)e.OriginalSource).Tag}]";
				};

				page = WindowHelper.SetupSimulatedAppPage();
				pageLoadedRegistration.Attach(page, (s, e) => pageLoadedEvent.Set());

				panel = new StackPanel();

				var button = new Button();
				button.Content = "Button";
				button.Tag = "B";
				buttonGotFocusRegistration.Attach(button, gotFocusHandler);
				panel.Children.Append(button);

				cmdBar = (CommandBar)XamlReader.Load(@"
					<CommandBar   >
                        <AppBarButton  Tag=""P1""/>
                        <AppBarButton  Tag=""P2""/>
                        <AppBarButton  Tag=""P3""/>
                        <CommandBar.SecondaryCommands>
                            <AppBarButton  Tag=""S1""/>
                            <AppBarButton  Tag=""S2""/>
                            <AppBarButton  Tag=""S3""/>
                        </CommandBar.SecondaryCommands>
                    </CommandBar>
				");
				commandBarGotFocusRegistration.Attach(cmdBar, gotFocusHandler);

				switch (location)
				{
					case Location.Inline:
						panel.Children.Append(cmdBar);
						break;
					case Location.Top:
						//TODO: TopAppBar not implemented
						//page.TopAppBar = cmdBar;
						break;
					case Location.Bottom:
						//TODO: BottomAppBar not implemented
						//page.BottomAppBar = cmdBar;
						break;
				}

				SetPageContent(panel, page);
			});
			await pageLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// Start with the focus on the moreButton.
				var moreButton = (Button)TreeHelper.GetVisualChildByName(cmdBar, "MoreButton");
				moreButton.Tag = "M";
				moreButtonGotFocusRegistration.Attach(moreButton, (s, e) => moreButtonGotFocusEvent.Set());
				moreButton.Focus(FocusState.Keyboard);

				cmdBar.IsOpen = true;
			});
			await moreButtonGotFocusEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await Task.Delay(2000);

			focusSequence = "";

			await RunOnUIThread(() =>
			{
				var overflowContentRoot = (UIElement)TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", panel);
				secondaryItemsPresenter = GetSecondaryItemsPresenter(cmdBar);
				overflowGotFocusRegistration.Attach(overflowContentRoot, gotFocusHandler);
			});

			// Tab several times to cycle focus through the CommandBar.
			for (int i = 0; i < numTabs; ++i)
			{
				KeyboardHelper.Tab(i == 1 ? secondaryItemsPresenter : cmdBar);
				await WindowHelper.WaitForIdle();
			}

			// Shift-Tab several times to cycle focus through the CommandBar in reverse.
			for (int i = 0; i < numTabs; ++i)
			{
				KeyboardHelper.ShiftTab(i == (numTabs - 1) ? secondaryItemsPresenter : cmdBar);
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT($"Expected focus sequence: {expectedFocusSequence}");
			LOG_OUTPUT($"Actual focus sequence: {focusSequence}");
			VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);

			await EmptyPageContent(page);
		}

		[TestMethod]

		[Description("Validates that a user can tab into the CommandBar's overflow menu when set as a Top/Bottom AppBar.")]
		[Ignore("TopAppBar/BottomAppBar not implemented.")]

		public async Task CanTabIntoOverflowMenuWhenTopOrBottom()
		{
			TestCleanupWrapper cleanup;

			Page page = null;
			CommandBar cmdBar = null;
			AppBarButton appBarButton = null;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				appBarButton = new AppBarButton();

				cmdBar.SecondaryCommands.Append(appBarButton);

				page = WindowHelper.SetupSimulatedAppPage();

				//TODO: TopAppBar not implemented
				//page.TopAppBar = cmdBar;

				SetWindowContent(page);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await WindowHelper.WaitForIdle();

			// Tab once to move into the overflow menu.
			KeyboardHelper.Tab(WindowHelper.WindowContent);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(appBarButton));

				cmdBar.IsOpen = false;

				page.TopAppBar = null;
				//TODO: BottomAppBar not implemented
				//page.BottomAppBar = cmdBar;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await WindowHelper.WaitForIdle();

			// Tab once to move into the overflow menu.
			KeyboardHelper.Tab(WindowHelper.WindowContent);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(appBarButton));

				cmdBar.IsOpen = false;
			});
		}

		[TestMethod]

		[Description("Validates that a minimal closed command bar with only secondary commands is visible.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		public async Task ValidateClosedMinimalCommandBarWithSecondaryCommandsOnlyIsVisible()
		{
			TestCleanupWrapper cleanup;

			CommandBar commandBar = null;

			var openedEvent = new Event();
			var openedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");

			FrameworkElement moreButton = null;

			await RunOnUIThread(() =>
			{
				commandBar = new CommandBar();
				commandBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
				commandBar.VerticalAlignment = VerticalAlignment.Center; // Center it to get it out from under the status bar.
				openedRegistration.Attach(commandBar, (s, e) => openedEvent.Set());

				// Add secondary commands
				var appBarButton = new AppBarButton();
				commandBar.SecondaryCommands.Append(appBarButton);

				var rootPanel = new Grid();
				rootPanel.Children.Append(commandBar);
				SetWindowContent(rootPanel);
			});
			await WindowHelper.WaitForIdle();

			// Try tapping the more button
			await RunOnUIThread(() => moreButton = (FrameworkElement)TreeHelper.GetVisualChildByName(commandBar, "MoreButton"));
			TestServices.InputHelper.Tap(moreButton);
			await openedEvent.WaitForDefault();
		}

		//--------------------------------------------------------------------------------------
		// Test case: Verifies that once you enter a bottom commandbar overflow,
		//            you can exit it by pressing Escape when the focus is on an overflow item
		//--------------------------------------------------------------------------------------
		[TestMethod]

		[Description("Verifies that once you enter a bottom commandbar overflow, you can exit it by pressing Escape and focus is restored to the Expand Button.")]
		[Ignore("BottomAppBar not implemented.")]
		public async Task DoesFocusExpandButtonWithOverflowEscKey()
		{
			TestCleanupWrapper cleanup;
			UIElement secondaryItemsPresenter = null;
			Page rootPage = (Page)XamlReader.Load(@"<Page
				xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
				xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

				<Grid Background=""{ThemeResource ApplicationPageBackgroundThemeBrush}"">
					<StackPanel>
						<CommandBar   x:Name=""topCommandBar"">
							<CommandBar.SecondaryCommands>
								<AppBarButton  Label=""AppBarButton"" VerticalAlignment=""Top"" x:Name=""topOverflowAppBarBtn0""/>
								<AppBarToggleButton  Label=""AppBarToggleButton"" VerticalAlignment=""Top"" x:Name=""topOverflowAppBarBtn1""/>
								<AppBarSeparator />
							</CommandBar.SecondaryCommands>
							<AppBarButton  Label=""AppBarButton"" Icon=""FourBars"" x:Name=""topAppBarBtn0""/>
							<AppBarToggleButton  Label=""AppBarToggleButton"" Icon=""HangUp"" x:Name=""topAppBarBtn1""/>
							<AppBarSeparator />
						</CommandBar>
						<Button x:Name=""btn"" Width=""250"" Height=""75"" Content=""Click!""/>
					</StackPanel>
				</Grid>
				<Page.BottomAppBar>
					<CommandBar   x:Name=""bottomCommandBar"">
						<AppBarToggleButton  Icon=""Shuffle"" Label=""Shuffle"" x:Name=""bottomAppBarBtn0""/>
						<AppBarToggleButton  Icon=""RepeatAl"" Label=""Repeat"" x:Name=""bottomAppBarBtn1""/>
						<AppBarSeparator />
						<AppBarButton  Icon=""Back"" Label=""Back"" x:Name=""bottomAppBarBtn2""/>
						<AppBarButton  Icon=""Stop"" Label=""Stop"" x:Name=""bottomAppBarBtn3""/>
						<AppBarButton  Icon=""Play"" Label=""Play"" x:Name=""bottomAppBarBtn4""/>

						<CommandBar.SecondaryCommands>
							<AppBarButton  Icon=""Like"" Label=""Like"" x:Name=""bottomOverflowAppBarBtn0""/>
							<AppBarButton  Icon=""Dislike"" Label=""Dislike"" x:Name=""bottomOverflowAppBarBtn1""/>
						</CommandBar.SecondaryCommands>
					</CommandBar>
				</Page.BottomAppBar>
			</Page>");
			VERIFY_IS_NOT_NULL(rootPage);

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				loadedRegistration.Attach(rootPage, (s, e) =>
				{
					LOG_OUTPUT("[RootPage]: Loaded");
					loadedEvent.Set();
				});

				SetWindowContent(rootPage);
			});
			await WindowHelper.WaitForIdle();
			await loadedEvent.WaitForDefault();

			CommandBar commandBar = null;
			Button expandButton = null;
			var openedEvent = new Event();
			var closedEvent = new Event();
			var openedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Closed");

			await RunOnUIThread(() =>
			{
				//TODO: BottomAppBar not implemented
				//commandBar = (CommandBar)rootPage.BottomAppBar;
				VERIFY_IS_NOT_NULL(commandBar);

				expandButton = (Button)TreeHelper.GetVisualChildByName(commandBar, "MoreButton");
				VERIFY_IS_NOT_NULL(expandButton);

				LOG_OUTPUT("Setting focus on bottomAppBarExpandButton");
				expandButton.Focus(FocusState.Keyboard);

				openedRegistration.Attach(commandBar, (s, e) =>
				{
					LOG_OUTPUT("[bottomCommandBar]: Opened Event Fired");
					openedEvent.Set();
				});

				closedRegistration.Attach(commandBar, (s, e) =>
				{
					LOG_OUTPUT("[bottomCommandBar]: Closed Event Fired");
					closedEvent.Set();
				});
			});
			await WindowHelper.WaitForIdle();
			KeyboardHelper.Enter(rootPage);
			await openedEvent.WaitForDefault();

			var gotFocusRegistration = CreateSafeEventRegistration<UIElement, RoutedEventHandler>("GotFocus");
			var gotFocusEvent = new Event();

			await RunOnUIThread(() =>
			{
				var overflowAppBarBtn0 = (AppBarButton)commandBar.SecondaryCommands.GetAt(0);
				secondaryItemsPresenter = GetSecondaryItemsPresenter(commandBar);
				VERIFY_IS_NOT_NULL(overflowAppBarBtn0);

				gotFocusRegistration.Attach(overflowAppBarBtn0, (s, e) =>
				{
					LOG_OUTPUT("[bottomOverflowAppBarBtn0]: Got Focus Event Fired");
					gotFocusEvent.Set();
				});
			});
			KeyboardHelper.Up(secondaryItemsPresenter);  //expandButton->bottomOverflowAppBarBtn1
			KeyboardHelper.Up(secondaryItemsPresenter);  //bottomOverflowAppBarBtn1->bottomOverflowAppBarBtn0
			await WindowHelper.WaitForIdle();
			await gotFocusEvent.WaitForDefault();

			KeyboardHelper.Escape(secondaryItemsPresenter);
			await closedEvent.WaitForDefault();
			await RunOnUIThread(() => VERIFY_IS_TRUE(expandButton.FocusState == FocusState.Keyboard));
		}

		[TestMethod]

		[Description("Validate the size of the CommandBar menu and its items based on different input modes (mouse, touch, etc.).")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		public async Task ValidateMenuSizingForDifferentInputModes()
		{
			TestCleanupWrapper cleanup;

			// The size of the CommandBar menu and the size of the items in it (AppBarButton/AppBarToggleButton) change
			// based on whether the CommandBar was opened via Touch or not.
			// We test opening the CommandBar using Touch, Mouse, Keyboard and Programmatically and validate that the
			// menu and its items size correctly in each case.

			// UNO TODO: Not Implemented
			WindowHelper.SetWindowSizeOverride(new Size(500, 600));

			// The expected sizes of the menu/items.
			// For the first two we can read the expected values from generic.xaml. For the latter two, there is no corresponding resource
			// so we just hard-code the values here.
			double expectedMenuWidth_Touch = 0;
			double expectedMenuWidth_NonTouch = 0;
			//double expectedMenuItemHeight_Touch = 40;
#if __IOS__
			double expectedMenuItemHeight_NonTouch = 31;
#elif __SKIA__
			double expectedMenuItemHeight_NonTouch = 30;
#else
			double expectedMenuItemHeight_NonTouch = 32;
#endif

			CommandBar cmdBar = null;
			AppBarButton appBarButton = null;
			AppBarToggleButton appBarToggleButton = null;
			FrameworkElement overflowContentRoot = null;

			Grid root = null;

			await RunOnUIThread(() =>
			{
				root = (Grid)XamlReader.Load(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" >
                        <CommandBar x:Name=""cmdBar"" VerticalAlignment=""Bottom"" HorizontalAlignment=""Center"" >
                            <CommandBar.SecondaryCommands>
                                <AppBarButton x:Name=""appBarButton"" Label=""Item 1"" />
                                <AppBarToggleButton x:Name=""appBarToggleButton"" Label=""Item 2"" />
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </Grid>");

				cmdBar = (CommandBar)root.FindName("cmdBar");

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				expectedMenuWidth_Touch = (double)cmdBar.Resources.Lookup("CommandBarOverflowTouchMinWidth");
				expectedMenuWidth_NonTouch = (double)cmdBar.Resources.Lookup("CommandBarOverflowMinWidth");
			});

			//BEGIN UNO ONLY: FindName does not work for secondary commands unless the popup is open
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await RunOnUIThread(() =>
			{
				appBarButton = (AppBarButton)root.FindName("appBarButton");
				appBarToggleButton = (AppBarToggleButton)root.FindName("appBarToggleButton");
			});
			await CloseCommandBar(cmdBar);
			await WindowHelper.WaitForIdle();
			//END UNO ONLY

			// TODO: InputDeviceType not supported, touch input modes are not set

			//// Open via Touch:
			//await OpenCommandBar(cmdBar, OpenMethod.Touch);
			//await RunOnUIThread(() =>
			//{
			//	overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);

			//	VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, expectedMenuWidth_Touch);
			//	VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, expectedMenuWidth_Touch);
			//	VERIFY_ARE_EQUAL(appBarButton.ActualHeight, expectedMenuItemHeight_Touch);
			//	VERIFY_ARE_EQUAL(appBarToggleButton.ActualHeight, expectedMenuItemHeight_Touch);
			//});
			//await CloseCommandBar(cmdBar);

			//// Open via Gamepad:
			//await OpenCommandBar(cmdBar, OpenMethod.Gamepad);
			//await RunOnUIThread(() =>
			//{
			//	overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);

			//	VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, expectedMenuWidth_Touch);
			//	VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, expectedMenuWidth_Touch);
			//	VERIFY_ARE_EQUAL(appBarButton.ActualHeight, expectedMenuItemHeight_Touch);
			//	VERIFY_ARE_EQUAL(appBarToggleButton.ActualHeight, expectedMenuItemHeight_Touch);
			//});
			//await CloseCommandBar(cmdBar);

			//// Open programmatically:
			//// We expect a programmatically opened command bar to size based on the most recently used input mode (touch in this case)
			//await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			//await RunOnUIThread(() =>
			//{
			//	overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);

			//	VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, expectedMenuWidth_Touch);
			//	VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, expectedMenuWidth_Touch);
			//	VERIFY_ARE_EQUAL(appBarButton.ActualHeight, expectedMenuItemHeight_Touch);
			//	VERIFY_ARE_EQUAL(appBarToggleButton.ActualHeight, expectedMenuItemHeight_Touch);
			//});
			//await CloseCommandBar(cmdBar);

			//UNO SPECIFIC: We need to Floor the ActualHeights as they are slightly larger than 32.0

			// Open via Mouse:
			await OpenCommandBar(cmdBar, OpenMethod.Mouse);
			await RunOnUIThread(() =>
			{
				overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);

				VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, expectedMenuWidth_NonTouch);
				VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, expectedMenuWidth_NonTouch);
				VERIFY_ARE_VERY_CLOSE(Math.Round(appBarButton.ActualHeight), expectedMenuItemHeight_NonTouch, tolerance: 4);
				VERIFY_ARE_VERY_CLOSE(Math.Round(appBarToggleButton.ActualHeight), expectedMenuItemHeight_NonTouch, tolerance: 4);
			});
			await CloseCommandBar(cmdBar);

			// Open via Keyboard (Keyboard not supported on Android)
#if !__ANDROID__ && !__IOS__
			await OpenCommandBar(cmdBar, OpenMethod.Keyboard);
			await RunOnUIThread(() =>
			{
				overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);

				VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, expectedMenuWidth_NonTouch);
				VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, expectedMenuWidth_NonTouch);
				VERIFY_ARE_VERY_CLOSE(Math.Round(appBarButton.ActualHeight), expectedMenuItemHeight_NonTouch, tolerance: 4);
				VERIFY_ARE_VERY_CLOSE(Math.Round(appBarToggleButton.ActualHeight), expectedMenuItemHeight_NonTouch, tolerance: 4);
			});
			await CloseCommandBar(cmdBar);
#endif
			// Open programmatically:
			// We expect a programmatically opened command bar to size based on the most recently used input mode (keyboard in this case)
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await RunOnUIThread(() =>
			{
				overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);

				VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, expectedMenuWidth_NonTouch);
				VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, expectedMenuWidth_NonTouch);
				VERIFY_ARE_VERY_CLOSE(Math.Round(appBarButton.ActualHeight), expectedMenuItemHeight_NonTouch, tolerance: 4);
				VERIFY_ARE_VERY_CLOSE(Math.Round(appBarToggleButton.ActualHeight), expectedMenuItemHeight_NonTouch, tolerance: 4);
			});
			await CloseCommandBar(cmdBar);
		}

		[TestMethod]

		[Description("Validates the sizing and the border for a full width menu.")]
		[Ignore("WindowHelper.SetWindowSizeOverride not implemented.")]
		public async Task ValidateVisualStateForFullWidthMenu()
		{
			TestCleanupWrapper cleanup;

			// At small window sizes (<=480px wide), the CommandBar stretches to fill the full width of the window.
			// When in this mode, we do not draw a border on all 4 sides, instead we only draw one at the edge
			// opposite to the direction the CommandBar opens.
			//
			// We test this scenario for a CommandBar opening UP and opening DOWN.
			//
			// We validate:
			//   a. The value of BorderThickness on the overflow menu.
			//   b. The MinWidth, MaxWidth and ActualWidth of the overflow container are all equal to
			//      the window width.
			//   c. The VisualState of the CommandBarOverflowPresenter ("FullWidthOpenUp" or "FullWidthOpenDown").

			float windowWidth = 300;

			WindowHelper.SetWindowSizeOverride(new Size(windowWidth, 600));

			CommandBar cmdBar = null;

			FrameworkElement overflowContentRoot = null; // The template part of the CommandBar that hosts the CommandBarOverFlowPresenter
			CommandBarOverflowPresenter overflowPresenter = null; // The CommandBarOverflowPresenter
			Grid overflowPresenterRoot = null; // The template root of the CommandBarOverflowPresenter. The Border is drawn on this element.

			await RunOnUIThread(() =>
			{
				var root = (Grid)XamlReader.Load(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" >
                        <CommandBar   x:Name=""cmdBar"" VerticalAlignment=""Bottom"" >
                            <CommandBar.SecondaryCommands>
                                <AppBarButton  Label=""Item 1"" />
                                <AppBarToggleButton  Label=""Item 2"" />
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </Grid>");

				cmdBar = (CommandBar)root.FindName("cmdBar");

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			// CommandBar starts aligned to the Bottom of the Window. It will open up.
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await RunOnUIThread(async () =>
			{
				overflowContentRoot = TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);
				overflowPresenter = TreeHelper.GetVisualChildByType<CommandBarOverflowPresenter>(overflowContentRoot);
				overflowPresenterRoot = (Grid)TreeHelper.GetVisualChildByName(overflowPresenter, "LayoutRoot");

				VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, windowWidth);
				VERIFY_ARE_EQUAL(overflowContentRoot.MaxWidth, windowWidth);
				VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, windowWidth);
				VERIFY_ARE_EQUAL(overflowPresenter.ActualWidth, windowWidth);
				VERIFY_ARE_EQUAL(ThicknessHelper.FromLengths(0, 1, 0, 0), overflowPresenterRoot.BorderThickness);
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(overflowPresenter, "DisplayModeStates", "FullWidthOpenUp"));
			});
			await CloseCommandBar(cmdBar);

			// Move the CommandBar to the Top of the Window so that it opens down.
			await RunOnUIThread(() => cmdBar.VerticalAlignment = VerticalAlignment.Top);
			await WindowHelper.WaitForIdle();

			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await RunOnUIThread(async () =>
			{
				VERIFY_ARE_EQUAL(overflowContentRoot.ActualWidth, windowWidth);
				VERIFY_ARE_EQUAL(overflowContentRoot.MaxWidth, windowWidth);
				VERIFY_ARE_EQUAL(overflowContentRoot.MinWidth, windowWidth);
				VERIFY_ARE_EQUAL(overflowPresenter.ActualWidth, windowWidth);
				VERIFY_ARE_EQUAL(ThicknessHelper.FromLengths(0, 0, 0, 1), overflowPresenterRoot.BorderThickness);
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(overflowPresenter, "DisplayModeStates", "FullWidthOpenDown"));
			});
			await CloseCommandBar(cmdBar);
		}

		[TestMethod]

		[Description("Verifies that items moved between Primary and Secondary commands go to the correct VisualStates.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task MoveItemsBetweenPrimaryAndSecondaryCommands()
		{
			TestCleanupWrapper cleanup;

			// AppBarButtons/AppBarToggleButtons get put into different visual states depending on whether
			// they are in PrimaryCommands or SecondayCommands.
			// Specifically:
			//   a. Items in SecondaryCommands should be in the Overflow state.
			//   b. If the SecondaryCommands contains both AppBarButtons and AppBarToggleButtons, the AppBarButtons should be
			//      in the OverflowWithToggleButtons state (instead of Overflow state).
			//   c. Items in SecondaryCommands should be in TouchInputMode state if the CommandBar was opened by touch.
			//   d. Items in the PrimaryCommands should never be in any of the above mentioned states. They should be in the
			//      "FullSize" (when the CommandBar is open) and "InputModeDefault" states.
			//
			// We verify that moving items between these two collections results in the items always being in the correct states.

			// UNO TODO:Not implemented
			WindowHelper.SetWindowSizeOverride(new Size(400, 600));

			CommandBar cmdBar = null;
			AppBarButton appBarButton1 = null;
			AppBarButton appBarButton2 = null;
			AppBarToggleButton appBarToggleButton1 = null;
			AppBarToggleButton appBarToggleButton2 = null;
			Grid root = null;

			await RunOnUIThread(() =>
			{
				root = (Grid)XamlReader.Load(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""    >
                        <CommandBar x:Name=""cmdBar"" VerticalAlignment=""Bottom"" HorizontalAlignment=""Center"" >
                            <CommandBar.PrimaryCommands>
                                <AppBarButton  x:Name=""appBarButton1"" Label=""Item 1"" />
                                <AppBarToggleButton  x:Name=""appBarToggleButton1"" Label=""Item 2"" />
                            </CommandBar.PrimaryCommands>
                            <CommandBar.SecondaryCommands>
                                <AppBarButton  x:Name=""appBarButton2"" Label=""Item 4"" />
                                <AppBarToggleButton  x:Name=""appBarToggleButton2"" Label=""Item 5"" />
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </Grid>");

				cmdBar = (CommandBar)root.FindName("cmdBar");

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			//BEGIN UNO ONLY: FindName does not work for secondary commands unless the popup is open
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await RunOnUIThread(() =>
			{
				appBarButton1 = (AppBarButton)root.FindName("appBarButton1");
				appBarButton2 = (AppBarButton)root.FindName("appBarButton2");
				appBarToggleButton1 = (AppBarToggleButton)root.FindName("appBarToggleButton1");
				appBarToggleButton2 = (AppBarToggleButton)root.FindName("appBarToggleButton2");
			});
			await CloseCommandBar(cmdBar);
			await WindowHelper.WaitForIdle();
			//END UNO ONLY

			await OpenCommandBar(cmdBar, OpenMethod.Touch);

			// Verify PrimaryCommands:
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton1, "ApplicationViewStates", "FullSize"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton1, "InputModeStates", "InputModeDefault"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton1, "ApplicationViewStates", "FullSize"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton1, "InputModeStates", "InputModeDefault"));

			// Verify SecondaryCommands:
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "ApplicationViewStates", "OverflowWithToggleButtons"));
			//UNO Only: TouchInputMode Not Supported
			//VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "InputModeStates", "TouchInputMode"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "InputModeStates", "InputModeDefault"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton2, "ApplicationViewStates", "Overflow"));
			//UNO Only: TouchInputMode Not Supported
			//VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton2, "InputModeStates", "TouchInputMode"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton2, "InputModeStates", "InputModeDefault"));

			// Move AppBarToggleButton from Secondary to Primary commands:
			await RunOnUIThread(() =>
			{
				ControlHelper.RemoveItem(cmdBar.SecondaryCommands, appBarToggleButton2);
				cmdBar.PrimaryCommands.Append(appBarToggleButton2);
			});
			await WindowHelper.WaitForIdle();

			// appBarButton2 should switch from "OverflowWithToggleButtons" to "Overflow"
			// (It is no longer sharing the overflow menu with any AppBarToggleButtons):
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "ApplicationViewStates", "Overflow"));
			//UNO Only: TouchInputMode Not Supported
			//VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "InputModeStates", "TouchInputMode"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "InputModeStates", "InputModeDefault"));

			// appBarToggleButton2 should no longer be in Overflow state or in Touch state:
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton2, "ApplicationViewStates", "FullSize"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton2, "InputModeStates", "InputModeDefault"));

			// Move AppBarButton and AppBarToggleButton from Primary to Secondary:
			await RunOnUIThread(() =>
			{
				ControlHelper.RemoveItem(cmdBar.PrimaryCommands, appBarButton1);
				ControlHelper.RemoveItem(cmdBar.PrimaryCommands, appBarToggleButton1);
				cmdBar.SecondaryCommands.Append(appBarButton1);
				cmdBar.SecondaryCommands.Append(appBarToggleButton1);
			});
			await WindowHelper.WaitForIdle();

			// appBarButton2 should switch from "Overflow" to "OverflowWithToggleButtons":
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "ApplicationViewStates", "OverflowWithToggleButtons"));
			//UNO Only: TouchInputMode Not Supported
			//VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "InputModeStates", "TouchInputMode"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "InputModeStates", "InputModeDefault"));

			// appBarButton1 and appBarButton2 should switch to Overflow and Touch states:
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton1, "ApplicationViewStates", "OverflowWithToggleButtons"));
			//UNO Only: TouchInputMode Not Supported
			//VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton1, "InputModeStates", "TouchInputMode"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton1, "InputModeStates", "InputModeDefault"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton1, "ApplicationViewStates", "Overflow"));
			//UNO Only: TouchInputMode Not Supported
			//VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton1, "InputModeStates", "TouchInputMode"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarToggleButton1, "InputModeStates", "InputModeDefault"));

			// Move AppBarButton from Secondary to Primary
			await RunOnUIThread(() =>
			{
				ControlHelper.RemoveItem(cmdBar.SecondaryCommands, appBarButton2);
				cmdBar.PrimaryCommands.Append(appBarButton2);
			});
			await WindowHelper.WaitForIdle();

			// appBarButton2 should no longer be in Overflow state or in Touch state:
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "ApplicationViewStates", "FullSize"));
			VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(appBarButton2, "InputModeStates", "InputModeDefault"));

			await CloseCommandBar(cmdBar);
		}

		[TestMethod]

		[Description("Validates the ActualWidth & Height of AppBar in various configurations.")]
		public async Task ValidateFootprint()
		{
			TestCleanupWrapper cleanup;

			//UNO ONLY: SetWindowSizeOverride is not supported, so we are fullscreen
			//double expectedCommandBarWidth = 500;

			double expectedCommandBarWidth = WindowHelper.IsXamlIsland ? WindowHelper.XamlRoot.Size.Width : WindowHelper.CurrentTestWindow!.Bounds.Width;

#if __IOS__
			await RunOnUIThread(() =>
			{
				expectedCommandBarWidth = NativeWindowWrapper.Instance.GetWindowSize().Width;
			});
#endif
			double expectedCommandBarCompactClosedHeight = 48;
			double expectedCommandBarCompactOpenHeight = 48;

			double expectedCommandBarMinimalClosedHeight = 24;
			double expectedCommandBarMinimalOpenHeight = 24;

			double expectedCommandBarHiddenClosedHeight = 0;
			double expectedCommandBarHiddenOpenHeight = 0;

			CommandBar cmdBarCompactClosed = null;
			CommandBar cmdBarCompactOpen = null;
			CommandBar cmdBarMinimalClosed = null;
			CommandBar cmdBarMinimalOpen = null;
			CommandBar cmdBarHiddenClosed = null;
			CommandBar cmdBarHiddenOpen = null;

			WindowHelper.SetWindowSizeOverride(new Size(500, 600));

			await RunOnUIThread(() =>
			{
				var rootPanel = (StackPanel)XamlReader.Load(@"<StackPanel   xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <CommandBar  x:Name=""cmdBarCompactClosed"" IsOpen=""False"" ClosedDisplayMode=""Compact""/>
                        <CommandBar  x:Name=""cmdBarCompactOpen"" IsOpen=""True"" ClosedDisplayMode=""Compact""/>
                        <CommandBar  x:Name=""cmdBarMinimalClosed"" IsOpen=""False"" ClosedDisplayMode=""Minimal""/>
                        <CommandBar  x:Name=""cmdBarMinimalOpen"" IsOpen=""True"" ClosedDisplayMode=""Minimal""/>
                        <CommandBar  x:Name=""cmdBarHiddenClosed"" IsOpen=""False"" ClosedDisplayMode=""Hidden""/>
                        <CommandBar  x:Name=""cmdBarHiddenOpen"" IsOpen=""True"" ClosedDisplayMode=""Hidden""/>
                    </StackPanel>");

				cmdBarCompactClosed = (CommandBar)rootPanel.FindName("cmdBarCompactClosed");
				cmdBarCompactOpen = (CommandBar)rootPanel.FindName("cmdBarCompactOpen");
				cmdBarMinimalClosed = (CommandBar)rootPanel.FindName("cmdBarMinimalClosed");
				cmdBarMinimalOpen = (CommandBar)rootPanel.FindName("cmdBarMinimalOpen");
				cmdBarHiddenClosed = (CommandBar)rootPanel.FindName("cmdBarHiddenClosed");
				cmdBarHiddenOpen = (CommandBar)rootPanel.FindName("cmdBarHiddenOpen");

				SetWindowContent(rootPanel);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(cmdBarCompactClosed.ActualWidth, expectedCommandBarWidth);
				VERIFY_ARE_EQUAL(cmdBarCompactClosed.ActualHeight, expectedCommandBarCompactClosedHeight);

				VERIFY_ARE_EQUAL(cmdBarCompactOpen.ActualWidth, expectedCommandBarWidth);
				VERIFY_ARE_EQUAL(cmdBarCompactOpen.ActualHeight, expectedCommandBarCompactOpenHeight);

				VERIFY_ARE_EQUAL(cmdBarMinimalClosed.ActualWidth, expectedCommandBarWidth);
				VERIFY_ARE_EQUAL(cmdBarMinimalClosed.ActualHeight, expectedCommandBarMinimalClosedHeight);

				VERIFY_ARE_EQUAL(cmdBarMinimalOpen.ActualWidth, expectedCommandBarWidth);
				VERIFY_ARE_EQUAL(cmdBarMinimalOpen.ActualHeight, expectedCommandBarMinimalOpenHeight);

				VERIFY_ARE_EQUAL(cmdBarHiddenClosed.ActualWidth, expectedCommandBarWidth);
				VERIFY_ARE_EQUAL(cmdBarHiddenClosed.ActualHeight, expectedCommandBarHiddenClosedHeight);

				VERIFY_ARE_EQUAL(cmdBarHiddenOpen.ActualWidth, expectedCommandBarWidth);
				VERIFY_ARE_EQUAL(cmdBarHiddenOpen.ActualHeight, expectedCommandBarHiddenOpenHeight);
			});
		}

		[TestMethod]

		[Description("Validates that setting DefaultLayoutPosition on the CommandBar propagates down to AppBarButtons and AppBarToggleButtons.")]
		[TestProperty("Hosting:Mode", "UAP")]
		public async Task ValidateDefaultLayoutPositionPropagates()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			AppBarButton btn1 = null;
			AppBarToggleButton btn2 = null;
			AppBarButton btn3 = null;
			AppBarToggleButton btn4 = null;
			AppBarButton btn5 = null;

			var loadedEvent = new Event();
			var pageLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
			var appBarButtonLoadedRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;

				btn1 = new AppBarButton();
				btn1.Label = "btn1";
				cmdBar.PrimaryCommands.Append(btn1);

				btn2 = new AppBarToggleButton();
				btn2.Label = "btn2";
				cmdBar.PrimaryCommands.Append(btn2);

				btn3 = new AppBarButton();
				btn3.Label = "btn3";
				btn3.LabelPosition = CommandBarLabelPosition.Collapsed;
				cmdBar.PrimaryCommands.Append(btn3);

				btn4 = new AppBarToggleButton();
				btn4.Label = "btn4";
				btn4.LabelPosition = CommandBarLabelPosition.Collapsed;
				cmdBar.PrimaryCommands.Append(btn4);

				var page = WindowHelper.SetupSimulatedAppPage();

				//UNO TODO: BottomAppBar not implemented.
				SetPageContent(cmdBar, page);
				//page.BottomAppBar = cmdBar;

				pageLoadedRegistration.Attach(page, (s, e) => loadedEvent.Set());
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				LOG_OUTPUT("DefaultLabelPosition initialized to Right.  Buttons without a LabelPosition set should be in the visual state LabelOnRight.");
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn1, "ApplicationViewStates", "LabelOnRight"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn2, "ApplicationViewStates", "LabelOnRight"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn3, "ApplicationViewStates", "LabelCollapsed"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn4, "ApplicationViewStates", "LabelCollapsed"));

				LOG_OUTPUT("Setting DefaultLabelPosition to Collapsed.  All buttons should be in the visual state LabelCollapsed.");
				cmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn1, "ApplicationViewStates", "LabelCollapsed"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn2, "ApplicationViewStates", "LabelCollapsed"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn3, "ApplicationViewStates", "LabelCollapsed"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn4, "ApplicationViewStates", "LabelCollapsed"));

				LOG_OUTPUT("Setting DefaultLabelPosition to Bottom.  Buttons without a LabelPosition set should be in the visual state Compact.");
				cmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn1, "ApplicationViewStates", "Compact"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn2, "ApplicationViewStates", "Compact"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn3, "ApplicationViewStates", "LabelCollapsed"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn4, "ApplicationViewStates", "LabelCollapsed"));

				LOG_OUTPUT("Setting DefaultLabelPosition to Right.  Buttons without a LabelPosition set should be in the visual state LabelOnRight.");
				cmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn1, "ApplicationViewStates", "LabelOnRight"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn2, "ApplicationViewStates", "LabelOnRight"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn3, "ApplicationViewStates", "LabelCollapsed"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn4, "ApplicationViewStates", "LabelCollapsed"));

				LOG_OUTPUT("Adding a new AppBarButton.  Its visual state should become LabelOnRight.");
				btn5 = new AppBarButton();
				btn5.Label = "btn5";
				cmdBar.PrimaryCommands.Append(btn5);

				appBarButtonLoadedRegistration.Attach(btn5, (s, e) => loadedEvent.Set());
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(btn5, "ApplicationViewStates", "LabelOnRight"));
			});
		}

		[TestMethod]

		[Description("Validates the overflow button is hidden when told to be hidden, or when there's nothing to be shown by clicking it, with no app bar buttons.")]
		public async Task ValidateOverflowButtonHidesWhenAppropriateWithNoAppBarButtons()
		{
			await ValidateOverflowButtonHidesWhenAppropriate(addPrimary: false, addSecondary: false);
		}


		[TestMethod]

		[Description("Validates the overflow button is hidden when told to be hidden, or when there's nothing to be shown by clicking it, with primary app bar buttons.")]
		public async Task ValidateOverflowButtonHidesWhenAppropriateWithPrimaryAppBarButtons()
		{
			await ValidateOverflowButtonHidesWhenAppropriate(addPrimary: true, addSecondary: false);
		}

		[TestMethod]

		[Description("Validates the overflow button is hidden when told to be hidden, or when there's nothing to be shown by clicking it, with secondary app bar buttons.")]
		public async Task ValidateOverflowButtonHidesWhenAppropriateWithSecondaryAppBarButtons()
		{
			await ValidateOverflowButtonHidesWhenAppropriate(addPrimary: false, addSecondary: true);
		}

		[TestMethod]

		[Description("Validates the overflow button is hidden when told to be hidden, or when there's nothing to be shown by clicking it, with both primary and secondary app bar buttons.")]
		public async Task ValidateOverflowButtonHidesWhenAppropriateWithPrimaryAndSecondaryAppBarButtons()
		{
			await ValidateOverflowButtonHidesWhenAppropriate(addPrimary: true, addSecondary: true);
		}

		// Validate the dynamic overflow behavior with on and off IsDynamicOverflowEnabled property
		[TestMethod]

		[Description("Validates the dynamic overflow On/Off.")]
		public async Task ValidateDynamicOverflowOnOff()
		{
			TestCleanupWrapper cleanup;

			int numButtonsToAddExtraToPrimary = 5;
			int numButtonsToAddToSecondary = 3;

			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: true, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			await RunOnUIThread(() =>
			{
				cmdBar.IsDynamicOverflowEnabled = false;
				LOG_OUTPUT("DynamicOverflow is disabled!");
			});
			await WindowHelper.WaitForIdle();

			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: false, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			var dynamicOverflowItemsChangingEvent = new Event();
			var dynamicOverflowItemsChangingRegistration = CreateSafeEventRegistration<CommandBar, TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs>>("DynamicOverflowItemsChanging");

			await RunOnUIThread(() =>
			{
				dynamicOverflowItemsChangingRegistration.Attach(cmdBar, (s, e) => dynamicOverflowItemsChangingEvent.Set());

				cmdBar.IsDynamicOverflowEnabled = true;
				LOG_OUTPUT("DynamicOverflow is enabled!");
			});
			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: true, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
		}

		[TestMethod]

		[Description("Validates the dynamic overflow with windows size change.")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("WindowHelper.SetWindowSizeOverride not implemented.")]
		public async Task ValidateDynamicOverflowByChangingWindowsSizeOverride()
		{
			TestCleanupWrapper cleanup;

			int numButtonsToAddExtraToPrimary = 0;
			int numButtonsToAddToSecondary = 3;

			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: false, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			WindowHelper.SetWindowSizeOverride(new Size(300, 600));

			numButtonsToAddExtraToPrimary = 2;
			(page, cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: true, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
		}

		[TestMethod]

		[Description("Validates the dynamic overflow for adding and removing the primary items.")]
		[TestProperty("Hosting:Mode", "UAP")]
		public async Task ValidateDynamicOverflowAddRemovePrimaryItems()
		{
			TestCleanupWrapper cleanup;

			int numButtonsToAddExtraToPrimary = 5;
			int numButtonsToAddToSecondary = 3;

			AppBarButton addedButton1 = null;
			AppBarButton addedButton2 = null;
			AppBarButton addedButton3 = null;

			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: true, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			await RunOnUIThread(() =>
			{
				int primaryCommandCount = cmdBar.PrimaryCommands.Count;

				addedButton1 = new AppBarButton();
				cmdBar.PrimaryCommands.Append(addedButton1);

				VERIFY_IS_TRUE(addedButton1 == cmdBar.PrimaryCommands.GetAt(primaryCommandCount));

				addedButton2 = new AppBarButton();
				cmdBar.PrimaryCommands.Insert(0, addedButton2);

				VERIFY_IS_TRUE(addedButton2 == cmdBar.PrimaryCommands.GetAt(0));

				addedButton3 = new AppBarButton();
				cmdBar.SecondaryCommands.Insert(0, addedButton3);

				VERIFY_IS_TRUE(addedButton3 == cmdBar.SecondaryCommands.GetAt(0));
			});
			await WindowHelper.WaitForIdle();

			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(addedButton1.IsInOverflow);
				VERIFY_IS_FALSE(addedButton2.IsInOverflow);
				VERIFY_IS_TRUE(addedButton3.IsInOverflow);

				cmdBar.PrimaryCommands.RemoveAt(0);
				VERIFY_IS_FALSE(addedButton2 == cmdBar.PrimaryCommands.GetAt(0));

				cmdBar.SecondaryCommands.RemoveAt(0);
				VERIFY_IS_FALSE(addedButton3 == cmdBar.PrimaryCommands.GetAt(0));
			});
			await WindowHelper.WaitForIdle();

			await CloseCommandBar(cmdBar);
		}

		[TestMethod]

		[Description("Validates the AppBarSeparator with dynamic overflow operation.")]
		[TestProperty("Hosting:Mode", "UAP")]
		public async Task ValidateDynamicOverflowAppBarSeparator()
		{
			TestCleanupWrapper cleanup;

			int numButtonsToAddExtraToPrimary = 0;
			int numButtonsToAddToSecondary = 3;

			AppBarSeparator addedSeparator1 = null;
			AppBarSeparator addedSeparator2 = null;
			AppBarButton addedButton1 = null;
			AppBarButton addedButton2 = null;

			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			await RunOnUIThread(() =>
			{
				int primaryCommandCount = cmdBar.PrimaryCommands.Count;

				addedSeparator1 = new AppBarSeparator();
				cmdBar.PrimaryCommands.Append(addedSeparator1);

				VERIFY_IS_TRUE(addedSeparator1 == cmdBar.PrimaryCommands.GetAt(primaryCommandCount));

				addedSeparator2 = new AppBarSeparator();
				cmdBar.PrimaryCommands.Insert(0, addedSeparator2);

				VERIFY_IS_TRUE(addedSeparator2 == cmdBar.PrimaryCommands.GetAt(0));

				addedButton1 = new AppBarButton();
				cmdBar.PrimaryCommands.Insert(0, addedButton1);

				VERIFY_IS_TRUE(addedButton1 == cmdBar.PrimaryCommands.GetAt(0));

				addedButton2 = new AppBarButton();
				cmdBar.PrimaryCommands.Insert(0, addedButton2);

				VERIFY_IS_TRUE(addedButton2 == cmdBar.PrimaryCommands.GetAt(0));
			});
			await WindowHelper.WaitForIdle();

			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);

			await RunOnUIThread(() =>
			{
				VERIFY_IS_FALSE(addedSeparator1.IsInOverflow);
				VERIFY_IS_FALSE(addedSeparator2.IsInOverflow);
				VERIFY_IS_FALSE(addedButton1.IsInOverflow);
				VERIFY_IS_FALSE(addedButton2.IsInOverflow);

				cmdBar.PrimaryCommands.RemoveAt(0);
				cmdBar.PrimaryCommands.RemoveAt(0);
			});
			await WindowHelper.WaitForIdle();

			await CloseCommandBar(cmdBar);

			await RunOnUIThread(() =>
			{
				VERIFY_IS_FALSE(addedSeparator1.IsInOverflow);
				VERIFY_IS_FALSE(addedSeparator2.IsInOverflow);
			});
		}

		[TestMethod]

		[Description("Validates the firing dynamic overflow items changing event.")]
		public async Task ValidateFireDynamicOverflowItemsChangingEvent()
		{
			TestCleanupWrapper cleanup;

			int numButtonsToAddExtraToPrimary = 2;
			int numButtonsToAddToSecondary = 3;

			//AppBarButton addedButton1 = null;
			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			await ValidateDynamicOverflowItemsChangingEventWorker(cmdBar, true);
			await ValidateDynamicOverflowItemsChangingEventWorker(cmdBar, false);
			await ValidateDynamicOverflowItemsChangingEventWorker(cmdBar, false);
			await ValidateDynamicOverflowItemsChangingEventWorker(cmdBar, true);
		}

		[TestMethod]

		[Description("Validates the dynamic overflow with changing the orientation.")]
		[TestProperty("TestPass:ExcludeOn", "Desktop")]
		[Ignore("WindowHelper.SetWindowSizeOverride not implemented.")]
		public async Task ValidateDynamicOverflowWithChangingOrientation()
		{
			TestCleanupWrapper cleanup;

			// The test sets up its tree to use a BottomAppBar, so make sure to call SetWindowSizeOverride() before setting up the tree
			// because it doesn't raise WindowSizeChanged and the AppBarService depends on that to correctly size Top/BottomAppBars.
			WindowHelper.SetWindowSizeOverride(new Size(800, 400));

			int numButtonsToAddExtraToPrimary = 0;
			int numButtonsToAddToSecondary = 3;

			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: false, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			WindowHelper.SetWindowSizeOverride(new Size(480, 800));

			numButtonsToAddExtraToPrimary = 5;
			(page, cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);

			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);

			await CloseCommandBar(cmdBar);

			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: true, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
		}

		[TestMethod]

		[Description("Validates the dynamic overflow moving order.")]
		public async Task ValidateDynamicOverflowOrderBasic()
		{
			TestCleanupWrapper cleanup;

			int numButtonsToAddExtraToPrimary = 2;
			int numButtonsToAddToSecondary = 3;

			AppBarButton addedButton1 = null;
			//AppBarSeparator addedSeparator1 = null;

			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary, isSetOrder: true);
			await ValidateDynamicOverflowWorker(cmdBar, isPrimaryCommandMovedToOverflow: true, numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary, isSetOrder: true);

			await RunOnUIThread(() =>
			{
				addedButton1 = new AppBarButton();
				addedButton1.DynamicOverflowOrder = 1;
				cmdBar.PrimaryCommands.Insert(0, addedButton1);
			});
			await WindowHelper.WaitForIdle();

			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);

			await RunOnUIThread(() => VERIFY_IS_TRUE(addedButton1.IsInOverflow));
			await WindowHelper.WaitForIdle();

			await CloseCommandBar(cmdBar);
		}

		[TestMethod]

		[Description("Validates the dynamic overflow moving order multiple test cases.")]
		public async Task ValidateDynamicOverflowOrderTestCases()
		{
			TestCleanupWrapper cleanup;

			StackPanel root = null;
			CommandBar cmdBar1 = null;
			CommandBar cmdBar2 = null;
			CommandBar cmdBar3 = null;
			CommandBar cmdBar4 = null;
			CommandBar cmdBar5 = null;
			CommandBar cmdBar6 = null;

			await RunOnUIThread(() =>
			{
				root = new StackPanel();

				cmdBar1 = CreateCommandBarWithPrimaryCommandOrderSet(DynamicOverflowOrderTest.OrderTestForAlternativeValue);
				cmdBar2 = CreateCommandBarWithPrimaryCommandOrderSet(DynamicOverflowOrderTest.OrderTestForAllSameValue);
				cmdBar3 = CreateCommandBarWithPrimaryCommandOrderSet(DynamicOverflowOrderTest.OrderTestForTwoPairedValue);
				cmdBar4 = CreateCommandBarWithPrimaryCommandOrderSet(DynamicOverflowOrderTest.OrderTestForFallbackDefault);
				cmdBar5 = CreateCommandBarWithPrimaryCommandOrderSet(DynamicOverflowOrderTest.OrderTestForMovingPriorSeparator);
				cmdBar6 = CreateCommandBarWithPrimaryCommandOrderSet(DynamicOverflowOrderTest.OrderTestForMovingPosteriorSeparator);

				root.Children.Append(cmdBar1);
				root.Children.Append(cmdBar2);
				root.Children.Append(cmdBar3);
				root.Children.Append(cmdBar4);
				root.Children.Append(cmdBar5);
				root.Children.Append(cmdBar6);

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			// Test for alternative order - {1,2,1,2,1,2,1,2,1,2}
			await ValidateDynamicOverflowOrderWorker(cmdBar1, DynamicOverflowOrderTest.OrderTestForAlternativeValue);

			//// Test for all same order - {1,1,1,1,1,1,1,1,1,1}
			await ValidateDynamicOverflowOrderWorker(cmdBar2, DynamicOverflowOrderTest.OrderTestForAllSameValue);

			// Test for paired order group - {1,2,3,4,5,1,2,3,4,5}
			await ValidateDynamicOverflowOrderWorker(cmdBar3, DynamicOverflowOrderTest.OrderTestForTwoPairedValue);

			// Test for order set and default rightmost dynamic overflow - {1, 1, 2, 2, 0, 0, 0, 0, 0, 0}
			await ValidateDynamicOverflowOrderWorker(cmdBar4, DynamicOverflowOrderTest.OrderTestForFallbackDefault);

			//// Test for order set with moving the separator together - {|, 1, |, 2, 3, 4, 5, 1, 2, 3, 4, 5}
			await ValidateDynamicOverflowOrderWorker(cmdBar5, DynamicOverflowOrderTest.OrderTestForMovingPriorSeparator);

			//// Test for order set and default rightmost dynamic overflow with moving the separator together  - {0, 0, 0, 0, 0, 0, 0, 0, 0, |, 1, |}
			await ValidateDynamicOverflowOrderWorker(cmdBar6, DynamicOverflowOrderTest.OrderTestForMovingPosteriorSeparator);
		}

		[TestMethod]

		[Description("Validates the layout of CommandBar.Content when IsDynamicOverflowEnabled is true or false.")]
		[TestProperty("Hosting:Mode", "UAP")]
		public async Task ValidateDynamicOverflowWithContentControl()
		{
			TestCleanupWrapper cleanup;

			int numButtonsToAddExtraToPrimary = 5;
			int numButtonsToAddToSecondary = 3;

			(Page page, CommandBar cmdBar) = await SetupDynamicOverflowTest(numButtonsToAddExtraToPrimary, numButtonsToAddToSecondary);
			TextBox textBox = null;

			await RunOnUIThread(() =>
			{
				textBox = new TextBox();
				textBox.Width = 100;

				cmdBar.Content = textBox;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				var primaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(cmdBar, "PrimaryItemsControl");

				Rect primaryItemsControlBounds = await ControlHelper.GetBounds(primaryItemsControl);
				Rect textBoxBounds = await ControlHelper.GetBounds(textBox);

				LOG_OUTPUT($"TextBox bounds left={textBoxBounds.Left} top={textBoxBounds.Top} width={textBoxBounds.Width} height={textBoxBounds.Height}");
				LOG_OUTPUT($"Primary ItemsControl bounds left={primaryItemsControlBounds.Left} top={primaryItemsControlBounds.Top} width={primaryItemsControlBounds.Width} height={primaryItemsControlBounds.Height}");

				// Validate the TextBox doesn't occlude with the primary ItemsControl by applying the dynamic overflow mechanism
				VERIFY_IS_LESS_THAN_OR_EQUAL(textBoxBounds.Right, primaryItemsControlBounds.X);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cmdBar.IsDynamicOverflowEnabled = false;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				var primaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(cmdBar, "PrimaryItemsControl");

				Rect primaryItemsControlBounds = await ControlHelper.GetBounds(primaryItemsControl);
				Rect textBoxBounds = await ControlHelper.GetBounds(textBox);

				LOG_OUTPUT($"TextBox bounds left={textBoxBounds.Left} top={textBoxBounds.Top} width={textBoxBounds.Width} height={textBoxBounds.Height}");
				LOG_OUTPUT($"Primary ItemsControl bounds left={primaryItemsControlBounds.Left} top={primaryItemsControlBounds.Top} width={primaryItemsControlBounds.Width} height={primaryItemsControlBounds.Height}");

				// Validate the TextBox is occluded with the primary ItemsControl when the primary ItemsControl has lots primary items
				// and disabled dynamic overflow mechanism
				VERIFY_IS_GREATER_THAN_OR_EQUAL(textBoxBounds.Right, primaryItemsControlBounds.X);
			});
			await WindowHelper.WaitForIdle();

			await EmptyPageContent(page);
		}

		[TestMethod]

		[Description("Validates the layout of CommandBar.Content when IsDynamicOverflowEnabled is true or false.")]
		[TestProperty("Hosting:Mode", "UAP")]
		public async Task ValidateVisualStateUpdatesWhenDynamicOverflowCausesItemsToMove()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			ItemsControl primaryItemsControl = null;

			bool expectItemsAdded = false;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.Width = 600;

				var appBarButton = new AppBarButton();
				appBarButton.Label = "Button";
				appBarButton.Icon = new SymbolIcon(Symbol.AddFriend);
				cmdBar.PrimaryCommands.Append(appBarButton);

				var button = new Button();
				button.Content = "Content Button";
				button.Width = 396;
				cmdBar.Content = button;

				var grid = new Grid();
				grid.Children.Append(cmdBar);
				loadedRegistration.Attach(grid, (s, e) => loadedEvent.Set());
				SetWindowContent(grid);
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				primaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(cmdBar, "PrimaryItemsControl");
			});

			var dynamicOverflowItemsChangingEvent = new Event();
			var dynamicOverflowItemsChangingRegistration = CreateSafeEventRegistration<CommandBar, TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs>>("DynamicOverflowItemsChanging");

			using var _ = dynamicOverflowItemsChangingRegistration.Attach(cmdBar, (s, e) =>
			{
				if (expectItemsAdded)
				{
					VERIFY_ARE_EQUAL(CommandBarDynamicOverflowAction.AddingToOverflow, e.Action);
				}
				else
				{
					VERIFY_ARE_EQUAL(CommandBarDynamicOverflowAction.RemovingFromOverflow, e.Action);
				}

				dynamicOverflowItemsChangingEvent.Set();
			});

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Change the width of the CommandBar to be 400.  The AppBarButton should be moved to the overflow.");
				expectItemsAdded = true;
				cmdBar.Width = 400;
			});

			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("The primary items control should now be collapsed since there are no AppBarButtons left in it.");
				VERIFY_ARE_EQUAL(primaryItemsControl.Visibility, Visibility.Collapsed);
			});

			LOG_OUTPUT("Now open and close the CommandBar.");
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await CloseCommandBar(cmdBar);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("The primary items control should still be collapsed.");
				VERIFY_ARE_EQUAL(primaryItemsControl.Visibility, Visibility.Collapsed);

				LOG_OUTPUT("Change the width of the CommandBar back to 600.  The AppBarButton should be moved back from the overflow.");
				expectItemsAdded = false;
				cmdBar.Width = 600;
			});

			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("The primary items control should now be visible since the AppBarButton is back in it.");
				VERIFY_ARE_EQUAL(primaryItemsControl.Visibility, Visibility.Visible);
			});
		}

		[TestMethod]

		[Description("Validates the dynamic overflow moving behavior with the CustomAppBarButton that implement ICommandbarElement.")]
		public async Task ValidateDynamicOverflowWithCustomAppBarButton()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				// Validate the dynamic overflow behaviors with the CustomAppBarButton that is implemented with the legacy
				// ICommandBarElement that doesn't have a property for setting DynamicOrder
				cmdBar = (CommandBar)XamlReader.Load(@"<CommandBar   xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                xmlns:local=""using:Windows.UI.Tests.Enterprise.CustomTypes"">
                        <CommandBar.Resources>
                            <Style TargetType=""local:CustomAppBarButton"">
                                <Setter Property=""Margin"" Value=""12,0""/>
                            </Style>
                        </CommandBar.Resources>
                        <local:CustomAppBarButton Content=""First""/>
                        <local:CustomAppBarButton Content=""Second""/>
                        <local:CustomAppBarButton Content=""Third""/>
                        <local:CustomAppBarButton Content=""Fourth""/>
                        <local:CustomAppBarButton Content=""Fifth""/>
                        <local:CustomAppBarButton Content=""Sixth""/>
                        <local:CustomAppBarButton Content=""Seventh""/>
                        <local:CustomAppBarButton Content=""Eighth""/>
                        <local:CustomAppBarButton Content=""Ninth""/>
                        <local:CustomAppBarButton Content=""Tenth""/>
                    </CommandBar>");

				var rootGrid = new Grid();
				rootGrid.Children.Append(cmdBar);
				SetWindowContent(rootGrid);
			});
			await WindowHelper.WaitForIdle();

			var dynamicOverflowItemsChangingEvent = new Event();
			var dynamicOverflowItemsChangingRegistration = CreateSafeEventRegistration<CommandBar, TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs>>("DynamicOverflowItemsChanging");

			using var _ = dynamicOverflowItemsChangingRegistration.Attach(cmdBar, (s, e) =>
			{
				VERIFY_ARE_EQUAL(e.Action, CommandBarDynamicOverflowAction.AddingToOverflow);
				dynamicOverflowItemsChangingEvent.Set();
			});

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Set CommandBar width = 200.  The CustomAppBarButton should be moved to the overflow.");
				cmdBar.Width = 200;
			});
			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]

		[Description("Validates the primary buttons width after applying the overflow style at the Right DefaultLabelPosition.")]
		[Ignore("Bug in Uno: Cannot re-measure during measure phase")]
		public async Task ValidatePrimaryButtonWidthAtRightDefaultLabelPosition()
		{
			TestCleanupWrapper cleanup;

			// This test validate the primary buttons width that is applying
			// the dynamic overflow and the overflow styles at the Right DefaultLabelPosition.

			CommandBar cmdBar = null;
			ItemsControl primaryItemsControl = null;

			double button1Width = 0;
			double button2Width = 0;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
				cmdBar.Width = 400;

				var appBarButton1 = new AppBarButton();
				appBarButton1.Label = "Button1";
				appBarButton1.Background = SolidColorBrushHelper.Red;
				appBarButton1.Icon = new SymbolIcon(Symbol.Add);


				var appBarButton2 = new AppBarToggleButton();
				appBarButton2.Label = "Button2";
				appBarButton2.Icon = new SymbolIcon(Symbol.AddFriend);

				cmdBar.PrimaryCommands.Append(appBarButton1);
				cmdBar.PrimaryCommands.Append(appBarButton2);

				var button = new Button();
				button.Content = "Content Button";
				button.Width = 150;
				cmdBar.Content = button;

				var grid = new Grid();
				grid.Children.Append(cmdBar);
				SetWindowContent(grid);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var button1 = (AppBarButton)cmdBar.PrimaryCommands.GetAt(0);
				button1Width = button1.ActualWidth;


				var button2 = (AppBarToggleButton)cmdBar.PrimaryCommands.GetAt(1);
				button2Width = button2.ActualWidth;

				primaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(cmdBar, "PrimaryItemsControl");
			});

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Change the CommandBar width to 200 to apply the DynamicOverflow.");
				cmdBar.Width = 200;
			});
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Now open and close the CommandBar to apply the Overflow style.");
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await CloseCommandBar(cmdBar);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Change back the CommandBar width to 400 to move the Primary buttons from the Overflow to the pirmary commands.");
				cmdBar.Width = 400;
			});
			await WindowHelper.WaitForIdle();

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var button1 = (AppBarButton)cmdBar.PrimaryCommands.GetAt(0);
				var button2 = (AppBarToggleButton)cmdBar.PrimaryCommands.GetAt(1);

				VERIFY_ARE_EQUAL(button1.ActualWidth, button1Width);
				VERIFY_ARE_EQUAL(button2.ActualWidth, button2Width);
			});
		}

		[TestMethod]

		[Description("Validates that a button removed while in the overflow and then re-inserted back into the primary area is not styled with the overflow style.")]
		public async Task DoesResetOverflowButtonStylingWhenRemovedAndAddedBack()
		{
			TestCleanupWrapper cleanup;

			AppBarButton button = null;
			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				button = new AppBarButton();
				button.Label = "button";
				button.Icon = new SymbolIcon(Symbol.Accept);

				cmdBar = new CommandBar();
				cmdBar.PrimaryCommands.Append(button);

				SetWindowContent(cmdBar);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_FALSE(await ControlHelper.IsInVisualState(button, "ApplicationViewStates", "OverflowWithMenuIcons"), "AppBarButton should *not* have the overflow style.");

				LOG_OUTPUT("Size CommandBar to force the button into the overflow.");
				cmdBar.Width = 60;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// This check is removed, the visual tree doesn't exist after CommandBar default with Reveal
				// because AppBarButton reveal style is update to NULL in CFrameworkElement::LeaveImpl
				//WEX::Common::Throw::IfFalse(ControlHelper::IsInVisualState(button, L"ApplicationViewStates", L"OverflowWithMenuIcons"), E_FAIL, L"AppBarButton *should* have the overflow style.");

				LOG_OUTPUT("Clear the CommandBar's primary commands while the button is in the overflow.");
				cmdBar.PrimaryCommands.Clear();

				LOG_OUTPUT("Reset the size of the CommandBar so that we no longer will put buttons into the overflow and add the button back.");
				cmdBar.Width = double.NaN;
				cmdBar.PrimaryCommands.Append(button);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_FALSE(await ControlHelper.IsInVisualState(button, "ApplicationViewStates", "OverflowWithMenuIcons"));
			});
		}

		[TestMethod]

		[Description("Validates that arrow keys will navigate you into the content area.")]
		[Ignore("UNO BUG: Focus not shifting in/out of ContentControl")]
		public async Task CanArrowIntoTheContentArea()
		{
			TestCleanupWrapper cleanup;
			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				cmdBar = (CommandBar)XamlReader.Load(@"<CommandBar   xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                        <AppBarButton  Label=""button"" Icon=""Accept""/>
                        <CommandBar.Content>
                            <Button Content=""content""/>
                        </CommandBar.Content>
                    </CommandBar>");

				SetWindowContent(cmdBar);
			});
			await WindowHelper.WaitForIdle();

			// Set Focus to the content area button
			await RunOnUIThread(() =>
			{
				var button = (Button)cmdBar.Content;
				button.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			Func<InputDevice, Task> runScenario = async (inputDevice) =>
			{
				LOG_OUTPUT("Navigate Right to move focus onto the primary command button.");
				CommonInputHelper.Right(inputDevice, cmdBar);
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					var focusedElement = FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					VERIFY_IS_TRUE(focusedElement.Equals(cmdBar.PrimaryCommands.GetAt(0)));
				});

				LOG_OUTPUT("Navigate Right to move focus onto the More button.");
				CommonInputHelper.Right(inputDevice, cmdBar);
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(async () =>
				{
					var focusedElement = FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					var moreButton = await GetMoreButton(cmdBar);
					VERIFY_IS_TRUE(focusedElement.Equals(moreButton), $"Input: {inputDevice}, Focused element ({focusedElement.GetHashCode()}) should be moreButton ({moreButton.GetHashCode()})");
				});

				LOG_OUTPUT("Navigate Left to move focus onto the primary command button.");
				CommonInputHelper.Left(inputDevice, cmdBar);
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					var focusedElement = FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					VERIFY_IS_TRUE(focusedElement.Equals(cmdBar.PrimaryCommands.GetAt(0)), $"Input: {inputDevice}, Focused element ({focusedElement.GetHashCode()}) should be primary command ({cmdBar.PrimaryCommands.GetAt(0).GetHashCode()})");
				});

				LOG_OUTPUT("Navigate Left to move focus onto custom content button.");
				CommonInputHelper.Left(inputDevice, cmdBar);
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					var focusedElement = FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					VERIFY_IS_TRUE(focusedElement.Equals(cmdBar.Content), $"Input: {inputDevice}, Focused element ({focusedElement.GetHashCode()}) should be content ({cmdBar.Content.GetHashCode()})");
				});
			};

			LOG_OUTPUT("Validate scenario for keyboard.");
			await runScenario(InputDevice.Keyboard);

			LOG_OUTPUT("Validate scenario for gamepad.");
			await runScenario(InputDevice.Gamepad);
		}

		[TestMethod]

		[Description("Validates that the overflow menu is opened when an arrow key is entered with focus on the more button.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[Ignore("UNO BUG: Focus not shifting to secondary commands on open")]
		public async Task DoesOpenOverflowWithArrowInputOnMoreButton()
		{
			TestCleanupWrapper cleanup;
			CommandBar cmdBar = null;

			var openedEvent = new Event();
			var openedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");

			await RunOnUIThread(() =>
			{
				cmdBar = (CommandBar)XamlReader.Load(@"<CommandBar   xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                        <CommandBar.SecondaryCommands>
                            <AppBarButton  Label=""menu item 1"" />
                            <AppBarButton  Label=""menu item 2"" />
                            <AppBarButton  Label=""menu item 3"" />
                        </CommandBar.SecondaryCommands>
                    </CommandBar>");

				openedRegistration.Attach(cmdBar, (s, e) => openedEvent.Set());

				SetWindowContent(cmdBar);
			});
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Set focus to the More button.");
			var moreButton = await GetMoreButton(cmdBar);
			await RunOnUIThread(() =>
			{
				moreButton.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Press the Up arrow key to open the CommandBar and focus the last overflow item.");
			CommonInputHelper.Up(InputDevice.Keyboard, cmdBar);
			await openedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				var focusedElement = FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
				VERIFY_IS_TRUE(focusedElement.Equals(cmdBar.SecondaryCommands.GetAt(2)));
			});
			await CloseCommandBar(cmdBar);

			LOG_OUTPUT("Press the Down arrow key to open the CommandBar and focus the first overflow item.");
			CommonInputHelper.Down(InputDevice.Keyboard, cmdBar);
			await openedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				var focusedElement = FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
				VERIFY_IS_TRUE(focusedElement.Equals(cmdBar.SecondaryCommands.GetAt(0)));
			});
			await CloseCommandBar(cmdBar);

			// The event's HasFired flag needs to get reset before the following tests.
			openedEvent.Reset();

			LOG_OUTPUT("Press the Gamepad Up button and validate that the CommandBar doesn't open the overflow.");
			CommonInputHelper.Up(InputDevice.Gamepad, cmdBar);
			await WindowHelper.WaitForIdle();
			VERIFY_IS_FALSE(openedEvent.HasFired());

			LOG_OUTPUT("Press the Gamepad Down button and validate that the CommandBar doesn't open the overflow.");
			CommonInputHelper.Down(InputDevice.Gamepad, cmdBar);
			await WindowHelper.WaitForIdle();
			VERIFY_IS_FALSE(openedEvent.HasFired());
		}

		//UNO TODO: ControlHelper.ValidateUIElementTree not implemented.

		//void CommandBarIntegrationTests::ValidateSymbolMenuIcons()
		//{
		//	TestCleanupWrapper cleanup;

		//	LOG_OUTPUT(L"Validating Symbol Menu Icons in the Overflow window");

		//	Platform::String ^ xamlString = LR"(<SymbolIcon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Symbol="Accept" />)";

		//	RunSecondaryIconTests(xamlString);
		//}

		//void CommandBarIntegrationTests::ValidatePathMenuIcons()
		//{
		//	TestCleanupWrapper cleanup;

		//	LOG_OUTPUT(L"Validating Path Menu Icons in the Overflow window");

		//	Platform::String ^ xamlString = LR"(<PathIcon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Data="F1 M 16,12 20,2L 20,16 1,16" HorizontalAlignment="Center" />)";

		//	RunSecondaryIconTests(xamlString);
		//}

		//void CommandBarIntegrationTests::ValidateFontMenuIcons()
		//{
		//	TestCleanupWrapper cleanup;

		//	LOG_OUTPUT(L"Validating Font Menu Icons in the Overflow window");

		//	Platform::String ^ xamlString = LR"(<FontIcon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" FontFamily="Candara" Glyph="&#x03A3;" />)";

		//	RunSecondaryIconTests(xamlString);
		//}

		//void CommandBarIntegrationTests::ValidateBitmapMenuIcons()
		//{
		//	TestCleanupWrapper cleanup;

		//	LOG_OUTPUT(L"Validating Bitmap Menu Icons in the Overflow window");

		//	Platform::String ^ xamlString = LR"(<BitmapIcon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" UriSource="ms-appx:///resources/native/controls/commandbar/bitimg.png" />)";

		//	RunSecondaryIconTests(xamlString);
		//}

		//void CommandBarIntegrationTests::ValidateBitmapMenuIconsNoMonochrome()
		//{
		//	TestCleanupWrapper cleanup;

		//	LOG_OUTPUT(L"Validating Bitmap Menu Icons in the Overflow window");

		//	Platform::String ^ xamlString = LR"(<BitmapIcon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" UriSource="ms-appx:///resources/native/controls/commandbar/bitimg.png" ShowAsMonochrome="False" />)";

		//	RunSecondaryIconTests(xamlString);
		//}

		//void CommandBarIntegrationTests::RunSecondaryIconTests(Platform::String^ xamlString)
		//{
		//	ControlHelper::ValidateUIElementTree(
		//		PopupHelper::AreWindowedPopupsEnabled() ? L"Windowed" : L"Unwindowed",
		//		wf::Size(400, 600),
		//		1.f,

		//		[&]()

		//{
		//		xaml_controls::Grid ^ root;
		//		xaml_controls::CommandBar ^ cmdBar;
		//		xaml_controls::AppBarButton ^ appBarButton;

		//		RunOnUIThread([&]()

		//	{
		//			root = safe_cast < xaml_controls::Grid ^> (xaml_markup::XamlReader::Load(
		//				LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		//							  xmlns:x = "http://schemas.microsoft.com/winfx/2006/xaml" >

		//							< CommandBar x: Name = "cmdBar" VerticalAlignment = "Bottom" HorizontalAlignment = "Center" >

		//								< CommandBar.PrimaryCommands >

		//									< AppBarButton x: Name = "appBarButton1" Label = "Item 1" Icon = "Accept" />

		//								</ CommandBar.PrimaryCommands >

		//								< CommandBar.SecondaryCommands >

		//									< AppBarButton x: Name = "appBarButton2" Label = "Button 1" />

		//									< AppBarToggleButton x: Name = "appBarToggleButton1" Label = "Button 2" />

		//								</ CommandBar.SecondaryCommands >

		//							</ CommandBar >

		//						</ Grid >)"));


		//		auto icon = safe_cast < xaml_controls::IconElement ^> (xaml_markup::XamlReader::Load(xamlString));

		//			cmdBar = safe_cast < xaml_controls::CommandBar ^> (root->FindName(L"cmdBar"));
		//			appBarButton = safe_cast < xaml_controls::AppBarButton ^> (root->FindName(L"appBarButton2"));

		//			appBarButton->Icon = icon;

		//			root->IsHitTestVisible = false;

		//			TestServices::WindowHelper->WindowContent = root;
		//		});

		//		TestServices::WindowHelper->WaitForIdle();

		//		RunOnUIThread([&]()

		//	{
		//			// The tool tip on the CommandBar can annoyingly inject itself into the UI tree dump
		//			// if it happens to have opened.  We'll remove it to avoid that circumstance.
		//			auto moreButton = TreeHelper::GetVisualChildByName(cmdBar, L"MoreButton");
		//			xaml_controls::ToolTipService::SetToolTip(moreButton, nullptr);
		//		});

		//		TestServices::WindowHelper->WaitForIdle();

		//		OpenCommandBar(cmdBar, OpenMethod::Programmatic);

		//		TestServices::WindowHelper->WaitForIdle();

		//		return root;
		//	});
		//}

		[TestMethod]

		[Description("Verify Foreground is correct for AppBarButton/AppBarToggleButton when XamlUICommand is used")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		[Ignore("IconSourceElement not implemented.")]
		public async Task ValidateForegroundForXamlUICommand()
		{
			TestCleanupWrapper cleanup;

			// Because of XamlUICommand, IconElement is used to convert IconSource to Icon. If no Foreground is set,
			// We expect IconElement Foreground is from its parent AppBarButton/AppBarButton
			// In this way, the visualstate of AppBarButton/AppBarButton like Disabled would apply to IconElement
			Grid root = null;
			AppBarButton button1 = null;
			AppBarButton buttonWithCommand1 = null;
			AppBarToggleButton buttonWithCommand2 = null;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>("Loaded");
			await RunOnUIThread(() =>
			{
				root = (Grid)XamlReader.Load(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
								xmlns:input=""using:Microsoft.UI.Xaml.Input"">
                            <Grid.Resources>
                                <input:XamlUICommand x:Key=""AcceptCommand"" Label=""Accept"">
                                    <input:XamlUICommand.IconSource>
                                        <SymbolIconSource Symbol=""Accept""/>
                                    </input:XamlUICommand.IconSource>
                                </input:XamlUICommand>
                                <input:XamlUICommand x:Key=""FavoriteCommand"" Label=""Favorite"">
                                    <input:XamlUICommand.IconSource>
                                        <SymbolIconSource Foreground=""Red"" Symbol=""Favorite"" />
                                    </input:XamlUICommand.IconSource>
                                </input:XamlUICommand>
                            </Grid.Resources>
                            <CommandBar   DefaultLabelPosition=""Right"" Foreground=""Blue"">
                                <AppBarButton  x:Name=""Button1"" IsEnabled=""False"" Label=""Accept"" >
                                    <IconSourceElement>
                                            <SymbolIconSource Symbol=""Favorite"" Foreground=""Red"" />
                                    </IconSourceElement>
                                </AppBarButton>
                                <AppBarButton  x:Name=""ButtonWithCommand1"" IsEnabled=""False"" Command=""{StaticResource AcceptCommand}"" />
                                <AppBarToggleButton  x:Name=""ButtonWithCommand2"" IsEnabled=""False"" Command=""{StaticResource FavoriteCommand}"" />
                            </CommandBar>
                        </Grid>");

				SetWindowContent(root);

				button1 = (AppBarButton)root.FindName("Button1");
				buttonWithCommand1 = (AppBarButton)root.FindName("ButtonWithCommand1");
				buttonWithCommand2 = (AppBarToggleButton)root.FindName("ButtonWithCommand2");
				loadedRegistration.Attach(root, (s, e) => loadedEvent.Set());
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Button1.IconSourceElement has it's own Foreground");
				var iconSourceElement = (IconElement)(button1.Icon);
				VERIFY_ARE_EQUAL(((SolidColorBrush)iconSourceElement.Foreground).Color, Colors.Red);

				LOG_OUTPUT("buttonWithCommand1.IconSourceElement doesn't have it's own Foreground");
				iconSourceElement = (IconElement)(buttonWithCommand1.Icon);
				VERIFY_ARE_EQUAL(iconSourceElement.Foreground, null);

				LOG_OUTPUT("buttonWithCommand2.IconSourceElement has it's own Foreground");
				iconSourceElement = (IconElement)(buttonWithCommand2.Icon);
				VERIFY_ARE_EQUAL(((SolidColorBrush)iconSourceElement.Foreground).Color, Colors.Red);
			});
		}

		[TestMethod]

		[Description("Validates that the CommandBar's visual state is properly updated when items move from the primary commands collection to the secondary commands collection, or vice versa.")]
		public async Task ValidateCollapsedItemsDoNotPreventReturnFromOverflow()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			AppBarButton appBarButton = null;

			bool expectItemsAdded = false;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.Width = 600;

				appBarButton = new AppBarButton();
				appBarButton.Label = "Button 1";
				appBarButton.Icon = new SymbolIcon(Symbol.AddFriend);
				cmdBar.PrimaryCommands.Append(appBarButton);

				var button = new Button();
				button.Content = "Content Button";
				button.Width = 400;
				cmdBar.Content = button;

				var grid = new Grid();
				grid.Children.Append(cmdBar);
				loadedRegistration.Attach(grid, (s, e) => loadedEvent.Set());
				SetWindowContent(grid);
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => appBarButton.Visibility = Visibility.Collapsed);

			await WindowHelper.WaitForIdle();

			var dynamicOverflowItemsChangingEvent = new Event();
			var dynamicOverflowItemsChangingRegistration = CreateSafeEventRegistration<CommandBar, TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs>>("DynamicOverflowItemsChanging");

			using var _ = dynamicOverflowItemsChangingRegistration.Attach(cmdBar, (s, e) =>
			{
				if (expectItemsAdded)
				{
					VERIFY_ARE_EQUAL(e.Action, CommandBarDynamicOverflowAction.AddingToOverflow);
				}
				else
				{
					VERIFY_ARE_EQUAL(e.Action, CommandBarDynamicOverflowAction.RemovingFromOverflow);
				}

				dynamicOverflowItemsChangingEvent.Set();
			});

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Change the width of the CommandBar to be 300.  The AppBarButton should be moved to the overflow.");
				expectItemsAdded = true;
				cmdBar.Width = 300;
			});

			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Change the width of the CommandBar back to 600.  The AppBarButton should be moved back from the overflow.");
				expectItemsAdded = false;
				cmdBar.Width = 600;
			});

			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates that adding elements to the secondary collection or changing the value of ClosedDisplayMode changes the visibility of the more button.")]
		public async Task ValidateMoreButtonCanShowWithoutSizeChanging()
		{
			TestCleanupWrapper cleanup;

			CommandBar cmdBar = null;
			ButtonBase moreButton = null;

			var hasLoadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<CommandBar, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				loadedRegistration.Attach(cmdBar, (s, e) => hasLoadedEvent.Set());

				var page = WindowHelper.SetupSimulatedAppPage();

				//UNO TODO: BottomAppBar not implemented
				//page.BottomAppBar = cmdBar;
				SetPageContent(cmdBar, page);
			});

			await hasLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			moreButton = (ButtonBase)(await GetMoreButton(cmdBar));
			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(moreButton.Visibility, Visibility.Collapsed);

				cmdBar.SecondaryCommands.Append(new AppBarButton());
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(moreButton.Visibility, Visibility.Visible);

				cmdBar.SecondaryCommands.Clear();
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(moreButton.Visibility, Visibility.Collapsed);

				cmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(moreButton.Visibility, Visibility.Visible);

				cmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(moreButton.Visibility, Visibility.Collapsed);

				cmdBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(moreButton.Visibility, Visibility.Visible);

				cmdBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Auto;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(moreButton.Visibility, Visibility.Collapsed);
			});
		}

		[TestMethod]

		[Description("Validates that AppBarButtons move to and from the overflow as expected when sizing the CommandBar less than the size it minimally needs to display all its content, and then sizing it to be larger than that.")]
		public async Task ValidateButtonsMoveToAndFromOverflowWithoutSizeChange()
		{
			TestCleanupWrapper cleanup;

			Grid rootGrid = null;
			ColumnDefinition primaryColumn = null;

			var sizeChangedEvent = new Event();
			var dynamicOverflowItemsChangingEvent = new Event();
			var sizeChangedRegistration = CreateSafeEventRegistration<Grid, SizeChangedEventHandler>("SizeChanged");
			var dynamicOverflowItemsChangingRegistration = CreateSafeEventRegistration<CommandBar, TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs>>("DynamicOverflowItemsChanging");

			bool expectItemAdded = false;

			await RunOnUIThread(() =>
			{
				rootGrid = (Grid)XamlReader.Load(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""   xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition x:Name=""PrimaryColumn"" Width=""320"" />
                                <ColumnDefinition />
                            </Grid.ColumnDefinitions>
                            <Grid>
                                <CommandBar HorizontalAlignment=""Right"" x:Name=""CommandBar"">
                                    <AppBarButton Icon=""Add"" Label=""Add""/>
                                </CommandBar>
                            </Grid>
                            <Border Grid.Column=""1"" Background=""Red"" />
                        </Grid>
                    </Grid>");

				var commandBar = (CommandBar)rootGrid.FindName("CommandBar");

				dynamicOverflowItemsChangingRegistration.Attach(commandBar, (s, e) =>
				{
					if (expectItemAdded)
					{
						VERIFY_ARE_EQUAL(e.Action, CommandBarDynamicOverflowAction.AddingToOverflow);
					}
					else
					{
						VERIFY_ARE_EQUAL(e.Action, CommandBarDynamicOverflowAction.RemovingFromOverflow);
					}

					dynamicOverflowItemsChangingEvent.Set();
				});

				var childGrid = rootGrid.Children[0] as Grid;
				primaryColumn = childGrid.ColumnDefinitions[0];

				SetWindowContent(rootGrid);
			});

			await WindowHelper.WaitForIdle();

			expectItemAdded = true;

			await RunOnUIThread(() => primaryColumn.Width = GridLengthHelper.FromPixels(0));

			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			VERIFY_ARE_EQUAL(dynamicOverflowItemsChangingEvent.FiredCount, 1);

			expectItemAdded = false;

			await RunOnUIThread(() => primaryColumn.Width = GridLengthHelper.FromPixels(320));

			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			VERIFY_ARE_EQUAL(dynamicOverflowItemsChangingEvent.FiredCount, 2);
		}

		[TestMethod]

		[Description("Verify that changing the visibility of an AppBarButton in a CommandBar correctly updates the visual state")]
		public async Task VerifyVisibilityChangeUpdatesCommandBarVisualState()
		{
			TestCleanupWrapper cleanup;
			CommandBar cmdBar = null;
			AppBarButton appBarButton = null;

			await RunOnUIThread(() =>
			{
				var root = (Grid)XamlReader.Load(@"<Grid   xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" >
                        <CommandBar  x:Name=""cmdBar"" VerticalAlignment=""Top"" >
                            <AppBarButton  Label=""Item 1"" Name=""button"" Visibility=""Collapsed""/>
                            <CommandBar.SecondaryCommands>
                                <AppBarButton Label=""Item 2""/>
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </Grid>");

				cmdBar = (CommandBar)root.FindName("cmdBar");
				appBarButton = (AppBarButton)cmdBar.PrimaryCommands.GetAt(0);

				SetWindowContent(root);
			});

			await WindowHelper.WaitForIdle();

			var isInState = await ControlHelper.IsInVisualState(cmdBar, "AvailableCommandsStates", "SecondaryCommandsOnly");
			VERIFY_IS_TRUE(isInState);

			await RunOnUIThread(() =>
			{
				appBarButton.Visibility = Visibility.Visible;
			});

			await WindowHelper.WaitForIdle();

			isInState = await ControlHelper.IsInVisualState(cmdBar, "AvailableCommandsStates", "BothCommands");
			VERIFY_IS_TRUE(isInState);
		}


		// When AppBarButton/AppBarButton is collapsed, CommandBar is notified in AppBarButton/AppBarToggleButton::OnVisibilityChanged. and ButtonBase::OnVisibilityChanged would ClearStateFlags.
		// AppBarButton/AppBarToggleButton is a subclass of ButtonBase and it should call super::OnVisibilityChanged to ClearStateFlags
		// In this test case, when AppBarButton/AppBarButton is clicked, it collapse itself. then show them all when click approve button. then verify ClearStateFlags are called by checking IsPointerOver flag.
		[TestMethod]

		[Description("Validates PointerOver on an appbarbutton doesn't persist after the command bar collapses.")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		public async Task ValidateResetingTheStateOfAppBarButton()
		{
			TestCleanupWrapper cleanup;

			Grid root = null;
			CommandBar cmdBar = null;
			AppBarButton button = null;
			AppBarToggleButton toggleButton = null;

			AppBarButton approveButton = null;
			var buttonEventRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("Click");
			var toggleButtonEventRegistration = CreateSafeEventRegistration<AppBarToggleButton, RoutedEventHandler>("Click");
			var approveButtonEventRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("Click");

			var buttonEvent = new Event();
			var toggleButtonEvent = new Event();
			var approveButtonEvent = new Event();

			await RunOnUIThread(() =>
			{
				root = (Grid)XamlReader.Load(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" >
                        <CommandBar  x:Name=""cmdBar"">
                            <AppBarButton  x:Name=""Button"" Icon=""Add""/>
                            <AppBarToggleButton  x:Name=""ToggleButton"" Icon=""AddFriend"" />
                            <AppBarButton  IsEnabled=""False"" Icon=""Cancel"" Label=""PlaceHolder"" />
                            <AppBarButton  x:Name=""ApproveButton"" Icon=""Accept"" />
                        </CommandBar>
                    </Grid>");

				cmdBar = (CommandBar)root.FindName("cmdBar");

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			//BEGIN UNO ONLY: FindName does not work for secondary commands unless the popup is open
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);
			await RunOnUIThread(() =>
			{
				button = (AppBarButton)root.FindName("Button");
				approveButton = (AppBarButton)root.FindName("ApproveButton");
				toggleButton = (AppBarToggleButton)root.FindName("ToggleButton");
			});
			await CloseCommandBar(cmdBar);
			await WindowHelper.WaitForIdle();
			//END UNO ONLY

			buttonEventRegistration.Attach(button, (s, e) =>
			{
				button.Visibility = Visibility.Collapsed;
				buttonEvent.Set();
			});

			toggleButtonEventRegistration.Attach(toggleButton, (s, e) =>
			{
				toggleButton.Visibility = Visibility.Collapsed;
				toggleButtonEvent.Set();
			});

			approveButtonEventRegistration.Attach(approveButton, (s, e) =>
			{
				button.Visibility = Visibility.Visible;
				toggleButton.Visibility = Visibility.Visible;
				approveButtonEvent.Set();
			});

			await WindowHelper.WaitForIdle();

			TestServices.InputHelper.Tap(button);
			await buttonEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			TestServices.InputHelper.LeftMouseClick(toggleButton);
			await toggleButtonEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();


			TestServices.InputHelper.LeftMouseClick(approveButton);
			await approveButtonEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_FALSE(button.IsPointerOver);
				VERIFY_IS_FALSE(toggleButton.IsPointerOver);
			});
		}

		[TestMethod]

		[Description("Verify that setting the Flyout property on an AppBarButton in the overflow functions normally as a sub-menu.")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("InputHelper.MoveMouse not implemented.")]
		public async Task VerifyCanMakeSubMenuBySettingFlyoutProperty()
		{
			TestCleanupWrapper cleanup;

			Button flyoutButton = null;
			Flyout commandBarFlyout = null;
			CommandBar commandBar = null;
			AppBarButton menuFlyoutAppBarButton1 = null;
			AppBarButton menuFlyoutAppBarButton2 = null;
			AppBarButton menuFlyoutAppBarButton3 = null;
			AppBarButton appBarButtonWithoutFlyout = null;
			AppBarToggleButton appBarToggleButton = null;
			MenuFlyoutItem menuFlyoutItem1 = null;
			MenuFlyoutItem menuFlyoutItem2 = null;
			MenuFlyoutSubItem menuFlyoutSubItem = null;
			MenuFlyoutItem menuFlyoutItem3 = null;

			MenuFlyout menuFlyout1 = null;
			MenuFlyout menuFlyout2 = null;
			MenuFlyout menuFlyout3 = null;

			var menuFlyoutAppBarButton1LoadedEvent = new Event();
			var menuFlyoutAppBarButton1LoadedEventRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("Loaded");
			var commandBarOpenedEvent = new Event();
			var commandBarOpenedEventRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");
			var commandBarClosedEvent = new Event();
			var commandBarClosedEventRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Closed");
			var menuFlyout1OpenedEvent = new Event();
			var menuFlyout1OpenedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Opened");
			var menuFlyout1ClosedEvent = new Event();
			var menuFlyout1ClosedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Closed");
			var menuFlyout2OpenedEvent = new Event();
			var menuFlyout2OpenedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Opened");
			var menuFlyout2ClosedEvent = new Event();
			var menuFlyout2ClosedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Closed");
			var menuFlyout3OpenedEvent = new Event();
			var menuFlyout3OpenedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Opened");
			var menuFlyout3ClosedEvent = new Event();
			var menuFlyout3ClosedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Closed");
			var menuFlyoutItem1ClickEvent = new Event();
			var menuFlyoutItem1ClickEventRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>("Click");
			var menuFlyoutItem2ClickEvent = new Event();
			var menuFlyoutItem2ClickEventRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>("Click");

			await RunOnUIThread(() =>
			{
				var page = WindowHelper.SetupSimulatedAppPage();

				var root = (Grid)XamlReader.Load(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
							xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
							Background=""Transparent"">
						<Button x:Name=""FlyoutButton"" Content=""Click for flyout"">
							<Button.Flyout>
								<Flyout x:Name=""CommandBarFlyout"">
									<CommandBar   x:Name=""CommandBar"" IsOpen=""True"" Width=""300"">
										<CommandBar.SecondaryCommands>
											<AppBarButton  x:Name=""MenuFlyoutAppBarButton1"" Label=""Open menu flyout"">
												<AppBarButton.Flyout>
													<MenuFlyout x:Name=""MenuFlyout1"">
														<MenuFlyoutItem x:Name=""TestMenuFlyoutItem1"" Text=""Click me"" />
													</MenuFlyout>
												</AppBarButton.Flyout>
											</AppBarButton>
											<AppBarButton  x:Name=""MenuFlyoutAppBarButton2"" Label=""Open other menu flyout"">
												<AppBarButton.Flyout>
													<MenuFlyout x:Name=""MenuFlyout2"">
														<MenuFlyoutSubItem x:Name=""TestMenuFlyoutSubItem"" Text=""Sub-menu item"">
															<MenuFlyoutItem x:Name=""TestMenuFlyoutItem2"" Text=""Click me"" />
														</MenuFlyoutSubItem>
													</MenuFlyout>
												</AppBarButton.Flyout>
											</AppBarButton>
											<AppBarButton  x:Name=""MenuFlyoutAppBarButton3"" Label=""Open third menu flyout"">
												<AppBarButton.Flyout>
													<MenuFlyout x:Name=""MenuFlyout3"">
														<MenuFlyoutItem x:Name=""TestMenuFlyoutItem3"" Text=""Click me"" />
													</MenuFlyout>
												</AppBarButton.Flyout>
											</AppBarButton>
											<AppBarButton  x:Name=""AppBarButtonWithoutFlyout"" Label=""No menu flyout"" />
											<AppBarToggleButton  x:Name=""AppBarToggleButton"" Label=""No menu flyout"" />
										</CommandBar.SecondaryCommands>
									</CommandBar>
								</Flyout>
							</Button.Flyout>
						</Button>
					</Grid>");

				flyoutButton = (Button)root.FindName("FlyoutButton");
				commandBarFlyout = (Flyout)flyoutButton.Flyout;
				commandBar = (CommandBar)commandBarFlyout.Content;
				menuFlyoutAppBarButton1 = (AppBarButton)commandBar.SecondaryCommands.GetAt(0);
				menuFlyout1 = (MenuFlyout)menuFlyoutAppBarButton1.Flyout;
				menuFlyoutItem1 = (MenuFlyoutItem)menuFlyout1.Items[0];
				menuFlyoutAppBarButton2 = (AppBarButton)commandBar.SecondaryCommands.GetAt(1);
				menuFlyout2 = (MenuFlyout)menuFlyoutAppBarButton2.Flyout;
				menuFlyoutSubItem = (MenuFlyoutSubItem)menuFlyout2.Items[0];
				menuFlyoutItem2 = (MenuFlyoutItem)menuFlyoutSubItem.Items[0];
				menuFlyoutAppBarButton3 = (AppBarButton)commandBar.SecondaryCommands.GetAt(2);
				menuFlyout3 = (MenuFlyout)menuFlyoutAppBarButton3.Flyout;
				menuFlyoutItem3 = (MenuFlyoutItem)menuFlyout3.Items[0];
				appBarButtonWithoutFlyout = (AppBarButton)commandBar.SecondaryCommands.GetAt(3);
				appBarToggleButton = (AppBarToggleButton)commandBar.SecondaryCommands.GetAt(4);

				commandBarOpenedEventRegistration.Attach(commandBar, (s, e) =>
				{
					LOG_OUTPUT("CommandBar opened.");
					commandBarOpenedEvent.Set();
				});

				commandBarClosedEventRegistration.Attach(commandBar, (s, e) =>
				{
					LOG_OUTPUT("CommandBar closed.");
					commandBarClosedEvent.Set();
				});

				menuFlyoutAppBarButton1LoadedEventRegistration.Attach(menuFlyoutAppBarButton1, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyoutAppBarButton1 loaded.");
					menuFlyoutAppBarButton1LoadedEvent.Set();
				});

				menuFlyoutItem1ClickEventRegistration.Attach(menuFlyoutItem1, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyoutItem1 clicked.");
					menuFlyoutItem1ClickEvent.Set();
				});

				menuFlyoutItem2ClickEventRegistration.Attach(menuFlyoutItem2, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyoutItem2 clicked.");
					menuFlyoutItem2ClickEvent.Set();
				});

				menuFlyout1OpenedEventRegistration.Attach(menuFlyout1, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout1 opened.");
					menuFlyout1OpenedEvent.Set();
				});

				menuFlyout1ClosedEventRegistration.Attach(menuFlyout1, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout1 closed.");
					menuFlyout1ClosedEvent.Set();
				});

				menuFlyout2OpenedEventRegistration.Attach(menuFlyout2, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout2 opened.");
					menuFlyout2OpenedEvent.Set();
				});

				menuFlyout2ClosedEventRegistration.Attach(menuFlyout2, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout2 closed.");
					menuFlyout2ClosedEvent.Set();
				});

				menuFlyout3OpenedEventRegistration.Attach(menuFlyout3, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout3 opened.");
					menuFlyout3OpenedEvent.Set();
				});

				menuFlyout3ClosedEventRegistration.Attach(menuFlyout3, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout3 closed.");
					menuFlyout3ClosedEvent.Set();
				});

				SetPageContent(root, page);

			});


			await WindowHelper.WaitForIdle();

			FlyoutHelper.OpenFlyout(commandBarFlyout, flyoutButton, FlyoutOpenMethod.Programmatic_ShowAt);

			// Since the CommandBar starts with IsOpen already true, we don't get an Opened event the first time.
			// We'll listen for the loaded event on MenuFlyoutAppBarButton1 instead.
			await menuFlyoutAppBarButton1LoadedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over MenuFlyoutAppBarButton1, which should open the first menu flyout.");
			TestServices.InputHelper.MoveMouse(menuFlyoutAppBarButton1);
			await menuFlyout1OpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Clicking on the MenuFlyoutItem, which should close both the menu flyout and the CommandBar.");
			TestServices.InputHelper.LeftMouseClick(menuFlyoutItem1);
			await menuFlyoutItem1ClickEvent.WaitForDefault();
			await commandBarClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				commandBar.IsOpen = true;
			});

			await commandBarOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over MenuFlyoutAppBarButton2, which should close the first menu flyout and open the second.");
			TestServices.InputHelper.MoveMouse(menuFlyoutAppBarButton2);
			await menuFlyout1ClosedEvent.WaitForDefault();
			await menuFlyout2OpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over MenuFlyoutSubItem, which should open the sub-menu.");
			TestServices.InputHelper.MoveMouse(menuFlyoutSubItem);
			// Wait for the sub menu to open. It opens after a delay - clicking and waiting for idle doesn't open it.
			// <MenuFlyout sub items don't expand on mouse click - they need to wait for the timeout> tracks this bug.
			await TestServices.WindowHelper.SynchronouslyTickUIThread(60);
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Clicking on the MenuFlyoutItem, which should close both the menu flyout and the CommandBar.");
			TestServices.InputHelper.LeftMouseClick(menuFlyoutItem2);
			await menuFlyoutItem2ClickEvent.WaitForDefault();
			await menuFlyout2ClosedEvent.WaitForDefault();
			await commandBarClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				commandBar.IsOpen = true;
			});

			await commandBarOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over MenuFlyoutAppBarButton3, which should open the third menu flyout.");
			TestServices.InputHelper.MoveMouse(menuFlyoutAppBarButton3);
			await menuFlyout3OpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over MenuFlyoutItem3, then over AppBarButtonWithoutFlyout, which should close the third menu flyout.");
			TestServices.InputHelper.MoveMouse(menuFlyoutItem3);
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.InputHelper.MoveMouse(appBarButtonWithoutFlyout);
			await menuFlyout3ClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over MenuFlyoutAppBarButton3, which should open the third menu flyout.");
			TestServices.InputHelper.MoveMouse(menuFlyoutAppBarButton3);
			await menuFlyout3OpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over MenuFlyoutItem3, then over AppBarToggleButton, which should close the third menu flyout.");
			TestServices.InputHelper.MoveMouse(menuFlyoutItem3);
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.InputHelper.MoveMouse(appBarToggleButton);
			await menuFlyout3ClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				commandBar.IsOpen = false;
			});

			await commandBarClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			FlyoutHelper.HideFlyout(commandBarFlyout);
		}

		[TestMethod]

		[Description("Verify that setting the Flyout property on an AppBarButton and then showing that flyout does not display a light-dismiss layer that prevents interaction with the rest of the CommandBar.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[Ignore("InputHelper.MoveMouse not implemented.")]
		public async Task VerifySubMenuDoesNotEatPointerInput()
		{
			TestCleanupWrapper cleanup;

			CommandBar commandBar = null;
			AppBarButton flyoutAppBarButton = null;
			AppBarButton otherAppBarButton = null;

			var commandBarOpenedEvent = new Event();
			var commandBarOpenedEventRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");
			var commandBarClosedEvent = new Event();
			var commandBarClosedEventRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Closed");
			var menuFlyoutOpenedEvent = new Event();
			var menuFlyoutOpenedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Opened");
			var menuFlyoutClosedEvent = new Event();
			var menuFlyoutClosedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Closed");

			await RunOnUIThread(() =>
			{
				var page = TestServices.WindowHelper.SetupSimulatedAppPage();

				var root = (Grid)(XamlReader.Load(
					@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
							  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
							  Background=""Transparent"">
							<CommandBar   x:Name=""CommandBar"" Width=""300"">
								<CommandBar.SecondaryCommands>
									<AppBarButton  x:Name=""FlyoutAppBarButton"" Label=""Open flyout"">
										<AppBarButton.Flyout>
											<MenuFlyout x:Name=""MenuFlyout"">
												<MenuFlyoutItem Text=""Click me"" />
											</MenuFlyout>
										</AppBarButton.Flyout>
									</AppBarButton>
									<AppBarButton  x:Name=""OtherAppBarButton"" Label=""Other AppBarButton"" />
								</CommandBar.SecondaryCommands>
							</CommandBar>
						</Grid>"));


				commandBar = (CommandBar)(root.FindName("CommandBar"));
				flyoutAppBarButton = (AppBarButton)(root.FindName("FlyoutAppBarButton"));
				otherAppBarButton = (AppBarButton)(root.FindName("OtherAppBarButton"));

				var menuFlyout = (MenuFlyout)(root.FindName("MenuFlyout"));

				commandBarOpenedEventRegistration.Attach(commandBar, (s, e) => { commandBarOpenedEvent.Set(); });
				commandBarClosedEventRegistration.Attach(commandBar, (s, e) => { commandBarClosedEvent.Set(); });
				menuFlyoutOpenedEventRegistration.Attach(menuFlyout, (s, e) => { menuFlyoutOpenedEvent.Set(); });
				menuFlyoutClosedEventRegistration.Attach(menuFlyout, (s, e) => { menuFlyoutClosedEvent.Set(); });

				SetPageContent(root, page);
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>

			{
				commandBar.IsOpen = true;
			});

			await commandBarOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over FlyoutAppBarButton, which should open the flyout.");
			TestServices.InputHelper.MoveMouse(flyoutAppBarButton);
			await menuFlyoutOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving mouse over OtherAppBarButton, which should close the flyout.");
			TestServices.InputHelper.MoveMouse(otherAppBarButton);
			await menuFlyoutClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>

			{
				commandBar.IsOpen = false;
			});

			await commandBarClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();
		}


		[TestMethod]

		[Description("Verify that setting the Flyout property on a primary AppBarButton does not cause that flyout to cease to have a light-dismiss layer.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[Ignore("InputHelper.Tap(Point) not implemented.")]
		public async Task VerifySubMenuHasLightDismissOnPrimaryAppBarButton()
		{
			TestCleanupWrapper cleanup;

			CommandBar commandBar = null;
			AppBarButton flyoutAppBarButton = null;

			var menuFlyoutOpenedEvent = new Event();
			var menuFlyoutOpenedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Opened");
			var menuFlyoutClosedEvent = new Event();
			var menuFlyoutClosedEventRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler>("Closed");

			await RunOnUIThread(() =>
			{
				var page = TestServices.WindowHelper.SetupSimulatedAppPage();

				var root = (Grid)(XamlReader.Load(
				@"(<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
							xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
							Background=""Transparent"">
						<CommandBar   x:Name=""CommandBar"" Width=""300"">
							<AppBarButton  x:Name=""FlyoutAppBarButton"" Label=""Open flyout"">
								<AppBarButton.Flyout>
									<MenuFlyout x:Name=""MenuFlyout"">
										<MenuFlyoutItem Text=""Click me"" />
									</MenuFlyout>
								</AppBarButton.Flyout>
							</AppBarButton>
						</CommandBar>
					</Grid>)"));

				commandBar = (CommandBar)(root.FindName("CommandBar"));
				flyoutAppBarButton = (AppBarButton)(root.FindName("FlyoutAppBarButton"));

				var menuFlyout = (MenuFlyout)(root.FindName("MenuFlyout"));

				menuFlyoutOpenedEventRegistration.Attach(menuFlyout, (s, e) => { menuFlyoutOpenedEvent.Set(); });
				menuFlyoutClosedEventRegistration.Attach(menuFlyout, (s, e) => { menuFlyoutClosedEvent.Set(); });

				SetPageContent(root, page);
			});

			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Tapping on FlyoutAppBarButton, which should open the flyout.");
			TestServices.InputHelper.Tap(flyoutAppBarButton);
			await menuFlyoutOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Tapping in empty space, which should close the flyout.");
			TestServices.InputHelper.Tap(new Point(300, 300));
			await menuFlyoutClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates that setting IsChecked on an AppBarToggleButton programatically will still result in the same visual effect")]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task ValidateAppBarToggleButtonIsCheckedProgramatically()
		{
			TestCleanupWrapper cleanup;

			CommandBar root = null;
			AppBarToggleButton button = null;

			await RunOnUIThread(() =>
			{
				root = (CommandBar)XamlReader.Load(@"

                            <CommandBar xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                                <AppBarToggleButton x:Name=""button""/>
                            </CommandBar>
				");

				button = (AppBarToggleButton)root.FindName("button");

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			await OpenCommandBar(root, OpenMethod.Programmatic);

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				button = (AppBarToggleButton)root.FindName("button");
			});

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(button, "CommonStates", "Normal"));
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				button.IsChecked = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(button, "CommonStates", "Checked"));
			});
			await WindowHelper.WaitForIdle();
		}


		private async Task ValidateDynamicOverflowOrderWorker(CommandBar cmdBar, DynamicOverflowOrderTest orderTestCase)
		{
			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);

			await RunOnUIThread(() =>
			{
				var primaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(cmdBar, "PrimaryItemsControl");
				var primaryItems = primaryItemsControl.Items;
				int primaryItemsCount = primaryItems.Count;

				var overflowContentRoot = (FrameworkElement)TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);
				var secondaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(overflowContentRoot, "SecondaryItemsControl");
				var secondaryItems = secondaryItemsControl.Items;
				int secondaryCount = secondaryItems.Count;

				LOG_OUTPUT($"Primary items counts : {primaryItemsCount}");
				LOG_OUTPUT($"Secondary items counts : {secondaryCount}");

				switch (orderTestCase)
				{
					case DynamicOverflowOrderTest.OrderTestForAlternativeValue: // Test for alternative order - {1,2,1,2,1,2,1,2,1,2}
						{
							for (int i = 0; i < 10; i++)
							{
								if (i % 2 == 0)
								{
									var button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(i));
									VERIFY_IS_TRUE(button.IsInOverflow);
								}
							}
							break;
						}
					case DynamicOverflowOrderTest.OrderTestForAllSameValue: // Test for all same order - {1,1,1,1,1,1,1,1,1,1}
						{
							for (int i = 0; i < 10; i++)
							{
								var button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(i));
								VERIFY_IS_TRUE(button.IsInOverflow);
							}
							break;
						}
					case DynamicOverflowOrderTest.OrderTestForTwoPairedValue: // Test for paired order group - {1,2,3,4,5,1,2,3,4,5}
						{
							VERIFY_IS_TRUE(primaryItemsCount == 4);
							VERIFY_IS_TRUE(secondaryCount == 6);

							var button1 = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(0));
							var button8 = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(7));
							VERIFY_IS_TRUE(button1.IsInOverflow);
							VERIFY_IS_TRUE(button1.IsInOverflow);
							break;
						}
					case DynamicOverflowOrderTest.OrderTestForFallbackDefault: // Test for order set and default rightmost dynamic overflow - {1,1,2,2,0,0,0,0,0,0}
						{
							VERIFY_IS_TRUE(primaryItemsCount == 5);
							VERIFY_IS_TRUE(secondaryCount == 5);

							var button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(0));
							VERIFY_IS_TRUE(button.IsInOverflow);
							button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(1));
							VERIFY_IS_TRUE(button.IsInOverflow);
							button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(2));
							VERIFY_IS_TRUE(button.IsInOverflow);
							button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(3));
							VERIFY_IS_TRUE(button.IsInOverflow);
							button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(9));
							VERIFY_IS_TRUE(button.IsInOverflow);
							break;
						}
					case DynamicOverflowOrderTest.OrderTestForMovingPriorSeparator: // Test for order set with moving the separator together - {|, 1, |, 2, 3, 4, 5, 1, 2, 3, 4, 5}
						{
							var separator = (AppBarSeparator)(cmdBar.PrimaryCommands.GetAt(0));
							VERIFY_IS_FALSE(separator.IsInOverflow);
							var button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(1));
							VERIFY_IS_TRUE(button.IsInOverflow);
							separator = (AppBarSeparator)(cmdBar.PrimaryCommands.GetAt(2));
							VERIFY_IS_FALSE(separator.IsInOverflow);
							break;
						}
					case DynamicOverflowOrderTest.OrderTestForMovingPosteriorSeparator: // Test for order set and default rightmost dynamic overflow with moving the separator together  - {0, 0, 0, 0, 0, 0, 0, 0, 0, |, 1, |}
						{
							var separator = (AppBarSeparator)(cmdBar.PrimaryCommands.GetAt(9));
							VERIFY_IS_FALSE(separator.IsInOverflow);
							var button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(10));
							VERIFY_IS_TRUE(button.IsInOverflow);
							separator = (AppBarSeparator)(cmdBar.PrimaryCommands.GetAt(11));
							VERIFY_IS_FALSE(separator.IsInOverflow);
							break;
						}
				}
			});
			await WindowHelper.WaitForIdle();

			await CloseCommandBar(cmdBar);
		}

		private CommandBar CreateCommandBarWithPrimaryCommandOrderSet(DynamicOverflowOrderTest orderTestCase)
		{
			AppBarButton button = null;

			var cmdBar = new CommandBar();
			cmdBar.HorizontalAlignment = HorizontalAlignment.Center;
			cmdBar.Width = 400;

			for (int i = 0; i < 10; i++)
			{
				button = new AppBarButton();
				button.Label = $"p_btn{(i + 1)}";
				cmdBar.PrimaryCommands.Append(button);
			}

			switch (orderTestCase)
			{
				case DynamicOverflowOrderTest.OrderTestForAlternativeValue: // Test for alternative order - {1,2,1,2,1,2,1,2,1,2}
					for (int i = 0; i < 10; i++)
					{
						button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(i));
						button.DynamicOverflowOrder = i % 2 == 0 ? 1 : 2;
					}
					break;
				case DynamicOverflowOrderTest.OrderTestForAllSameValue: // Test for all same order - {1,1,1,1,1,1,1,1,1,1}

					for (int i = 0; i < 10; i++)
					{
						button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(i));
						button.DynamicOverflowOrder = 1;
					}
					break;

				case DynamicOverflowOrderTest.OrderTestForTwoPairedValue: // Test for paired order group - {1,2,3,4,5,1,2,3,4,5}

					for (int i = 0; i < 10; i++)
					{
						button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(i));
						button.DynamicOverflowOrder = i % 5 + 1;
					}
					break;

				case DynamicOverflowOrderTest.OrderTestForFallbackDefault: // Test for order set and default rightmost dynamic overflow - {1,1,2,2,0,0,0,0,0,0}

					for (int i = 0; i < 10; i++)
					{
						button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(i));
						if (i < 2)
						{
							button.DynamicOverflowOrder = 1;
						}
						else if (i < 4)
						{
							button.DynamicOverflowOrder = 2;
						}
					}
					break;

				case DynamicOverflowOrderTest.OrderTestForMovingPriorSeparator: // Test for separator moving that is in the prior index of moving primary command

					for (int i = 0; i < 10; i++)
					{
						button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(i));
						button.DynamicOverflowOrder = i % 5 + 1;
					}
					var separator1 = new AppBarSeparator();
					cmdBar.PrimaryCommands.Insert(1, separator1);
					var separator2 = new AppBarSeparator();
					cmdBar.PrimaryCommands.Insert(0, separator2);
					break;

				case DynamicOverflowOrderTest.OrderTestForMovingPosteriorSeparator: // Test for separator moving that is in the posterior index of moving primary command

					button = (AppBarButton)(cmdBar.PrimaryCommands.GetAt(9));
					button.DynamicOverflowOrder = 1;
					var separatorOne = new AppBarSeparator();
					cmdBar.PrimaryCommands.Insert(10, separatorOne);
					var separatorTwo = new AppBarSeparator();
					cmdBar.PrimaryCommands.Insert(9, separatorTwo);
					break;
			}

			return cmdBar;
		}

		private async Task ValidateDynamicOverflowItemsChangingEventWorker(CommandBar cmdBar, bool isAdding)
		{
			var dynamicOverflowItemsChangingEvent = new Event();
			var dynamicOverflowItemsChangingRegistration = CreateSafeEventRegistration<CommandBar, TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs>>("DynamicOverflowItemsChanging");

			using var _ = dynamicOverflowItemsChangingRegistration.Attach(cmdBar, (s, e) =>
			{
				if (e.Action == CommandBarDynamicOverflowAction.AddingToOverflow)
				{
					VERIFY_IS_TRUE(isAdding);
				}
				else
				{
					VERIFY_IS_FALSE(isAdding);
				}
				dynamicOverflowItemsChangingEvent.Set();
			});

			await RunOnUIThread(() =>
			{
				if (isAdding)
				{
					var addedButton1 = new AppBarButton();
					cmdBar.PrimaryCommands.Append(addedButton1);
				}
				else
				{
					cmdBar.PrimaryCommands.RemoveAt(0);
				}
			});
			await dynamicOverflowItemsChangingEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

		}

		private async Task<(Page, CommandBar)> SetupDynamicOverflowTest(
			int numButtonsToAddExtraToPrimary,
			int numButtonsToAddToSecondary,
			bool isSetOrder = false)
		{
			Page page = null;
			CommandBar cmdBar = null;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();


				page = WindowHelper.SetupSimulatedAppPage();
				//page.BottomAppBar = cmdBar;
				SetPageContent(cmdBar, page);

				var button = new AppBarButton();
				cmdBar.PrimaryCommands.Append(button);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () =>
			{
				var button = (AppBarButton)cmdBar.PrimaryCommands.GetAt(0);
				button.Label = "p_btn0";

				var moreButton = await GetMoreButton(cmdBar);

				LOG_OUTPUT($"CommandBar ActualWidth = {cmdBar.ActualWidth}");
				LOG_OUTPUT($"AppBarButton ActualWidth = {button.ActualWidth}");
				LOG_OUTPUT($"MoreButton ActualWidth = {moreButton.ActualWidth}");

				VERIFY_ARE_NOT_EQUAL(button.ActualWidth, 0);

				int numButtonsToStayOnPrimary = (int)((cmdBar.ActualWidth - moreButton.ActualWidth) / button.ActualWidth);
				int numButtonsToAddToPrimary = numButtonsToStayOnPrimary + numButtonsToAddExtraToPrimary - 1;

				for (int i = 0; i < numButtonsToAddToPrimary; ++i)
				{
					button = new AppBarButton();
					button.Label = $"p_btn{(i + 1)}";
					if (isSetOrder)
					{
						button.DynamicOverflowOrder = i;
					}
					cmdBar.PrimaryCommands.Append(button);
				}

				for (int i = 0; i < numButtonsToAddToSecondary; ++i)
				{
					button = new AppBarButton();
					button.Label = $"s_btn{i}";
					cmdBar.SecondaryCommands.Append(button);
				}
			});
			await WindowHelper.WaitForIdle();

			return (page, cmdBar);
		}

		private async Task ValidateOverflowButtonHidesWhenAppropriate(bool addPrimary, bool addSecondary)
		{
			TestCleanupWrapper cleanup;

			for (int closedDisplayModeValue = 0; closedDisplayModeValue < 3; closedDisplayModeValue++)
			{
				for (int defaultLabelPositionValue = 0; defaultLabelPositionValue < 3; defaultLabelPositionValue++)
				{
					for (int overflowButtonVisibilityValue = 0; overflowButtonVisibilityValue < 3; overflowButtonVisibilityValue++)
					{
						AppBarClosedDisplayMode closedDisplayMode = (AppBarClosedDisplayMode)closedDisplayModeValue;
						CommandBarDefaultLabelPosition defaultLabelPosition = (CommandBarDefaultLabelPosition)defaultLabelPositionValue;
						CommandBarOverflowButtonVisibility overflowButtonVisibility = (CommandBarOverflowButtonVisibility)overflowButtonVisibilityValue;

						LOG_OUTPUT($"Testing the overflow button's visibility with {(addPrimary ? "a primary button" : "no primary buttons")}, {(addSecondary ? "a secondary button" : "no secondary buttons")}, ClosedDisplayMode = {(closedDisplayMode == AppBarClosedDisplayMode.Compact ? "Compact" : (closedDisplayMode == AppBarClosedDisplayMode.Minimal ? "Minimal" : "Hidden"))}, DefaultLabelPosition = {(defaultLabelPosition == CommandBarDefaultLabelPosition.Bottom ? "Bottom" : (defaultLabelPosition == CommandBarDefaultLabelPosition.Right ? "Right" : "Collapsed"))}, and OverflowButtonVisibility = {(overflowButtonVisibility == CommandBarOverflowButtonVisibility.Auto ? "Auto" : (overflowButtonVisibility == CommandBarOverflowButtonVisibility.Visible ? "Visible" : "Collapsed"))}.");

						// We expect the overflow button to be visible if we tell it to be visible through OverflowButtonVisibility,
						// or if OverflowButtonVisibility is Auto and either there are secondary items or there is at least one primary item
						// with a bottom-aligned label, or if ClosedDisplayMode is something other than Compact.
						Visibility expectedOverflowButtonVisibility =
							overflowButtonVisibility == CommandBarOverflowButtonVisibility.Visible ||
							(overflowButtonVisibility == CommandBarOverflowButtonVisibility.Auto &&
							(addSecondary ||
							(addPrimary && defaultLabelPosition == CommandBarDefaultLabelPosition.Bottom) ||
							closedDisplayMode != AppBarClosedDisplayMode.Compact)) ?
							Visibility.Visible :
							Visibility.Collapsed;

						LOG_OUTPUT($"We expect the overflow button to be {(expectedOverflowButtonVisibility == Visibility.Visible ? "visible" : "collapsed")}.");

						await ValidateOverflowButtonState(
							addPrimary,
							addSecondary,
							closedDisplayMode,
							defaultLabelPosition,
							overflowButtonVisibility,
							expectedOverflowButtonVisibility);
					}
				}
			}
		}

		private async Task ValidateDynamicOverflowWorker(
			CommandBar cmdBar,
			bool isPrimaryCommandMovedToOverflow,
			int numButtonsToAddExtraToPrimary,
			int numButtonsToAddToSecondary,
			bool isSetOrder = false)
		{
			int primaryItemsCount = 0;

			await RunOnUIThread(async () =>
			{
				var primaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(cmdBar, "PrimaryItemsControl");
				var primaryItems = primaryItemsControl.Items;

				primaryItemsCount = primaryItems.Count;

				LOG_OUTPUT($"Number of buttons in CommandBar.PrimaryCommands: {cmdBar.PrimaryCommands.Count}");
				LOG_OUTPUT($"Number of buttons in CommandBar.SecondaryCommands: {cmdBar.SecondaryCommands.Count}");
				LOG_OUTPUT($"Number of buttons in CommandBar.PrimaryItemsControl Count: {primaryItemsCount}");
				LOG_OUTPUT($"PrimaryItemsControl ActualWidth = {primaryItemsControl.ActualWidth}");

				if (isPrimaryCommandMovedToOverflow)
				{
					var button = (AppBarButton)cmdBar.PrimaryCommands.GetAt(0);
					var moreButton = await GetMoreButton(cmdBar);
					int numButtonsToStayOnPrimary = (int)((cmdBar.ActualWidth - moreButton.ActualWidth) / button.ActualWidth);

					VERIFY_IS_TRUE(primaryItemsCount == numButtonsToStayOnPrimary);
					VERIFY_IS_TRUE(cmdBar.PrimaryCommands.Count == primaryItemsCount + numButtonsToAddExtraToPrimary);
				}
				else
				{
					VERIFY_IS_TRUE(cmdBar.PrimaryCommands.Count == primaryItemsCount);
				}

				bool areAllPrimaryItemsCompact = true;
				for (int i = 0; i < primaryItems.Count; i++)
				{
					var button = (AppBarButton)primaryItems.GetAt(i);
					areAllPrimaryItemsCompact = areAllPrimaryItemsCompact && button.IsCompact;
				}
				VERIFY_IS_TRUE(areAllPrimaryItemsCompact);
			});

			await OpenCommandBar(cmdBar, OpenMethod.Programmatic);

			await RunOnUIThread(async () =>
			{
				int numButtonsPrimaryCommandsInOverflow = cmdBar.PrimaryCommands.Count - primaryItemsCount;

				var overflowContentRoot = (FrameworkElement)TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);
				var secondaryItemsControl = (ItemsControl)TreeHelper.GetVisualChildByName(overflowContentRoot, "SecondaryItemsControl");
				var secondaryItems = secondaryItemsControl.Items;

				LOG_OUTPUT($"Number of primary buttons in current Primary items : {primaryItemsCount}");
				LOG_OUTPUT($"Number of buttons in current SecondaryCommands items : {secondaryItems.Size}");
				LOG_OUTPUT($"Number of primary buttons in overflow: {numButtonsPrimaryCommandsInOverflow}");

				VERIFY_IS_TRUE(isPrimaryCommandMovedToOverflow ? numButtonsPrimaryCommandsInOverflow == numButtonsToAddExtraToPrimary : numButtonsPrimaryCommandsInOverflow == 0);
				VERIFY_IS_TRUE(secondaryItems.Count == numButtonsPrimaryCommandsInOverflow + numButtonsToAddToSecondary + (isPrimaryCommandMovedToOverflow ? 1 : 0));

				for (int i = 0; i < secondaryItems.Count; i++)
				{
					if (i < numButtonsPrimaryCommandsInOverflow)
					{
						var button = (AppBarButton)(secondaryItems.GetAt(i));
						VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(button, "ApplicationViewStates", "Overflow"));
					}
					else if (isPrimaryCommandMovedToOverflow && i == numButtonsPrimaryCommandsInOverflow)
					{
						var overflowSeparator = (AppBarSeparator)(secondaryItems.GetAt(i));
						VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(overflowSeparator, "ApplicationViewStates", "Overflow"));
					}
					else
					{
						var button = (AppBarButton)(secondaryItems.GetAt(i));
						VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(button, "ApplicationViewStates", "Overflow"));
					}
				}

				if (isSetOrder)
				{
					int maxOrderInOverflow = 0;

					for (int i = 0; i < numButtonsPrimaryCommandsInOverflow; i++)
					{
						var button = (AppBarButton)(secondaryItems.GetAt(i));
						if (button.DynamicOverflowOrder > maxOrderInOverflow)
						{
							maxOrderInOverflow = button.DynamicOverflowOrder;
						}
					}

					var primaryItemsControl = (ItemsControl)(TreeHelper.GetVisualChildByName(cmdBar, "PrimaryItemsControl"));
					var primaryItems = primaryItemsControl.Items;

					for (int i = 0; i < primaryItems.Count; i++)
					{
						var button = (AppBarButton)(primaryItems.GetAt(i));
						if (button.DynamicOverflowOrder != 0)
						{
							VERIFY_IS_TRUE(button.DynamicOverflowOrder > maxOrderInOverflow);
						}
					}
				}

			});

			await CloseCommandBar(cmdBar);
		}

		private async Task ValidateOverflowButtonState(
			bool addPrimary,
			bool addSecondary,
			AppBarClosedDisplayMode closedDisplayMode,
			CommandBarDefaultLabelPosition defaultLabelPosition,
			CommandBarOverflowButtonVisibility overflowButtonVisibility,
			Visibility expectedOverflowButtonVisibility)
		{
			CommandBar cmdBar = null;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				var rootGrid = new Grid();

				loadedRegistration.Attach(rootGrid, (s, e) => loadedEvent.Set());

				cmdBar = new CommandBar();
				cmdBar.ClosedDisplayMode = closedDisplayMode;
				cmdBar.DefaultLabelPosition = defaultLabelPosition;
				cmdBar.OverflowButtonVisibility = overflowButtonVisibility;

				if (addPrimary)
				{
					var button = new AppBarButton();
					button.Label = "button";
					cmdBar.PrimaryCommands.Append(button);
				}

				if (addSecondary)
				{
					var button = new AppBarButton();
					button.Label = "button";
					cmdBar.SecondaryCommands.Append(button);
				}

				rootGrid.Children.Append(cmdBar);
				SetWindowContent(rootGrid);
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(TreeHelper.GetVisualChildByName(cmdBar, "MoreButton").Visibility, expectedOverflowButtonVisibility);
			});
		}

		private async Task OpenCommandBar(CommandBar cmdBar, OpenMethod openMethod)
		{
			Event openedEvent = new Event();
			var openedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");
			openedRegistration.Attach(cmdBar, (s, e) => openedEvent.Set());
			Control moreButton = await GetMoreButton(cmdBar);

			if (openMethod == OpenMethod.Mouse)
			{
				await ControlHelper.DoClickUsingAP(moreButton as Button);
			}
			else if (openMethod == OpenMethod.Touch)
			{
				await ControlHelper.DoClickUsingAP(moreButton as Button);
			}
			else if (openMethod == OpenMethod.Keyboard)
			{
				await RunOnUIThread(() => moreButton.Focus(FocusState.Keyboard));
				await WindowHelper.WaitForIdle();
				KeyboardHelper.PressKeySequence(" ", moreButton);
			}
			else if (openMethod == OpenMethod.Gamepad)
			{
				await RunOnUIThread(() => moreButton.Focus(FocusState.Keyboard));
				await WindowHelper.WaitForIdle();
				CommonInputHelper.Accept(InputDevice.Gamepad, moreButton);
			}
			else if (openMethod == OpenMethod.Programmatic)
			{
				await RunOnUIThread(() => cmdBar.IsOpen = true);

			}

			await openedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();
			await Task.Delay(2000);
		}

		private async Task CloseCommandBar(CommandBar cmdBar)
		{
			var closedEvent = new Event();
			var closedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Closed");
			closedRegistration.Attach(cmdBar, (s, e) => closedEvent.Set());

			await RunOnUIThread(() => cmdBar.IsOpen = false);
			await closedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();
		}

		private async Task EmptyPageContent(Page page)
		{
			await RunOnUIThread(() =>
			{
				page.TopAppBar = null;
				page.BottomAppBar = null;
				page.Content = null;
			});
			await WindowHelper.WaitForIdle();
		}

		private async Task SimulateTabAsync(UIElement container) => await SimulateNavigationDirectionAsync(container, FocusNavigationDirection.Next);

		private async Task SimulateShiftTabAsync(UIElement container) => await SimulateNavigationDirectionAsync(container, FocusNavigationDirection.Previous);

		private async Task SimulateUpAsync(UIElement container) => await SimulateNavigationDirectionAsync(container, FocusNavigationDirection.Up);

		private async Task SimulateNavigationDirectionAsync(UIElement container, FocusNavigationDirection direction)
		{
			await RunOnUIThread(() =>
			{
				var nextElement = FocusManager.FindNextElement(direction, new FindNextElementOptions()
				{
#if !WINAPPSDK
					SearchRoot = container.XamlRoot.Content
#endif
				});
				if (nextElement != null && nextElement is UIElement uiElement)
				{
					uiElement.Focus(FocusState.Keyboard);
				}
			});
			// Small delay to ensure the focus event have time to fire
			await Task.Delay(50);
		}


		private static UIElement GetSecondaryItemsPresenter(CommandBar cmdBar)
		{
			UIElement secondaryItemsPresenter;
			var commandBarOverflowPresenter = TreeHelper.GetVisualChildByNameFromOpenPopups("SecondaryItemsControl", cmdBar);
			secondaryItemsPresenter = TreeHelper.GetVisualChildByName(commandBarOverflowPresenter, "ItemsPresenter");
			return secondaryItemsPresenter;
		}


		private Panel ValidateUIElementTestSetup(bool addPrimary, bool addSecondary)
		{
			//      xaml_controls::Grid^ rootGrid = nullptr;
			//      RunOnUIThread([&] ()

			//{
			//	rootGrid = ref new xaml_controls::Grid();
			//	TestServices::WindowHelper->WindowContent = rootGrid;
			//});
			//      TestServices::WindowHelper->WaitForIdle();

			//// Inject a tab to ensure that the last input device type used is consistent,
			//// since that affects the layout of the CommandBar.
			//TestServices::KeyboardHelper->Tab();

			//xaml_controls::AppBarButton^ lastAddedButton = nullptr;

			//      RunOnUIThread([&] ()

			//{
			//	const size_t numClosedDisplayModes = 3;
			//	const size_t numDefaultLabelPositions = 3;

			//	for (size_t mode = 0; mode < numClosedDisplayModes; ++mode)
			//	{
			//		for (size_t isOpen = 0; isOpen < 2; ++isOpen)
			//		{
			//			for (size_t defaultLabelPosition = 0; defaultLabelPosition < numDefaultLabelPositions; ++defaultLabelPosition)
			//			{
			//				auto cmdBar = ref new xaml_controls::CommandBar();
			//				cmdBar->IsOpen = (isOpen > 0);
			//				cmdBar->ClosedDisplayMode = static_cast<xaml_controls::AppBarClosedDisplayMode>(mode);
			//				cmdBar->DefaultLabelPosition = static_cast<xaml_controls::CommandBarDefaultLabelPosition>(defaultLabelPosition);

			//				if (addPrimary)
			//				{
			//					lastAddedButton = ref new xaml_controls::AppBarButton();
			//					lastAddedButton->Label = "button";
			//					cmdBar->PrimaryCommands->Append(lastAddedButton);
			//				}

			//				if (addSecondary)
			//				{
			//					lastAddedButton = ref new xaml_controls::AppBarButton();
			//					lastAddedButton->Label = "button";
			//					cmdBar->SecondaryCommands->Append(lastAddedButton);
			//				}

			//				rootGrid->Children->Append(cmdBar);
			//				xaml_controls::Grid::SetRow(cmdBar, 2 * static_cast<int>(mode) + static_cast<int>(isOpen));
			//				xaml_controls::Grid::SetColumn(cmdBar, static_cast<int>(defaultLabelPosition));
			//			}
			//		}
			//	}

			//	for (size_t rowCount = 0; rowCount < 2 * numClosedDisplayModes; rowCount++)
			//	{
			//		rootGrid->RowDefinitions->Append(ref new xaml_controls::RowDefinition());
			//	}

			//	for (size_t columnCount = 0; columnCount < numDefaultLabelPositions; columnCount++)
			//	{
			//		rootGrid->ColumnDefinitions->Append(ref new xaml_controls::ColumnDefinition());
			//	}
			//});
			//      TestServices::WindowHelper->WaitForIdle();

			//// Set focus on a primary button or a secondary button because having focus present on the MoreButton (by default)
			//// causes the "See More" ToolTip to appear depending upon timing and cause unpredictable failures.
			//RunOnUIThread([&] ()

			//{
			//	lastAddedButton->Focus(xaml::FocusState::Programmatic);
			//});
			//      TestServices::WindowHelper->WaitForIdle();

			//      return rootGrid;
			return new Grid();
		}

		private void ValidateRightClickBehaviorWorker(AppBarClosedDisplayMode closedDisplayMode)
		{
			//	// CoreWindow isn't agile, so we can't use the SafeEventRegistration utility,
			//	// so we have to manage it manually.
			//	wf::EventRegistrationToken coreWindowPointerPressedToken = { };

			//	TestCleanupWrapper cleanup([&coreWindowPointerPressedToken]()

			//{
			//		RunOnUIThread([&coreWindowPointerPressedToken]()

			//	{
			//			xaml::Window::Current->CoreWindow->PointerPressed -= coreWindowPointerPressedToken;
			//			coreWindowPointerPressedToken = { };
			//		});

			//		TestServices::WindowHelper->ResetWindowContentAndWaitForIdle();
			//	});

			//	// CommandBars should never open in response to right-click.
			//	const bool expectedTopBottomIsOpenValue = false;
			//	const bool expectedInlineIsOpenValue = false;

			//	xaml_controls::Page ^ page = nullptr;
			//	xaml_controls::CommandBar ^ topCmdBar = nullptr;
			//	xaml_controls::CommandBar ^ bottomCmdBar = nullptr;
			//	xaml_controls::CommandBar ^ inlineCmdBar = nullptr;

			//	auto rightClickProcessedEvent = std::make_shared<Event>();

			//	RunOnUIThread([&]()

			//{
			//		topCmdBar = ref new xaml_controls::CommandBar();
			//		bottomCmdBar = ref new xaml_controls::CommandBar();
			//		inlineCmdBar = ref new xaml_controls::CommandBar();

			//		topCmdBar->ClosedDisplayMode = closedDisplayMode;
			//		bottomCmdBar->ClosedDisplayMode = closedDisplayMode;
			//		inlineCmdBar->ClosedDisplayMode = closedDisplayMode;

			//		auto grid = ref new xaml_controls::Grid();
			//		grid->Children->Append(inlineCmdBar);

			//		coreWindowPointerPressedToken = xaml::Window::Current->CoreWindow->PointerPressed +=
			//			ref new wf::TypedEventHandler<wuc::CoreWindow^, wuc::PointerEventArgs ^> ([rightClickProcessedEvent](wuc::CoreWindow ^, wuc::PointerEventArgs ^) {
			//			rightClickProcessedEvent->Set();
			//		});

			//		page = TestServices::WindowHelper->SetupSimulatedAppPage();
			//		auto frame = safe_cast < xaml_controls::Frame ^> (Window::Current->Content);

			//		page->TopAppBar = topCmdBar;
			//		page->BottomAppBar = bottomCmdBar;
			//		page->Content = grid;
			//	});
			//	TestServices::WindowHelper->WaitForIdle();

			//	// Inject right-click.
			//	TestServices::InputHelper->MoveMouse(page);
			//	TestServices::InputHelper->MouseButtonDown(page, 0, 0, MouseButton::Right);
			//	TestServices::InputHelper->MouseButtonUp(page, 0, 0, MouseButton::Right);
			//	TestServices::WindowHelper->WaitForIdle();
			//	rightClickProcessedEvent->WaitForDefault();

			//	RunOnUIThread([&]()

			//{
			//		VERIFY_ARE_EQUAL(topCmdBar->IsOpen, expectedTopBottomIsOpenValue);
			//		VERIFY_ARE_EQUAL(bottomCmdBar->IsOpen, expectedTopBottomIsOpenValue);
			//		VERIFY_ARE_EQUAL(inlineCmdBar->IsOpen, expectedInlineIsOpenValue);
			//	});
			//	TestServices::WindowHelper->WaitForIdle();

			//	EmptyPageContent(page);
		}

		private async Task ValidateOverflowPlacementWorker(OverflowOpenDirection openDirection, OverflowAlignment alignment, bool isRTL)
		{
			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>("Loaded");

			CommandBar cmdBar = null;
			Grid root = null;

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.Width = 150;
				var menuitem = new AppBarButton();
				menuitem.Label = "menu item";
				menuitem.Width = 200;
				cmdBar.SecondaryCommands.Append(menuitem);

				cmdBar.VerticalAlignment = openDirection == OverflowOpenDirection.Up ? VerticalAlignment.Bottom : VerticalAlignment.Top;
				cmdBar.HorizontalAlignment = alignment == OverflowAlignment.Left ? HorizontalAlignment.Left : HorizontalAlignment.Right;

				root = new Grid();
				root.Children.Append(cmdBar);
				if (isRTL)
				{
					root.FlowDirection = FlowDirection.RightToLeft;
				}

				loadedRegistration.Attach(root, (s, e) =>
				{
					LOG_OUTPUT("Grid.Loaded raised.");
					loadedEvent.Set();
				});

				SetWindowContent(root);
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			//UNO TODO: IsOpen on load
			await RunOnUIThread(() => cmdBar.IsOpen = true);
			await Task.Delay(2000);

			await RunOnUIThread(() =>
			{
				var overflowContentRoot = (UIElement)TreeHelper.GetVisualChildByNameFromOpenPopups("OverflowContentRoot", cmdBar);
				VERIFY_IS_NOT_NULL(overflowContentRoot);

				var transform = overflowContentRoot.TransformToVisual(cmdBar);
				var overflowTransformedBounds = transform.TransformBounds(new Rect(0, 0, overflowContentRoot.DesiredSize.Width, overflowContentRoot.DesiredSize.Height));

				if (openDirection == OverflowOpenDirection.Up)
				{
					VERIFY_IS_LESS_THAN(overflowTransformedBounds.Y, 0);
				}
				else // OverflowOpenDirection::Down
				{
					VERIFY_IS_GREATER_THAN(overflowTransformedBounds.Y, 0);
				}

				if (alignment == OverflowAlignment.Left)
				{
					VERIFY_ARE_VERY_CLOSE(overflowTransformedBounds.X, 0);
				}
				else // OverflowAlignment::Right
				{
					VERIFY_ARE_VERY_CLOSE(cmdBar.ActualWidth, overflowTransformedBounds.X + overflowTransformedBounds.Width);
				}
			});
			await WindowHelper.WaitForIdle();
		}

		private async Task<Control> GetMoreButton(CommandBar cmdBar)
		{
			Control moreButton = null;
			await RunOnUIThread(() =>
			{
				moreButton = (Control)TreeHelper.GetVisualChildByName(cmdBar, "MoreButton");
			});

			return moreButton;
		}

		private async Task ValidateOpenAndCloseWorker(Func<CommandBar, Task> openFunc, Func<CommandBar, Task> closeFunc)
		{
			CommandBar cmdBar = null;

			var openedEvent = new Event();
			var closedEvent = new Event();

			var openedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<CommandBar, EventHandler<object>>("Closed");

			await RunOnUIThread(() =>
			{
				cmdBar = new CommandBar();
				cmdBar.VerticalAlignment = VerticalAlignment.Center; // Center it to get it out from under the status bar.

				// Add at least one primary command and one secondary command to the CommandBar.
				cmdBar.PrimaryCommands.Add(new AppBarButton());
				cmdBar.SecondaryCommands.Add(new AppBarButton());

				openedRegistration.Attach(cmdBar, (s, e) => openedEvent.Set());
				closedRegistration.Attach(cmdBar, (s, e) => closedEvent.Set());

				var root = new Grid();
				root.Children.Add(cmdBar);

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			await openFunc(cmdBar);
			await WindowHelper.WaitForIdle();
			await openedEvent.WaitForDefault();

			await closeFunc(cmdBar);
			await WindowHelper.WaitForIdle();
			await closedEvent.WaitForDefault();
		}

		private void SetWindowContent(UIElement content)
		{
			//SetResources(content);
			WindowHelper.WindowContent = content;
		}

		private void SetPageContent(UIElement content, Page page)
		{
			//SetResources(content);
			page.Content = content;
		}

		private void SetResources(DependencyObject content)
		{
			var cmdBar = content as CommandBar;
			if (cmdBar != null)
			{
				cmdBar.Resources.MergedDictionaries.Add(new XamlControlsResources());
				return;
			}

			foreach (var child in VisualTreeHelper.GetChildren(content))
			{
				SetResources(child);
			}
		}

		private enum Location
		{
			Inline,
			Top,
			Bottom
		}

		private enum OverflowOpenDirection
		{
			Up = 0,
			Down
		}

		private enum OverflowAlignment
		{
			Left = 0,
			Right
		}

		private enum OpenMethod
		{
			Mouse,
			Touch,
			Keyboard,
			Gamepad,
			Programmatic
		}

		private enum DynamicOverflowOrderTest
		{
			OrderTestForAlternativeValue,           // Test for alternative order - {1,2,1,2,1,2,1,2,1,2}
			OrderTestForAllSameValue,               // Test for all same order - {1,1,1,1,1,1,1,1,1,1}
			OrderTestForTwoPairedValue,             // Test for paired order group - {1,2,3,4,5,1,2,3,4,5}
			OrderTestForFallbackDefault,            // Test for order set and default rightmost dynamic overflow - {1,1,2,2,0,0,0,0,0,0}
			OrderTestForMovingPriorSeparator,       // Test for separator moving that is in the prior index of moving primary command
			OrderTestForMovingPosteriorSeparator    // Test for separator moving that is in the posterior index of moving primary command
		}
	}
}
#endif
