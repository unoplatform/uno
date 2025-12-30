#if HAS_UNO
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.System;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;
using FocusHelper = Uno.UI.RuntimeTests.MUX.Input.Focus.FocusHelper;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Tests.Common;
#endif

namespace Uno.UI.RuntimeTests.MUX.Input.KeyboardAccelerators;

[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaMobile)]
public partial class KeyboardAcceleratorTests : MUXApiTestBase
{
	#region BasicKeyboardAcceleratorToolTipVerification

	[TestMethod]
	public async Task ValidateKeyboardAcceleratorToolTipsOnPivot()
	{
		{
			await TestServices.RunOnUIThread(async () =>
			{
				// Introducing a use of Pivot.set_Title so that the linker doesn't remove it :/
				new Pivot() { Title = "Workaround" };
			});
			const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Pivot x:Name='rootPivot' Title='Pivot With Keyboard Accelerator' >
                            <PivotItem x:Name = 'pivotItem1' Header='Pivot Item 1' ToolTipService.ToolTip='Press Ctrl and 1'>
                                <TextBlock Text='Content of pivot item 1.' />
                                <PivotItem.KeyboardAccelerators>
                                    <KeyboardAccelerator Modifiers='Control' Key='Number1' />
                                </PivotItem.KeyboardAccelerators>
                            </PivotItem>
                        </Pivot>
                    </StackPanel>";

			StackPanel rootPanel = null;
			PivotItem pivotItem1 = null;
			String toolTipString = null;
			UIElement keyboardAcceleratorPlacementTarget = null; ;
			ToolTip pivotItemHeadertoolTip;

			await TestServices.RunOnUIThread(async () =>
			{
				rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
				TestServices.WindowHelper.WindowContent = rootPanel;
				await TestServices.WindowHelper.WaitForLoaded(rootPanel);

				pivotItem1 = (PivotItem)rootPanel.FindName("pivotItem1");
				keyboardAcceleratorPlacementTarget = (UIElement)pivotItem1.KeyboardAcceleratorPlacementTarget;
			});

			await TestServices.WindowHelper.WaitForIdle();
			pivotItemHeadertoolTip = await TestServices.WindowHelper.TestGetActualToolTip(keyboardAcceleratorPlacementTarget);
			await TestServices.RunOnUIThread(() =>
			{
				toolTipString = ToolTipService.GetToolTip(pivotItem1) as String;
				Verify.AreEqual(toolTipString, "Press Ctrl and 1");
				// Should match with accelerator as there is no any actual tooltip defined on item header
				Verify.AreEqual(pivotItemHeadertoolTip.Content, "Ctrl+1");
				// Set actual tooltip on header
				ToolTipService.SetToolTip(keyboardAcceleratorPlacementTarget, "Actual ToolTip on Pivot Item Header");

			});

			pivotItemHeadertoolTip = await TestServices.WindowHelper.TestGetActualToolTip(keyboardAcceleratorPlacementTarget);
			await TestServices.RunOnUIThread(() =>
			{
				toolTipString = ToolTipService.GetToolTip(pivotItem1) as String;
				Verify.AreEqual(toolTipString, "Press Ctrl and 1");
				// As now actual tooltip on header is set, it will get preference over keyboard accelerator tooltip
				Verify.AreEqual(pivotItemHeadertoolTip.Content, "Actual ToolTip on Pivot Item Header");

			});

			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates actual tooltip and keyboard accelerator tooltips behavior on button")]
	public async Task ValidateKeyboardAcceleratorToolTips()
	{
		{
			const string rootPanelXaml =
					@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button1' Content='Button-1' ToolTipService.ToolTip='Press Ctrl,Alt and A'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAcceleratorA2' Modifiers='Control,Menu' Key='A' />
                            </Button.KeyboardAccelerators>
                        </Button>
                        <Button x:Name='button2' Content='Button-2'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator Modifiers='Menu' Key='A'/>
                                <KeyboardAccelerator Modifiers='Control,Menu' Key='A'/>
                            </Button.KeyboardAccelerators>
                        </Button>
                        <Button x:Name='button3' Content='Button-3'/>
                    </StackPanel>";

			StackPanel rootPanel = null;
			Button button1 = null;
			Button button2 = null;
			Button button3 = null;
			String toolTipString = null;
			ToolTip toolTip1 = null;
			ToolTip toolTip2 = null;
			ToolTip toolTip3 = null;

			await TestServices.RunOnUIThread(async () =>
			{
				rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
				TestServices.WindowHelper.WindowContent = rootPanel;
				await TestServices.WindowHelper.WaitForLoaded(rootPanel);

				button1 = (Button)rootPanel.FindName("button1");
				button2 = (Button)rootPanel.FindName("button2");
				button3 = (Button)rootPanel.FindName("button3");
			});
			await TestServices.WindowHelper.WaitForIdle();
			toolTip1 = await TestServices.WindowHelper.TestGetActualToolTip(button1);
			toolTip2 = await TestServices.WindowHelper.TestGetActualToolTip(button2);
			toolTip3 = await TestServices.WindowHelper.TestGetActualToolTip(button3);

			await TestServices.RunOnUIThread(() =>
			{
				// Button-1:
				toolTipString = ToolTipService.GetToolTip(button1) as String;
				Verify.AreEqual(toolTipString, "Press Ctrl,Alt and A");
				// As actual tooltip is set on control, it will get preference over keyboard accelerator tooltip
				Verify.AreEqual(toolTip1.Content, "Press Ctrl,Alt and A");

				//Button-2
				toolTipString = ToolTipService.GetToolTip(button2) as String;
				Verify.IsNull(toolTipString);
				// As no actual tooltip is defined, we should expect keyboard accelerator tooltip
				Verify.AreEqual(toolTip2.Content, "Alt+A");

				//Button-3 : No tooltip is defined.
				toolTipString = ToolTipService.GetToolTip(button3) as String;
				Verify.IsNull(toolTipString);
				Verify.IsNull(toolTip3);
			});

			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates ToolTips on KeyboardAccelerator added through code")]
	public async Task ValidateToolTipForAcceleratorDefinedInCode()
	{
		StackPanel rootPanel = null;
		Button button1 = null;
		KeyboardAccelerator ctrl1Button1Accelerator = null;
		String toolTipString = null;
		ToolTip toolTip1 = null;

		await TestServices.RunOnUIThread(() =>
		{
			rootPanel = new StackPanel();
			button1 = new Button();
			ctrl1Button1Accelerator = new KeyboardAccelerator()
			{
				Modifiers = global::Windows.System.VirtualKeyModifiers.Control,
				Key = global::Windows.System.VirtualKey.Number1
			};
			button1.KeyboardAccelerators.Add(ctrl1Button1Accelerator);
			rootPanel.Children.Add(button1);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();
		toolTip1 = await TestServices.WindowHelper.TestGetActualToolTip(button1);

		await TestServices.RunOnUIThread(() =>
		{
			toolTipString = ToolTipService.GetToolTip(button1) as String;
			Verify.IsNull(toolTipString);
			Verify.AreEqual(toolTip1.Content, "Ctrl+1");
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAcceleratorPlacementMode is Hidden by default for AppBar/ AppBarToggleButton/ MenuFlyoutItem when added through xaml script")]
	public async Task ValidateKeyboardAcceleratorPlacementMode()
	{
		{
			const string rootPanelXaml =
					@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <AppBarButton x:Name='AppBarButton' Content='AppBar Button'/>
                        <AppBarToggleButton x:Name='AppBarTButton' Content='AppBar Toggle Button'/>
                        <MenuFlyoutItem x:Name='MenuFlyoutItem' />
                    </StackPanel>";

			StackPanel rootPanel = null;
			AppBarButton appBarButton = null;
			AppBarToggleButton appBarTButton = null;
			MenuFlyoutItem menuFlyoutItem = null;
			await TestServices.RunOnUIThread(async () =>
			{
				rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
				TestServices.WindowHelper.WindowContent = rootPanel;
				await TestServices.WindowHelper.WaitForLoaded(rootPanel);

				appBarButton = (AppBarButton)rootPanel.FindName("AppBarButton");
				appBarTButton = (AppBarToggleButton)rootPanel.FindName("AppBarTButton");
				menuFlyoutItem = (MenuFlyoutItem)rootPanel.FindName("MenuFlyoutItem");

				Verify.AreEqual(
					appBarButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Hidden);
				Verify.AreEqual(
					appBarTButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Hidden);
				Verify.AreEqual(
					menuFlyoutItem.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Hidden);
			});

			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that when a parent element has KeyboardAcceleratorPlacementMode Hidden, it propagates to its children.")]
	public async Task ValidateKeyboardAcceleratorPlacementModeInheritedByChildren()
	{
		{
			const string rootPanelXaml =
					@"<StackPanel x:Name='outerPanel' Width='400' Height='400' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                            <Button x:Name='oButton' Height='80' Width='200' Content='Outer Button'/>
                            <StackPanel x:Name='innerPanel'>
                                <Button x:Name='iButton' Height='80' Width='200' Content='Inner Button'/>
                            </StackPanel>
                        </StackPanel>";

			StackPanel rootPanel = null;
			StackPanel innerPanel = null;
			Button outerButton = null;
			Button innerButton = null;
			await TestServices.RunOnUIThread(async () =>
			{
				rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
				TestServices.WindowHelper.WindowContent = rootPanel;
				await TestServices.WindowHelper.WaitForLoaded(rootPanel);

				innerPanel = (StackPanel)rootPanel.FindName("innerPanel");
				outerButton = (Button)rootPanel.FindName("oButton");
				innerButton = (Button)rootPanel.FindName("iButton");

				// By default KeyboardAcceleratorPlacementMode is set to Auto
				Verify.AreEqual(
					outerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Auto);
				Verify.AreEqual(
					innerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Auto);

				// Set KeyboardAcceleratorPlacementMode on outer panel to Hidden, and check if it gets propagated to it's children
				rootPanel.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
				Verify.AreEqual(
					outerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Hidden);
				Verify.AreEqual(
					innerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Hidden);

				// Set KeyboardAcceleratorPlacementMode on inner panel, and check if it gets propagated to it's children
				// and does not affects it's siblings/ parent
				innerPanel.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Auto;
				Verify.AreEqual(
					outerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Hidden);
				Verify.AreEqual(
					innerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Auto);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verify inherited property KeyboardAcceleratorPlacementMode with UIElement tree dump.")]
	public async Task ValidateKeyboardAcceleratorPlacementModeInheritedProperty()
	{
		{
			const string rootPanelXaml =
					@"<StackPanel x:Name='outerPanel' Width='400' Height='400' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                            <Button x:Name='oButton' Height='80' Width='200' Content='Outer Button'/>
                            <StackPanel x:Name='innerPanel'>
                                <Button x:Name='iButton' Height='80' Width='200' Content='Inner Button'/>
                            </StackPanel>
                        </StackPanel>";

			StackPanel rootPanel = null;
			StackPanel innerPanel = null;
			Button outerButton = null;
			Button innerButton = null;
			await TestServices.RunOnUIThread(async () =>
			{
				rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
				TestServices.WindowHelper.WindowContent = rootPanel;
				await TestServices.WindowHelper.WaitForLoaded(rootPanel);

				innerPanel = (StackPanel)rootPanel.FindName("innerPanel");
				outerButton = (Button)rootPanel.FindName("oButton");
				innerButton = (Button)rootPanel.FindName("iButton");

				// Set KeyboardAcceleratorPlacementMode on inner panel, and check if it gets propagated to it's children
				// and does not affects it's siblings/ parent
				innerPanel.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
				Verify.AreEqual(
					outerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Auto);
				Verify.AreEqual(
					innerButton.KeyboardAcceleratorPlacementMode,
					KeyboardAcceleratorPlacementMode.Hidden);
			});
			await TestServices.WindowHelper.WaitForIdle();
			await FocusHelper.EnsureFocusAsync(outerButton, FocusState.Keyboard);

			TestServices.Utilities.VerifyUIElementTree();
		}
	}

	#endregion

	#region BasicInvokeBehavior
	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior.")]
	public async Task ValidateKeyboardAcceleratorEventInvoked()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A' />
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator ctrlAAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.A,
				global::Windows.System.VirtualKeyModifiers.Control,
				button,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlAAccelerator = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button, "Click"))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
			await keyboardAcceleratorInvoked.Wait();
			Log.Comment("Validating button automation action invoked.");
			await buttonClickTester.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior.")]
#if __SKIA__
	[Ignore("https://github.com/unoplatform/uno/issues/9080")]
#endif
	public async Task ValidateKeyboardAcceleratorEventNotInvokedWhenCollapsed()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <StackPanel x:Name='innerPanel' Visibility='Collapsed'>
                            <Button x:Name='button' Content='button'>
                                <Button.KeyboardAccelerators>
                                    <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A' />
                                </Button.KeyboardAccelerators>
                            </Button>
                        </StackPanel>
                        <Button x:Name='button2'> Button2 </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		StackPanel innerPanel = null;
		Button button = null;
		Button button2 = null;
		KeyboardAccelerator ctrlAAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked despite parent not being visible.");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			innerPanel = (StackPanel)rootPanel.FindName("innerPanel");
			button = (Button)rootPanel.FindName("button");
			button2 = (Button)rootPanel.FindName("button2");
			ctrlAAccelerator = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button2, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button, "Click"))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await buttonClickTester.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
		}

		await TestServices.RunOnUIThread(() =>
		{
			Log.Comment("Toggling visibility.");
			innerPanel.Visibility = Visibility.Visible;
		});
		await TestServices.WindowHelper.WaitForIdle();

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked"))
		using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button, "Click"))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
			await keyboardAcceleratorInvoked.Wait();
			await buttonClickTester.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators can work with a key outside the VirtualKey enum")]
	public async Task ValidateEqualsKeyCanBeAKeyboardAccelerator()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control'/>
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		const int equalKeyValue = 187;// Code for '=' button since it is not in the VK enum

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator ctrlEqualAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				(global::Windows.System.VirtualKey)equalKeyValue,
				global::Windows.System.VirtualKeyModifiers.Control,
				button,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlEqualAccelerator = button.KeyboardAccelerators[0];
			ctrlEqualAccelerator.Key = (global::Windows.System.VirtualKey)equalKeyValue;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlEqualAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button, "Click"))
		{
			Log.Comment("Press accelerator sequence: Ctrl + =");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_equal#$u$_ctrlscan#$u$_equal");
			await keyboardAcceleratorInvoked.Wait();
			Log.Comment("Validating button automation action invoked.");
			await buttonClickTester.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerator behavior when no modifiers are set.")]
	public async Task ValidateKeyboardAcceleratorBehaviorWithNoModifiers()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Key='F5' />
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator f5Accelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.F5,
				global::Windows.System.VirtualKeyModifiers.None,
				button,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			f5Accelerator = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(f5Accelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: F5");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_f5#$u$_f5");
			await keyboardAcceleratorInvoked.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerator behavior for Back which is non symbol access keys.")]
	public async Task ValidateKeyboardAcceleratorBehaviorWithBackspace()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Key='Back' />
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator backAccelerator = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			backAccelerator = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard); // XAML Island explicitly needs focus to be set

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Back,
				global::Windows.System.VirtualKeyModifiers.None,
				button,
				false /*handled*/);
		});

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(backAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button, "Click"))
		{
			Log.Comment("Press accelerator : Backspace");
			await TestServices.KeyboardHelper.Backspace();
			await keyboardAcceleratorInvoked.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerator behavior when multiple modifiers are required.")]
#if __SKIA__
	[Ignore("https://github.com/unoplatform/uno/issues/9080")]
#endif
	public async Task ValidateKeyboardAcceleratorBehaviorWithMultipleModifiers()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Key='S' Modifiers='Control,Shift' />
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator ctrlShiftSAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.S,
				global::Windows.System.VirtualKeyModifiers.Control | global::Windows.System.VirtualKeyModifiers.Shift,
				button,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlShiftSAccelerator = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlShiftSAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press sequence: Ctrl + S");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_s#$u$_s#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorInvoked.HasFired);


			Log.Comment("Press sequence: Shift + S");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_shift#$d$_s#$u$_s#$u$_shift");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorInvoked.HasFired);

			Log.Comment("Press sequence: Control + Shift + S");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_shift#$d$_s#$u$_s#$u$_shift#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior for Ctrl+Tab.")]
	public async Task ValidateKeyboardAcceleratorEventInvokedForCtrlTab()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Tab' />
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator ctrlTabAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Tab,
				global::Windows.System.VirtualKeyModifiers.Control,
				button,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlTabAccelerator = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		// TODO:MZ: Needed?
		// using (TestServices.KeyboardHelper.CreateKeyboardWaitKindGuard(KeyboardWaitKind.None))
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlTabAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button, "Click"))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Tab");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.CtrlTab();
			await keyboardAcceleratorInvoked.Wait();
			Log.Comment("Validating button automation action invoked.");
			await buttonClickTester.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates access keys on pivot control.")]
	[Ignore("Requires Pivot support for Keyboard Accelerators #17135")]
	public async Task VerifyAcceleratorsWorkOnPivotControl()
	{
		const string rootPanelXaml =
			@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Pivot x:Name='rootPivot' Title='PIVOT WITH ACCESS KEYS' >
                        <Pivot.RightHeader>
                            <CommandBar ClosedDisplayMode='Compact'>
                                <AppBarButton x:Name='Back' Icon='Back' Label='Previous'  />
                                <AppBarButton x:Name='Forward' Icon='Forward' Label='Next'   />
                            </CommandBar>
                        </Pivot.RightHeader>
                        <PivotItem x:Name = 'pivotItem1' Header='Pivot Item 1'>
                            <TextBlock Text='Content of pivot item 1.' />
                            <PivotItem.KeyboardAccelerators>
                                <KeyboardAccelerator Modifiers='Control' Key='Number1'/>
                            </PivotItem.KeyboardAccelerators>
                        </PivotItem>
                        <PivotItem x:Name = 'pivotItem2' Header='Pivot Item 2'>
                            <TextBlock Text='Content of pivot item 2.'/>
                            <PivotItem.KeyboardAccelerators>
                                <KeyboardAccelerator Modifiers='Control' Key='Number2'/>
                            </PivotItem.KeyboardAccelerators>
                        </PivotItem>
                        <PivotItem x:Name = 'pivotItem3' Header='Pivot Item 3'>
                            <TextBlock Text='Content of pivot item 3.'/>
                            <PivotItem.KeyboardAccelerators>
                                <KeyboardAccelerator Modifiers='Control' Key='Number3'/>
                            </PivotItem.KeyboardAccelerators>
                        </PivotItem>
                    </Pivot>
                </StackPanel>";

		StackPanel rootPanel = null;
		Pivot pivot = null;

		var selectionChangedEventHandler_1 = new Action<object, SelectionChangedEventArgs>((source, args) =>
		{
			verifySelectedPivotItemChanged(source, 0);
		});

		var selectionChangedEventHandler_2 = new Action<object, SelectionChangedEventArgs>((source, args) =>
		{
			verifySelectedPivotItemChanged(source, 1);
		});

		var selectionChangedEventHandler_3 = new Action<object, SelectionChangedEventArgs>((source, args) =>
		{
			verifySelectedPivotItemChanged(source, 2);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			pivot = (Pivot)rootPanel.FindName("rootPivot");
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(pivot, FocusState.Keyboard);
		Log.Comment("Verifying keyboard accelerators on PivotItems");

		using (var eventTester3 = new EventTester<Pivot, SelectionChangedEventArgs>(pivot, "SelectionChanged", selectionChangedEventHandler_3))
		{
			Log.Comment("Press ctrl+3");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_3#$u$_3#$u$_ctrlscan");
			await eventTester3.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}

		using (var eventTester1 = new EventTester<Pivot, SelectionChangedEventArgs>(pivot, "SelectionChanged", selectionChangedEventHandler_1))
		{
			Log.Comment("Press 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await eventTester1.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}

		using (var eventTester2 = new EventTester<Pivot, SelectionChangedEventArgs>(pivot, "SelectionChanged", selectionChangedEventHandler_2))
		{
			Log.Comment("Press 2");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await eventTester2.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	#endregion

	#region EventOrdering
	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event's ordering.")]
	public async Task ValidateOnProcessKeyboardAcceleratorsEventOrdering()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='rootPanel' Width='300' Height='300'>
                    </StackPanel>";

		StackPanel rootPanel = null;
		KeyboardAcceleratorTests.ButtonWithEventOrdering button = null;
		KeyboardAccelerator altAAccelerator = null;
		FrameworkElement[] elementList = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = new KeyboardAcceleratorTests.ButtonWithEventOrdering();
			button.Name = "button";

			rootPanel.Children.Add(button);
			altAAccelerator = new KeyboardAccelerator();
			altAAccelerator.Key = global::Windows.System.VirtualKey.A;
			altAAccelerator.Modifiers = global::Windows.System.VirtualKeyModifiers.Menu; // Menu is the modifier for the Alt key
			button.KeyboardAccelerators.Add(altAAccelerator);

			elementList = new FrameworkElement[2] { button, rootPanel };
			button.eventOrder = new StringBuilder();
			button.shouldSetHandled = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		await using (var keyboardAcceleratorOrdering = await KeyboardAcceleratorEventOrderingTester.CreateAsync(elementList, button.eventOrder))
		using (var rootPanelKeyDown = EventTester<UIElement, KeyRoutedEventArgs>.FromRoutedEvent(rootPanel, "KeyDown", (t, u) => { } /*No action required*/))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
			await button.Wait();
			await rootPanelKeyDown.Wait();
			await TestServices.WindowHelper.WaitForIdle();
			Verify.AreEqual("[buttonKeyDown:Control][rootPanelKeyDown:Control]" +
							"[buttonOnProcessKeyboardAccelerators:A:Control][buttonProcessKeyboardAccelerators:A:Control]" +
							"[buttonKeyDown:A][rootPanelProcessKeyboardAccelerators:A:Control][rootPanelKeyDown:A]", button.eventOrder.ToString());
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event's ordering when the virtual sets handled to true.")]
	public async Task ValidateOnProcessKeyboardAcceleratorsEventOrderingWhenHandlingArgs()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='rootPanel' Width='300' Height='300'>
                    </StackPanel>";

		StackPanel rootPanel = null;
		KeyboardAcceleratorTests.ButtonWithEventOrdering button = null;
		KeyboardAccelerator altAAccelerator = null;
		FrameworkElement[] elementList = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = new KeyboardAcceleratorTests.ButtonWithEventOrdering();
			button.Name = "button";

			rootPanel.Children.Add(button);
			altAAccelerator = new KeyboardAccelerator();
			altAAccelerator.Key = global::Windows.System.VirtualKey.A;
			altAAccelerator.Modifiers = global::Windows.System.VirtualKeyModifiers.Menu; // Menu is the modifier for Alt key
			button.KeyboardAccelerators.Add(altAAccelerator);

			elementList = new FrameworkElement[2] { button, rootPanel };
			button.eventOrder = new StringBuilder();
			button.shouldSetHandled = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		await using (var keyboardAcceleratorOrdering = await KeyboardAcceleratorEventOrderingTester.CreateAsync(elementList, button.eventOrder))
		using (var rootPanelKeyDown = EventTester<UIElement, KeyRoutedEventArgs>.FromRoutedEvent(rootPanel, "KeyDown", (t, u) => { } /*No action required*/))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
			await button.Wait();
			await rootPanelKeyDown.Wait();
			await TestServices.WindowHelper.WaitForIdle();

			Verify.AreEqual("[buttonKeyDown:Control][rootPanelKeyDown:Control]" +
							"[buttonOnProcessKeyboardAccelerators:A:Control:Handled]" +
							"[buttonKeyDown:A:Handled][rootPanelKeyDown:A:Handled]", button.eventOrder.ToString());
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	[TestProperty("Description", "Validates the order and priority of accelerator operations.")]
	public async Task ValidateOrderOfAcceleratorOperations()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='rootPanel'>
                        <Button x:Name='ownedButton'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A' />
                            </Button.KeyboardAccelerators>
                        </Button>
                     </StackPanel>";

		StackPanel rootPanel = null;
		KeyboardAcceleratorTests.ButtonWithEventOrdering button = null;
		KeyboardAccelerator ctrlAAccelerator = null;
		Button ownedButton = null;
		KeyboardAccelerator ownedCtrlAAccelerator = null;
		FrameworkElement[] elementList = null;
		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.A,
				global::Windows.System.VirtualKeyModifiers.Control,
				button,
				false /*handled*/);
			args.Handled = true;
		});

		var ownedKeyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.A,
				global::Windows.System.VirtualKeyModifiers.Control,
				ownedButton,
				false /*handled*/);
			args.Handled = true;
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = new KeyboardAcceleratorTests.ButtonWithEventOrdering();
			button.Name = "button";

			rootPanel.Children.Add(button);
			ctrlAAccelerator = new KeyboardAccelerator();
			ctrlAAccelerator.Key = global::Windows.System.VirtualKey.A;
			ctrlAAccelerator.Modifiers = global::Windows.System.VirtualKeyModifiers.Control;
			button.KeyboardAccelerators.Add(ctrlAAccelerator);

			ownedButton = (Button)rootPanel.FindName("ownedButton");
			ownedCtrlAAccelerator = ownedButton.KeyboardAccelerators[0];
			ownedCtrlAAccelerator.ScopeOwner = button;

			elementList = new FrameworkElement[3] { button, rootPanel, ownedButton };
			button.eventOrder = new StringBuilder();
			button.shouldSetHandled = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		await using (var keyboardAcceleratorOrdering = await KeyboardAcceleratorEventOrderingTester.CreateAsync(elementList, button.eventOrder))
		{
			Log.Comment("Try to process local accelerators defined on this element");
			using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
			using (var rootPanelKeyDown = EventTester<UIElement, KeyRoutedEventArgs>.FromRoutedEvent(rootPanel, "KeyDown", (t, u) => { } /*No action required*/))
			{
				Log.Comment("Press accelerator sequence: Ctrl + A");
				await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
				await keyboardAcceleratorInvoked.Wait();
				await button.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
				await rootPanelKeyDown.Wait();
				await TestServices.WindowHelper.WaitForIdle();
				Verify.AreEqual("[buttonKeyDown:Control][rootPanelKeyDown:Control][buttonKeyDown:A:Handled][rootPanelKeyDown:A:Handled]",
					button.eventOrder.ToString());
			}

			Log.Comment("Try to process accelerators that are 'owned' by this element. Removing accelerator from button.");
			using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked"))
			using (var ownedKeyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ownedCtrlAAccelerator, "Invoked", ownedKeyboardAcceleratorInvokedHandler))
			using (var rootPanelKeyDown = EventTester<UIElement, KeyRoutedEventArgs>.FromRoutedEvent(rootPanel, "KeyDown", (t, u) => { } /*No action required*/))
			{
				await TestServices.RunOnUIThread(() =>
				{
					button.eventOrder.Clear();
					button.KeyboardAccelerators.Remove(ctrlAAccelerator);
				});
				await TestServices.WindowHelper.WaitForIdle();

				Log.Comment("Press accelerator sequence: Ctrl + A");
				await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
				await ownedKeyboardAcceleratorInvoked.Wait();
				await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
				await button.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
				await rootPanelKeyDown.Wait();
				await TestServices.WindowHelper.WaitForIdle();
				Verify.AreEqual("[buttonKeyDown:Control][rootPanelKeyDown:Control][buttonKeyDown:A:Handled][rootPanelKeyDown:A:Handled]",
								   button.eventOrder.ToString());
			}

			using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked"))
			using (var ownedKeyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ownedCtrlAAccelerator, "Invoked", ownedKeyboardAcceleratorInvokedHandler))
			{
				Log.Comment("Give the element a chance to handle its own accelerators by calling the OnProcessKeyboardAccelerators protected virtual.");
				using (var rootPanelKeyDown = EventTester<UIElement, KeyRoutedEventArgs>.FromRoutedEvent(rootPanel, "KeyDown", (t, u) => { } /*No action required*/))
				{
					await TestServices.RunOnUIThread(() =>
					{
						button.eventOrder.Clear();
						ownedButton.KeyboardAccelerators.Remove(ownedCtrlAAccelerator);
						button.shouldSetHandled = true;
					});
					await TestServices.WindowHelper.WaitForIdle();

					Log.Comment("Press accelerator sequence: Ctrl + A");
					await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");

					await ownedKeyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
					await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
					await button.Wait();
					await rootPanelKeyDown.Wait();
					Verify.AreEqual("[buttonKeyDown:Control][rootPanelKeyDown:Control]" +
									"[buttonOnProcessKeyboardAccelerators:A:Control:Handled]" +
									"[buttonKeyDown:A:Handled][rootPanelKeyDown:A:Handled]",
									button.eventOrder.ToString());
				}

				using (var buttonProcessKeyboardAccelerators = new EventTester<Button, ProcessKeyboardAcceleratorEventArgs>(button, "ProcessKeyboardAccelerators"))
				using (var rootPanelKeyDown = EventTester<UIElement, KeyRoutedEventArgs>.FromRoutedEvent(rootPanel, "KeyDown", (t, u) => { } /*No action required*/))
				{
					Log.Comment("If the element does not handle its own accelerators in OnProcessKeyboardAccelerators protected virtual, " +
						"raise the public ProcessKeyboardAccelerators event.");
					await TestServices.RunOnUIThread(() =>
					{
						button.eventOrder.Clear();
						button.shouldSetHandled = false;
					});
					await TestServices.WindowHelper.WaitForIdle();

					Log.Comment("Press accelerator sequence: Ctrl + A");

					await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
					await ownedKeyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
					await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
					await buttonProcessKeyboardAccelerators.Wait();
					await rootPanelKeyDown.Wait();
					Verify.AreEqual("[buttonKeyDown:Control][rootPanelKeyDown:Control]" +
									"[buttonOnProcessKeyboardAccelerators:A:Control][buttonProcessKeyboardAccelerators:A:Control]" +
									"[buttonKeyDown:A][rootPanelProcessKeyboardAccelerators:A:Control][rootPanelKeyDown:A]",
									button.eventOrder.ToString());
				}
			}
		}
	}


	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event's ordering on a non-control UIElement.")]
	public async Task ValidateKeyboardAcceleratorEventOrderingOnUIElement()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='rootPanel'>
                        <StackPanel.KeyboardAccelerators>
                            <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A' />
                        </StackPanel.KeyboardAccelerators>
                        <Button x:Name='button' Content='button'></Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator ctrlAAccelerator = null;
		FrameworkElement[] elementList = null;
		StringBuilder eventOrder = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.A,
				global::Windows.System.VirtualKeyModifiers.Control,
				rootPanel,
				false /*handled*/);

			eventOrder.Append("[KeyboardAcceleratorInvoked:Control:A]");
		});

		var rootPanelProcessKeyboardAcceleratorsHandler = new Action<object, ProcessKeyboardAcceleratorEventArgs>((source, args) =>
		{
			args.Handled = true;
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlAAccelerator = rootPanel.KeyboardAccelerators[0];

			elementList = new FrameworkElement[2] { button, rootPanel };
			eventOrder = new StringBuilder();
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		await using (var keyboardAcceleratorOrdering = await KeyboardAcceleratorEventOrderingTester.CreateAsync(elementList, eventOrder))
		using (var rootPanelProcessKeyboardAccelerators = new EventTester<StackPanel, ProcessKeyboardAcceleratorEventArgs>(rootPanel, "ProcessKeyboardAccelerators", rootPanelProcessKeyboardAcceleratorsHandler))
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var rootPanelKeyUp = EventTester<UIElement, KeyRoutedEventArgs>.FromRoutedEvent(rootPanel, "KeyUp", (t, u) => { } /*No action required*/))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
			await keyboardAcceleratorInvoked.Wait();
			await rootPanelKeyUp.Wait();
			await rootPanelProcessKeyboardAccelerators.Wait();

			Verify.AreEqual(
				"[buttonKeyDown:Control][rootPanelKeyDown:Control][buttonProcessKeyboardAccelerators:A:Control][buttonKeyDown:A]" +
				"[KeyboardAcceleratorInvoked:Control:A][rootPanelProcessKeyboardAccelerators:A:Control]"
				+ "[rootPanelKeyDown:A:Handled]", eventOrder.ToString());
		}
	}
	#endregion

	#region DisabledAccelerators
	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior.")]
	public async Task ValidateDisabledKeyboardAcceleratorNeverInvoked()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A'/>
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator ctrlAAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlAAccelerator = button.KeyboardAccelerators[0];
			ctrlAAccelerator.IsEnabled = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_a#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorInvoked.HasFired);
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event is not invoked when the accelerator's parent element is disabled.")]
	[TestProperty("VelocityTestPass:OneCoreStrict", "Desktop")]
	public async Task ValidateDisabledElementKeyboardAcceleratorNeverInvoked()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button1' Content='button1' IsEnabled='false'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A'/>
                            </Button.KeyboardAccelerators>
                        </Button>
                        <Button x:Name='button2'> Button2 </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button2 = null;
		KeyboardAccelerator ctrlAAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked.");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button2 = (Button)rootPanel.FindName("button2");
			ctrlAAccelerator = ((Button)rootPanel.FindName("button1")).KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button2, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_a#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorInvoked.HasFired);
		}
	}
	#endregion

	#region AutomationActionInvokedTests
	[TestMethod]
	[TestProperty("Description", "Validates that KeyboardAccelerators event gets invoked.")]
	[TestProperty("VelocityTestPass:OneCoreStrict", "Desktop")]
	[TestProperty("Hosting:Mode", "UAP")]  // Task 19240999
	public async Task ValidateKeyboardAcceleratorsCanInvokeControlAutomationAction()
	{
		StackPanel rootPanel = null;
		MyButton button = null;
		KeyboardAccelerator altAAccelerator = null;

		await TestServices.RunOnUIThread(() =>
		{
			rootPanel = new StackPanel();
			button = new MyButton();
			rootPanel.Children.Add(button);

			altAAccelerator = new KeyboardAccelerator();
			altAAccelerator.Key = global::Windows.System.VirtualKey.A;
			altAAccelerator.Modifiers = global::Windows.System.VirtualKeyModifiers.Menu;
			button.KeyboardAccelerators.Add(altAAccelerator);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var buttonClick = new EventTester<MyButton, RoutedEventArgs>(button, "Click"))
		{
			Log.Comment("Press accelerator sequence: Alt + A");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$d$_a#$u$_a#$u$_alt");
			//Wait on the overridden action
			await button.Wait();
			await buttonClick.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates that KeyboardAccelerators event gets invoked.")]
	public async Task ValidateHandlingKeyboardAcceleratorsCanPreventAutomationAction()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A' />
                            </Button.KeyboardAccelerators>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		KeyboardAccelerator ctrlAAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.A,
				global::Windows.System.VirtualKeyModifiers.Control,
				button,
				false /*handled*/);

			Log.Comment("Setting handled to true.");
			args.Handled = true;
		});

		var clickEventHandler = new Action<object, RoutedEventArgs>((source, args) =>
		{
			Verify.Fail("Button clicked.");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlAAccelerator = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button, "Click", clickEventHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_a#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
			Log.Comment("Validating button automation action invoked.");
			await buttonClickTester.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
	#endregion

	#region GlobalScopeTests
	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior.")]
	public async Task ValidateKeyboardAcceleratorsAreGlobalByDefault()
	{
		//using (var testCleanup = new TestCleanupWrapper())
		{
			const string rootPanelXaml =
					@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <StackPanel>
                            <Button x:Name='button1' Content='Button1'>
                                <Button.KeyboardAccelerators>
                                    <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Q' />
                                </Button.KeyboardAccelerators>
                            </Button>
                        </StackPanel>
                        <StackPanel>
                            <Button x:Name='button2'> Button2 </Button>
                        </StackPanel>
                    </StackPanel>";

			StackPanel rootPanel = null;
			Button button1 = null;
			Button button2 = null;

			KeyboardAccelerator ctrlQAccelerator = null;

			var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
			{
				VerifyKeyboardAcceleratorInvokedEventArgs(
					source,
					args,
					global::Windows.System.VirtualKey.Q,
					global::Windows.System.VirtualKeyModifiers.Control,
					button1,
					false /*handled*/);
			});

			await TestServices.RunOnUIThread(async () =>
			{
				rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
				TestServices.WindowHelper.WindowContent = rootPanel;
				await TestServices.WindowHelper.WaitForLoaded(rootPanel);

				button1 = (Button)rootPanel.FindName("button1");
				button2 = (Button)rootPanel.FindName("button2");
				ctrlQAccelerator = button1.KeyboardAccelerators[0];
			});
			await TestServices.WindowHelper.WaitForIdle();

			await FocusHelper.EnsureFocusAsync(button2, FocusState.Keyboard);

			using (var buttonClickTester = new EventTester<Button, RoutedEventArgs>(button1, "Click"))
			using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
			{
				Log.Comment("Press accelerator sequence: Ctrl + Q");
				await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
				await keyboardAcceleratorInvoked.Wait();
				await buttonClickTester.Wait();

			}

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				TestServices.WindowHelper.WindowContent = null;
			});
		}
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that Button control fires the accelerators on its attached MenuFlyout.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyButtonFlyoutWithMenuFlyoutCanInvokeAcceleratorDefinedOnMenuItem()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='focusButton' Content='Click for MenuFlyout'>
                            <Button.Flyout>
                                <MenuFlyout x:Name='MyMenuFlyout'>
                                    <MenuFlyoutItem x:Name='FlyoutItem1' Text='FlyoutItem1'/>
                                    <MenuFlyoutItem x:Name='FlyoutItem2' Text='FlyoutItem2'>
                                        <MenuFlyoutItem.KeyboardAccelerators>
                                            <KeyboardAccelerator x:Name='flyoutAccelerator' Modifiers='Control' Key='Number1' />
                                        </MenuFlyoutItem.KeyboardAccelerators>
                                    </MenuFlyoutItem>
                                </MenuFlyout>
                            </Button.Flyout>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		KeyboardAccelerator ctrl1Accelerator = null;
		MenuFlyoutItem FlyoutItem1 = null;
		MenuFlyoutItem FlyoutItem2 = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Number1,
				global::Windows.System.VirtualKeyModifiers.Control,
				FlyoutItem2,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			FlyoutItem1 = (MenuFlyoutItem)rootPanel.FindName("FlyoutItem1");
			focusButton = (Button)rootPanel.FindName("focusButton");
			FlyoutItem2 = (MenuFlyoutItem)rootPanel.FindName("FlyoutItem2");
			ctrl1Accelerator = FlyoutItem2.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Accelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Verify accelerators submenuitem in menuflyout on button control.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyAcceleratorsDefinedOnSubMenuItemInMenuFlyoutOnButton()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='ButtonWithFlyout' Content='Click for MenuFlyout'>
                            <Button.Flyout>
                                <MenuFlyout>
                                    <MenuFlyoutItem x:Name='MenuItem' Text='MenuItem'>
                                        <MenuFlyoutItem.KeyboardAccelerators>
                                            <KeyboardAccelerator x:Name='MenuItem_KA' Modifiers='Control' Key='Number1' />
                                        </MenuFlyoutItem.KeyboardAccelerators>
                                    </MenuFlyoutItem>
                                    <MenuFlyoutSubItem Text='Sub-Menu'>
                                        <MenuFlyoutSubItem Text='Sub-Sub-Menu'>
                                            <MenuFlyoutItem x:Name='SubSubMenuItem' Text='SubSubMenuItem'>
                                                <MenuFlyoutItem.KeyboardAccelerators>
                                                    <KeyboardAccelerator x:Name='Sub_Sub_MenuItem_KA' Modifiers='Control' Key='Number3' />
                                                </MenuFlyoutItem.KeyboardAccelerators>
                                            </MenuFlyoutItem>
                                        </MenuFlyoutSubItem>
                                        <MenuFlyoutItem x:Name='SubMenuItem' Text='SubMenuItem'>
                                            <MenuFlyoutItem.KeyboardAccelerators>
                                                <KeyboardAccelerator x:Name='Sub_MenuItem_KA' Modifiers='Control' Key='Number2' />
                                            </MenuFlyoutItem.KeyboardAccelerators>
                                        </MenuFlyoutItem>
                                    </MenuFlyoutSubItem>
                                </MenuFlyout>
                            </Button.Flyout>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button ButtonWithFlyout = null;
		KeyboardAccelerator MenuItem_KA = null;
		KeyboardAccelerator Sub_MenuItem_KA = null;
		KeyboardAccelerator Sub_Sub_MenuItem_KA = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			ButtonWithFlyout = (Button)rootPanel.FindName("ButtonWithFlyout");
			MenuItem_KA = ((MenuFlyoutItem)rootPanel.FindName("MenuItem")).KeyboardAccelerators[0];
			Sub_MenuItem_KA = ((MenuFlyoutItem)rootPanel.FindName("Sub_MenuItem_KA")).KeyboardAccelerators[0];
			Sub_Sub_MenuItem_KA = ((MenuFlyoutItem)rootPanel.FindName("Sub_Sub_MenuItem_KA")).KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(ButtonWithFlyout, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(MenuItem_KA, "Invoked"))
		{
			Log.Comment("Press accelerator sequence for MenuItem_KA: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(Sub_MenuItem_KA, "Invoked"))
		{
			Log.Comment("Press accelerator sequence for Sub_MenuItem_KA: Ctrl + 2");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(Sub_Sub_MenuItem_KA, "Invoked"))
		{
			Log.Comment("Press accelerator sequence for Sub_Sub_MenuItem_KA: Ctrl + 3");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_3#$u$_3#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Verify accelerators submenuitem in menuflyout on menubar control.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyAcceleratorsDefinedOnSubMenuItemInMenuFlyoutOnMenuBar()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <MenuBar>
                            <MenuBarItem Title='Click for options'>
                                <MenuFlyoutItem x:Name='MenuItem' Text='MenuItem'>
                                    <MenuFlyoutItem.KeyboardAccelerators>
                                        <KeyboardAccelerator x:Name='MenuItem_KA' Modifiers='Control' Key='Number1' />
                                    </MenuFlyoutItem.KeyboardAccelerators>
                                </MenuFlyoutItem>
                                <MenuFlyoutSubItem Text='Sub-Menu'>
                                    <MenuFlyoutSubItem Text='Sub-Sub-Menu'>
                                        <MenuFlyoutItem x:Name='SubSubMenuItem' Text='SubSubMenuItem'>
                                            <MenuFlyoutItem.KeyboardAccelerators>
                                                <KeyboardAccelerator x:Name='Sub_Sub_MenuItem_KA' Modifiers='Control' Key='Number3' />
                                            </MenuFlyoutItem.KeyboardAccelerators>
                                        </MenuFlyoutItem>
                                    </MenuFlyoutSubItem>
                                    <MenuFlyoutItem x:Name='SubMenuItem' Text='SubMenuItem'>
                                        <MenuFlyoutItem.KeyboardAccelerators>
                                            <KeyboardAccelerator x:Name='Sub_MenuItem_KA' Modifiers='Control' Key='Number2' />
                                        </MenuFlyoutItem.KeyboardAccelerators>
                                    </MenuFlyoutItem>
                                </MenuFlyoutSubItem>
                            </MenuBarItem>
                        </MenuBar>
                        <Button x:Name='ButtonWithoutFlyout' Content='TestButton'/>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button ButtonWithoutFlyout = null;
		KeyboardAccelerator MenuItem_KA = null;
		KeyboardAccelerator Sub_MenuItem_KA = null;
		KeyboardAccelerator Sub_Sub_MenuItem_KA = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			ButtonWithoutFlyout = (Button)rootPanel.FindName("ButtonWithoutFlyout");
			MenuItem_KA = ((MenuFlyoutItem)rootPanel.FindName("MenuItem")).KeyboardAccelerators[0];
			Sub_MenuItem_KA = ((MenuFlyoutItem)rootPanel.FindName("Sub_MenuItem_KA")).KeyboardAccelerators[0];
			Sub_Sub_MenuItem_KA = ((MenuFlyoutItem)rootPanel.FindName("Sub_Sub_MenuItem_KA")).KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(ButtonWithoutFlyout, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(MenuItem_KA, "Invoked"))
		{
			Log.Comment("Press accelerator sequence for MenuItem_KA: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(Sub_MenuItem_KA, "Invoked"))
		{
			Log.Comment("Press accelerator sequence for Sub_MenuItem_KA: Ctrl + 2");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(Sub_Sub_MenuItem_KA, "Invoked"))
		{
			Log.Comment("Press accelerator sequence for Sub_Sub_MenuItem_KA: Ctrl + 3");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_3#$u$_3#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that accelerators on MenuBar works when menu item is collapsed.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyAcceleratorDefinedOnMenuBarMenuItems()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                           <Button x:Name='focusButton' Content='FocusedButton'/> 
                           <MenuBar>
                                <MenuBarItem x:Name='FileMenu' Title='File'>
                                    <MenuFlyoutItem x:Name='FlyoutItem1' Text='Hola'/>
                                    <MenuFlyoutItem x:Name='FlyoutItem2' Text='Delete'>
                                        <MenuFlyoutItem.KeyboardAccelerators>
                                            <KeyboardAccelerator x:Name='ctrlSAccelerator' Key='S' Modifiers='Control'/>
                                        </MenuFlyoutItem.KeyboardAccelerators>
                                    </MenuFlyoutItem>
                                </MenuBarItem>
                           </MenuBar>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		KeyboardAccelerator ctrlSAccelerator = null;
		MenuFlyoutItem FlyoutItem2 = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.S,
				global::Windows.System.VirtualKeyModifiers.Control,
				FlyoutItem2,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			FlyoutItem2 = (MenuFlyoutItem)rootPanel.FindName("FlyoutItem2");
			ctrlSAccelerator = FlyoutItem2.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();
		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlSAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + S");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_s#$u$_s#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that accelerators on MenuBar works when menu item is opened up.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyAcceleratorDefinedOnMenuBarMenuItemsWhenItsOpened()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                           <Button x:Name='focusButton' Content='FocusedButton'/> 
                           <MenuBar>
                                <MenuBarItem x:Name='fileMenu' Title='File'>
                                    <MenuFlyoutItem x:Name='flyoutItem1' Text='Hello'/>
                                    <MenuFlyoutItem x:Name='flyoutItem2' Text='Delete'>
                                        <MenuFlyoutItem.KeyboardAccelerators>
                                            <KeyboardAccelerator x:Name='ctrlSAccelerator' Key='S' Modifiers='Control'/>
                                        </MenuFlyoutItem.KeyboardAccelerators>
                                    </MenuFlyoutItem>
                                </MenuBarItem>
                           </MenuBar>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		KeyboardAccelerator ctrlSAccelerator = null;
		MenuFlyoutItem flyoutItem2 = null;
		MenuBarItem fileMenu = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.S,
				global::Windows.System.VirtualKeyModifiers.Control,
				flyoutItem2,
				false); // handled
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			flyoutItem2 = (MenuFlyoutItem)rootPanel.FindName("flyoutItem2");
			ctrlSAccelerator = flyoutItem2.KeyboardAccelerators[0];
			fileMenu = (MenuBarItem)rootPanel.FindName("fileMenu");
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(fileMenu, FocusState.Keyboard);
		Log.Comment("Press Space to open-up MenuBarItem Flyout");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlSAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + S");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_s#$u$_s#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that accelerators on MenuBar works after menu item is opened up and closed again.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyAcceleratorDefinedOnMenuBarMenuItemsWhenItsOpenedAndClosed()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                           <Button x:Name='focusButton' Content='FocusedButton'/> 
                           <MenuBar>
                                <MenuBarItem x:Name='fileMenu' Title='File'>
                                    <MenuFlyoutItem x:Name='flyoutItem1' Text='Hello'/>
                                    <MenuFlyoutItem x:Name='flyoutItem2' Text='Delete'>
                                        <MenuFlyoutItem.KeyboardAccelerators>
                                            <KeyboardAccelerator x:Name='ctrlSAccelerator' Key='S' Modifiers='Control'/>
                                        </MenuFlyoutItem.KeyboardAccelerators>
                                    </MenuFlyoutItem>
                                </MenuBarItem>
                           </MenuBar>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		KeyboardAccelerator ctrlSAccelerator = null;
		MenuFlyoutItem flyoutItem2 = null;
		MenuBarItem fileMenu = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.S,
				global::Windows.System.VirtualKeyModifiers.Control,
				flyoutItem2,
				false); // handled
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			flyoutItem2 = (MenuFlyoutItem)rootPanel.FindName("flyoutItem2");
			ctrlSAccelerator = flyoutItem2.KeyboardAccelerators[0];
			fileMenu = (MenuBarItem)rootPanel.FindName("fileMenu");
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(fileMenu, FocusState.Keyboard);
		Log.Comment("Press Spacebar to open-up File MenuBarItem Flyout");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();
		Log.Comment("Press Spacebar again to close File MenuBarItem Flyout");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlSAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + S");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_s#$u$_s#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that StandardUICommands defined on MenuBar after setting window content, works when menu item is collapsed.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyStandarUICommandsDefinedOnMenuBarMenuItemsAfterSettingWindowContent()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                           <Button x:Name='focusButton' Content='FocusedButton'/> 
                           <MenuBar>
                                <MenuBarItem x:Name='FileMenu' Title='File'>
                                    <MenuFlyoutItem x:Name='FlyoutItem1' Text='Hola'/>
                                    <MenuFlyoutItem x:Name='deleteFlyoutItem' Text='Delete'/>
                                </MenuBarItem>
                           </MenuBar>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		MenuFlyoutItem deleteFlyoutItem = null;
		StandardUICommand deleteCommand = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			deleteFlyoutItem = (MenuFlyoutItem)rootPanel.FindName("deleteFlyoutItem");
		});
		await TestServices.WindowHelper.WaitForIdle();
		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		await TestServices.RunOnUIThread(() =>
		{
			// Add StandardUICommand on menubar item.
			deleteCommand = new StandardUICommand(StandardUICommandKind.Delete);
			deleteFlyoutItem.Command = deleteCommand;
		});

		using (var standardUICommandInvoked = new EventTester<StandardUICommand, ExecuteRequestedEventArgs>(deleteCommand, "ExecuteRequested"))
		{
			Log.Comment("Press accelerator sequence: Delete");
			await TestServices.KeyboardHelper.Delete();
			await standardUICommandInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that StandardUICommands on MenuBar works when menu item closed.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyStandarUICommandsDefinedOnMenuBarMenuItems()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                           <Button x:Name='focusButton' Content='FocusedButton'/> 
                           <MenuBar>
                                <MenuBarItem x:Name='FileMenu' Title='File'>
                                    <MenuFlyoutItem x:Name='FlyoutItem1' Text='Hola'/>
                                    <MenuFlyoutItem x:Name='deleteFlyoutItem' Text='Delete'/>
                                </MenuBarItem>
                           </MenuBar>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		MenuFlyoutItem deleteFlyoutItem = null;
		StandardUICommand deleteCommand = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			deleteFlyoutItem = (MenuFlyoutItem)rootPanel.FindName("deleteFlyoutItem");

			// Add StandardUICommand on menubar item.
			deleteCommand = new StandardUICommand(StandardUICommandKind.Delete);
			deleteFlyoutItem.Command = deleteCommand;
		});
		await TestServices.WindowHelper.WaitForIdle();
		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var standardUICommandInvoked = new EventTester<StandardUICommand, ExecuteRequestedEventArgs>(deleteCommand, "ExecuteRequested"))
		{
			Log.Comment("Press accelerator sequence: Delete");
			await TestServices.KeyboardHelper.Delete();
			await standardUICommandInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that StandarUICommands on MenuBar works when menu item is opened up.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyStandarUICommandsDefinedOnMenuBarMenuItemsWhenItsOpened()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                           <Button x:Name='focusButton' Content='FocusedButton'/> 
                           <MenuBar>
                                <MenuBarItem x:Name='fileMenu' Title='File'>
                                    <MenuFlyoutItem x:Name='flyoutItem1' Text='Hello'/>
                                    <MenuFlyoutItem x:Name='deleteFlyoutItem' Text='Delete'/>
                                </MenuBarItem>
                           </MenuBar>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		MenuFlyoutItem deleteFlyoutItem = null;
		MenuBarItem fileMenu = null;
		StandardUICommand deleteCommand = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			deleteFlyoutItem = (MenuFlyoutItem)rootPanel.FindName("deleteFlyoutItem");
			fileMenu = (MenuBarItem)rootPanel.FindName("fileMenu");

			// Add StandardUICommand on menubar item.
			deleteCommand = new StandardUICommand(StandardUICommandKind.Delete);
			deleteFlyoutItem.Command = deleteCommand;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(fileMenu, FocusState.Keyboard);
		Log.Comment("Press Space to open-up MenuBarItem Flyout");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();

		using (var standardUICommandInvoked = new EventTester<StandardUICommand, ExecuteRequestedEventArgs>(deleteCommand, "ExecuteRequested"))
		{
			Log.Comment("Press accelerator sequence: Delete");
			await TestServices.KeyboardHelper.Delete();
			await standardUICommandInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that StandarUICommands on MenuBar works after menu item opened up and closed again.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17133")]
	public async Task VerifyStandarUICommandsDefinedOnMenuBarMenuItemsWhenItsOpenedAndClosed()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                           <Button x:Name='focusButton' Content='FocusedButton'/> 
                           <MenuBar>
                                <MenuBarItem x:Name='fileMenu' Title='File'>
                                    <MenuFlyoutItem x:Name='flyoutItem1' Text='Hello'/>
                                    <MenuFlyoutItem x:Name='deleteFlyoutItem' Text='Delete'/>
                                </MenuBarItem>
                           </MenuBar>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		MenuFlyoutItem deleteFlyoutItem = null;
		MenuBarItem fileMenu = null;
		StandardUICommand deleteCommand = null;

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			deleteFlyoutItem = (MenuFlyoutItem)rootPanel.FindName("deleteFlyoutItem");
			fileMenu = (MenuBarItem)rootPanel.FindName("fileMenu");

			// Add StandardUICommand on menubar item.
			deleteCommand = new StandardUICommand(StandardUICommandKind.Delete);
			deleteFlyoutItem.Command = deleteCommand;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(fileMenu, FocusState.Keyboard);
		Log.Comment("Press Spacebar to open-up File MenuBarItem Flyout");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();
		Log.Comment("Press Spacebar again to close File MenuBarItem Flyout");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();

		using (var standardUICommandInvoked = new EventTester<StandardUICommand, ExecuteRequestedEventArgs>(deleteCommand, "ExecuteRequested"))
		{
			Log.Comment("Press accelerator sequence: Delete");
			await TestServices.KeyboardHelper.Delete();
			await standardUICommandInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that Button control fires the accelerators on its attached Flyout.")]
	[Ignore("Requires Flyout support for Keyboard Accelerators #17134")]
	public async Task VerifyButtonFlyoutCanInvokeAcceleratorsDefinedOnFlyoutContent()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='focusButton1' Content='Click for Button Flyout'>
                            <Button.Flyout>
                                <Flyout x:Name='ButtonFlyout'>
                                    <StackPanel x:Name='flyoutStackPanel'>
                                       <Button x:Name='flyoutButton1' Content='flyoutButton1'>
                                            <Button.KeyboardAccelerators>
                                                <KeyboardAccelerator x:Name='flyoutAccelerator1' Modifiers='Control' Key='Number1' />
                                            </Button.KeyboardAccelerators>
                                       </Button>
                                        <Button x:Name='flyoutButton2' Content='flyoutButton2'>
                                            <Button.KeyboardAccelerators>
                                                <KeyboardAccelerator x:Name='flyoutAccelerator2' Modifiers='Control' Key='Number2' />
                                            </Button.KeyboardAccelerators>
                                       </Button>
                                    </StackPanel>
                                </Flyout>
                            </Button.Flyout>
                        </Button>
                        <Button x:Name='focusButton' Content='Focus Button' />
                    </StackPanel>";

		StackPanel rootPanel = null;

		Flyout ButtonFlyout = null;
		Button focusButton = null;
		KeyboardAccelerator ctrl1Accelerator = null;
		KeyboardAccelerator ctrl2Accelerator = null;
		Button flyoutButton1 = null;
		Button flyoutButton2 = null;

		var keyboardAcceleratorInvokedHandler1 = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked");
		});
		var keyboardAcceleratorInvokedHandler2 = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Number2,
				global::Windows.System.VirtualKeyModifiers.Control,
				flyoutButton2,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			var focusButton1 = (Button)rootPanel.FindName("focusButton1");
			ButtonFlyout = (Flyout)focusButton1.Flyout;
			flyoutButton1 = (Button)VisualTreeUtils.FindVisualChildByName((FrameworkElement)ButtonFlyout.Content, "flyoutButton1");
			flyoutButton2 = (Button)VisualTreeUtils.FindVisualChildByName((FrameworkElement)ButtonFlyout.Content, "flyoutButton2");
			ctrl1Accelerator = flyoutButton1.KeyboardAccelerators[0];
			ctrl1Accelerator.ScopeOwner = ButtonFlyout;
			ctrl2Accelerator = flyoutButton2.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked1 = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Accelerator, "Invoked", keyboardAcceleratorInvokedHandler1))
		using (var keyboardAcceleratorInvoked2 = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl2Accelerator, "Invoked", keyboardAcceleratorInvokedHandler2))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked1.WaitForNoThrow(TimeSpan.FromMilliseconds(100));

			Log.Comment("Press accelerator sequence: Ctrl + 2");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await keyboardAcceleratorInvoked2.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that Button control fires the accelerators on its attached Flyout.")]
	[Ignore("Requires MenuFlyout support for Keyboard Accelerators #17134")]
	public async Task VerifyButtonContextFlyoutWithFlyoutCanInvokeAcceleratorDefinedOnFlyoutContent()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='focusButton1' Content='Click for Button Flyout'>
                            <Button.ContextFlyout>
                                <Flyout x:Name='ButtonFlyout'>
                                    <StackPanel x:Name='flyoutStackPanel'>
                                       <Button x:Name='flyoutButton1' Content='flyoutButton1'>
                                            <Button.KeyboardAccelerators>
                                                <KeyboardAccelerator x:Name='flyoutAccelerator1' Modifiers='Control' Key='Number1' />
                                            </Button.KeyboardAccelerators>
                                       </Button>
                                        <Button x:Name='flyoutButton2' Content='flyoutButton2'>
                                            <Button.KeyboardAccelerators>
                                                <KeyboardAccelerator x:Name='flyoutAccelerator2' Modifiers='Control' Key='Number2' />
                                            </Button.KeyboardAccelerators>
                                       </Button>
                                    </StackPanel>
                                </Flyout>
                            </Button.ContextFlyout>
                        </Button>
                        <Button x:Name='focusButton' Content='Focus Button' />
                    </StackPanel>";

		StackPanel rootPanel = null;

		Flyout ButtonFlyout = null;
		Button focusButton = null;
		KeyboardAccelerator ctrl1Accelerator = null;
		KeyboardAccelerator ctrl2Accelerator = null;
		Button flyoutButton1 = null;
		Button flyoutButton2 = null;

		var keyboardAcceleratorInvokedHandler1 = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked");
		});
		var keyboardAcceleratorInvokedHandler2 = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Number2,
				global::Windows.System.VirtualKeyModifiers.Control,
				flyoutButton2,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			ButtonFlyout = (Flyout)rootPanel.FindName("ButtonFlyout");
			flyoutButton1 = (Button)rootPanel.FindName("flyoutButton1");
			flyoutButton2 = (Button)rootPanel.FindName("flyoutButton2");
			focusButton = (Button)rootPanel.FindName("focusButton");
			ctrl1Accelerator = flyoutButton1.KeyboardAccelerators[0];
			ctrl1Accelerator.ScopeOwner = ButtonFlyout;
			ctrl2Accelerator = flyoutButton2.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked1 = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Accelerator, "Invoked", keyboardAcceleratorInvokedHandler1))
		using (var keyboardAcceleratorInvoked2 = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl2Accelerator, "Invoked", keyboardAcceleratorInvokedHandler2))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked1.WaitForNoThrow(TimeSpan.FromMilliseconds(100));

			Log.Comment("Press accelerator sequence: Ctrl + 2");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await keyboardAcceleratorInvoked2.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that Flyout processing on Button control does not crash due to stackoverflow.")]
	public async Task VerifyButtonFlyoutDoesNotIntroduceStackOverflow()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='focusButton' Content='Click for Button Flyout'>
                            <Button.ContextFlyout>
                                <Flyout x:Name='ButtonFlyout'>
                                    <StackPanel x:Name='flyoutStackPanel'>
                                       <Button x:Name='flyoutButton1' Content='flyoutButton1'>
                                            <Button.KeyboardAccelerators>
                                                <KeyboardAccelerator x:Name='flyoutAccelerator1' Modifiers='Control' Key='Number1' />
                                            </Button.KeyboardAccelerators>
                                       </Button>
                                    </StackPanel>
                                </Flyout>
                            </Button.ContextFlyout>
                        </Button>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Flyout ButtonFlyout = null;
		Button focusButton = null;
		KeyboardAccelerator ctrl1Accelerator = null;
		Button flyoutButton1 = null;

		var keyboardAcceleratorInvokedHandler1 = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("focusButton");
			ButtonFlyout = (Flyout)focusButton.ContextFlyout;
			flyoutButton1 = VisualTreeUtils.FindVisualChildByType<Button>(ButtonFlyout.Content);
			ctrl1Accelerator = flyoutButton1.KeyboardAccelerators[0];
			ctrl1Accelerator.ScopeOwner = ButtonFlyout;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked1 = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Accelerator, "Invoked", keyboardAcceleratorInvokedHandler1))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked1.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that Button control fires the global accelerators on its attached MenuFlyout.")]
	[Ignore("Requires MenuFlyout and ContextFlyout support for Keyboard Accelerators #17133 and #17134")]
	public async Task VerifyButtonContextFlyoutWithMenuFlyoutCanInvokeAcceleratorDefinedOnMenuFlyout()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='focusButton1' Content='Click for MenuFlyout'>
                            <Button.ContextFlyout>
                                <MenuFlyout x:Name='MyMenuFlyout'>
                                    <MenuFlyoutItem x:Name='FlyoutItem1' Text='FlyoutItem1'/>
                                    <MenuFlyoutItem x:Name='FlyoutItem2' Text='FlyoutItem2'>
                                        <MenuFlyoutItem.KeyboardAccelerators>
                                            <KeyboardAccelerator x:Name='flyoutAccelerator' Modifiers='Control' Key='Number1' />
                                        </MenuFlyoutItem.KeyboardAccelerators>
                                    </MenuFlyoutItem>
                                </MenuFlyout>
                            </Button.ContextFlyout>
                        </Button>
                        <Button x:Name='focusButton' Content='FocusButton' />
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button focusButton = null;
		KeyboardAccelerator ctrl1Accelerator = null;
		MenuFlyoutItem FlyoutItem1 = null;
		MenuFlyoutItem FlyoutItem2 = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Number1,
				global::Windows.System.VirtualKeyModifiers.Control,
				FlyoutItem2,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			FlyoutItem1 = (MenuFlyoutItem)rootPanel.FindName("FlyoutItem1");
			focusButton = (Button)rootPanel.FindName("focusButton");
			FlyoutItem2 = (MenuFlyoutItem)rootPanel.FindName("FlyoutItem2");
			ctrl1Accelerator = FlyoutItem2.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Accelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior in a commandbar.")]
	[Ignore("Requires CommandBar support for Keyboard Accelerators #17132")]
	public async Task ValidateKeyboardAcceleratorsWorkInCommandBarSecondaryCommands()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='FocusButton'> Focus </Button>
                        <CommandBar x:Name='commandBar' OverflowButtonVisibility='Visible'>
                            <CommandBar.SecondaryCommands>
                                <AppBarButton x:Name='overflowButton' Content='overflowButton'>
                                    <AppBarButton.KeyboardAccelerators>
                                        <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Q' />
                                    </AppBarButton.KeyboardAccelerators>
                                </AppBarButton>
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button focusButton = null;
		KeyboardAccelerator ctrlQAccelerator = null;
		CommandBar commandBar = null;
		AppBarButton overflowButton = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Q,
				global::Windows.System.VirtualKeyModifiers.Control,
				overflowButton,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			focusButton = (Button)rootPanel.FindName("FocusButton");
			commandBar = (CommandBar)rootPanel.FindName("commandBar");
			overflowButton = (AppBarButton)commandBar.SecondaryCommands[0];
			ctrlQAccelerator = overflowButton.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators gets invoked in secondary commands in commandbar even after opening and closing them explicitly.")]
	[Ignore("Requires CommandBar support for Keyboard Accelerators #17132")]
	public async Task ValidateKeyboardAcceleratorsWorkInCommandBarSecondaryCommandsAfterOpeningThemExplicitly()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='FocusButton'> Focus </Button>
                        <CommandBar x:Name='commandBar' OverflowButtonVisibility='Visible'>
                            <CommandBar.SecondaryCommands>
                                <AppBarButton x:Name='secondaryButton'>
                                    <AppBarButton.KeyboardAccelerators>
                                        <KeyboardAccelerator x:Name='ctrlQAccelerator' Modifiers='Control' Key='Q' />
                                    </AppBarButton.KeyboardAccelerators>
                                </AppBarButton>
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button focusButton = null;
		KeyboardAccelerator ctrlQAccelerator = null;
		CommandBar commandBar = null;
		AppBarButton secondaryButton = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Q,
				global::Windows.System.VirtualKeyModifiers.Control,
				secondaryButton,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			commandBar = (CommandBar)rootPanel.FindName("commandBar");
			focusButton = (Button)rootPanel.FindName("FocusButton");
			secondaryButton = (AppBarButton)commandBar.SecondaryCommands[0];
			ctrlQAccelerator = secondaryButton.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		// Tab will move focus to the secondaryButton
		await TestServices.KeyboardHelper.PressKeySequence("$d$_tab#$u$_tab");
		await TestServices.WindowHelper.WaitForIdle();
		// Space will open the secondary command flyout
		await TestServices.KeyboardHelper.PressKeySequence("$d$_space#$u$_space");
		await TestServices.WindowHelper.WaitForIdle();
		// Another space will dismiss the secondary command flyout
		await TestServices.KeyboardHelper.PressKeySequence("$d$_space#$u$_space");
		await TestServices.WindowHelper.WaitForIdle();

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior in a commandbar.")]
	public async Task ValidateKeyboardAcceleratorsNotInvokedSecondaryCommandsOfACollapsedCommandBar()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='FocusButton'> Focus </Button>
                        <CommandBar x:Name='commandBar' OverflowButtonVisibility='Visible' Visibility='Collapsed'>
                            <CommandBar.SecondaryCommands>
                                <AppBarButton x:Name='overflowButton' Content='overflowButton'>
                                    <AppBarButton.KeyboardAccelerators>
                                        <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Q' />
                                    </AppBarButton.KeyboardAccelerators>
                                </AppBarButton>
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button focusButton = null;
		KeyboardAccelerator ctrlQAccelerator = null;
		CommandBar commandBar = null;
		AppBarButton overflowButton = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("CommandBar Accelerator invoked.");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			commandBar = (CommandBar)rootPanel.FindName("commandBar");
			focusButton = (Button)rootPanel.FindName("FocusButton");
			overflowButton = (AppBarButton)commandBar.SecondaryCommands[0];
			ctrlQAccelerator = overflowButton.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior in a commandbar.")]
	public async Task ValidateKeyboardAcceleratorsWorkInCommandBarPrimaryCommands()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='FocusButton'> Focus </Button>
                        <CommandBar x:Name='commandBar' OverflowButtonVisibility='Visible'>
                            <AppBarButton x:Name='overflowButton' Content='overflowButton'>
                                <AppBarButton.KeyboardAccelerators>
                                    <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Q' />
                                </AppBarButton.KeyboardAccelerators>
                            </AppBarButton>
                        </CommandBar>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button focusButton = null;
		KeyboardAccelerator ctrlQAccelerator = null;
		CommandBar commandBar = null;
		AppBarButton overflowButton = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Q,
				global::Windows.System.VirtualKeyModifiers.Control,
				overflowButton,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			commandBar = (CommandBar)rootPanel.FindName("commandBar");
			focusButton = (Button)rootPanel.FindName("FocusButton");
			overflowButton = (AppBarButton)commandBar.PrimaryCommands[0];
			ctrlQAccelerator = overflowButton.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
		}
	}
	#endregion

	#region ScopingAndBubblingTests

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior.")]
	public async Task ValidateKeyboardAcceleratorsCanBeScoped()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <StackPanel x:Name='parentPanel'>
                            <Button x:Name='button1' Content='Button1'>
                                <Button.KeyboardAccelerators>
                                    <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Q'/>
                                </Button.KeyboardAccelerators>
                            </Button>
                        </StackPanel>
                        <StackPanel>
                            <Button x:Name='button2'> Button2 </Button>
                        </StackPanel>
                    </StackPanel>";

		StackPanel rootPanel = null;
		StackPanel parentPanel = null;
		Button button2 = null;
		KeyboardAccelerator ctrlQAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button2 = (Button)rootPanel.FindName("button2");
			var button1 = (Button)rootPanel.FindName("button1");
			ctrlQAccelerator = button1.KeyboardAccelerators[0];
			parentPanel = (StackPanel)rootPanel.FindName("parentPanel");

			ctrlQAccelerator.ScopeOwner = parentPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button2, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates ProcessKeyboardAccelerators event behavior.")]
	public async Task ValidateProcessKeyboardAcceleratorsEventBehavior()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <StackPanel x:Name='parentPanel'>
                            <StackPanel.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Q'/>
                            </StackPanel.KeyboardAccelerators>
                            <Button x:Name='button1' Content='Button1'></Button>
                        </StackPanel>
                    </StackPanel>";

		StackPanel rootPanel = null;
		StackPanel parentPanel = null;
		Button button1 = null;
		KeyboardAccelerator ctrlQAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Log.Comment("Setting handled to true.");
			args.Handled = true;
		});

		var parentPanelProcessKeyboardAcceleratorsHandler = new Action<object, ProcessKeyboardAcceleratorEventArgs>((source, args) =>
		{
			Verify.Fail("Unexpected: ProcessKeyboardAccelerators raised!");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button1 = (Button)rootPanel.FindName("button1");
			parentPanel = (StackPanel)rootPanel.FindName("parentPanel");
			ctrlQAccelerator = parentPanel.KeyboardAccelerators[0];

			ctrlQAccelerator.ScopeOwner = parentPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button1, FocusState.Keyboard);

		using (var parentPanelProcessKeyboardAccelerators = new EventTester<StackPanel, ProcessKeyboardAcceleratorEventArgs>(parentPanel, "ProcessKeyboardAccelerators", parentPanelProcessKeyboardAcceleratorsHandler))
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
			await parentPanelProcessKeyboardAccelerators.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates ProcessKeyboardAccelerators event behavior for Controls.")]
	public async Task ValidateProcessKeyboardAcceleratorsEventBehaviorForControls()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <StackPanel x:Name='parentPanel'>
                            <Button x:Name='button1' Content='Button1'>
                                <Button.KeyboardAccelerators>
                                    <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='Q'/>
                                </Button.KeyboardAccelerators></Button>
                        </StackPanel>
                    </StackPanel>";

		StackPanel rootPanel = null;
		StackPanel parentPanel = null;
		Button button1 = null;
		KeyboardAccelerator ctrlQAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Log.Comment("Setting handled to true.");
			args.Handled = true;
		});

		var button1ProcessKeyboardAcceleratorsHandler = new Action<object, Microsoft.UI.Xaml.Input.ProcessKeyboardAcceleratorEventArgs>((source, args) =>
		{
			Verify.Fail("Unexpected: ProcessKeyboardAccelerators raised.");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button1 = (Button)rootPanel.FindName("button1");
			parentPanel = (StackPanel)rootPanel.FindName("parentPanel");
			ctrlQAccelerator = button1.KeyboardAccelerators[0];

			ctrlQAccelerator.ScopeOwner = parentPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button1, FocusState.Keyboard);

		using (var button1ProcessKeyboardAccelerators = new EventTester<Button, ProcessKeyboardAcceleratorEventArgs>(button1, "ProcessKeyboardAccelerators", button1ProcessKeyboardAcceleratorsHandler))
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlQAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + Q");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_q#$u$_q#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
			await button1ProcessKeyboardAccelerators.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked event behavior when two accelerators are shared and the first is disabled.")]
	public async Task VerifyParentOfDisabledControlCanBeInvoked()
	{
		await TestServices.RunOnUIThread(async () =>
		{
			// Introducing a use of FrameworkElement.set_AllowFocusWhenDisabled so that the linker doesn't remove it :/
			new Button() { AllowFocusWhenDisabled = true };
		});
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <StackPanel x:Name='parentPanel'>
                            <StackPanel.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator1' Modifiers='Control' Key='A' />
                            </StackPanel.KeyboardAccelerators>
                            <Button x:Name='button' Content='button' IsEnabled='false' AllowFocusWhenDisabled='true'>
                                <Button.KeyboardAccelerators>
                                    <KeyboardAccelerator x:Name='keyboardAccelerator2' Modifiers='Control' Key='A' />
                                </Button.KeyboardAccelerators>
                            </Button>
                        </StackPanel>
                    </StackPanel>";

		StackPanel rootPanel = null;

		Button button = null;
		StackPanel parentPanel = null;
		KeyboardAccelerator ctrlAAccelerator1 = null;
		KeyboardAccelerator ctrlAAccelerator2 = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.A,
				global::Windows.System.VirtualKeyModifiers.Control,
				parentPanel,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);
			button = (Button)rootPanel.FindName("button");
			parentPanel = (StackPanel)rootPanel.FindName("parentPanel");
			ctrlAAccelerator1 = parentPanel.KeyboardAccelerators[0];
			ctrlAAccelerator2 = button.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(button, FocusState.Keyboard);

		using (var keyboardAccelerator1Invoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator1, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var keyboardAccelerator2Invoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator2, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_a#$u$_ctrlscan");
			await keyboardAccelerator1Invoked.Wait();
			await keyboardAccelerator2Invoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			Verify.IsFalse(keyboardAccelerator2Invoked.HasFired);
		}
	}

	[TestMethod]
	[TestProperty("Description", "Validates that ListView fires the accelerators on its attached ContextFlyout.")]
	[Ignore("Requires ContextFlyout support for Keyboard Accelerators #17134")]
	public async Task VerifyListViewContextFlyoutCanInvokeHiddenAccelerator()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <ListView x:Name='listView'>
                          <ListView.ContextFlyout>
                              <MenuFlyout x:Name='flyout'>
                                <MenuFlyoutItem x:Name='action1' Text= 'Action1'>
                                    <MenuFlyoutItem.KeyboardAccelerators>
                                        <KeyboardAccelerator x:Name='flyoutAccelerator' Modifiers='Control' Key='Number1' />
                                    </MenuFlyoutItem.KeyboardAccelerators>
                                </MenuFlyoutItem>
                              </MenuFlyout>
                          </ListView.ContextFlyout>
                          <ListViewItem x:Name='lvItem'>Uno</ListViewItem>
                        </ListView>
                    </StackPanel>";

		StackPanel rootPanel = null;

		ListView listView = null;
		ListViewItem lvItem = null;
		KeyboardAccelerator ctrl1Accelerator1 = null;
		MenuFlyoutItem action1 = null;
		FlyoutBase contextFlyout = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Number1,
				global::Windows.System.VirtualKeyModifiers.Control,
				action1,
				false /*handled*/);
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);
			listView = (ListView)rootPanel.FindName("listView");
			lvItem = (ListViewItem)rootPanel.FindName("lvItem");
			contextFlyout = (FlyoutBase)rootPanel.FindName("flyout");
			action1 = (MenuFlyoutItem)rootPanel.FindName("action1");
			ctrl1Accelerator1 = action1.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(lvItem, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Accelerator1, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that TryInvokeKeyboardAccelerator searches a subtree appropriately.")]
	public async Task VerifyTryInvokeKeyboardAcceleratorBehavior()
	{
		StackPanelWithProcessKeyboardAcceleratorOverride rootPanel = null;

		StackPanel innerPanel = null;
		Button buttonWithAccelerator = null;
		Button focusedButton = null;
		KeyboardAccelerator ctrl1Accelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Keyboard Accelerator invoked!");
		});

		var innerPanelKeyDown = new Action<object, KeyRoutedEventArgs>((source, args) =>
		{
			int i = 0;
			i++;
		});

		await TestServices.RunOnUIThread(() =>
		{
			// Corresponding markup would look something like this:
			//<StackPanelWithProcessKeyboardAcceleratorOverride>
			//    <StackPanel x:Name='innerPanel'>
			//        <Button Content="buttonWithAccelerator">
			//            <Button.KeyboardAccelerators>
			//                <KeyboardAccelerator Modifiers='Control' Key='Number1' ScopeOwner = {x:Bind rootPanel}/>
			//            </Button.KeyboardAccelerators>
			//        </Button>
			//    </StackPanel>
			//    <Button> focusedButton </ Button >
			//</StackPanelWithProcessKeyboardAcceleratorOverride>

			rootPanel = new StackPanelWithProcessKeyboardAcceleratorOverride();
			focusedButton = new Button() { Content = "FocusedButton" };
			innerPanel = new StackPanel();
			buttonWithAccelerator = new Button();
			ctrl1Accelerator = new KeyboardAccelerator()
			{
				Modifiers = global::Windows.System.VirtualKeyModifiers.Control,
				Key = global::Windows.System.VirtualKey.Number1
			};

			buttonWithAccelerator.KeyboardAccelerators.Add(ctrl1Accelerator);
			innerPanel.Children.Add(buttonWithAccelerator);
			ctrl1Accelerator.ScopeOwner = rootPanel;
			rootPanel.Children.Add(innerPanel);
			rootPanel.Children.Add(focusedButton);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusedButton, FocusState.Keyboard);

		using (var innerPanelKD = new EventTester<StackPanel, KeyRoutedEventArgs>(rootPanel, "KeyDown", innerPanelKeyDown))
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Accelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 2, not Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
			await rootPanel.Wait();
			Verify.IsTrue(rootPanel.HasFired);
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that TryInvokeKeyboardAccelerator does not call locally scoped accelerators.")]
#if __SKIA__
	[Ignore("https://github.com/unoplatform/uno/issues/9080")]
#endif
	public async Task VerifyTryInvokeKeyboardAcceleratorBehaviorForLocallyScopedAccelerator()
	{
		StackPanelWithProcessKeyboardAcceleratorOverride tryInvokePanel = null;

		StackPanel rootPanel = null;
		StackPanel scopedPanel = null;
		StackPanel innerPanel = null;
		Button button1WithAccelerator = null;
		Button button2WithAccelerator = null;
		Button focusedButton = null;
		KeyboardAccelerator ctrl1Button1Accelerator = null;
		KeyboardAccelerator ctrl1Button1GlobalAccelerator = null;
		KeyboardAccelerator ctrl1Button2Accelerator = null;

		var button1KeyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Unexpected: Accelerator invoked");
		});
		var button1GlobalKeyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				 source,
				 args,
				 global::Windows.System.VirtualKey.Number1,
				 global::Windows.System.VirtualKeyModifiers.Control,
				 button1WithAccelerator,
				 false /*handled*/);
		});
		var button2KeyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Unexpected: Accelerator invoked");
		});

		await TestServices.RunOnUIThread(() =>
		{
			// Corresponding markup would look something like this:
			//<StackPanel x:Name='rootPanel'>
			//      <StackPanel x:Name='scopedPanel'>
			//          <Button Content='anotherButton' />
			//      </StackPanel>
			//      <StackPanelWithProcessKeyboardAcceleratorOverride x:Name='tryInvokePanel'>
			//          <StackPanel x:Name='innerPanel'>
			//              <Button Content='button1WithAccelerator'>
			//                  <Button.KeyboardAccelerators>
			//                  < !--As this is locally scoped, it should never get invoked.-->
			//                      <KeyboardAccelerator Modifiers='Control' Key='Number1' ScopeOwner = {x:Bind innerPanel}/>
			//                  < !--As no scope owner has been mentioned, by default it's global and can be invoked. -->
			//                      <KeyboardAccelerator Modifiers='Control' Key='Number1' />
			//                  </Button.KeyboardAccelerators>
			//              </Button>
			//              <Button Content='button2WithAccelerator'>
			//                  <Button.KeyboardAccelerators>
			//                  < !--As this is also locally scoped, it should never get invoked.-->
			//                      <KeyboardAccelerator Modifiers='Control' Key='Number1' ScopeOwner = {x:Bind scopedPanel}/>
			//                  </Button.KeyboardAccelerators>
			//              </Button>
			//          </StackPanel>
			//          <Button> focusedButton </ Button >
			//      </StackPanelWithProcessKeyboardAcceleratorOverride>
			//<StackPanel>

			rootPanel = new StackPanel();
			scopedPanel = new StackPanel();
			innerPanel = new StackPanel();
			tryInvokePanel = new StackPanelWithProcessKeyboardAcceleratorOverride();
			focusedButton = new Button() { Content = "FocusedButton" };
			button1WithAccelerator = new Button();
			button2WithAccelerator = new Button();
			ctrl1Button1Accelerator = new KeyboardAccelerator()
			{
				Modifiers = global::Windows.System.VirtualKeyModifiers.Control,
				Key = global::Windows.System.VirtualKey.Number1
			};
			ctrl1Button1GlobalAccelerator = new KeyboardAccelerator()
			{
				Modifiers = global::Windows.System.VirtualKeyModifiers.Control,
				Key = global::Windows.System.VirtualKey.Number1
			};
			ctrl1Button2Accelerator = new KeyboardAccelerator()
			{
				Modifiers = global::Windows.System.VirtualKeyModifiers.Control,
				Key = global::Windows.System.VirtualKey.Number1
			};

			button1WithAccelerator.KeyboardAccelerators.Add(ctrl1Button1Accelerator);
			button1WithAccelerator.KeyboardAccelerators.Add(ctrl1Button1GlobalAccelerator);
			button2WithAccelerator.KeyboardAccelerators.Add(ctrl1Button2Accelerator);
			rootPanel.Children.Add(scopedPanel);
			rootPanel.Children.Add(tryInvokePanel);
			innerPanel.Children.Add(button1WithAccelerator);
			innerPanel.Children.Add(button2WithAccelerator);
			ctrl1Button1Accelerator.ScopeOwner = innerPanel;
			ctrl1Button2Accelerator.ScopeOwner = scopedPanel;
			tryInvokePanel.Children.Add(innerPanel);
			tryInvokePanel.Children.Add(focusedButton);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(focusedButton, FocusState.Keyboard);

		using (var button1KeyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Button1Accelerator, "Invoked", button1KeyboardAcceleratorInvokedHandler))
		using (var button1GlobalKeyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Button1GlobalAccelerator, "Invoked", button1GlobalKeyboardAcceleratorInvokedHandler))
		using (var button2KeyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1Button2Accelerator, "Invoked", button2KeyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await button1KeyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
			await button1GlobalKeyboardAcceleratorInvoked.Wait();
			await button2KeyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates that ListView does not fire the accelerators on its attached ContextFlyout, as it is scoped to be local.")]
	[Ignore("Requires ContextFlyout support for Keyboard Accelerators #17134")]
	public async Task VerifyListViewContextFlyoutCanNotInvokeHiddenLocallyScopedAccelerator()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <ListView x:Name='listView'>
                          <ListView.ContextFlyout>
                              <MenuFlyout x:Name='flyout'>
                                <MenuFlyoutItem x:Name='action1' Text= 'Action1'>
                                    <MenuFlyoutItem.KeyboardAccelerators>
                                        <KeyboardAccelerator x:Name='flyoutAccelerator' Modifiers='Control' Key='Number1' />
                                        <KeyboardAccelerator x:Name='listViewAccelerator' Modifiers='Control' Key='Number1' />
                                    </MenuFlyoutItem.KeyboardAccelerators>
                                </MenuFlyoutItem>
                              </MenuFlyout>
                          </ListView.ContextFlyout>
                          <ListViewItem x:Name='lvItem'>Uno</ListViewItem>
                        </ListView>
                    </StackPanel>";

		StackPanel rootPanel = null;
		ListView listView = null;
		ListViewItem lvItem = null;
		KeyboardAccelerator ctrl1AcceleratorScopedtoContextFlyout = null; // As it is local, should never get called.
		KeyboardAccelerator ctrl1AcceleratorScopedtoListView = null; // As it is scoped to Listview, it can be called.
		MenuFlyoutItem action1 = null;
		FlyoutBase contextFlyout = null;

		var keyboardAcceleratorInvokedForContextFlyoutHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Unexpected: Accelerator invoked");
		});
		var keyboardAcceleratorInvokedForListViewHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.Number1,
				global::Windows.System.VirtualKeyModifiers.Control,
				action1,
				false /*handled*/);
		});
		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			listView = (ListView)rootPanel.FindName("listView");
			lvItem = (ListViewItem)rootPanel.FindName("lvItem");
			contextFlyout = (FlyoutBase)rootPanel.FindName("flyout");
			action1 = (MenuFlyoutItem)rootPanel.FindName("action1");
			ctrl1AcceleratorScopedtoContextFlyout = action1.KeyboardAccelerators[0];
			ctrl1AcceleratorScopedtoContextFlyout.ScopeOwner = contextFlyout;
			ctrl1AcceleratorScopedtoListView = action1.KeyboardAccelerators[1];
			ctrl1AcceleratorScopedtoListView.ScopeOwner = listView;

		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(lvItem, FocusState.Keyboard);

		using (var keyboardAcceleratorScopedtoContextFlyoutInvoked =
			new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1AcceleratorScopedtoContextFlyout, "Invoked", keyboardAcceleratorInvokedForContextFlyoutHandler))
		using (var keyboardAcceleratorScopedtoListViewInvoked =
			new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrl1AcceleratorScopedtoListView, "Invoked", keyboardAcceleratorInvokedForListViewHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + 1");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_1#$u$_1#$u$_ctrlscan");
			await keyboardAcceleratorScopedtoContextFlyoutInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
			await keyboardAcceleratorScopedtoListViewInvoked.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators.Invoked is not fired when elements are in the background when using a content dialog.")]
	[Ignore("Requires ContentDialog support #17131")]
	public async Task ValidateContentDialogPreventsBackgroundAcceleratorInvoke()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button x:Name='button' Content='button'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A' />
                            </Button.KeyboardAccelerators>
                        </Button>
                        <ContentDialog x:Name='dialog'>
                            <Button x:Name='dialogButton'>DialogButton</Button>
                        </ContentDialog>
                    </StackPanel>";

		StackPanel rootPanel = null;
		Button button = null;
		Button dialogButton = null;
		KeyboardAccelerator ctrlAAccelerator = null;
		ContentDialog dialog = null;
		AutoResetEvent contentDialogShown = new AutoResetEvent(false);

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Verify.Fail("Accelerator invoked");
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			button = (Button)rootPanel.FindName("button");
			ctrlAAccelerator = button.KeyboardAccelerators[0];

			dialog = (ContentDialog)rootPanel.FindName("dialog");
			dialogButton = (Button)rootPanel.FindName("dialogButton");
		});
		await TestServices.WindowHelper.WaitForIdle();

		_ = TestServices.RunOnUIThread(async () =>
		{
			await dialog.ShowAsync();
			contentDialogShown.Set();
		});

		contentDialogShown.WaitOne(TimeSpan.FromSeconds(1));
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(dialogButton, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			Log.Comment("Press accelerator sequence: Ctrl + A");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(200));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorInvoked.HasFired);
		}

		await TestServices.RunOnUIThread(() =>
		{
			dialog.Hide();
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates content dialog behavior. Content dialog should not block key inputs to its text box.")]
	[Ignore("Requires ContentDialog support #17131")]
	public async Task ValidateTextBoxInContentDialogReceivesInput()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <ContentDialog x:Name='dialog'>
                            <TextBox x:Name='textBox' >
                                <TextBox.KeyboardAccelerators>
                                    <KeyboardAccelerator x:Name='keyboardAccelerator' Key='A' />
                                </TextBox.KeyboardAccelerators>
                            </TextBox>
                        </ContentDialog>
                    </StackPanel>";

		StackPanel rootPanel = null;
		TextBox textBox = null;
		KeyboardAccelerator acceleratorA = null;
		ContentDialog dialog = null;
		AutoResetEvent contentDialogShown = new AutoResetEvent(false);

		var textBoxTextChangedHandler = new Action<object, TextChangedEventArgs>((source, args) =>
		{
			Log.Comment("TextChanged fired");
		});
		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
			   source,
			   args,
			   global::Windows.System.VirtualKey.A,
			   global::Windows.System.VirtualKeyModifiers.None,
			   textBox,
			   false /*handled*/);

			Log.Comment("Setting Handled to true.");
			args.Handled = true;
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			textBox = (TextBox)rootPanel.FindName("textBox");
			acceleratorA = textBox.KeyboardAccelerators[0];

			dialog = (ContentDialog)rootPanel.FindName("dialog");
		});
		await TestServices.WindowHelper.WaitForIdle();

		_ = TestServices.RunOnUIThread(async () =>
		{
			await dialog.ShowAsync();
			contentDialogShown.Set();
		});

		contentDialogShown.WaitOne(TimeSpan.FromSeconds(1));
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(textBox, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(acceleratorA, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var textChangedTester = new EventTester<TextBox, TextChangedEventArgs>(textBox, "TextChanged", textBoxTextChangedHandler))
		{
			await TestServices.KeyboardHelper.PressKeySequence("aTester");
			await keyboardAcceleratorInvoked.Wait();
			await textChangedTester.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}

		await TestServices.RunOnUIThread(() =>
		{
			Verify.AreEqual(textBox.Text, "Tester");
			dialog.Hide();
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators and Text Input behavior. Key input in currently focused TextBox should only be used to generate text input.")]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	public async Task ValidateTextInputAndKeyboardAccelerator()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Button Content='Click Me'>
                            <Button.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAcceleratorA' Key='A'></KeyboardAccelerator>
                                <KeyboardAccelerator x:Name='keyboardAcceleratorLeft' Key='Left'></KeyboardAccelerator>
                                <KeyboardAccelerator x:Name='keyboardAcceleratorF3' Key='F3'></KeyboardAccelerator>
                                <KeyboardAccelerator x:Name='keyboardAcceleratorCtrlS' Modifiers='Control' Key='S' />
                            </Button.KeyboardAccelerators>
                         </Button>
                         <TextBox x:Name='textBox'/>
                    </StackPanel>";

		StackPanel rootPanel = null;
		TextBox textBox = null;
		KeyboardAccelerator acceleratorA = null;
		KeyboardAccelerator acceleratorLeft = null;
		KeyboardAccelerator acceleratorF3 = null;
		KeyboardAccelerator acceleratorCtrlS = null;

		var textBoxTextChangedHandler = new Action<object, TextChangedEventArgs>((source, args) =>
		{
			Log.Comment("TextChanged fired");
		});
		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			Log.Comment("Setting Handled to true.");
			args.Handled = true;
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			textBox = (TextBox)rootPanel.FindName("textBox");
			var button = (Button)rootPanel.Children[0];
			acceleratorA = button.KeyboardAccelerators[0];
			acceleratorLeft = button.KeyboardAccelerators[1];
			acceleratorF3 = button.KeyboardAccelerators[2];
			acceleratorCtrlS = button.KeyboardAccelerators[3];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(textBox, FocusState.Keyboard);
		Log.Comment("Input a character should generate text input and not firing the accelerator event.");

		using (var keyboardAcceleratorAInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(acceleratorA, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var textChangedTester = new EventTester<TextBox, TextChangedEventArgs>(textBox, "TextChanged", textBoxTextChangedHandler))
		{
			await TestServices.KeyboardHelper.PressKeySequence("a");
			await keyboardAcceleratorAInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorAInvoked.HasFired);
			await textChangedTester.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}

		await TestServices.RunOnUIThread(() =>
		{
			Verify.AreEqual(textBox.Text, "a");
		});

		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Left will move the cursor and handled by RichEdit, it should not generate accelerator event.");
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(acceleratorLeft, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			await TestServices.KeyboardHelper.Left();
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorInvoked.HasFired);
		}

		Log.Comment("Second left key will not move the cursor, but it should still not generate accelerator event.");
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(acceleratorLeft, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			await TestServices.KeyboardHelper.Left();
			await keyboardAcceleratorInvoked.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsFalse(keyboardAcceleratorInvoked.HasFired);
		}

		Log.Comment("Press F3 key should generate accelerator event.");
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(acceleratorF3, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			await TestServices.KeyboardHelper.PressKeySequence("$d$_f3#$u$_f3");
			await keyboardAcceleratorInvoked.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}

		Log.Comment("Press Ctrl-S key should generate accelerator event.");
		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(acceleratorCtrlS, "Invoked", keyboardAcceleratorInvokedHandler))
		{
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_s#$u$_s#$u$_ctrl");
			await keyboardAcceleratorInvoked.Wait();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
	#endregion

	#region OverridingControlAccelerators
	[TestMethod]
	[TestProperty("Description", "Validates KeyboardAccelerators can override the control accelerators for TextBox.")]
#if __SKIA__
	[Ignore("https://github.com/unoplatform/uno/issues/9080")]
#endif
	public async Task VerifyKeyboardAcceleratorCanOverrideControlAccelerator()
	{
		const string rootPanelXaml =
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <TextBox x:Name='txb1' Text='Lorem Ipsum'>
                            <TextBox.KeyboardAccelerators>
                                <KeyboardAccelerator x:Name='keyboardAccelerator' Modifiers='Control' Key='A' />
                            </TextBox.KeyboardAccelerators>
                        </TextBox>
                    </StackPanel>";

		StackPanel rootPanel = null;
		TextBox txb1 = null;
		KeyboardAccelerator ctrlAAccelerator = null;

		var keyboardAcceleratorInvokedHandler = new Action<object, KeyboardAcceleratorInvokedEventArgs>((source, args) =>
		{
			VerifyKeyboardAcceleratorInvokedEventArgs(
				source,
				args,
				global::Windows.System.VirtualKey.A,
				global::Windows.System.VirtualKeyModifiers.Control,
				txb1,
				false /*handled*/);

			Log.Comment("Setting Handled to true.");
			args.Handled = true;
		});

		await TestServices.RunOnUIThread(async () =>
		{
			rootPanel = (StackPanel)XamlReader.Load(rootPanelXaml);
			TestServices.WindowHelper.WindowContent = rootPanel;
			await TestServices.WindowHelper.WaitForLoaded(rootPanel);

			txb1 = (TextBox)rootPanel.FindName("txb1");
			ctrlAAccelerator = txb1.KeyboardAccelerators[0];
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FocusHelper.EnsureFocusAsync(txb1, FocusState.Keyboard);

		using (var keyboardAcceleratorInvoked = new EventTester<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>(ctrlAAccelerator, "Invoked", keyboardAcceleratorInvokedHandler))
		using (var txb1SelectionChangedHandler = new EventTester<TextBox, RoutedEventArgs>(txb1, "SelectionChanged"))
		{
			await TestServices.RunOnUIThread(() =>
			{
				Verify.AreEqual(txb1.SelectedText, "");
			});
			Log.Comment("Press accelerator sequence: Ctrl + A");
			//Key up order should not affect accelerators
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_a#$u$_ctrlscan#$u$_a");

			await keyboardAcceleratorInvoked.Wait();
			Log.Comment("Validating SelectionChanged event was not fired");
			await txb1SelectionChangedHandler.WaitForNoThrow(TimeSpan.FromMilliseconds(100));
			await TestServices.WindowHelper.WaitForIdle();

			Verify.IsFalse(txb1SelectionChangedHandler.HasFired);
			await TestServices.RunOnUIThread(() =>
			{
				Verify.AreEqual(txb1.SelectedText, "");
			});
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
	#endregion

	[TestMethod]
	[TestProperty("Description", "Validates that all values of global::Windows.System.VirtualKey are accounted for when converting to a keyboard accelerator string representation.")]
	public async Task VerifyAllVirtualKeysAccountedForInKeyboardAcceleratorStringRepresentations()
	{
		foreach (VirtualKey vk in Enum.GetValues<VirtualKey>())
		{
			await TestServices.RunOnUIThread(() =>
			{
				MenuFlyoutItem item = new MenuFlyoutItem();
				item.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = vk, Modifiers = VirtualKeyModifiers.None });
				Log.Comment("Verifying that VirtualKey." + vk + " has a keyboard accelerator string representation.");
				Verify.IsNotNull(item.KeyboardAcceleratorTextOverride);
			});
		}
	}

	#region Helpers
	void VerifyKeyboardAcceleratorInvokedEventArgs(
		object sender,
		KeyboardAcceleratorInvokedEventArgs args,
		global::Windows.System.VirtualKey key,
		global::Windows.System.VirtualKeyModifiers modifiers,
		DependencyObject element,
		bool handled)
	{
		Log.Comment($"VerifyKeyboardAcceleratorInvokedEventArgs => Sender:{sender}, args:{args}, key:{key}, modifiers:{modifiers}, element:{element}, handled:{handled}");
		KeyboardAccelerator senderAsKA = sender as KeyboardAccelerator;
		Verify.IsTrue(senderAsKA != null);
		Verify.AreEqual(senderAsKA.Key, key);
		Verify.AreEqual(senderAsKA.Modifiers, modifiers);
		Verify.AreEqual(args.Handled, handled);
		Verify.AreEqual(args.Element, element);
	}

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
	public partial class MyButton : Button
	{
		private readonly UnoAutoResetEvent resetEvent = new UnoAutoResetEvent(false);
		public bool HasFired = false;

		protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
		{
			args.Handled = true;
			HasFired = true;
			resetEvent.Set();
		}

		public async Task Wait()
		{
			await this.resetEvent.WaitOne(TimeSpan.FromSeconds(1));
		}

		public async Task WaitForNoThrow(TimeSpan timeout)
		{
			await this.resetEvent.WaitOne(timeout);
		}
	}

	public partial class ButtonWithEventOrdering : Button
	{
		private readonly UnoAutoResetEvent resetEvent = new UnoAutoResetEvent(false);
		public StringBuilder eventOrder = null;
		public bool HasFired = false;
		public bool shouldSetHandled = false;

		protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
		{
			args.Handled = shouldSetHandled;
			HasFired = true;
			eventOrder?.Append("[" + this.Name + "OnProcessKeyboardAccelerators:" + args.Key + ":" + args.Modifiers + GetHandled(shouldSetHandled) + "]");
			resetEvent.Set();
		}

		public async Task Wait()
		{
			await this.resetEvent.WaitOne(TimeSpan.FromSeconds(1));
		}

		public async Task WaitForNoThrow(TimeSpan timeout)
		{
			await this.resetEvent.WaitOne(timeout);
		}

		private string GetHandled(bool handled)
		{
			if (handled)
			{
				return ":Handled";
			}
			return "";
		}
	}

	public partial class StackPanelWithProcessKeyboardAcceleratorOverride : StackPanel
	{
		private readonly UnoAutoResetEvent resetEvent = new UnoAutoResetEvent(false);

		public bool HasFired = false;
		protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
		{
			foreach (UIElement current in this.Children)
			{
				current.TryInvokeKeyboardAccelerator(args);
			}

			args.Handled = true;
			HasFired = true;
			resetEvent.Set();
		}

		public async Task Wait()
		{
			await this.resetEvent.WaitOne(TimeSpan.FromSeconds(1));
		}

		public async Task WaitForNoThrow(TimeSpan timeout)
		{
			await this.resetEvent.WaitOne(timeout);
		}
	}
#pragma warning restore CS0108

	public static void verifySelectedPivotItemChanged(
		object sender,
		int key)
	{
		Pivot senderAsPivot = sender as Pivot;
		Verify.IsTrue(senderAsPivot != null);
		Verify.AreEqual(senderAsPivot.SelectedIndex, key);
	}
	public static void verifyKaPlacementMode(KeyboardAcceleratorPlacementMode pMode)
	{
		Verify.AreEqual(pMode, KeyboardAcceleratorPlacementMode.Hidden);
	}

	#endregion
}
#endif
