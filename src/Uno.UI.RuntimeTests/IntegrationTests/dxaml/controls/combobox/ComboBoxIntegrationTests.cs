#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;
using ComboBoxHelper = Microsoft.UI.Xaml.Tests.Common.ComboBoxHelper;

namespace Microsoft.UI.Xaml.Tests.Enterprise.ComboBoxTests;

[TestClass]
public class ComboBoxIntegrationTests : BaseDxamlTestClass
{
	[ClassInitialize]
	public static void ClassSetup()
	{
		CommonTestSetupHelper.CommonTestClassSetup();
	}

	// TODO Uno tests: PersonObject

	[TestMethod]
	[RunsOnUIThread]
	public async Task CanInstantiate()
	{
		var act = () => new ComboBox();
		act.Should().NotThrow();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task CanEnterAndLeaveLiveTree()
	{
		var combo = new ComboBox();
		bool loaded = false;
		bool unloaded = false;
		combo.Loaded += (s, e) => loaded = true;
		combo.Unloaded += (s, e) => unloaded = true;
		TestServices.WindowHelper.WindowContent = combo;
		await TestServices.WindowHelper.WaitFor(() => loaded);

		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitFor(() => unloaded);
	}

	[TestMethod]
	public async Task CanComboBoxLoadFromXaml()
	{
		ComboBox comboBox = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				""""
				<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
				  <ComboBox x:Name='comboBox' Background='RoyalBlue' SelectedIndex='1'  Width='350' > 
				    <ComboBoxItem Content='item one' />
				    <ComboBoxItem Content='item two' />
				    <ComboBoxItem Content='item three' />
				  </ComboBox>
				</Grid>
				""""));

			comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
			VERIFY_IS_NOT_NULL(comboBox);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

		{
			LOG_OUTPUT("CanComboBoxLoadFromXaml: ComboBox Width=%f Height=%f", comboBox.ActualWidth, comboBox.ActualHeight);
			VERIFY_IS_GREATER_THAN(comboBox.ActualWidth, 0);
			VERIFY_IS_GREATER_THAN(comboBox.ActualHeight, 0);
		});

		LOG_OUTPUT("CanComboBoxLoadFromXaml: OpenComboBox().");
		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

		LOG_OUTPUT("CanComboBoxLoadFromXaml: VerifySelectedIndex().");
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

		await RunOnUIThread(() =>

		{
			comboBox.SelectedIndex = 2;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, 2);
		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	[TestMethod]
	public async Task VerfifyDefaultProperties()
	{
		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			VERIFY_IS_NOT_NULL(rootPanel);

			var comboBox = new ComboBox();
			VERIFY_IS_NOT_NULL(comboBox);

			rootPanel.Children.Add(comboBox);
			TestServices.WindowHelper.WindowContent = rootPanel;

			VERIFY_IS_FALSE(comboBox.IsDropDownOpen);
			VERIFY_IS_FALSE(comboBox.IsEditable);
			VERIFY_IS_TRUE(comboBox.IsHitTestVisible);
			VERIFY_IS_FALSE(comboBox.IsSelectionBoxHighlighted);

			VERIFY_IS_NULL(comboBox.ItemContainerStyle);
			VERIFY_IS_NOT_NULL(comboBox.Items);

			//TODO Uno: This should be null https://github.com/unoplatform/uno/issues/17982
			//VERIFY_IS_NULL(comboBox.Items[0]);
			VERIFY_IS_TRUE(comboBox.IsTabStop);
			VERIFY_IS_NULL(comboBox.ItemsSource);
			VERIFY_IS_NULL(comboBox.ItemTemplate);
			VERIFY_IS_NOT_NULL(comboBox.MaxDropDownHeight);
			VERIFY_IS_NULL(comboBox.SelectedItem);
			VERIFY_ARE_EQUAL(-1, comboBox.SelectedIndex);
			VERIFY_IS_NOT_NULL(comboBox.FontFamily);
			VERIFY_IS_NOT_NULL(comboBox.Foreground);
			VERIFY_ARE_EQUAL(comboBox.SelectionChangedTrigger, ComboBoxSelectionChangedTrigger.Committed);
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task CanExpandAndClose()
	{

		ComboBox comboBox = null;

		await RunOnUIThread(() =>

		{
			var rootPanel = new Grid();
			VERIFY_IS_NOT_NULL(rootPanel);

			comboBox = new ComboBox();
			VERIFY_IS_NOT_NULL(comboBox);
			comboBox.Width = 222;
			comboBox.Margin = ThicknessHelper.FromUniformLength(25);

			rootPanel.Children.Add(comboBox);
			TestServices.WindowHelper.WindowContent = rootPanel;

			for (var i = 0; i < 5; i++)
			{
				var item = new ComboBoxItem();
				var stringItem = "ComboBox Item ";
				stringItem += i;
				item.Content = stringItem;
				comboBox.Items.Add(item);
			}
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);
		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	[TestMethod]
	public async Task CanInsertItemAfterExpandAndClose()
	{
		ComboBox comboBox = null;
		var growingList = new List<string>();

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			VERIFY_IS_NOT_NULL(rootPanel);

			comboBox = new ComboBox();
			VERIFY_IS_NOT_NULL(comboBox);
			comboBox.Width = 222;
			comboBox.Margin = ThicknessHelper.FromUniformLength(25);

			rootPanel.Children.Add(comboBox);
			TestServices.WindowHelper.WindowContent = rootPanel;

			growingList.Add("StartingItem");
			comboBox.ItemsSource = growingList;
			comboBox.SelectedIndex = 0;

			growingList.Insert(0, "Prepend");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			comboBox.IsDropDownOpen = true;
			growingList.Insert(0, "Prepend2");
			comboBox.IsDropDownOpen = false;
			growingList.Insert(0, "Prepend3");
		});
	}

	//	[TestMethod] public async Task ValidateUIElementTree()
	//	{

	//		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

	//		// We need a special rule for ComboBox to blacklist IsSelectionBoxHighlighted in
	//		// addition to FocusState.
	//		var validationRules = new Platform.String(
	//			LR"(<?xml version='1.0' encoding='UTF-8'?>
	//				< Rules >

	//					< Rule Applicability =\"//Element[@Type='Microsoft.UI.Xaml.Controls.ComboBox']\" Inclusion='Blacklist'>
	//						< Property Name = 'FocusState' />

	//						< Property Name = 'IsSelectionBoxHighlighted' />

	//					</ Rule >

	//				</ Rules >)");


	//		ComboBox restComboBox = null;
	//		ComboBox pointerOverComboBox = null;
	//		ComboBox pressedComboBox = null;
	//		ComboBox focusedComboBox = null;
	//		ComboBox focusedPointerOverComboBox = null;
	//		ComboBox disabledComboBox = null;

	//		ComboBox openedComboBox = null;
	//		ComboBoxItem restUnselectedComboBoxItem = null;
	//		ComboBoxItem pointerOverUnselectedComboBoxItem = null;
	//		ComboBoxItem pressedUnselectedComboBoxItem = null;
	//		ComboBoxItem disabledUnselectedComboBoxItem = null;

	//		ComboBoxItem restSelectedComboBoxItem = null;
	//		ComboBoxItem pointerOverSelectedComboBoxItem = null;
	//		ComboBoxItem pressedSelectedComboBoxItem = null;
	//		ComboBoxItem focusedSelectedComboBoxItem = null;
	//		ComboBoxItem disabledSelectedComboBoxItem = null;

	//#if WI_IS_FEATURE_PRESENT(Feature_HeaderPlacement)
	//        ComboBox topHeaderComboBox = null;
	//        ComboBox leftHeaderComboBox = null;
	//#endif

	//		StackPanel rootPanel = null;

	//		await RunOnUIThread(() =>

	//		{
	//			rootPanel = new StackPanel();

	//#if WI_IS_FEATURE_PRESENT(Feature_HeaderPlacement)
	//            topHeaderComboBox = new ComboBox();
	//            topHeaderComboBox.Header = "Top Header";
	//            topHeaderComboBox.HeaderPlacement = ControlHeaderPlacement.Top;
	//            topHeaderComboBox.PlaceholderText = "Placeholder";
	//            topHeaderComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//            rootPanel.Children.Add(topHeaderComboBox);

	//            leftHeaderComboBox = new ComboBox();
	//            leftHeaderComboBox.Header = "Left Header";
	//            leftHeaderComboBox.HeaderPlacement = ControlHeaderPlacement.Left;
	//            leftHeaderComboBox.PlaceholderText = "Placeholder";
	//            leftHeaderComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//            rootPanel.Children.Add(leftHeaderComboBox);
	//#endif

	//			restComboBox = new ComboBox();
	//			restComboBox.Items.Add("Rest Combo Box");
	//			restComboBox.SelectedIndex = 0;
	//			restComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//			rootPanel.Children.Add(restComboBox);

	//			pointerOverComboBox = new ComboBox();
	//			pointerOverComboBox.Items.Add("Pointer Over Combo Box");
	//			pointerOverComboBox.SelectedIndex = 0;
	//			pointerOverComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//			rootPanel.Children.Add(pointerOverComboBox);

	//			pressedComboBox = new ComboBox();
	//			pressedComboBox.Items.Add("Pressed Combo Box");
	//			pressedComboBox.SelectedIndex = 0;
	//			pressedComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//			rootPanel.Children.Add(pressedComboBox);

	//			focusedComboBox = new ComboBox();
	//			focusedComboBox.Items.Add("Focused Combo Box");
	//			focusedComboBox.SelectedIndex = 0;
	//			focusedComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//			rootPanel.Children.Add(focusedComboBox);

	//			focusedPointerOverComboBox = new ComboBox();
	//			focusedPointerOverComboBox.Items.Add("Focused Pointer Over Combo Box");
	//			focusedPointerOverComboBox.SelectedIndex = 0;
	//			focusedPointerOverComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//			rootPanel.Children.Add(focusedPointerOverComboBox);

	//			disabledComboBox = new ComboBox();
	//			disabledComboBox.Items.Add("Disabled Combo Box");
	//			disabledComboBox.SelectedIndex = 0;
	//			disabledComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//			disabledComboBox.IsEnabled = false;
	//			rootPanel.Children.Add(disabledComboBox);

	//			openedComboBox = new ComboBox();
	//			openedComboBox.Items.Add("Opened Combo Box");
	//			openedComboBox.Margin = ThicknessHelper.FromLengths(0, 350, 0, 0);
	//			openedComboBox.SelectedIndex = 0;
	//			openedComboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
	//			rootPanel.Children.Add(openedComboBox);

	//			restUnselectedComboBoxItem = new ComboBoxItem();
	//			var restUnselectedComboBoxItemContent = new TextBlock();
	//			restUnselectedComboBoxItemContent.Text = "Rest Unselected ComboBoxItem";
	//			restUnselectedComboBoxItem.Content = restUnselectedComboBoxItemContent;
	//			openedComboBox.Items.Add(restUnselectedComboBoxItem);

	//			pointerOverUnselectedComboBoxItem = new ComboBoxItem();
	//			var pointerOverUnselectedComboBoxItemContent = new TextBlock();
	//			pointerOverUnselectedComboBoxItemContent.Text = "Pointer Over Unselected ComboBoxItem";
	//			pointerOverUnselectedComboBoxItem.Content = pointerOverUnselectedComboBoxItemContent;
	//			openedComboBox.Items.Add(pointerOverUnselectedComboBoxItem);

	//			pressedUnselectedComboBoxItem = new ComboBoxItem();
	//			var pressedUnselectedComboBoxItemContent = new TextBlock();
	//			pressedUnselectedComboBoxItemContent.Text = "Pressed Unselected ComboBoxItem";
	//			pressedUnselectedComboBoxItem.Content = pressedUnselectedComboBoxItemContent;
	//			openedComboBox.Items.Add(pressedUnselectedComboBoxItem);

	//			disabledUnselectedComboBoxItem = new ComboBoxItem();
	//			var disabledUnselectedComboBoxItemContent = new TextBlock();
	//			disabledUnselectedComboBoxItemContent.Text = "Disabled Unselected ComboBoxItem";
	//			disabledUnselectedComboBoxItem.Content = disabledUnselectedComboBoxItemContent;
	//			disabledUnselectedComboBoxItem.IsEnabled = false;
	//			openedComboBox.Items.Add(disabledUnselectedComboBoxItem);

	//			restSelectedComboBoxItem = new ComboBoxItem();
	//			var restSelectedComboBoxItemContent = new TextBlock();
	//			restSelectedComboBoxItemContent.Text = "Rest Selected ComboBoxItem";
	//			restSelectedComboBoxItem.Content = restSelectedComboBoxItemContent;
	//			restSelectedComboBoxItem.IsSelected = true;
	//			openedComboBox.Items.Add(restSelectedComboBoxItem);

	//			pointerOverSelectedComboBoxItem = new ComboBoxItem();
	//			var pointerOverSelectedComboBoxItemContent = new TextBlock();
	//			pointerOverSelectedComboBoxItemContent.Text = "Pointer Over Selected ComboBoxItem";
	//			pointerOverSelectedComboBoxItem.Content = pointerOverSelectedComboBoxItemContent;
	//			pointerOverSelectedComboBoxItem.IsSelected = true;
	//			openedComboBox.Items.Add(pointerOverSelectedComboBoxItem);

	//			pressedSelectedComboBoxItem = new ComboBoxItem();
	//			var pressedSelectedComboBoxItemContent = new TextBlock();
	//			pressedSelectedComboBoxItemContent.Text = "Pressed Selected ComboBoxItem";
	//			pressedSelectedComboBoxItem.Content = pressedSelectedComboBoxItemContent;
	//			pressedSelectedComboBoxItem.IsSelected = true;
	//			openedComboBox.Items.Add(pressedSelectedComboBoxItem);

	//			focusedSelectedComboBoxItem = new ComboBoxItem();
	//			var focusedSelectedComboBoxItemContent = new TextBlock();
	//			focusedSelectedComboBoxItemContent.Text = "Focused Selected ComboBoxItem";
	//			focusedSelectedComboBoxItem.Content = focusedSelectedComboBoxItemContent;
	//			focusedSelectedComboBoxItem.IsSelected = true;
	//			openedComboBox.Items.Add(focusedSelectedComboBoxItem);

	//			disabledSelectedComboBoxItem = new ComboBoxItem();
	//			var disabledSelectedComboBoxItemContent = new TextBlock();
	//			disabledSelectedComboBoxItemContent.Text = "Disabled Selected ComboBoxItem";
	//			disabledSelectedComboBoxItem.Content = disabledSelectedComboBoxItemContent;
	//			disabledSelectedComboBoxItem.IsSelected = true;
	//			disabledSelectedComboBoxItem.IsEnabled = false;
	//			openedComboBox.Items.Add(disabledSelectedComboBoxItem);

	//			TestServices.WindowHelper.WindowContent = rootPanel;
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();

	//		await ComboBoxHelper.OpenComboBox(openedComboBox, ComboBoxHelper.OpenMethod.Programmatic);
	//		await TestServices.WindowHelper.WaitForIdle();

	//		var setupValidationState = [&]()

	//		{
	//			await RunOnUIThread(() =>

	//			{
	//				// ComboBox states
	//				VisualStateManager.GoToState(pointerOverComboBox, "PointerOver", false);
	//				VisualStateManager.GoToState(pressedComboBox, "Pressed", false);
	//				VisualStateManager.GoToState(focusedComboBox, "Focused", false);
	//				VisualStateManager.GoToState(focusedPointerOverComboBox, "Focused", false);
	//				VisualStateManager.GoToState(focusedPointerOverComboBox, "PointerOver", false);

	//				// ComboBoxItem unselected states
	//				VisualStateManager.GoToState(pointerOverUnselectedComboBoxItem, "PointerOver", false);
	//				VisualStateManager.GoToState(pressedUnselectedComboBoxItem, "Pressed", false);

	//				// ComboBoxItem selected states
	//				VisualStateManager.GoToState(restSelectedComboBoxItem, "Selected", false);
	//				VisualStateManager.GoToState(pointerOverSelectedComboBoxItem, "SelectedPointerOver", false);
	//				VisualStateManager.GoToState(pressedSelectedComboBoxItem, "SelectedPressed", false);
	//				VisualStateManager.GoToState(focusedSelectedComboBoxItem, "Focused", false);
	//			});
	//			await TestServices.WindowHelper.WaitForIdle();
	//		};

	//		// Validate the Dark theme of controls.
	//		{
	//			setupValidationState();
	//			TestServices.Utilities.VerifyUIElementTreeWithRulesInline("Dark", validationRules);
	//		}

	//		// Validate the light theme of controls.
	//		{
	//			await RunOnUIThread(() =>

	//			{
	//				rootPanel.RequestedTheme = ElementTheme.Light;
	//				rootPanel.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
	//			});
	//			await TestServices.WindowHelper.WaitForIdle();

	//			setupValidationState();
	//			TestServices.Utilities.VerifyUIElementTreeWithRulesInline("Light", validationRules);
	//		}

	//		// Validate the high-contrast theme of controls.
	//		{
	//			await RunOnUIThread(() =>

	//			{
	//				TestServices.ThemingHelper.HighContrastTheme = HighContrastTheme.Test;
	//				rootPanel.Background = new SolidColorBrush(mu.Colors.Black);
	//			});
	//			await TestServices.WindowHelper.WaitForIdle();

	//			setupValidationState();

	//			// This method will turn on high-contrast mode before it does the validation.
	//			TestServices.Utilities.VerifyUIElementTreeWithRulesInline("HC", validationRules);

	//			LOG_OUTPUT("Validate High-Contrast Colors.");
	//			TestServices.Utilities.VerifyOutputFileHighContrastColors("HC", HighContrastTheme.Test);
	//		}
	//	}

	[TestMethod]
	public async Task ValidateVeryWideComboBoxItems()
	{
		var comboBox = await SetupBasicComboBoxTest();

		var veryLongString = "Human reason, in one sphere of its cognition, is called upon to consider questions, which it cannot decline, as they are presented by its own nature, but which it cannot answer, as they transcend every faculty of the mind.";

		await RunOnUIThread(() =>

		{
			var item = new ComboBoxItem();
			item.Content = veryLongString;
			comboBox.Items.Add(item);
			comboBox.FontSize = 30;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.CloseComboBox(comboBox);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task ValidateOpenComboBoxWithAllItemsLongWidth()
	{
		// This ComboBox items is the block shape that has all long item width instead of having one item width.
		// This is the different coverage with above ValidateVeryWideComboBoxItems() by having the all items long width.
		ComboBox comboBox = null;

		await RunOnUIThread(() =>

		{
			var rootPanel = new Grid();
			comboBox = new ComboBox();
			comboBox.VerticalAlignment = VerticalAlignment.Center;
			comboBox.Height = 10.0;
			rootPanel.Children.Add(comboBox);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("ValidateOpenComboBoxWithAllItemsLongWidth: Generate Items.");

		await RunOnUIThread(() =>

		{
			// Create the long item
			var stringItem = "ComboBox Item ";
			for (uint j = 0; j < 20.0; j++)
			{
				stringItem += j;
			}

			for (uint i = 0; i < 10.0; i++)
			{
				var item = new ComboBoxItem();
				item.Content = stringItem;
				comboBox.Items.Add(item);
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("ValidateOpenComboBoxWithAllItemsLongWidth: Open ComboBox.");
		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Touch);

		// Validate no layout crash by opening the ComboBox that has a long item width.

		LOG_OUTPUT("ValidateOpenComboBoxWithAllItemsLongWidth: Close ComboBox.");
		await ComboBoxHelper.CloseComboBox(comboBox);
		await TestServices.WindowHelper.WaitForIdle();
	}

	//public async Task CanCloseWithBackButton()
	//{


	//	var comboBox = await SetupBasicComboBoxTest();

	//	var comboBoxClosedEvent = new Event();
	//	var closedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");

	//	closedRegistration.Attach(comboBox, new wf.EventHandler<Platform.Object^> ([comboBoxClosedEvent](Platform.Object ^, Platform.Object ^)

	//	{
	//		comboBoxClosedEvent.Set();
	//	}));

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	bool backButtonPressHandled = false;
	//	LOG_OUTPUT("Close ComboBox using Back button");
	//	TestServices.Utilities.InjectBackButtonPress(&backButtonPressHandled);
	//	VERIFY_IS_TRUE(backButtonPressHandled);

	//	LOG_OUTPUT("Waiting for ComboBox to close");
	//	comboBoxClosedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("After closing a ComboBox, further back button presses should not get handled");
	//	TestServices.Utilities.InjectBackButtonPress(&backButtonPressHandled);
	//	VERIFY_IS_FALSE(backButtonPressHandled);
	//}

	[TestMethod]
	public async Task DropDownClosesOnComboBoxUnloaded()
	{
		// We need to verify that the DropDown closes when the ComboBox is removed from the visual tree.
		// If this does not happen, ComobBox is left in an invalid state and so if it gets added back
		// to the visual tree it does not behave correctly.

		var comboBox = await SetupBasicComboBoxTest();

		var comboBoxClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");

		closedRegistration.Attach(comboBox, (s, e) =>
		{
			comboBoxClosedEvent.Set();
		});

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

		{
			var rootPanel = (Panel)(comboBox.Parent);
			rootPanel.Children.Clear();
		});

		await comboBoxClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(comboBox.IsDropDownOpen);
		});
	}

	//	void EventsHandledOnGamepadB()
	//	{
	//		CloseComboBoxWithParentHandler("$d$_GamepadB#$u$_GamepadB", true /* expectedKeyDownHandledValue */, true /* verifyKeyDown */, true /* expectedKeyUpHandledValue */, true /* verifyKeyUp */);
	//	}

	//	void EventHandledOnEscape()
	//	{
	//		CloseComboBoxWithParentHandler("$d$_esc#$u$_esc", true /* expectedKeyDownHandledValue */, true /* verifyKeyDown */, true /* expectedKeyUpHandledValue */, true /* verifyKeyUp */);
	//	}

	//	void CloseComboBoxWithParentHandler(Platform.String^ keySequence, bool expectedKeyDownHandledValue, bool verifyKeyDown, bool expectedKeyUpHandledValue, bool verifyKeyUp)
	//	{


	//		UIElement ^ parent = null;
	//		var comboBox = await SetupBasicComboBoxTest();
	//		var comboBoxClosedEvent = new Event();
	//		var closedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
	//		var comboBoxParentKeyDownRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyDownEvent);
	//		var comboBoxParentKeyUpRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyUpEvent);

	//		await RunOnUIThread(() =>

	//		{
	//			parent = (UIElement ^)(VisualTreeHelper.GetParent(comboBox));
	//		});

	//		await TestServices.WindowHelper.WaitForIdle();

	//		await RunOnUIThread(() =>

	//		{
	//			if (verifyKeyDown)
	//			{
	//				comboBoxParentKeyDownRegistration.Attach(parent,
	//					new KeyEventHandler([expectedKeyDownHandledValue](Platform.Object ^ sender, KeyRoutedEventArgs ^ e)

	//				{
	//					VERIFY_ARE_EQUAL(e.Handled, expectedKeyDownHandledValue);
	//				}));
	//	}

	//			if (verifyKeyUp)
	//			{
	//				comboBoxParentKeyUpRegistration.Attach(parent,
	//					new KeyEventHandler([expectedKeyUpHandledValue](Platform.Object ^ sender, KeyRoutedEventArgs ^ e)

	//				{
	//					VERIFY_ARE_EQUAL(e.Handled, expectedKeyUpHandledValue);
	//}));
	//			}

	//			closedRegistration.Attach(comboBox,
	//				new wf.EventHandler<Platform.Object^> ([comboBoxClosedEvent](Platform.Object ^, Platform.Object ^)

	//			{
	//	comboBoxClosedEvent.Set();
	//}));
	//		});

	//await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
	//await TestServices.WindowHelper.WaitForIdle();

	//await RunOnUIThread(() =>

	//{
	//	comboBox.Focus(FocusState.Keyboard);
	//});

	//await TestServices.WindowHelper.WaitForIdle();

	//LOG_OUTPUT("ComboBox: Injecting key sequence");
	//LOG_OUTPUT("%s", keySequence.Data());

	//await TestServices.KeyboardHelper.PressKeySequence(keySequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//comboBoxClosedEvent.WaitFor(std.chrono.milliseconds(8000)); //TFS #3636501 is tracking an issue where input injection is a taking too long

	//await RunOnUIThread(() =>

	//{
	//if (verifyKeyDown)
	//{
	//	comboBoxParentKeyDownRegistration.Detach();
	//	comboBoxParentKeyDownRegistration.Attach(parent,
	//				new KeyEventHandler([](Platform.Object ^ sender, KeyRoutedEventArgs ^ e)


	//		{
	//					VERIFY_ARE_EQUAL(e.Handled, false);
	//		}));
	//			}

	//			if (verifyKeyUp)
	//{
	//	comboBoxParentKeyUpRegistration.Detach();
	//	comboBoxParentKeyUpRegistration.Attach(parent,
	//		new KeyEventHandler([](Platform.Object ^ sender, KeyRoutedEventArgs ^ e)


	//	{
	//					VERIFY_ARE_EQUAL(e.Handled, false);
	//	}));
	//}
	//		});

	//LOG_OUTPUT("ComboBox: Injecting key sequence again to make sure no events are handled with the drop-down closed.");
	//LOG_OUTPUT("%s", keySequence.Data());

	//await TestServices.KeyboardHelper.PressKeySequence(keySequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//	}

	//	void EventsContinuallyHandledOnPressAndHoldGamepadB()
	//{
	//	EventsContinuallyHandledOnPressAndHold("$d$_GamepadB", "$u$_GamepadB");
	//}

	//void EventsContinuallyHandledOnPressAndHold(Platform.String^ keyDownSequence, Platform.String^ keyUpSequence)
	//{


	//	UIElement ^ parent = null;
	//	var comboBox = await SetupBasicComboBoxTest();
	//	var comboBoxClosedEvent = new Event();
	//	var comboBoxKeyDownRepeatedEvent = new Event();
	//	var closedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
	//	var comboBoxParentKeyDownRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyDownEvent);
	//	var comboBoxParentKeyUpRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyUpEvent);

	//	int keyDownCount = 0;

	//	await RunOnUIThread(() =>

	//	{
	//		parent = (UIElement ^)(VisualTreeHelper.GetParent(comboBox));
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		comboBoxParentKeyDownRegistration.Attach(parent,
	//			new KeyEventHandler([&keyDownCount, comboBoxKeyDownRepeatedEvent](Platform.Object ^ sender, KeyRoutedEventArgs ^ e)


	//		{
	//				VERIFY_ARE_EQUAL(e.Handled, true);

	//				keyDownCount++;



	//			if (keyDownCount >= 2)
	//		{
	//			comboBoxKeyDownRepeatedEvent.Set();
	//		}
	//	}));

	//	comboBoxParentKeyUpRegistration.Attach(parent,
	//		new KeyEventHandler([](Platform.Object ^ sender, KeyRoutedEventArgs ^ e)


	//	{
	//				VERIFY_ARE_EQUAL(e.Handled, true);
	//	}));

	//	closedRegistration.Attach(comboBox,
	//		new wf.EventHandler<Platform.Object^> ([comboBoxClosedEvent](Platform.Object ^, Platform.Object ^)

	//			{
	//		comboBoxClosedEvent.Set();
	//	}));
	//});

	//await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
	//await TestServices.WindowHelper.WaitForIdle();

	//await RunOnUIThread(() =>

	//		{
	//	comboBox.Focus(FocusState.Keyboard);
	//});

	//await TestServices.WindowHelper.WaitForIdle();

	//LOG_OUTPUT("Injecting key-down sequence.");
	//LOG_OUTPUT("%s", keyDownSequence.Data());

	//await TestServices.KeyboardHelper.PressKeySequence(keyDownSequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//comboBoxClosedEvent.WaitFor(std.chrono.milliseconds(8000)); //TFS #3636501 is tracking an issue where input injection is a taking too long

	//LOG_OUTPUT("Injecting key-down sequence again to simulate the situation where the user is holding down the key.");
	//LOG_OUTPUT("%s", keyDownSequence.Data());

	//await TestServices.KeyboardHelper.PressKeySequence(keyDownSequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//comboBoxKeyDownRepeatedEvent.WaitFor(std.chrono.milliseconds(8000));

	//LOG_OUTPUT("Injecting key-up sequence.");
	//LOG_OUTPUT("%s", keyUpSequence.Data());

	//await TestServices.KeyboardHelper.PressKeySequence(keyUpSequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//    }

	//[TestMethod] public async Task ValidateResettingSelectedIndexBringsBackPlaceholderText()
	//{


	//	var comboBox = await SetupBasicComboBoxTest();
	//	TextBlock ^ placeholderTextBlock = null;

	//	await RunOnUIThread(() =>

	//		{
	//			placeholderTextBlock = TextBlock ^> (TreeHelper.GetVisualChildByName(comboBox, "PlaceholderTextBlock"));
	//		});

	//	LOG_OUTPUT("Open the ComboBox's drop down and set the selected index to 3, then close it.");
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.SelectedIndex = 3;
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//		{
	//			LOG_OUTPUT("Since there's a selected item, the placeholder TextBlock should be transparent.");
	//			VERIFY_ARE_EQUAL(0, placeholderTextBlock.Opacity);
	//		});

	//	await RunOnUIThread(() =>

	//		{
	//			LOG_OUTPUT("Now set the selected index to -1.");
	//			comboBox.SelectedIndex = -1;
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//		{
	//			LOG_OUTPUT("Since there no longer is a selected item, the placeholder TextBlock should be visible.");
	//			VERIFY_ARE_EQUAL(1, placeholderTextBlock.Opacity);

	//			LOG_OUTPUT("Now set the selected index to 3.");
	//			comboBox.SelectedIndex = 3;
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//		{
	//			LOG_OUTPUT("Since there is now a selected item again, the placeholder TextBlock should again be transparent.");
	//			VERIFY_ARE_EQUAL(0, placeholderTextBlock.Opacity);
	//		});
	//}

	//public async Task CanSmoothlyScrollOpenComboBoxWithVariableWidthItems()
	//{


	//	ComboBox comboBox;
	//	ScrollViewer scrollViewer;
	//	var verticalSnapPointsChangedRegistration = CreateSafeEventRegistration(ItemsPresenter, VerticalSnapPointsChanged);

	//	await RunOnUIThread(() =>

	//		{
	//		var rootPanel = new Grid();
	//		comboBox = new ComboBox();
	//		comboBox.HorizontalAlignment = HorizontalAlignment.Center;
	//		comboBox.VerticalAlignment = VerticalAlignment.Center;

	//		rootPanel.Children.Add(comboBox);
	//		TestServices.WindowHelper.WindowContent = rootPanel;

	//		std.mt19937 gen(2);
	//	std.uniform_real_distribution<> distr(0, 200.0;

	//	for (int i = 0; i < 10.0; ++i)
	//	{
	//		var item = new ComboBoxItem();
	//		var stringItem = "ComboBox Item " + i;

	//		// Variable width items are important for this bug to repro.
	//		for (int j = 0; j < distr(gen); ++j)
	//		{
	//			stringItem += "_";
	//		}

	//		item.Content = stringItem;
	//		comboBox.Items.Add(item);
	//		comboBox.SelectedIndex = 0;
	//	}
	//});

	//await TestServices.WindowHelper.WaitForIdle();
	//TestServices.InputHelper.Tap(comboBox);
	//await TestServices.WindowHelper.WaitForIdle();

	//await RunOnUIThread(() =>

	//	{
	//	var popup = TreeHelper.GetVisualChildByType<Popup>(comboBox);
	//	var popupChild = FrameworkElement> (popup.Child);
	//	scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>(popupChild);

	//	verticalSnapPointsChangedRegistration.Attach(
	//		TreeHelper.GetVisualChildByType<ItemsPresenter>(scrollViewer),
	//		new wf.EventHandler<Platform.Object^> ([](Platform.Object ^, Platform.Object ^)

	//			{
	//		VERIFY_FAIL();
	//	}));
	//	});

	//for (int i = 0; i < 10; ++i)
	//{
	//	TestServices.InputHelper.Flick(scrollViewer, FlickDirection.North);
	//}

	//await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	private async Task<ComboBox> SetupBasicComboBoxTest(int numberOfItems = 5, bool adjustMargin = true, bool isEditable = false)
	{
		ComboBox comboBox = null;

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<ComboBox, RoutedEventHandler>("Loaded");

		await RunOnUIThread(() =>
		{
			comboBox = new ComboBox();
			comboBox.Width = 222;
			comboBox.Margin = ThicknessHelper.FromUniformLength(25);

			loadedRegistration.Attach(comboBox, (s, e) =>

				{
					LOG_OUTPUT("ComboBox.Loaded event raised.");
					loadedEvent.Set();
				});

			var rootPanel = new Grid();
			if (adjustMargin)
			{
				rootPanel.Margin = ThicknessHelper.FromLengths(0, 25, 0, 0);
			}
			rootPanel.Children.Add(comboBox);
			TestServices.WindowHelper.WindowContent = rootPanel;

			for (uint i = 0; i < numberOfItems; i++)
			{
				var item = new ComboBoxItem();
				var stringItem = "ComboBox Item ";
				stringItem += i;
				item.Content = stringItem;
				comboBox.Items.Add(item);
			}

			if (isEditable)
			{
				comboBox.IsEditable = true;
			}
		});

		LOG_OUTPUT("Waiting for ComboBox.Loaded event...");
		await loadedEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();

		return comboBox;
	}

	// For these purposes, an "Ascending combobox" is a box that has data in a triangular pattern.
	// This forces us to resize the combobox to get bigger every time we navigate down since we realize wider items.
	// It resembles the following:
	// X
	// XX
	// XXX
	// XXXX
	private async Task<ComboBox> SetupAscendingComboBoxTest(uint numberOfItems = 50)
	{
		ComboBox comboBox = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			VERIFY_IS_NOT_NULL(rootPanel);

			comboBox = new ComboBox();
			VERIFY_IS_NOT_NULL(comboBox);

			rootPanel.Children.Add(comboBox);
			TestServices.WindowHelper.WindowContent = rootPanel;

			for (uint i = 0; i < numberOfItems; i++)
			{
				var item = new ComboBoxItem();

				var stringItem = "";
				for (uint j = 1; j <= i + 1; j++)
				{
					stringItem += "X";
				}

				item.Content = stringItem;
				comboBox.Items.Add(item);
			}
		});

		await TestServices.WindowHelper.WaitForIdle();

		return comboBox;
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task CanOpenWithTouch()
	{
		var comboBox = await SetupBasicComboBoxTest(20 /* itemSize */);

		await RunOnUIThread(() =>
		{
			comboBox.Height = 50;
			comboBox.SelectedIndex = 1;
		});

		await TestServices.WindowHelper.WaitForIdle();

		var comboBoxOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownOpened");

		openedRegistration.Attach(comboBox, (s, e) =>
		{
			LOG_OUTPUT("CanOpenWithTouch: Fire the ComboBox Opened event!");
			comboBoxOpenedEvent.Set();
		});

		LOG_OUTPUT("CanOpenWithTouch: Open the ComboBox with touch input.");
		TestServices.InputHelper.Tap(comboBox);

		await TestServices.WindowHelper.WaitForIdle();
		await comboBoxOpenedEvent.WaitForDefault();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);
		await ComboBoxHelper.CloseComboBox(comboBox);

		await RunOnUIThread(() =>

			{
				comboBox.SelectedIndex = 19;
			});

		LOG_OUTPUT("CanOpenWithTouch: Open the ComboBox with touch input.");
		TestServices.InputHelper.Tap(comboBox);

		await TestServices.WindowHelper.WaitForIdle();
		await comboBoxOpenedEvent.WaitForDefault();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 19);
		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	[TestMethod]
	[Ignore("The first verify fails as ComboBox pre-selects index 0 due to SelectionChangedTrigger.Always. This is likely a new behavior! #17988")]
	public async Task ValidateKeyboardInteraction()
	{
		var comboBox = await SetupBasicComboBoxTest();

		var dropDownOpenedEvent = new Event();
		var dropDownOpenedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownOpened");
		dropDownOpenedRegistration.Attach(comboBox, (s, e) =>
		{
			dropDownOpenedEvent.Set();
		});

		var dropDownClosedEvent = new Event();
		var dropDownClosedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
		dropDownClosedRegistration.Attach(comboBox, (s, e) =>
		{
			dropDownClosedEvent.Set();
		});

		await RunOnUIThread(() =>
		{
			comboBox.SelectionChangedTrigger = ComboBoxSelectionChangedTrigger.Always;
			comboBox.Focus(FocusState.Programmatic);
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the ComboBox with the Space key");
		await TestServices.KeyboardHelper.PressKeySequence(" ");
		await TestServices.WindowHelper.WaitForIdle();
		await dropDownOpenedEvent.WaitForDefault();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Select the first item with the Down key");
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

		LOG_OUTPUT("Change selection with the Down key");
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

		// We verify ComboBoxItem.GotFocus and ComboBoxItem.LostFocus on the 2nd ComboBoxItem.
		ComboBoxItem comboBoxItem2 = null;
		await RunOnUIThread(() =>
		{
			comboBoxItem2 = (ComboBoxItem)(comboBox.ContainerFromIndex(2));
		});

		var comboBoxItem2gotFocusEvent = new Event();
		var comboBoxItem2gotFocusRegistration = CreateSafeEventRegistration<ComboBoxItem, RoutedEventHandler>("GotFocus");
		comboBoxItem2gotFocusRegistration.Attach(comboBoxItem2, (s, e) =>
		{
			comboBoxItem2gotFocusEvent.Set();
		});

		var comboBoxItem2lostFocusEvent = new Event();
		var comboBoxItem2lostFocusRegistration = CreateSafeEventRegistration<ComboBoxItem, RoutedEventHandler>("LostFocus");
		comboBoxItem2lostFocusRegistration.Attach(comboBoxItem2, (s, e) =>
		{
			comboBoxItem2lostFocusEvent.Set();
		});

		LOG_OUTPUT("Change selection with the Down key");
		await TestServices.KeyboardHelper.Down();
		await comboBoxItem2gotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 2);

		LOG_OUTPUT("Change selection with the Up key");
		await TestServices.KeyboardHelper.Up();
		await comboBoxItem2lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

		LOG_OUTPUT("Accept selection with the Space key");
		await TestServices.KeyboardHelper.PressKeySequence(" ");
		await TestServices.WindowHelper.WaitForIdle();
		await dropDownClosedEvent.WaitForDefault();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);
	}

	[TestMethod]
	public async Task CanCloseComboBoxWithAltDown()
	{
		await CanCloseComboBoxWithKeySequence("$d$_alt#$d$_down#$u$_down#$u$_alt");
	}

	[TestMethod]
	public async Task CanCloseComboBoxWithF4()
	{
		await CanCloseComboBoxWithKeySequence("$d$_f4#$u$_f4");
	}

	private async Task CanCloseComboBoxWithKeySequence(string keySequence)
	{
		var comboBox = await SetupBasicComboBoxTest();

		var dropDownClosedEvent = new Event();
		var dropDownClosedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
		dropDownClosedRegistration.Attach(comboBox, (s, e) => dropDownClosedEvent.Set());

		await RunOnUIThread(() =>
		{
			comboBox.Focus(FocusState.Programmatic);
			comboBox.IsDropDownOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Close ComboBox using specified key sequence");
		await TestServices.KeyboardHelper.PressKeySequence(keySequence);
		await TestServices.WindowHelper.WaitForIdle();
		await dropDownClosedEvent.WaitForDefault();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);
	}


	[TestMethod]
	[Ignore("ScrollMouseWheel is not supported yet #17988")]
	public async Task CanCycleThroughItemsWithMouseWheel()
	{
		var comboBox = await SetupBasicComboBoxTest();

		await RunOnUIThread(() =>
		{
			comboBox.Focus(FocusState.Programmatic);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		TestServices.InputHelper.MoveMouse(comboBox);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Select the first item with the mouse wheel");
		TestServices.InputHelper.ScrollMouseWheel(comboBox, -1);
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

		LOG_OUTPUT("Select the second item with the mouse wheel");
		TestServices.InputHelper.ScrollMouseWheel(comboBox, -1);
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

		LOG_OUTPUT("Select the first item again by scrolling the mouse wheel back the other way");
		TestServices.InputHelper.ScrollMouseWheel(comboBox, 1);
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task CanLightDismissDropdown()
	{
		ComboBox comboBox = null;
		Button button = null;

		await RunOnUIThread(() =>

		{
			comboBox = new ComboBox();

			for (int i = 0; i < 5; ++i)
			{
				var item = new ComboBoxItem();
				item.Content = "Item";
				comboBox.Items.Add(item);
			}

			button = new Button();
			button.Width = 222;
			// Add a top margin to push the button out from under the status bar on phone.
			button.Margin = ThicknessHelper.FromLengths(0, 32, 0, 0);

			var root = new StackPanel();
			root.Children.Add(button);
			root.Children.Add(comboBox);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		var dropDownClosedEvent = new Event();
		var dropDownClosedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
		dropDownClosedRegistration.Attach(comboBox, (s, e) => { dropDownClosedEvent.Set(); });

		await RunOnUIThread(() =>

		{
			comboBox.Focus(FocusState.Programmatic);
			comboBox.IsDropDownOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Click the button and see if the drop down light dismisses
		TestServices.InputHelper.Tap(button);
		await TestServices.WindowHelper.WaitForIdle();
		await dropDownClosedEvent.WaitForDefault();
	}

	[TestMethod]
#if !UNO_HAS_ENHANCED_LIFECYCLE
	[Ignore("Due to lifecycle differences, the selection gets updated before the index is reset. Probably fixed by #18261. #17988")]
#endif
	public async Task ValidateResetItemsSource()
	{
		var comboBox = await SetupBasicComboBoxTest();

		// Reset the ComboBox ItemsSource with new items
		await RunOnUIThread(() =>
		{
			comboBox.SelectedIndex = 0;

			var itemList = new List<string>();

			for (int i = 0; i < 5; i++)
			{
				string itemText = "Item " + i;
				itemList.Add(itemText);
			}
			comboBox.ItemsSource = itemList;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("ValidateResetItemsSource: Open and Close ComboBox.");

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.CloseComboBox(comboBox);
		await TestServices.WindowHelper.WaitForIdle();

		// The selected index must be -1 by reset ItemsSource
		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);
	}

	//	[TestMethod]
	//	public async Task ValidateDropdownPlacement()
	//	{
	//		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

	//		// This test validates that ComboBox can open its Popup outside of the bounds of the window.
	//		// The test relies on the DoValidateDropdownPlacement helper function to do the validation, passing in various combinations of params to test.
	//		//
	//		// All combinations of values for the parameter are valid test cases, but we do not want to execute the full set of all possible combinations (2^5=32 test cases).
	//		// Instead we:
	//		//   1. Explicitly test the specific scenarios that we are particularly interested in
	//		//   2. Ensure pairwise coverage in the parameters by adding a set of test cases with parameters generated by PICT

	//		// If a ComboBox is near the Right edge of the window and the ComboBoxItems are wider than the ComboBox, the popup position should be allowed to go outside the window:
	//		DoValidateDropdownPlacement(VerticalAlignment.Top, HorizontalAlignment.Right, true  /*useWideItems*/, 0   /*rotationAngle*/, FlowDirection.LeftToRight, PopupHelper.AreWindowedPopupsEnabled() /* expectOutside */);

	//		// If a ComboBox is near the Bottom of the window, the popup position should be allowed to go outside the window:
	//		DoValidateDropdownPlacement(VerticalAlignment.Bottom, HorizontalAlignment.Right, false /*useWideItems*/, 0   /*rotationAngle*/, FlowDirection.LeftToRight, PopupHelper.AreWindowedPopupsEnabled() /* expectOutside */);

	//		// ComboBox popup placement should correctly handle RightToLeft flow direction:
	//		DoValidateDropdownPlacement(VerticalAlignment.Top, HorizontalAlignment.Left, false /*useWideItems*/, 0   /*rotationAngle*/, FlowDirection.RightToLeft, PopupHelper.AreWindowedPopupsEnabled() /* expectOutside */);

	//		// Validate cases where a parent of the ComboBox has a RotateTransform of 90, 180 or 270 degrees.
	//		// Note: ComboBox does not handle rotation angles other than multiples of 90, so we only test these supported values.
	//		DoValidateDropdownPlacement(VerticalAlignment.Bottom, HorizontalAlignment.Left, false /*useWideItems*/, 90  /*rotationAngle*/, FlowDirection.LeftToRight, false /* expectOutside */);
	//		DoValidateDropdownPlacement(VerticalAlignment.Bottom, HorizontalAlignment.Left, false /*useWideItems*/, 180 /*rotationAngle*/, FlowDirection.LeftToRight, false /* expectOutside */);
	//		DoValidateDropdownPlacement(VerticalAlignment.Top, HorizontalAlignment.Left, false /*useWideItems*/, 270 /*rotationAngle*/, FlowDirection.LeftToRight, false /* expectOutside */);

	//		// Extra scenarios from PICT:
	//		DoValidateDropdownPlacement(VerticalAlignment.Bottom, HorizontalAlignment.Right, true  /*useWideItems*/, 180 /*rotationAngle*/, FlowDirection.RightToLeft, false /* expectOutside */);
	//		DoValidateDropdownPlacement(VerticalAlignment.Top, HorizontalAlignment.Left, true  /*useWideItems*/, 180 /*rotationAngle*/, FlowDirection.LeftToRight, false /* expectOutside */);
	//		DoValidateDropdownPlacement(VerticalAlignment.Bottom, HorizontalAlignment.Right, true  /*useWideItems*/, 270 /*rotationAngle*/, FlowDirection.RightToLeft, false /* expectOutside */);
	//		DoValidateDropdownPlacement(VerticalAlignment.Top, HorizontalAlignment.Right, true  /*useWideItems*/, 90  /*rotationAngle*/, FlowDirection.RightToLeft, false /* expectOutside */);
	//	}

	//	private void DoValidateDropdownPlacement(VerticalAlignment verticalAlignment, HorizontalAlignment horizontalAlignment, bool useWideItems, int rotationAngle, FlowDirection flowDirection, bool expectOutside)
	//	{
	//		LOG_OUTPUT("DoValidateDropdownPlacement: VerticalAlignment=%s, HorizontalAlignment=%s, useWideItems=%d, rotationAngle=%d, FlowDirection=%s ", verticalAlignment.ToString().Data(), horizontalAlignment.ToString().Data(), useWideItems, rotationAngle, flowDirection.ToString().Data());

	//		ComboBox comboBox;
	//		Grid rootPanel;

	//		await RunOnUIThread(() =>

	//		{
	//			// Note: The ComboBox container needs a 6px Margin applied to it due to:
	//			// WINTH:3722949 "ComboBox dropdown can be placed slightly offscreen when ComboBox is near edge of window".
	//			//
	//			// We set ComboBox.SelectedIndex=2 so that there are items both above and below the selected item
	//			// (ComboBox tries to open the popup with the selected item centered over the ComboBox).
	//			rootPanel = Grid> (XamlReader.Load(
	//				LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" >

	//						< Grid x: Name = "comboBoxContainer" Width = "300" Height = "300" Background = "Green" Margin = "8" RenderTransformOrigin = "0.5, 0.5" >

	//							< Grid.RenderTransform >

	//								< RotateTransform Angle = "0" />

	//							</ Grid.RenderTransform >

	//							< ComboBox x: Name = "comboBox" Width = "300" SelectedIndex = "2" >

	//								< ComboBoxItem Content = "item one" />

	//								< ComboBoxItem Content = "item two" />

	//								< ComboBoxItem Content = "item three" />

	//								< ComboBoxItem Content = "item four" />

	//								< ComboBoxItem Content = "item five" />

	//							</ ComboBox >

	//						</ Grid >

	//					</ Grid >)"));


	//			comboBox = ComboBox > (rootPanel.FindName("comboBox"));
	//			comboBox.HorizontalAlignment = horizontalAlignment;
	//			comboBox.VerticalAlignment = verticalAlignment;

	//			var comboBoxContainer = Grid> (rootPanel.FindName("comboBoxContainer"));
	//			comboBoxContainer.HorizontalAlignment = horizontalAlignment;
	//			comboBoxContainer.VerticalAlignment = verticalAlignment;

	//			var rotateTransform = RotateTransform ^> (comboBoxContainer.RenderTransform);
	//			rotateTransform.Angle = rotationAngle;

	//			if (useWideItems)
	//			{
	//				// We set the ComboBoxItems to a width wider than the ComboBox:
	//				for (var item : comboBox.Items)
	//				{
	//					var comboBoxItem = ComboBoxItem> ((Platform.Object ^)(item));
	//					comboBoxItem.Width = 380;
	//				}
	//			}

	//			rootPanel.FlowDirection = flowDirection;

	//			TestServices.WindowHelper.WindowContent = rootPanel;
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();

	//		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

	//		await RunOnUIThread(() =>

	//		{
	//		var dropdownScrollViewer = ScrollViewer> (TreeHelper.GetVisualChildByNameFromOpenPopups("ScrollViewer", rootPanel));
	//		Rect dropdownBounds = ControlHelper.GetBounds(dropdownScrollViewer);
	//		Rect rootBounds = ControlHelper.GetBounds(rootPanel);

	//		LOG_OUTPUT("dropdownBounds: (%f, %f, %f, %f)", dropdownBounds.X, dropdownBounds.Y, dropdownBounds.Width, dropdownBounds.Height);
	//		LOG_OUTPUT("rootBounds:     (%f, %f, %f, %f)", rootBounds.X, rootBounds.Y, rootBounds.Width, rootBounds.Height);

	//		if (expectOutside)
	//		{
	//			// We still expect the ComboBox to be contained within the monitor bounds.
	//			wf.Point windowPosition = TestServices.WindowHelper.ConvertToPhysicalDisplayLocation({ 0, 0 });
	//		Rect monitorBounds = TestServices.WindowHelper.MonitorBounds;
	//		Rect physicalDropdownBounds = dropdownBounds;

	//		// We need to convert the drop-down bounds to screen coordinates here in order to
	//		// be able to compare them to the monitor bounds.
	//		physicalDropdownBounds.X += windowPosition.X;
	//		physicalDropdownBounds.Y += windowPosition.Y;

	//		LOG_OUTPUT("windowPosition:         (%f, %f)", windowPosition.X, windowPosition.Y);
	//		LOG_OUTPUT("physicalDropdownBounds: (%f, %f, %f, %f)", physicalDropdownBounds.X, physicalDropdownBounds.Y, physicalDropdownBounds.Width, physicalDropdownBounds.Height);
	//		LOG_OUTPUT("monitorBounds:          (%f, %f, %f, %f)", monitorBounds.X, monitorBounds.Y, monitorBounds.Width, monitorBounds.Height);

	//		VERIFY_IS_FALSE(ControlHelper.IsContainedIn(dropdownBounds, rootBounds));
	//		VERIFY_IS_TRUE(ControlHelper.IsContainedIn(physicalDropdownBounds, monitorBounds));
	//	}
	//			else
	//			{
	//				VERIFY_IS_TRUE(ControlHelper.IsContainedIn(dropdownBounds, rootBounds));
	//}
	//		});
	//await TestServices.WindowHelper.WaitForIdle();
	//	}

	[TestMethod]
	public async Task DoesGetFocusWhenProgrammaticallyOpened()
	{
		ComboBox comboBox = null;
		Button button = null;
		ComboBoxItem firstItem = null;

		await RunOnUIThread(() =>

		{
			comboBox = new ComboBox();

			for (int i = 0; i < 5; ++i)
			{
				var item = new ComboBoxItem();
				firstItem ??= item;
				item.Content = "Item";
				comboBox.Items.Add(item);
			}

			button = new Button();
			button.VerticalAlignment = VerticalAlignment.Top;

			var root = new Grid();
			root.Children.Add(button);
			root.Children.Add(comboBox);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

		{
			// Explicitly set focus to a button to make sure ComboBox
			// is actually grabbing focus rather than just starting
			// with focus.
			button.Focus(FocusState.Programmatic);

			comboBox.IsDropDownOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// This test originally tested for focus of the ComboBox itself, but the recent changes to the control logic
			// make the first item focused instead (even in WinUI) - hence the condition here is changed.
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(firstItem));
		});
	}

	[TestMethod]
	public async Task CanNavigateAscendingComboBoxesWithGamepad()
	{
		await CanNavigateAscendingComboBoxes(InputDevice.Gamepad);
	}

	[TestMethod]
	public async Task CanNavigateAscendingComboBoxesWithKeyboard()
	{
		await CanNavigateAscendingComboBoxes(InputDevice.Keyboard);
	}

	private async Task CanNavigateAscendingComboBoxes(InputDevice device)
	{
		var comboBox = await SetupAscendingComboBoxTest();
		await RunOnUIThread(() =>

		{
			comboBox.SelectedIndex = 0;
		});
		await TestServices.WindowHelper.WaitForIdle();

		var dropDownOpenedEvent = new Event();
		var dropDownOpenedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownOpened");
		dropDownOpenedRegistration.Attach(comboBox, (s, e) => { dropDownOpenedEvent.Set(); });

		var dropDownClosedEvent = new Event();
		var dropDownClosedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
		dropDownClosedRegistration.Attach(comboBox, (s, e) => { dropDownClosedEvent.Set(); });

		await RunOnUIThread(() =>
		{
			comboBox.Focus(FocusState.Programmatic);
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the ComboBox with the accept button.");
		await CommonInputHelper.Accept(device);

		await TestServices.WindowHelper.WaitForIdle();
		await dropDownOpenedEvent.WaitForDefault();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

		for (int i = 0; i < 10; i++)
		{
			LOG_OUTPUT("Move focus down.");
			await CommonInputHelper.Down(device);

			await TestServices.WindowHelper.WaitForIdle();
		}

		LOG_OUTPUT("Close the ComboBox with the accept button.");
		await CommonInputHelper.Accept(device);

		await dropDownClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 10);
	}

	[TestMethod]
	public async Task SelectionChangedIsNotRaisedUntilClose()
	{
		InputDevice device = InputDevice.Gamepad;

		var comboBox = await SetupBasicComboBoxTest();

		var dropDownOpenedEvent = new Event();
		var dropDownOpenedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownOpened");
		dropDownOpenedRegistration.Attach(comboBox, (s, e) =>
		{
			dropDownOpenedEvent.Set();
		});

		var dropDownClosedEvent = new Event();
		var dropDownClosedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
		dropDownClosedRegistration.Attach(comboBox, (s, e) =>
		{
			dropDownClosedEvent.Set();
		});

		await RunOnUIThread(() =>

		{
			comboBox.Focus(FocusState.Programmatic);
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the ComboBox with the accept button.");
		await CommonInputHelper.Accept(device);

		await TestServices.WindowHelper.WaitForIdle();
		await dropDownOpenedEvent.WaitForDefault();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Focus the first item with down button.");
		await CommonInputHelper.Down(device);

		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Focus the first item with down button.");
		await CommonInputHelper.Down(device);

		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Close the ComboBox with the cancel button. No selection change should have happened.");
		await CommonInputHelper.Cancel(device);

		await dropDownClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Open the ComboBox with the accept button.");
		await CommonInputHelper.Accept(device);

		await TestServices.WindowHelper.WaitForIdle();
		await dropDownOpenedEvent.WaitForDefault();

		LOG_OUTPUT("Focus the first item with down button.");
		await CommonInputHelper.Down(device);

		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Focus the first item with down button.");
		await CommonInputHelper.Down(device);

		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Close the ComboBox with the accept button.");
		await CommonInputHelper.Accept(device);

		await dropDownClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Uno: the selected index seems to be 2 based on the recent changes to the focus handling
		// within the control. However, if this test starts failing, maybe there is some discrepancy
		// compared to WinUI.
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 2);
	}

	//	[TestMethod]
	//	public async Task ValidateDropdownSizingForDifferentInputModes()
	//	{
	//		double expectedDropdownWidth_Touch = 240;
	//		double expectedDropdownWidth_NonTouch = 80;
	//		double expectedComboBoxItemHeight_Touch = 40;
	//		double expectedComboBoxItemHeight_NonTouch = 32;
	//		Thickness expectedDropdownContentMargin_TouchAndCarousel = new Thickness(0, 0, 0, 0);
	//		Thickness expectedDropdownContentMargin_NonTouchAndCarousel = new Thickness(0, 4, 0, 4);

	//		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

	//		ComboBox comboBox;
	//		ComboBoxItem comboBoxItem;
	//		FrameworkElement comboBoxDropdownRoot;
	//		ItemsPresenter comboBoxDropdownItemsPresenter;

	//		await RunOnUIThread(() =>

	//		{
	//		var root = Grid > (XamlReader.Load(
	//				LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	//						  xmlns:x = "http://schemas.microsoft.com/winfx/2006/xaml" >




	//						< ComboBox x: Name = "comboBox" Height = "100" >




	//							< ComboBoxItem x: Name = "comboBoxItem" Content = "i0" />




	//						</ ComboBox >




	//					</ Grid >)"));



	//			comboBox = ComboBox > (root.FindName("comboBox"));
	//		comboBoxItem = (ComboBoxItem)(root.FindName("comboBoxItem"));

	//		TestServices.WindowHelper.WindowContent = root;
	//	});
	//await TestServices.WindowHelper.WaitForIdle();

	//	// Open via Touch.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Touch);
	//	await RunOnUIThread(() =>

	//{
	//		comboBoxDropdownRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("ScrollViewer", comboBox));
	//		comboBoxDropdownItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(comboBoxDropdownRoot);
	//		VERIFY_IS_NOT_NULL(comboBoxDropdownRoot);
	//		VERIFY_IS_NOT_NULL(comboBoxDropdownItemsPresenter);

	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_Touch, comboBoxDropdownRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_Touch, comboBoxDropdownRoot.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxItemHeight_Touch, comboBoxItem.ActualHeight);
	//		VERIFY_ARE_EQUAL(expectedDropdownContentMargin_NonTouchAndCarousel, comboBoxDropdownItemsPresenter.Margin);
	//	});
	//await ComboBoxHelper.CloseComboBox(comboBox);

	//	// Open via Gamepad.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Gamepad);
	//	await RunOnUIThread(() =>

	//{
	//		comboBoxDropdownRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("ScrollViewer", comboBox));
	//		comboBoxDropdownItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(comboBoxDropdownRoot);
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_Touch, comboBoxDropdownRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_Touch, comboBoxDropdownRoot.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxItemHeight_Touch, comboBoxItem.ActualHeight);
	//		VERIFY_ARE_EQUAL(expectedDropdownContentMargin_NonTouchAndCarousel, comboBoxDropdownItemsPresenter.Margin);
	//	});
	//await ComboBoxHelper.CloseComboBox(comboBox);

	//	// Open via Mouse.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//	await RunOnUIThread(() =>

	//{
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_NonTouch, comboBoxDropdownRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_NonTouch, comboBoxDropdownRoot.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxItemHeight_NonTouch, comboBoxItem.ActualHeight);
	//		VERIFY_ARE_EQUAL(expectedDropdownContentMargin_NonTouchAndCarousel, comboBoxDropdownItemsPresenter.Margin);
	//	});
	//await ComboBoxHelper.CloseComboBox(comboBox);

	//	// Open via Keyboard.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Keyboard);
	//	await RunOnUIThread(() =>

	//{
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_NonTouch, comboBoxDropdownRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_NonTouch, comboBoxDropdownRoot.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxItemHeight_NonTouch, comboBoxItem.ActualHeight);
	//		VERIFY_ARE_EQUAL(expectedDropdownContentMargin_NonTouchAndCarousel, comboBoxDropdownItemsPresenter.Margin);
	//	});
	//await ComboBoxHelper.CloseComboBox(comboBox);

	//	// Open programmatically.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
	//	await RunOnUIThread(() =>

	//{
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_NonTouch, comboBoxDropdownRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_NonTouch, comboBoxDropdownRoot.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxItemHeight_NonTouch, comboBoxItem.ActualHeight);
	//		VERIFY_ARE_EQUAL(expectedDropdownContentMargin_NonTouchAndCarousel, comboBoxDropdownItemsPresenter.Margin);
	//	});
	//await ComboBoxHelper.CloseComboBox(comboBox);

	//	// Adding 15 more items so that the max limit of ComboBoxItems to show is reached.
	//	// After 15 items, the Dropdown starts showing a ScrollBar to see other ComboBoxItems.
	//	await RunOnUIThread(() =>

	//{
	//		for (uint i = 1; i < 16; ++i)
	//		{
	//			var cbItem = new ComboBoxItem();
	//			cbItem.Content = "i" + i;
	//			comboBox.Items.Add(cbItem);
	//		}
	//	});

	//// Open via Touch. Now, since the Dropdown have a ScrollBar, when opened with touch, the Dropdown will Carousel,
	//// that is, it loops around the ComboBoxItems. In this case, we want to make sure there is no extra Top/Bottom
	//// Padding/Margin on the Content.
	//await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Touch);
	//	await RunOnUIThread(() =>

	//{
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_Touch, comboBoxDropdownRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedDropdownWidth_Touch, comboBoxDropdownRoot.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxItemHeight_Touch, comboBoxItem.ActualHeight);
	//		VERIFY_ARE_EQUAL(expectedDropdownContentMargin_TouchAndCarousel, comboBoxDropdownItemsPresenter.Margin);
	//	});
	//await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	//[TestMethod]
	//public async Task ValidateFootprint()
	//{


	//	double expectedComboBoxWidth = 64;
	//	double expectedComboBoxWidth_WithWideContent = 200 + 44;
	//	double expectedComboBoxWidth_WithWideHeader = 20.0;

	//	double expectedComboBoxHeight_NoHeader = 32;
	//	double expectedComboBoxHeight_WithHeader = 19 + 4 + expectedComboBoxHeight_NoHeader;

	//	ComboBox comboBox;
	//	ComboBox comboBoxWithHeader;
	//	ComboBox comboBoxWithWideContent;
	//	ComboBox comboBoxWithWideHeader;
	//	StackPanel rootPanel;

	//	await RunOnUIThread(() =>

	//	{
	//		rootPanel = StackPanel> (XamlReader.Load(
	//			LR"(<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" >


	//					< ComboBox x: Name = "comboBox" >


	//						< ComboBoxItem Content = "1" />


	//						< ComboBoxItem Content = "2" />


	//						< ComboBoxItem Content = "3" />


	//					</ ComboBox >


	//					< ComboBox x: Name = "comboBoxWithHeader" Header = "H" >


	//						< ComboBoxItem Content = "1" />


	//						< ComboBoxItem Content = "2" />


	//						< ComboBoxItem Content = "3" />


	//					</ ComboBox >


	//					< ComboBox x: Name = "comboBoxWithWideContent" >


	//						< ComboBoxItem IsSelected = "True" >


	//							< Rectangle Height = "10" Width = "200" Fill = "Red" />


	//						</ ComboBoxItem >


	//						< ComboBoxItem Content = "2" />


	//						< ComboBoxItem Content = "3" />


	//					</ ComboBox >


	//					< ComboBox x: Name = "comboBoxWithWideHeader" >


	//						< ComboBox.Header >


	//							< Rectangle Height = "19" Width = "200" Fill = "Red" />


	//						</ ComboBox.Header >


	//						< ComboBoxItem Content = "1" />


	//						< ComboBoxItem Content = "2" />


	//						< ComboBoxItem Content = "3" />


	//					</ ComboBox >


	//				</ StackPanel >)"));



	//		comboBox = ComboBox > (rootPanel.FindName("comboBox"));
	//		comboBoxWithHeader = ComboBox > (rootPanel.FindName("comboBoxWithHeader"));
	//		comboBoxWithWideContent = ComboBox > (rootPanel.FindName("comboBoxWithWideContent"));
	//		comboBoxWithWideHeader = ComboBox > (rootPanel.FindName("comboBoxWithWideHeader"));

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await RunOnUIThread(() =>

	//	{
	//		VERIFY_ARE_EQUAL(expectedComboBoxWidth, comboBox.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxHeight_NoHeader, comboBox.ActualHeight);

	//		VERIFY_ARE_EQUAL(expectedComboBoxWidth, comboBoxWithHeader.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxHeight_WithHeader, comboBoxWithHeader.ActualHeight);

	//		VERIFY_ARE_EQUAL(expectedComboBoxWidth_WithWideContent, comboBoxWithWideContent.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxHeight_NoHeader, comboBoxWithWideContent.ActualHeight);

	//		VERIFY_ARE_EQUAL(expectedComboBoxWidth_WithWideHeader, comboBoxWithWideHeader.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedComboBoxHeight_WithHeader, comboBoxWithWideHeader.ActualHeight);
	//	});
	//}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task CanSelectItemWithTap()
	{
		ComboBoxItem comboBoxItem = null;

		var comboBox = await SetupBasicComboBoxTest();

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			comboBoxItem = (ComboBoxItem)(comboBox.ContainerFromIndex(0));
			VERIFY_IS_NOT_NULL(comboBoxItem);
		});
		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(comboBoxItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

		{
			VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 0);
			VERIFY_IS_FALSE(comboBox.IsDropDownOpen);
		});
	}

	[TestMethod]
	public async Task ValidateMaximumHeightIsHonored()
	{
		var comboBox = await SetupBasicComboBoxTest(10, true);

		await RunOnUIThread(() =>
		{
			comboBox.MaxDropDownHeight = 10.0;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

		{
			var popupBorder = TreeHelper.GetVisualChildByNameFromOpenPopups("PopupBorder", comboBox);
			VERIFY_IS_LESS_THAN(popupBorder.ActualHeight, 100);
		});
	}

	//[TestMethod]
	//public async Task ValidateSelectedValuePathProperty()
	//{
	//	ComboBox comboBox = null;

	//	await RunOnUIThread(() =>

	//	{
	//		var rootPanel = new Grid();
	//		VERIFY_IS_NOT_NULL(rootPanel);

	//		Platform.Collections.Vector < PersonObject ^> ^itemList = new Platform.Collections.Vector<PersonObject^> ();
	//		itemList.Add(new PersonObject("Roger", "Federer"));
	//		itemList.Add(new PersonObject("Cristiano", "Ronaldo"));
	//		itemList.Add(new PersonObject("LeBron", "James"));

	//		comboBox = new ComboBox();
	//		VERIFY_IS_NOT_NULL(comboBox);

	//		comboBox.ItemsSource = itemList;
	//		comboBox.DisplayMemberPath = "FirstName";
	//		comboBox.SelectedValuePath = "LastName";

	//		rootPanel.Children.Add(comboBox);
	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		comboBox.SelectedIndex = 1;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		var expectedSelectedValueString = "Ronaldo";
	//		var selectedValueString = (string)(comboBox.SelectedValue);

	//		VERIFY_IS_NOT_NULL(selectedValueString);
	//		VERIFY_ARE_EQUAL(expectedSelectedValueString, selectedValueString);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		// changing the SelectedValue to something invalid (not in collection)
	//		comboBox.SelectedValue = "Hello";
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		// after changing the SelectedValue to something unrecognizable, SelectedValue should change to null
	//		VERIFY_IS_NULL(comboBox.SelectedValue);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	//[TestMethod]
	//public async Task ValidateCustomizedPaddingWithTouch()
	//{


	//	ScrollViewer scrollViewer = null;

	//	var comboBox = await SetupComboBoxCustomizedPaddingTest();

	//	// Validate the touch input mode padding for each item.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Touch);
	//	await RunOnUIThread(() =>

	//	{
	//		var popup = TreeHelper.GetVisualChildByType<Popup>(comboBox);
	//		var popupChild = (FrameworkElement)(popup.Child);
	//		scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>(popupChild);
	//	});
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Touch);
	//	for (int i = 0; i < 10; ++i)
	//	{
	//		TestServices.InputHelper.Flick(comboBox, FlickDirection.North);
	//	}
	//	await TestServices.WindowHelper.WaitForIdle();
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Touch);
	//	for (int i = 0; i < 10; ++i)
	//	{
	//		TestServices.InputHelper.Flick(comboBox, FlickDirection.South);
	//	}
	//	await TestServices.WindowHelper.WaitForIdle();
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Touch);
	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	//[TestMethod]
	//public async Task ValidateCustomizedPaddingWithMouseAndKeyboard()
	//{
	//	ScrollViewer scrollViewer = null;

	//	var comboBox = await SetupComboBoxCustomizedPaddingTest();

	//	// Validate the mouse input mode padding for each item.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//	await RunOnUIThread(() =>

	//	{
	//		var popup = TreeHelper.GetVisualChildByType<Popup>(comboBox);
	//		var popupChild = (FrameworkElement)(popup.Child);
	//		scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>(popupChild);
	//	});
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//	TestServices.InputHelper.MoveMouse(scrollViewer);
	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.InputHelper.ScrollMouseWheel(scrollViewer, 10 /* numberOfWheelClicks */);
	//	await TestServices.WindowHelper.WaitForIdle();
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//	TestServices.InputHelper.ScrollMouseWheel(scrollViewer, -10 /* numberOfWheelClicks */);
	//	await TestServices.WindowHelper.WaitForIdle();
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//	await ComboBoxHelper.CloseComboBox(comboBox);

	//	// Validate the keyboard input mode padding for each item.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Keyboard);
	//	await TestServices.KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown");
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await TestServices.KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown");
	//	await TestServices.WindowHelper.WaitForIdle();
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Keyboard);
	//	await TestServices.KeyboardHelper.PressKeySequence("$d$_pageup#$u$_pageup");
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await TestServices.KeyboardHelper.PressKeySequence("$d$_pageup#$u$_pageup");
	//	await TestServices.WindowHelper.WaitForIdle();
	//	ValidateComboBoxItemPadding(comboBox, ComboBoxHelper.OpenMethod.Keyboard);
	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	//ComboBox ^ SetupComboBoxCustomizedPaddingTest()
	//{
	//	ComboBox comboBox = null;

	//	await RunOnUIThread(() =>

	//	{
	//		var rootPanel = Grid > (XamlReader.Load(
	//			LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" VerticalAlignment="Center" >



	//					< Grid.Resources >




	//						< Style TargetType = "ComboBoxItem" >




	//							< Setter Property = "Background" Value = "Red" />




	//							< Setter Property = "Padding" Value = "21, 22, 23, 24" />




	//						</ Style >




	//					</ Grid.Resources >




	//					< ComboBox x: Name = 'comboBoxCustomPaddingItem' Background = 'RoyalBlue' SelectedIndex = '0'  Width = '350' >




	//						< ComboBoxItem Content = 'Apps and Games (1)' />




	//						< ComboBoxItem Content = 'Files, Folders, and Online Storage (2)' />




	//						< ComboBoxItem Content = 'Hardware, Devices, and Drivers (3)' />




	//						< ComboBoxItem Content = 'Windows Installation and Setup (4)' />




	//						< ComboBoxItem Content = 'Microsoft Edge and IE (5)' />




	//						< ComboBoxItem Content = 'Networks (6)' />




	//						< ComboBoxItem Content = 'Personalization and Ease of Access (7)' />




	//						< ComboBoxItem Content = 'Power and Battery (8)' />




	//						< ComboBoxItem Content = 'Windows Recovery (9)' />




	//						< ComboBoxItem Content = 'Cortana and search (10)' />




	//						< ComboBoxItem Content = 'Start (11)' />




	//						< ComboBoxItem Content = 'Desktop (12)' />




	//						< ComboBoxItem Content = 'Security, Privacy, and Accounts (13)' />




	//						< ComboBoxItem Content = 'Input and Interaction Methods (14)' />




	//						< ComboBoxItem Content = 'Store (15)' />




	//						< ComboBoxItem Content = 'Developer Platform (16)' />




	//						< ComboBoxItem Content = 'Internal Feedback Tools (17)' />




	//						< ComboBoxItem Content = 'Preview Programs (18)' />




	//					</ ComboBox >




	//				</ Grid >)"));





	//		comboBox = ComboBox > (TreeHelper.GetVisualChildByName(rootPanel, "comboBoxCustomPaddingItem"));
	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	return comboBox;
	//}

	private async Task ValidateComboBoxItemPadding(ComboBox comboBox, ComboBoxHelper.OpenMethod openMethod)
	{
		await RunOnUIThread(() =>
		{
			foreach (var item in comboBox.Items)
			{
				var comboBoxItem = (ComboBoxItem)item;
				var contentPresenter = (ContentPresenter)TreeHelper.GetVisualChildByName(comboBoxItem, "ContentPresenter");

				if (contentPresenter is not null)
				{
					LOG_OUTPUT("Item padding left=%f top=%f right=%f bottom=%f", contentPresenter.Margin.Left, contentPresenter.Margin.Top, contentPresenter.Margin.Right, contentPresenter.Margin.Bottom);

					if (openMethod == ComboBoxHelper.OpenMethod.Touch)
					{
						Thickness expectedThickness = new Thickness(10, 8, 10, 11);
						VERIFY_ARE_EQUAL(expectedThickness, contentPresenter.Margin);
					}
					else
					{
						VERIFY_ARE_EQUAL(comboBoxItem.Padding, contentPresenter.Margin);
					}
				}
				else
				{
					LOG_OUTPUT("Item isn't realized yet!");
				}
			}
		});
	}

	//    [TestMethod] public async Task ValidateItemTemplateSettingWithTouch()
	//{


	//	ComboBox comboBox = null;
	//	ScrollViewer scrollViewer = null;

	//	await RunOnUIThread(() =>

	//		{
	//			var rootPanel = Grid > (XamlReader.Load(
	//				LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" VerticalAlignment="Center" >


	//						< Grid.Resources >



	//							< Style x: Key = "ContentTextStyle" TargetType = "TextBlock" >



	//								< Setter Property = "Foreground" Value = "Blue" />



	//								< Setter Property = "HorizontalAlignment" Value = "Center" />



	//								< Setter Property = "VerticalAlignment" Value = "Center" />



	//								< Setter Property = "FontSize" Value = "40" />



	//								< Setter Property = "TextWrapping" Value = "NoWrap" />



	//								< Setter Property = "FontFamily" Value = "segoeuil.ttf#Segoe UI" />



	//							</ Style >



	//						</ Grid.Resources >



	//						< StackPanel Orientation = "Horizontal" >



	//							< ComboBox x: Name = 'comboBoxNotificationSetting' Background = 'RoyalBlue' Width = "200" >



	//								< ComboBox.ItemTemplate >



	//									< DataTemplate >



	//										< Border Height = "60" >



	//											< Border BorderBrush = "Black" BorderThickness = "0" >



	//												< TextBlock Text = "{Binding}" Style = "{StaticResource ContentTextStyle}" />



	//											</ Border >



	//										</ Border >



	//									</ DataTemplate >



	//								</ ComboBox.ItemTemplate >



	//							</ ComboBox >



	//						</ StackPanel >



	//					</ Grid >)"));


	//			comboBox = ComboBox > (TreeHelper.GetVisualChildByName(rootPanel, "comboBoxNotificationSetting"));

	//			// Add the notification setting items
	//			var itemList = new List<string>();
	//			itemList.Add("All settings(1)");
	//			itemList.Add("Connect (2)");
	//			itemList.Add("Battery saver (3)");
	//			itemList.Add("Flashlight (4)");
	//			itemList.Add("Note (5)");
	//			itemList.Add("VPN (6)");
	//			itemList.Add("Location (7)");
	//			itemList.Add("Airplane mode (8)");
	//			itemList.Add("Bluetooth (9)");
	//			itemList.Add("Camera (10)");
	//			itemList.Add("Cellular (11)");
	//			itemList.Add("Mobile hotspot (12)");
	//			itemList.Add("Quiet hours (13)");
	//			itemList.Add("Wi-Fi (14)");
	//			itemList.Add("Rotation lock (15)");
	//			itemList.Add("Brightness (16)");
	//			comboBox.ItemsSource = itemList;

	//			TestServices.WindowHelper.WindowContent = rootPanel;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Validate the touch input mode padding for each item.
	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Touch);

	//	await RunOnUIThread(() =>

	//		{
	//			var popup = TreeHelper.GetVisualChildByType<Popup>(comboBox);
	//			scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>((FrameworkElement)(popup.Child));
	//		});

	//	LOG_OUTPUT("Panning to North.");

	//	for (int i = 0; i < 25; ++i)
	//	{
	//		TestServices.InputHelper.Flick(comboBox, FlickDirection.North);
	//	}
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Panning to South.");

	//	for (int i = 0; i < 25; ++i)
	//	{
	//		TestServices.InputHelper.Flick(comboBox, FlickDirection.South);
	//	}
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("No layout cycle reported!");

	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	//[TestMethod]
	//public async Task ValidateOpenedComboBoxPositionByTouchInput()
	//{
	//	await DoValidatePosition(5, ComboBoxHelper.OpenMethod.Touch, false /* addMouseOpenMethod */);
	//	await DoValidatePosition(50, ComboBoxHelper.OpenMethod.Touch, false /* addMouseOpenMethod */);
	//}

	//[TestMethod]
	//public async Task ValidateOpenedComboBoxPositionWithDifferentInput()
	//{
	//	await DoValidatePosition(5, ComboBoxHelper.OpenMethod.Touch, true /* addMouseOpenMethod */);
	//	await DoValidatePosition(50, ComboBoxHelper.OpenMethod.Touch, true /* addMouseOpenMethod */);
	//}

	//private async Task DoValidatePosition(int itemCount, ComboBoxHelper.OpenMethod openMethod, bool addMouseOpenMethod, bool isVerticalAlignment)
	//{
	//	var comboBox = await SetupBasicComboBoxTest(itemCount /* itemSize */);

	//	await RunOnUIThread(() =>
	//	{
	//		comboBox.Margin = new Thickness(0, 0, 0, 0);
	//	});

	//	if (isVerticalAlignment)
	//	{
	//		LOG_OUTPUT("DoValidatePosition Horizontal:Left Vertical:Top.");
	//		await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Top, comboBox, openMethod);
	//		if (addMouseOpenMethod)
	//		{
	//			await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Top, comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//		}

	//		LOG_OUTPUT("DoValidatePosition Horizontal:Left Vertical:Center.");
	//		await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Center, comboBox, openMethod);
	//		if (addMouseOpenMethod)
	//		{
	//			await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Center, comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//		}

	//		LOG_OUTPUT("DoValidatePosition Horizontal:Left Vertical:Bottom.");
	//		await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Bottom, comboBox, openMethod);
	//		if (addMouseOpenMethod)
	//		{
	//			await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Bottom, comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//		}
	//	}
	//	else
	//	{
	//		LOG_OUTPUT("DoValidatePosition Horizontal:Left Vertical:Top.");
	//		await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Top, comboBox, openMethod);
	//		if (addMouseOpenMethod)
	//		{
	//			await ValidatePosition(HorizontalAlignment.Left, VerticalAlignment.Top, comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//		}

	//		LOG_OUTPUT("DoValidatePosition Horizontal:Center Vertical:Top.");
	//		await ValidatePosition(HorizontalAlignment.Center, VerticalAlignment.Top, comboBox, openMethod);
	//		if (addMouseOpenMethod)
	//		{
	//			await ValidatePosition(HorizontalAlignment.Center, VerticalAlignment.Top, comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//		}

	//		LOG_OUTPUT("DoValidatePosition Horizontal:Right Vertical:Top.");
	//		await ValidatePosition(HorizontalAlignment.Right, VerticalAlignment.Top, comboBox, openMethod);
	//		if (addMouseOpenMethod)
	//		{
	//			await ValidatePosition(HorizontalAlignment.Right, VerticalAlignment.Top, comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//		}
	//	}
	//}

	//private async Task ValidatePosition(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, ComboBox comboBox, ComboBoxHelper.OpenMethod openMethod)
	//{
	//	ScrollViewer scrollViewer = null;

	//	await RunOnUIThread(() =>
	//	{
	//		var rootPanel = (Grid)comboBox.Parent;
	//		rootPanel.HorizontalAlignment = horizontalAlignment;
	//		rootPanel.VerticalAlignment = verticalAlignment;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBox, openMethod);

	//	await RunOnUIThread(async () =>

	//		{
	//			var popup = TreeHelper.GetVisualChildByType<Popup>(comboBox);
	//			var popupChild = (FrameworkElement)popup.Child;
	//			scrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByName(popupChild, "ScrollViewer"));

	//			Rect scrollViewerBounds = await ControlHelper.GetBounds(scrollViewer);
	//			Rect visibleBounds = TestServices.WindowHelper.VisibleBounds;

	//			LOG_OUTPUT("scrollViewerBounds: (%f, %f, %f, %f)", scrollViewerBounds.X, scrollViewerBounds.Y, scrollViewerBounds.Width, scrollViewerBounds.Height);
	//			LOG_OUTPUT("visibleBounds:      (%f, %f, %f, %f)", visibleBounds.X, visibleBounds.Y, visibleBounds.Width, visibleBounds.Height);

	//			Point topLeftCorner = default;

	//			// If we're on desktop with windowed popups enabled, then we expect the popup to be able to overlap with the window chrome.
	//			if (PopupHelper.AreWindowedPopupsEnabled())
	//			{
	//				Rect windowBounds = TestServices.WindowHelper.WindowBounds;
	//				LOG_OUTPUT("windowBounds:       (%f, %f, %f, %f)", windowBounds.X, windowBounds.Y, windowBounds.Width, windowBounds.Height);

	//				topLeftCorner.X -= windowBounds.X;
	//				topLeftCorner.Y -= windowBounds.Y;
	//				visibleBounds.X += windowBounds.X;
	//				visibleBounds.Y += windowBounds.Y;
	//			}

	//			VERIFY_IS_TRUE(scrollViewerBounds.X >= topLeftCorner.X);
	//			VERIFY_IS_TRUE(scrollViewerBounds.X + scrollViewerBounds.Width <= topLeftCorner.X + visibleBounds.X + visibleBounds.Width);
	//			VERIFY_IS_TRUE(scrollViewerBounds.Y >= topLeftCorner.Y);
	//			VERIFY_IS_TRUE(scrollViewerBounds.Y + scrollViewerBounds.Height <= topLeftCorner.Y + visibleBounds.Y + visibleBounds.Height);
	//		});

	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	//[TestMethod]
	//public async Task ValidateOpenedComboBoxPositionWhenUsingExtendedTitleBar()
	//{


	//	//ExtendViewIntoTitleBar requires that we have a loaded xaml page, so lets load something before we set this flag.
	//	await RunOnUIThread(() =>

	//		{
	//			TestServices.WindowHelper.WindowContent = new Grid();
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//		{
	//			CoreApplicationViewTitleBar coreApplicationViewTitleBar = null;

	//			coreApplicationViewTitleBar = CoreApplication.GetCurrentView().TitleBar;
	//			coreApplicationViewTitleBar.ExtendViewIntoTitleBar = true;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Wait for 2 frame to update the content on the extend title area.
	//	await TestServices.WindowHelper.SynchronouslyTickUIThread(2);

	//	DoValidatePosition(50, ComboBoxHelper.OpenMethod.Touch, false /* addMouseOpenMethod */);
	//}

	//[TestMethod]
	//public async Task ValidateOpenedComboBoxPositionWhenUsingCoreWindowBounds()
	//{
	//	TestServices.WindowHelper.SetVisibleBounds(Rect(0, 32, 480, 768));

	//	var comboBox = await SetupBasicComboBoxTest(50.0;

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.Margin = { 0,0,0,0 };
	//			comboBox.Height = 10.0;
	//			comboBox.Width = 30.0;

	//			wuv.ApplicationView.GetForCurrentView().SetDesiredBoundsMode(wuv.ApplicationViewBoundsMode.UseCoreWindow);
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	DoValidatePosition(50, ComboBoxHelper.OpenMethod.Programmatic, false /* addMouseOpenMethod */, true /* isVerticalAlignment */);

	//	// Set the window with the landscape mode with specifying the visible bounds.
	//	TestServices.WindowHelper.SetWindowSizeOverride(new Size(800, 480));
	//	TestServices.WindowHelper.SetVisibleBounds(Rect(44, 0, 756, 480));
	//	await TestServices.WindowHelper.WaitForIdle();

	//	DoValidatePosition(50, ComboBoxHelper.OpenMethod.Programmatic, false /* addMouseOpenMethod */, false /* isVerticalAlignment */);
	//}

	[TestMethod]
	[Ignore("Light dismiss is currently implemented differently in Uno #17988")]
	public async Task ValidateLightDismissOverlayMode()
	{
		var comboBox = await SetupBasicComboBoxTest();

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

		LOG_OUTPUT("Validate that the default is Auto and the ComboBox's popup is set to Off (or On if on Xbox)");
		{
			await RunOnUIThread(() =>

				{
					VERIFY_ARE_EQUAL(comboBox.LightDismissOverlayMode, LightDismissOverlayMode.Auto);
				});

			await ValidateComboBoxPopupLightDismissOverlayMode(
				TestServices.Utilities.IsXBox ? LightDismissOverlayMode.On : LightDismissOverlayMode.Off);
		}

		LOG_OUTPUT("Validate that when set to On the ComboBox's popup is also set to On.");
		{
			await RunOnUIThread(() =>

				{
					comboBox.LightDismissOverlayMode = LightDismissOverlayMode.On;
				});

			await ValidateComboBoxPopupLightDismissOverlayMode(LightDismissOverlayMode.On);
		}

		LOG_OUTPUT("Validate that when set to Off the ComboBox's popup is also set to Off.");
		{
			await RunOnUIThread(() =>

				{
					comboBox.LightDismissOverlayMode = LightDismissOverlayMode.Off;
				});

			await ValidateComboBoxPopupLightDismissOverlayMode(LightDismissOverlayMode.Off);
		}

		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	[TestMethod]
	[Ignore("Light dismiss is currently implemented differently in Uno #17988")]
	public async Task DoesAutoLightDismissOverlayModeCreateOverlayOnXbox()
	{
		var comboBox = await SetupBasicComboBoxTest();

		await RunOnUIThread(() =>

			{
				comboBox.LightDismissOverlayMode = LightDismissOverlayMode.Auto;
			});

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

		await ValidateComboBoxPopupLightDismissOverlayMode(LightDismissOverlayMode.On);

		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	//[TestMethod]
	//public async Task ValidateOverlayBrush()
	//{
	//	var comboBox = await SetupBasicComboBoxTest();

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.LightDismissOverlayMode = LightDismissOverlayMode.On;
	//		});

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

	//	await RunOnUIThread(() =>

	//		{
	//			var expectedBrush = SolidColorBrush(Application.Current.Resources.Lookup("ComboBoxLightDismissOverlayBackground"));

	//			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(comboBox.XamlRoot);
	//			WEX.Common.Throw.IfFalse(popups.Size == 1, E_FAIL, "Expected exactly one open Popup.");

	//			var popup = popups.GetAt(0.0;

	//			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
	//			THROW_IF_null_WITH_MSG(overlayElement, "An overlay element should exist.");

	//			var overlayRect = (xaml_shapes.Rectangle ^)(overlayElement);
	//			THROW_IF_null_WITH_MSG(overlayRect, "The overlay element should be a rectangle.");

	//			var overlayBrush = SolidColorBrush ^> (overlayRect.Fill);
	//			VERIFY_IS_NOT_NULL(overlayBrush);
	//			VERIFY_IS_TRUE(overlayBrush.Equals(expectedBrush));
	//		});

	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	private async Task ValidateComboBoxPopupLightDismissOverlayMode(LightDismissOverlayMode expectedMode)
	{
		await RunOnUIThread(() =>

			{
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);
				Assert.AreEqual(1, popups.Count, "Expected exactly one open Popup.");

				var popup = popups[0];

				VERIFY_ARE_EQUAL(popup.LightDismissOverlayMode, expectedMode);

				if (expectedMode == LightDismissOverlayMode.On)
				{
					var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
					VERIFY_IS_NOT_NULL(overlayElement);
				}
			});
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task ValidateFocusStateForComboBoxOpenedWithTouch()
	{
		await ValidateFocusStateForComboBoxWorker(ComboBoxHelper.OpenMethod.Touch, FocusState.Pointer);
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.LeftMouseClick properly on input injector targets. #17988")]
#endif
	public async Task ValidateFocusStateForComboBoxOpenedWithMouse()
	{
		await ValidateFocusStateForComboBoxWorker(ComboBoxHelper.OpenMethod.Mouse, FocusState.Pointer);
	}

	[TestMethod]
	public async Task ValidateFocusStateForComboBoxOpenedWithKeyboard()
	{
		await ValidateFocusStateForComboBoxWorker(ComboBoxHelper.OpenMethod.Keyboard, FocusState.Keyboard);
	}

	[TestMethod]
	public async Task ValidateFocusStateForComboBoxOpenedWithGamepad()
	{
		await ValidateFocusStateForComboBoxWorker(ComboBoxHelper.OpenMethod.Gamepad, FocusState.Keyboard);
	}

	private async Task ValidateFocusStateForComboBoxWorker(ComboBoxHelper.OpenMethod openMethod, FocusState expectedFocusState)
	{
		ComboBox comboBox = await SetupBasicComboBoxTest();

		await RunOnUIThread(() =>
		{
			comboBox.SelectedIndex = 0;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.OpenComboBox(comboBox, openMethod);

		await RunOnUIThread(() =>

			{
				var selectedValue = (ComboBoxItem)(comboBox.SelectedValue);
				VERIFY_IS_NOT_NULL(selectedValue);
				var focusState = selectedValue.FocusState;
				VERIFY_ARE_EQUAL(expectedFocusState, focusState);
			});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.CloseComboBox(comboBox);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("We currently only support InputHelper.LeftMouseClick on Skia targets. #17988")]
#endif
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedAndClosedWithMouse()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Mouse, ComboBoxHelper.CloseMethod.Mouse, FocusState.Pointer);
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("We currently only support InputHelper.LeftMouseClick on Skia targets. #17988")]
#endif
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedAndClosedWithTouch()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Touch, ComboBoxHelper.CloseMethod.Touch, FocusState.Pointer);
	}

	[TestMethod]
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedAndClosedWithKeyboard()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Keyboard, ComboBoxHelper.CloseMethod.Keyboard, FocusState.Keyboard);
	}

	[TestMethod]
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedAndClosedWithGamepad()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Gamepad, ComboBoxHelper.CloseMethod.Gamepad, FocusState.Keyboard);
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedWithTouchAndClosedWithKeyboard()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Touch, ComboBoxHelper.CloseMethod.Keyboard, FocusState.Keyboard);
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedWithTouchAndClosedWithGamepad()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Touch, ComboBoxHelper.CloseMethod.Gamepad, FocusState.Keyboard);
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.LeftMouseClick properly on input injector targets. #17988")]
#endif
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedWithMouseAndClosedWithKeyboard()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Mouse, ComboBoxHelper.CloseMethod.Keyboard, FocusState.Keyboard);
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.LeftMouseClick properly on input injector targets. #17988")]
#endif
	public async Task ValidateFocusStateOfClosedComboBoxWhenOpenedWithMouseAndClosedWithGamepad()
	{
		await ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod.Mouse, ComboBoxHelper.CloseMethod.Gamepad, FocusState.Keyboard);
	}

	private async Task ValidateFocusStateOfClosedComboBoxWorker(ComboBoxHelper.OpenMethod openMethod, ComboBoxHelper.CloseMethod closeMethod, FocusState expectedFocusState)
	{
		ComboBox comboBox = await SetupBasicComboBoxTest();

		await RunOnUIThread(() =>

			{
				comboBox.SelectedIndex = 0;
			});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.OpenComboBox(comboBox, openMethod);

		await ComboBoxHelper.CloseComboBox(comboBox, closeMethod);

		await RunOnUIThread(() =>

			{
				var focusState = comboBox.FocusState;
				VERIFY_ARE_EQUAL(expectedFocusState, focusState);
			});
		await TestServices.WindowHelper.WaitForIdle();
	}

	//[TestMethod]
	//public async Task ValidatePagingKeyInteraction()
	//{
	//	int totalNumberOfItems = 20;
	//	int expectedNumberOfItemsToScrollWithPageKeys = 14;

	//	ComboBox comboBox = await SetupBasicComboBoxTest(totalNumberOfItems);

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.SelectionChangedTrigger = ComboBoxSelectionChangedTrigger.Always;
	//			comboBox.SelectedIndex = 0;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

	//	LOG_OUTPUT("Changing selection with PageDown");
	//	TestServices.KeyboardHelper.PageDown();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, expectedNumberOfItemsToScrollWithPageKeys);

	//	LOG_OUTPUT("Changing selection with PageUp");
	//	TestServices.KeyboardHelper.PageUp();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

	//	LOG_OUTPUT("Changing selection with End");
	//	TestServices.KeyboardHelper.End();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, totalNumberOfItems - 1);

	//	LOG_OUTPUT("Changing selection with Home");
	//	TestServices.KeyboardHelper.Home();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

	//	await ComboBoxHelper.CloseComboBox(comboBox, ComboBoxHelper.CloseMethod.Programmatic);
	//}

	//	void OpenComboBoxWhileSIPIsUp()
	//	{
	//		wf.EventRegistrationToken inputPaneShowingToken = default;
	//		wf.EventRegistrationToken inputPaneHidingToken = default;

	//		// Since InputPane is not agile, we can't use SafeEventRegistration. We need to manage the SIP events manually.
	//		TestCleanupWrapper cleanup([&inputPaneShowingToken, &inputPaneHidingToken]()



	//		{
	//			RunOnUIThread([&inputPaneShowingToken, &inputPaneHidingToken]()

	//			{
	//				InputPane.GetForCurrentView().Showing -= inputPaneShowingToken;
	//				InputPane.GetForCurrentView().Hiding -= inputPaneHidingToken;
	//				inputPaneShowingToken = default;
	//				inputPaneHidingToken = default;
	//			});

	//			TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
	//		});

	//		ComboBox combobox;
	//		TextBox textbox;
	//		ComboBoxItem itemToSelect;

	//		await RunOnUIThread(() =>

	//			{
	//				var rootPanel = StackPanel> (XamlReader.Load(
	//					LR"(<StackPanel








	//								xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"








	//								xmlns: x = "http://schemas.microsoft.com/winfx/2006/xaml" >








	//							< ComboBox x: Name = "combobox" Header = "ComboBox Header" Margin = "12" FontSize = "40" SelectedIndex = "0" >








	//								< ComboBoxItem Content = "ComboBoxItem A" />








	//								< ComboBoxItem Content = "ComboBoxItem B" />








	//								< ComboBoxItem Content = "ComboBoxItem C" x: Name = "itemToSelect" />








	//								< ComboBoxItem Content = "ComboBoxItem D" />








	//								< ComboBoxItem Content = "ComboBoxItem E" />








	//								< ComboBoxItem Content = "ComboBoxItem F" />








	//								< ComboBoxItem Content = "ComboBoxItem G" />








	//							</ ComboBox >








	//							< TextBox x: Name = "textbox" Header = "TextBox" Width = "200" HorizontalAlignment = "Left" Margin = "12" />








	//						</ StackPanel >)"));







	//				combobox = ComboBox > (rootPanel.FindName("combobox"));
	//				textbox = (TextBox)(rootPanel.FindName("textbox"));
	//				itemToSelect = (ComboBoxItem)(rootPanel.FindName("itemToSelect"));

	//				TestServices.WindowHelper.WindowContent = rootPanel;
	//			});
	//		await TestServices.WindowHelper.WaitForIdle();

	//		Event selectionChangedEvent;
	//		var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);
	//		selectionChangedRegistration.Attach(comboBox, (s, e) => { selectionChangedEvent.Set(); });

	//		var SIPShowingEvent = new Event();
	//		var SIPHidingEvent = new Event();
	//		await RunOnUIThread(() =>

	//			{
	//			InputPane ^ inputPane = InputPane.GetForCurrentView();
	//			inputPaneShowingToken = inputPane.Showing += new wf.TypedEventHandler<InputPane^, InputPaneVisibilityEventArgs ^> ([&](InputPane ^ pane, InputPaneVisibilityEventArgs ^ e)






	//				{
	//				SIPShowingEvent.Set();
	//			});
	//		inputPaneHidingToken = inputPane.Hiding += new wf.TypedEventHandler<InputPane^, InputPaneVisibilityEventArgs ^> ([&](InputPane ^ pane, InputPaneVisibilityEventArgs ^ e)



	//			{
	//			SIPHidingEvent.Set();
	//		});
	//	});

	//// We want to know the height of the open ComboBox popup (without the SIP being involved).
	//double fullHeightOfComboBoxPopup = 0;
	//	await ComboBoxHelper.OpenComboBox(combobox, ComboBoxHelper.OpenMethod.Touch);
	//	await RunOnUIThread(() =>

	//{
	//		var popupBorder = TreeHelper.GetVisualChildByNameFromOpenPopups("PopupBorder", combobox);
	//		fullHeightOfComboBoxPopup = popupBorder.ActualHeight;
	//	});
	//await ComboBoxHelper.CloseComboBox(combobox);

	//	// Tap the TextBox to bring up the SIP.
	//	TestServices.InputHelper.Tap(textbox);
	//SIPShowingEvent.WaitForDefault();
	//await TestServices.WindowHelper.WaitForIdle();

	//	// We want to test the scenario where the full size of the ComboBox popup would not fit in the window space
	//	// available when the SIP is up.
	//	// We verify that this is actually the case (otherwise we are not testing what we want to test).
	//	await RunOnUIThread(() =>

	//{
	//		var sipRect = InputPane.GetForCurrentView().OccludedRect;
	//		var windowBounds = Window.Current.Bounds;

	//		var usableHeight = windowBounds.Height - sipRect.Height;
	//		VERIFY_IS_GREATER_THAN(fullHeightOfComboBoxPopup, usableHeight);
	//	});

	//// Tap on the ComboBox when the SIP is up.
	//// The ComboBox will open and the SIP will dismiss at the same time.
	//await ComboBoxHelper.OpenComboBox(combobox, ComboBoxHelper.OpenMethod.Touch);
	//	SIPHidingEvent.WaitForDefault();
	//await TestServices.WindowHelper.WaitForIdle();

	//	// Select an item from the open ComboBox.
	//	TestServices.InputHelper.Tap(itemToSelect);
	//selectionChangedEvent.WaitForDefault();

	//await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(itemToSelect.Equals(combobox.SelectedItem));
	//	});
	//}


	[TestMethod]
	[Ignore("We are now layouting ComboBox Popup differently than WinUI. #17988")]
	public async Task ValidatePopupPlacementAtBottomOfScreen()
	{
		int numberOfItems = 8;
		float heightOfWindow = 40;

		Size size = new(400, heightOfWindow);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = await SetupBasicComboBoxTest(numberOfItems);

		await RunOnUIThread(() =>

			{
				// Ensure that the combobox is partially off screen
				TranslateTransform translateTransform = new Microsoft.UI.Xaml.Media.TranslateTransform();
				translateTransform.Y = 20;
				comboBox.RenderTransform = translateTransform;

				comboBox.Margin = ThicknessHelper.FromUniformLength(0);
				comboBox.HorizontalAlignment = HorizontalAlignment.Center;
				comboBox.VerticalAlignment = VerticalAlignment.Bottom;

				// Select the last item. We will get the bounds of this item and ensure it is rendered on-screen.
				comboBox.SelectedIndex = numberOfItems - 1;
			});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

		ComboBoxItem selectedItem;
		await RunOnUIThread(async () =>

			{
				// Get the selected item
				selectedItem = (ComboBoxItem)(comboBox.SelectedItem);

				// Get the bounds of the item
				Rect selectedItemBounds = await ControlHelper.GetBounds(selectedItem);
				LOG_OUTPUT("selectedItemBounds: (%f, %f, %f, %f)", selectedItemBounds.X, selectedItemBounds.Y, selectedItemBounds.Width, selectedItemBounds.Height);

				// Ensure that the bounds of the item are rendered within the height of the window,
				// unless windowed popups are enabled, in which case we're fine with it appearing outside.
				//if (PopupHelper.AreWindowedPopupsEnabled())
				//{
				//	VERIFY_IS_GREATER_THAN(selectedItemBounds.Y + selectedItemBounds.Height, heightOfWindow);
				//}
				//else
				{
					VERIFY_IS_LESS_THAN_OR_EQUAL(selectedItemBounds.Y + selectedItemBounds.Height, heightOfWindow);
				}
			});

		await ComboBoxHelper.CloseComboBox(comboBox, ComboBoxHelper.CloseMethod.Programmatic);
	}

	//[TestMethod]
	//public async Task ValidateOpenedDropDownHeightUsingMouse()
	//{
	//	ComboBox comboBoxTop;
	//	ComboBox comboBoxCenter;
	//	ComboBox comboBoxBottom;
	//	Rect scrollViewerTopBounds = default;
	//	Rect scrollViewerCenterBounds = default;
	//	Rect scrollViewerBottomBounds = default;

	//	await RunOnUIThread(() =>

	//		{
	//			var rootPanel = Grid > (XamlReader.Load(
	//				LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" >



	//						< ComboBox x: Name = "comboBoxTop" Width = "300" HorizontalAlignment = "Center" VerticalAlignment = "Top" >





	//							< ComboBoxItem Content = "item one" />





	//							< ComboBoxItem Content = "item two" />





	//							< ComboBoxItem Content = "item three" />





	//							< ComboBoxItem Content = "item four" />





	//							< ComboBoxItem Content = "item five" />





	//						</ ComboBox >





	//						< ComboBox x: Name = "comboBoxCenter" Width = "300" HorizontalAlignment = "Center" VerticalAlignment = "Center" >





	//							< ComboBoxItem Content = "item one" />





	//							< ComboBoxItem Content = "item two" />





	//							< ComboBoxItem Content = "item three" />





	//							< ComboBoxItem Content = "item four" />





	//							< ComboBoxItem Content = "item five" />





	//						</ ComboBox >





	//						< ComboBox x: Name = "comboBoxBottom" Width = "300" HorizontalAlignment = "Center" VerticalAlignment = "Bottom" >





	//							< ComboBoxItem Content = "item one" />





	//							< ComboBoxItem Content = "item two" />





	//							< ComboBoxItem Content = "item three" />





	//							< ComboBoxItem Content = "item four" />





	//							< ComboBoxItem Content = "item five" />





	//						</ ComboBox >





	//					</ Grid >)"));




	//			comboBoxTop = ComboBox > (rootPanel.FindName("comboBoxTop"));
	//			comboBoxCenter = ComboBox > (rootPanel.FindName("comboBoxCenter"));
	//			comboBoxBottom = ComboBox > (rootPanel.FindName("comboBoxBottom"));

	//			TestServices.WindowHelper.WindowContent = rootPanel;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBoxTop, ComboBoxHelper.OpenMethod.Mouse);
	//	await RunOnUIThread(() =>

	//		{
	//			var popup = TreeHelper.GetVisualChildByType<Popup>(comboBoxTop);
	//			var popupChild = (FrameworkElement)(popup.Child);
	//			var scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>(popupChild);
	//			scrollViewerTopBounds = ControlHelper.GetBounds(scrollViewer);
	//		});
	//	await ComboBoxHelper.CloseComboBox(comboBoxTop);

	//	await ComboBoxHelper.OpenComboBox(comboBoxCenter, ComboBoxHelper.OpenMethod.Mouse);
	//	await RunOnUIThread(() =>

	//		{
	//			var popup = TreeHelper.GetVisualChildByType<Popup>(comboBoxCenter);
	//			var popupChild = (FrameworkElement)(popup.Child);
	//			var scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>(popupChild);
	//			scrollViewerCenterBounds = ControlHelper.GetBounds(scrollViewer);
	//		});
	//	await ComboBoxHelper.CloseComboBox(comboBoxCenter);

	//	await ComboBoxHelper.OpenComboBox(comboBoxBottom, ComboBoxHelper.OpenMethod.Mouse);
	//	await RunOnUIThread(() =>

	//		{
	//			var popup = TreeHelper.GetVisualChildByType<Popup>(comboBoxBottom);
	//			var popupChild = (FrameworkElement)(popup.Child);
	//			var scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>(popupChild);
	//			scrollViewerBottomBounds = ControlHelper.GetBounds(scrollViewer);

	//			VERIFY_IS_TRUE(scrollViewerTopBounds.Height == scrollViewerCenterBounds.Height);
	//			VERIFY_IS_TRUE(scrollViewerCenterBounds.Height == scrollViewerBottomBounds.Height);
	//			VERIFY_IS_TRUE(scrollViewerBottomBounds.Height == scrollViewerTopBounds.Height);
	//		});
	//	await ComboBoxHelper.CloseComboBox(comboBoxBottom);
	//}

	//[TestMethod]
	//public async Task ValidateFocusTrappedWithNoSelectionPressingGamepadUp()
	//{
	//	Size size = new(400, 400);
	//	TestServices.WindowHelper.SetWindowSizeOverride(size);

	//	ComboBox comboBox = null;
	//	Button focusableButton = null;

	//	await RunOnUIThread(() =>

	//		{
	//			var rootPanel = (Grid)(XamlReader.Load(
	//				"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"





	//				"  <ScrollViewer>"





	//				"    <StackPanel>"





	//				"      <Button x:Name='focusableButton' Content='Focusable Button' />"





	//				"      <Grid Height='1000' Background='LightBlue' />"





	//				"      <ComboBox x:Name='comboBox'>"





	//				"        <ComboBoxItem Content='item one' />"





	//				"        <ComboBoxItem Content='item two' />"





	//				"        <ComboBoxItem Content='item three' />"





	//				"        <ComboBoxItem Content='item four' />"





	//				"      </ComboBox>"





	//				"    </StackPanel>"





	//				"  </ScrollViewer>"





	//				"</Grid>"));

	//			comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
	//			VERIFY_IS_NOT_NULL(comboBox);

	//			focusableButton = (Button)(rootPanel.FindName("focusableButton"));
	//			VERIFY_IS_NOT_NULL(focusableButton);

	//			TestServices.WindowHelper.WindowContent = rootPanel;
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

	//	// Even though the ComboBox does not have a selected item, pressing up should /not/
	//	// go to the focusable button, but rather remain trapped in the ComboBox.
	//	TestServices.KeyboardHelper.GamepadDpadUp();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Verify that the focusable button does not have focus.
	//	await RunOnUIThread(() =>

	//		{
	//			VERIFY_IS_TRUE(focusableButton.FocusState == FocusState.Unfocused);
	//		});

	//	await ComboBoxHelper.CloseComboBox(comboBox, ComboBoxHelper.CloseMethod.Programmatic);
	//}

	//[TestMethod]
	//public async Task ValidateTriggerKeyFocusNavigation()
	//{


	//	int totalNumberOfItems = 9;
	//	int expectedNumberOfItemsToScrollWithTriggers = 8;

	//	ComboBox comboBox = await SetupBasicComboBoxTest(totalNumberOfItems, false /* adjustMargin */);

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.SelectedIndex = 0;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

	//	// Pressing the trigger key should not move selection, only focus
	//	LOG_OUTPUT("Changing selection with RightTrigger");
	//	TestServices.KeyboardHelper.GamepadRightTrigger();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

	//	// Pressing Gamepad A "commits" the selection
	//	await TestServices.KeyboardHelper.GamepadA();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Verify that selection has now changed and the proper item has been selected
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, expectedNumberOfItemsToScrollWithTriggers);

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

	//	// Pressing the trigger will move focus to the first item, but not change selection
	//	LOG_OUTPUT("Changing selection with LeftTrigger");
	//	TestServices.KeyboardHelper.GamepadLeftTrigger();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, expectedNumberOfItemsToScrollWithTriggers);

	//	// Pressing Gamepad A "commits" the selection
	//	await TestServices.KeyboardHelper.GamepadA();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// The selection is now the first item
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);
	//}

	//[TestMethod]
	//public async Task ValidateTriggerKeysDoNotChangeSelection()
	//{


	//	int totalNumberOfItems = 5;

	//	ComboBox comboBox = await SetupBasicComboBoxTest(totalNumberOfItems);

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.SelectedIndex = 1;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);

	//	// Selection starts on item at index 1
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

	//	// Pressing the trigger keys moves focus to item 5, but not selection
	//	LOG_OUTPUT("Changing selection with RightTrigger");
	//	TestServices.KeyboardHelper.GamepadRightTrigger();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

	//	// Similarly, pressing the dpad should also not move selection
	//	LOG_OUTPUT("Changing selection with GamepadDpadUp");
	//	TestServices.KeyboardHelper.GamepadDpadUp();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

	//	LOG_OUTPUT("Changing selection with LeftTrigger");
	//	TestServices.KeyboardHelper.GamepadLeftTrigger();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

	//	await ComboBoxHelper.CloseComboBox(comboBox, ComboBoxHelper.CloseMethod.Programmatic);

	//	// Finally, since we close the ComboBox Programmatically and not with A, selection remains on item 1
	//	await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);
	//}

	//[TestMethod]
	//public async Task ValidateOpenedDropDownHeightWithMouseAfterTouchInput()
	//{
	//	TestServices.WindowHelper.SetWindowSizeOverride(new Size(600, 600));
	//	WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

	//	// Validate the ComboBox DComp output when ComboBox is opened with the mouse input
	//	// after opened/closed the ComboBox with the touch input.
	//	var comboBox = await SetupBasicComboBoxTest(20 /* itemSize */, false /* adjustMargin */);

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.Height = 50;
	//			comboBox.VerticalAlignment = VerticalAlignment.Center;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//	await ComboBoxHelper.CloseComboBox(comboBox);

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Touch);
	//	await ComboBoxHelper.CloseComboBox(comboBox);

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Mouse);
	//	TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	[TestMethod]
	public async Task ValidateOpeningWithPendingClosedEventDoesNotCloseComboBox()
	{
		var comboBox = await SetupBasicComboBoxTest();

		var dropDownClosedEvent = new Event();
		var dropDownClosedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");

		dropDownClosedRegistration.Attach(
			comboBox,
			async (s, e) =>
			{
				dropDownClosedRegistration.Detach();
				dropDownClosedEvent.Set();

				// In order to ensure that we've reopened the ComboBox's drop-down before
				// Popup.Closed is handled, we'll set IsDropDownOpen to true inside the handler
				// for DropDownClosed.  DropDownClosed is raised at the same time that we
				// initially close the Popup, meaning that Popup.Closed will have been raised
				// at this point, but not yet handled.
				await RunOnUIThread(() =>
				{
					comboBox.IsDropDownOpen = true;
				});
			});

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
		await ComboBoxHelper.CloseComboBox(comboBox);

		await dropDownClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_IS_TRUE(comboBox.IsDropDownOpen);
			});

		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	[TestMethod]
	public async Task CanSetIsDropDownOpenBeforeTemplateIsAppliedAndGotFocus()
	{
		ComboBox comboBox = null;
		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<ComboBox, RoutedEventHandler>("Loaded");

		await RunOnUIThread(() =>

			{
				comboBox = new ComboBox();
				comboBox.PlaceholderText = "Placeholder Text";
				comboBox.Items.Add("Item #1");
				comboBox.Items.Add("Item #2");
				comboBox.Items.Add("Item #3");

				// We set IsDropDownOpen to true before the template is applied.
				// ComboBox's behavior is to close (IsDropDownOpen == false) when
				// the template gets eventually applied.
				// In doing so, it should correctly restore the "placeholder"
				// in the template.
				// The focus visual state targets the placeholder text and,
				// if it's not there, it will fail to resolve and we raise an
				// exception.
				comboBox.IsDropDownOpen = true;

				loadedRegistration.Attach(comboBox, (s, e) =>
				{
					LOG_OUTPUT("ComboBox loaded");
					comboBox.Focus(FocusState.Keyboard);
					loadedEvent.Set();
				});

				TestServices.WindowHelper.WindowContent = comboBox;
			});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

		{
			VERIFY_IS_FALSE(comboBox.IsDropDownOpen);
		});
	}

	//[TestMethod]
	//public async Task DoesNotChangeSelectedIndexWithHorizontalArrowKeys()
	//{
	//	KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind.None);

	//	uint numItems = 3;
	//	ComboBox comboBox = await SetupBasicComboBoxTest(numItems);

	//	Event selectionChangedEvent;
	//	var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);
	//	selectionChangedRegistration.Attach(comboBox, [&]() { selectionChangedEvent.Set(); });

	//	await RunOnUIThread(() =>

	//		{
	//			WEX.Common.Throw.If(comboBox.SelectedIndex != -1, E_FAIL, "Nothing should be selected by default.");

	//			// Make sure the ComboBox has keyboard focus before trying to interact.
	//			comboBox.Focus(FocusState.Keyboard);
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Press Right %d times, once for each item . SelectedIndex should not change.", numItems);
	//	for (int i = 0; i < numItems; ++i)
	//	{
	//		await CommonInputHelper.Right(InputDevice.Keyboard);
	//		await TestServices.WindowHelper.WaitForIdle();

	//		VERIFY_IS_FALSE(selectionChangedEvent.HasFired());
	//		await RunOnUIThread(() =>

	//			{
	//				VERIFY_ARE_EQUAL(-1, comboBox.SelectedIndex);
	//			});
	//	}

	//	LOG_OUTPUT("Press Left %d times, once for each item . SelectedIndex should not change.", numItems);
	//	for (int i = 0; i < numItems; ++i)
	//	{
	//		await CommonInputHelper.Left(InputDevice.Keyboard);
	//		await TestServices.WindowHelper.WaitForIdle();

	//		VERIFY_IS_FALSE(selectionChangedEvent.HasFired());
	//		await RunOnUIThread(() =>

	//				{
	//					VERIFY_ARE_EQUAL(-1, comboBox.SelectedIndex);
	//				});
	//	}
	//}

	//	[TestMethod]
	//	public async Task CanHorizontallyNavigatePastAComboBox()
	//	{
	//		KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind.None);

	//		ComboBox comboBox = null;
	//		Button firstButton = null;
	//		Button lastButton = null;

	//		Event gotFocusEvent;
	//		var gotFocusRegistration = CreateSafeEventRegistration(StackPanel, GotFocus);

	//		await RunOnUIThread(() =>

	//			{
	//			comboBox = new ComboBox();

	//			firstButton = new Button();
	//			firstButton.Content = "First Button";

	//			lastButton = new Button();
	//			lastButton.Content = "Last Button";

	//			var root = new StackPanel();
	//			root.HorizontalAlignment = HorizontalAlignment.Center;
	//			root.VerticalAlignment = VerticalAlignment.Center;
	//			root.Orientation = Orientation.Horizontal;
	//			root.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

	//			root.Children.Add(firstButton);
	//			root.Children.Add(comboBox);
	//			root.Children.Add(lastButton);

	//			gotFocusRegistration.Attach(root, [&]() { gotFocusEvent.Set(); });

	//		TestServices.WindowHelper.WindowContent = root;
	//	});
	//await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		// Make sure the first button has keyboard focus before trying to interact.
	//		firstButton.Focus(FocusState.Keyboard);
	//	});
	//await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Move Right 2 times, which should move past the ComboBox and land on the last button.");
	//	await CommonInputHelper.Right(InputDevice.Keyboard);
	//await TestServices.WindowHelper.WaitForIdle();
	//	gotFocusEvent.WaitForDefault();

	//await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(comboBox));
	//	});

	//await CommonInputHelper.Right(InputDevice.Keyboard);
	//await TestServices.WindowHelper.WaitForIdle();
	//	gotFocusEvent.WaitForDefault();

	//await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(lastButton));
	//	});

	//LOG_OUTPUT("Move Left 2 times, which should move past the ComboBox and land back on the first button.");
	//	await CommonInputHelper.Left(InputDevice.Keyboard);
	//await TestServices.WindowHelper.WaitForIdle();
	//	gotFocusEvent.WaitForDefault();

	//await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(comboBox));
	//	});

	//await CommonInputHelper.Left(InputDevice.Keyboard);
	//await TestServices.WindowHelper.WaitForIdle();
	//	gotFocusEvent.WaitForDefault();

	//await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(firstButton));
	//	});
	//}

	//[TestMethod]
	//public async Task DoesRevertSelectionOnCancel()
	//{
	//	KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind.None);

	//	uint numItems = 3;
	//	ComboBox comboBox = await SetupBasicComboBoxTest(numItems);

	//	int expectedSelectedIndex = 1;

	//	Event selectionChangedEvent;
	//	var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);
	//	selectionChangedRegistration.Attach(comboBox, [&]() { selectionChangedEvent.Set(); });

	//	await RunOnUIThread(() =>

	//		{
	//			// Set a selected index that we'll verify gets reverted back to.
	//			comboBox.SelectedIndex = expectedSelectedIndex;

	//			// Make sure SelectedIndex will change as we navigate through the items.
	//			comboBox.SelectionChangedTrigger = ComboBoxSelectionChangedTrigger.Always;

	//			// Make sure the ComboBox has keyboard focus before trying to interact.
	//			comboBox.Focus(FocusState.Keyboard);
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var runScenario = [&](std.function < void() > cancelAction)


	//	{
	//		FocusTestHelper.EnsureFocus(comboBox, FocusState.Keyboard);

	//		await RunOnUIThread(() =>

	//			{
	//				comboBox.IsDropDownOpen = true;
	//			});
	//		await TestServices.WindowHelper.WaitForIdle();

	//		LOG_OUTPUT("Press down to change the selection.");
	//		await CommonInputHelper.Down(InputDevice.Keyboard);
	//		await TestServices.WindowHelper.WaitForIdle();
	//		selectionChangedEvent.WaitForDefault();

	//		LOG_OUTPUT("Cancel the selection.");
	//		cancelAction();
	//		await TestServices.WindowHelper.WaitForIdle();
	//		selectionChangedEvent.WaitForDefault();

	//		await RunOnUIThread(() =>

	//			{
	//				VERIFY_ARE_EQUAL(expectedSelectedIndex, comboBox.SelectedIndex);
	//			});
	//		await TestServices.WindowHelper.WaitForIdle();
	//	};

	//	LOG_OUTPUT("Validate reverting selection on cancel using Escape key.");
	//	runScenario([]() { await CommonInputHelper.Cancel(InputDevice.Keyboard); });

	//	LOG_OUTPUT("Validate reverting selection on cancel using GamepadB.");
	//	runScenario([]() { await CommonInputHelper.Cancel(InputDevice.Gamepad); });

	//	LOG_OUTPUT("Validate reverting selection on cancel by clicking outside with mouse.");
	//	runScenario([&]() { await ComboBoxHelper.CloseComboBox(comboBox, ComboBoxHelper.CloseMethod.Mouse); });

	//	LOG_OUTPUT("Validate reverting selection on cancel by tapping outside.");
	//	runScenario([&]() { await ComboBoxHelper.CloseComboBox(comboBox, ComboBoxHelper.CloseMethod.Touch); });

	//	LOG_OUTPUT("Validate reverting selection on cancel using the Back-button.");
	//	runScenario([]() {
	//		bool backButtonPressHandled = false;
	//		TestServices.Utilities.InjectBackButtonPress(&backButtonPressHandled);
	//		WEX.Common.Throw.IfFalse(backButtonPressHandled, E_FAIL, "Back button should have been handled.");
	//	});
	//}

	//[TestMethod]
	//public async Task DoesRevertSelectionOnCancelWithManyItems()
	//{
	//	KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind.None);

	//	uint numItems = 50.0;
	//	ComboBox comboBox = await SetupBasicComboBoxTest(numItems);

	//	int expectedSelectedIndex = 1;

	//	Event selectionChangedEvent;
	//	var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);
	//	selectionChangedRegistration.Attach(comboBox, [&]() { selectionChangedEvent.Set(); });

	//	await RunOnUIThread(() =>

	//		{
	//			// Set a selected index that we'll verify gets reverted back to.
	//			comboBox.SelectedIndex = expectedSelectedIndex;

	//			// Make sure SelectedIndex will change as we navigate through the items.
	//			comboBox.SelectionChangedTrigger = ComboBoxSelectionChangedTrigger.Always;

	//			// Make sure the ComboBox has keyboard focus before trying to interact.
	//			comboBox.Focus(FocusState.Keyboard);

	//			comboBox.IsDropDownOpen = true;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Press End to change the selection to the last item.");
	//	TestServices.KeyboardHelper.End();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	selectionChangedEvent.WaitForDefault();

	//	LOG_OUTPUT("Cancel the selection.");
	//	await CommonInputHelper.Cancel(InputDevice.Keyboard);
	//	await TestServices.WindowHelper.WaitForIdle();
	//	selectionChangedEvent.WaitForDefault();

	//	await RunOnUIThread(() =>

	//		{
	//			VERIFY_ARE_EQUAL(expectedSelectedIndex, comboBox.SelectedIndex);
	//		});
	//}

	//[TestMethod]
	//public async Task ValidateSelectionChangedTrigger()
	//{
	//	int numItems = 3;
	//	ComboBox comboBox = await SetupBasicComboBoxTest(numItems);

	//	Event selectionChangedEvent;
	//	var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);
	//	selectionChangedRegistration.Attach(comboBox, [&]() { selectionChangedEvent.Set(); });

	//	await RunOnUIThread(() =>

	//		{
	//			// Validate the default value for SelectionChangedTrigger is Committed.
	//			VERIFY_ARE_EQUAL(ComboBoxSelectionChangedTrigger.Committed, comboBox.SelectionChangedTrigger);
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var runScenario = [&](InputDevice inputDevice, ComboBoxSelectionChangedTrigger trigger)


	//	{
	//		FocusTestHelper.EnsureFocus(comboBox, FocusState.Keyboard);

	//		await RunOnUIThread(() =>

	//			{
	//				comboBox.SelectedIndex = -1;
	//				comboBox.SelectionChangedTrigger = trigger;
	//			});

	//		selectionChangedEvent.Reset();

	//		// Open the ComboBox.
	//		await CommonInputHelper.Accept(inputDevice);
	//		await TestServices.WindowHelper.WaitForIdle();

	//		LOG_OUTPUT("Navigate through the items.");
	//		for (int i = 0; i < numItems; ++i)
	//		{
	//			await CommonInputHelper.Down(inputDevice);
	//			await TestServices.WindowHelper.WaitForIdle();

	//			if (trigger == ComboBoxSelectionChangedTrigger.Always)
	//			{
	//				LOG_OUTPUT("Wait for the SelectionChanged event to fire.");
	//				selectionChangedEvent.WaitForDefault();
	//			}
	//			else
	//			{
	//				VERIFY_IS_FALSE(selectionChangedEvent.HasFired());
	//			}
	//		}

	//		selectionChangedEvent.Reset();

	//		LOG_OUTPUT("Accept the last item as the new selection");
	//		await CommonInputHelper.Accept(inputDevice);
	//		await TestServices.WindowHelper.WaitForIdle();

	//		if (trigger == ComboBoxSelectionChangedTrigger.Committed)
	//		{
	//			LOG_OUTPUT("Wait for the SelectionChanged event to fire.");
	//			selectionChangedEvent.WaitForDefault();
	//		}
	//		else
	//		{
	//			VERIFY_IS_FALSE(selectionChangedEvent.HasFired());
	//		}

	//		await RunOnUIThread(() =>

	//			{
	//				VERIFY_ARE_EQUAL((int)(numItems - 1), comboBox.SelectedIndex);
	//			});
	//		await TestServices.WindowHelper.WaitForIdle();
	//	};

	//	LOG_OUTPUT("Run scenario with keyboard input and SelectionChangedTrigger = Committed.");
	//	runScenario(InputDevice.Keyboard, ComboBoxSelectionChangedTrigger.Committed);

	//	LOG_OUTPUT("Run scenario with keyboard input and SelectionChangedTrigger = Always.");
	//	runScenario(InputDevice.Keyboard, ComboBoxSelectionChangedTrigger.Always);

	//	LOG_OUTPUT("Run scenario with gamepad input and SelectionChangedTrigger = Committed.");
	//	runScenario(InputDevice.Gamepad, ComboBoxSelectionChangedTrigger.Committed);

	//	LOG_OUTPUT("Run scenario with gamepad input and SelectionChangedTrigger = Always.");
	//	runScenario(InputDevice.Gamepad, ComboBoxSelectionChangedTrigger.Always);
	//}

	//void DoHomeAndEndChangeSelectionWhenClosed()
	//{
	//	KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind.None);

	//	uint numItems = 3;
	//	ComboBox comboBox = await SetupBasicComboBoxTest(numItems);

	//	Event selectionChangedEvent;
	//	var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);
	//	selectionChangedRegistration.Attach(comboBox, [&]() { selectionChangedEvent.Set(); });

	//	await RunOnUIThread(() =>

	//		{
	//			// Make sure the ComboBox has keyboard focus before trying to interact.
	//			comboBox.Focus(FocusState.Keyboard);
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Press the Home key . the first item should get selected.");
	//	TestServices.KeyboardHelper.Home();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	selectionChangedEvent.WaitForDefault();

	//	await RunOnUIThread(() =>

	//		{
	//			VERIFY_ARE_EQUAL(0, comboBox.SelectedIndex);
	//		});

	//	LOG_OUTPUT("Press the End key . the last item should get selected.");
	//	TestServices.KeyboardHelper.End();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	selectionChangedEvent.WaitForDefault();

	//	await RunOnUIThread(() =>

	//		{
	//			VERIFY_ARE_EQUAL((int)(numItems - 1), comboBox.SelectedIndex);
	//		});
	//}

	//	[TestMethod]
	//	public async Task ValidateSelectedInfoPropertiesStayInSync()
	//	{


	//		KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind.None);

	//		ComboBox comboBox = null;

	//		Event selectionChangedEvent;
	//		var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);

	//		std.array < PersonObject ^, 3 > personObjects = {
	//			new PersonObject("Roger", "Federer"),
	//            new PersonObject("Cristiano", "Ronaldo"),
	//            new PersonObject("LeBron", "James")


	//		};

	//		await RunOnUIThread(() =>

	//			{
	//			comboBox = new ComboBox();

	//			foreach (var personObject in personObjects)
	//			{
	//				comboBox.Items.Add(personObject);
	//			}

	//			comboBox.DisplayMemberPath = "FirstName";
	//			comboBox.SelectedValuePath = "LastName";

	//			selectionChangedRegistration.Attach(comboBox, [&]() { selectionChangedEvent.Set(); });

	//		TestServices.WindowHelper.WindowContent = comboBox;
	//	});
	//await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		VERIFY_ARE_EQUAL(personObjects.size(), comboBox.Items.Size);

	//		LOG_OUTPUT("Validate that all Selected info properties stay in sync when changing SelectedIndex.");
	//		for (int i = 0; i < (int)(personObjects.size()); ++i)
	//		{
	//			comboBox.SelectedIndex = i;
	//			VERIFY_ARE_EQUAL(personObjects[i], comboBox.SelectedItem);
	//			VERIFY_ARE_EQUAL(PersonObject ^> (comboBox.Items[i]).LastName, string > (comboBox.SelectedValue));
	//		}

	//		LOG_OUTPUT("Validate that all Selected info properties stay in sync when changing SelectedItem.");
	//		for (int i = 0; i < (int)(personObjects.size()); ++i)
	//		{
	//			comboBox.SelectedItem = personObjects[i];
	//			VERIFY_ARE_EQUAL(i, comboBox.SelectedIndex);
	//			VERIFY_ARE_EQUAL(PersonObject ^> (comboBox.Items[i]).LastName, string > (comboBox.SelectedValue));
	//		}

	//		LOG_OUTPUT("Validate that all Selected info properties stay in sync when changing SelectedValue.");
	//		for (int i = 0; i < (int)(personObjects.size()); ++i)
	//		{
	//			comboBox.SelectedValue = personObjects[i].LastName;
	//			VERIFY_ARE_EQUAL(i, comboBox.SelectedIndex);
	//			VERIFY_ARE_EQUAL(personObjects[i], comboBox.SelectedItem);
	//		}
	//	});
	//}

	[TestMethod]
	public async Task DoesNotShowMulitpleSelectionVisuals()
	{
		ComboBox comboBox = await SetupBasicComboBoxTest(2);
		ComboBoxItem firstComboBoxItem = null;
		ComboBoxItem secondComboBoxItem = null;

		await RunOnUIThread(() =>

			{
				Assert.AreEqual(ComboBoxSelectionChangedTrigger.Committed, comboBox.SelectionChangedTrigger, "Expected that the ComboBox is in commit mode.");

				comboBox.Focus(FocusState.Keyboard);
			});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Press SPACE to open the ComboBox.");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				firstComboBoxItem = (ComboBoxItem)(comboBox.Items[0]);
				secondComboBoxItem = (ComboBoxItem)(comboBox.Items[1]);
			});

		LOG_OUTPUT("Press DOWN to select the first item.");
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Press SPACE to commit the selection.");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Press DOWN to change the selected item to the second item.");
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Press SPACE to open the ComboBox again.");
		await TestServices.KeyboardHelper.Space();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>

			{
				VERIFY_IS_FALSE(await ControlHelper.IsInVisualState(firstComboBoxItem, "CommonStates", "Selected"));
				VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(secondComboBoxItem, "CommonStates", "Selected"));
			});

		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	[TestMethod]
	[Ignore("We are now layouting ComboBox Popup differently than WinUI. #17988")]
	public async Task VerifyItemBeforeVisibleItemsInPopupIsRealized()
	{
		ComboBox comboBox = null;

		await RunOnUIThread(() =>

			{
				comboBox = new ComboBox();
				comboBox.VerticalAlignment = VerticalAlignment.Center;

				var rootPanel = new Grid();
				rootPanel.Children.Add(comboBox);

				for (int i = 0; i < 100; i++)
				{
					var item = new ComboBoxItem();
					var stringItem = "ComboBoxItem";
					stringItem += i;
					item.Content = stringItem;
					item.Height = 40;
					item.Name = stringItem;
					comboBox.Items.Add(item);
				}

				// We choose a MaxDropDownHeight so that 7 items will fit in the popup:
				comboBox.MaxDropDownHeight = 320;

				// Set SelectedIndex to the last item:
				comboBox.SelectedIndex = 99;

				TestServices.WindowHelper.WindowContent = rootPanel;
			});
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Mouse);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>

			{
				var carouselPanel = TreeHelper.GetVisualChildByTypeFromOpenPopups<CarouselPanel>(comboBox);

				// Verify that the item before the first visible item has been realized (99-7=92).
				var item92 = (ComboBoxItem)(TreeHelper.GetVisualChildByName(carouselPanel, "ComboBoxItem92"));
				VERIFY_IS_NOT_NULL(item92, "Expected the item to be realized");

				// Verify that this item is not visible in the popup:
				var popupBorder = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("PopupBorder", comboBox));
				var popupBorderBounds = await ControlHelper.GetBounds(popupBorder);
				LOG_OUTPUT("popupBorder bounds: %f, %f, %f, %f", popupBorderBounds.Left, popupBorderBounds.Top, popupBorderBounds.Width, popupBorderBounds.Height);

				var item92Bounds = await ControlHelper.GetBounds(item92);
				LOG_OUTPUT("Item92 bounds: %f, %f, %f, %f", item92Bounds.Left, item92Bounds.Top, item92Bounds.Width, item92Bounds.Height);
				VERIFY_IS_FALSE(ControlHelper.IsContainedIn(item92Bounds, popupBorderBounds));
			});

		await ComboBoxHelper.CloseComboBox(comboBox);
	}

	[TestMethod]
	public async Task CanSetEditableMode()
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = null;

		await RunOnUIThread(() =>

			{
				var rootPanel = (Grid)(XamlReader.Load(
					""""
					<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
					  <ComboBox x:Name='comboBox' IsEditable='true'>
					      <ComboBoxItem Content='item one' />
					      <ComboBoxItem Content='item two' />
					      <ComboBoxItem Content='item three' />
					      <ComboBoxItem Content='item four' />
					  </ComboBox>
					</Grid>
					""""));

				comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
				VERIFY_IS_NOT_NULL(comboBox);

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBox.IsEditable, true);

				comboBox.IsEditable = false;
			});

		await TestServices.WindowHelper.WaitForIdle();


		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBox.IsEditable, false);
			});
	}

	[TestMethod]
	public async Task CanSetComboBoxTextOnNonEditableMode()
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = null;
		TextBox editableTextBox = null;

		await RunOnUIThread(() =>

			{
				var rootPanel = (Grid)(XamlReader.Load(
					""""
					<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
					  <ComboBox x:Name='comboBox'>
					      <ComboBoxItem Content='item one' />
					      <ComboBoxItem Content='item two' />
					      <ComboBoxItem Content='item three' />
					      <ComboBoxItem Content='item four' />
					  </ComboBox>
					</Grid>
					""""));

				comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
				VERIFY_IS_NOT_NULL(comboBox);

				comboBox.Text = "Test";

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				comboBox.IsEditable = true;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				editableTextBox = (TextBox)(TreeHelper.GetVisualChildByName(comboBox, "EditableText"));
				VERIFY_IS_NOT_NULL(editableTextBox);

				VERIFY_ARE_EQUAL(editableTextBox.Text, "Test");
			});
	}

	[TestMethod]
#if !HAS_UNO_WINUI
	[Ignore("This test is failing on UWP. #17988")]
#endif
	public async Task VerifyTabBehavior()
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		StackPanel stackPanel = null;
		Button button1 = null;
		ComboBox comboBox = null;
		TextBox textBox = null;
		Button button2 = null;

		await RunOnUIThread(() =>

			{
				button1 = new Button();
				comboBox = (ComboBox)(XamlReader.Load(
					""""
					<ComboBox x:Name='comboBox' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
					    <ComboBoxItem Content='item one' />
					    <ComboBoxItem Content='item two' />
					    <ComboBoxItem Content='item three' />
					    <ComboBoxItem Content='item four' />
					</ComboBox>
					""""));
				VERIFY_IS_NOT_NULL(comboBox);
				button2 = new Button();

				stackPanel = new StackPanel();
				stackPanel.Children.Add(button1);
				stackPanel.Children.Add(comboBox);
				stackPanel.Children.Add(button2);

				TestServices.WindowHelper.WindowContent = stackPanel;
			});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				textBox = (TextBox)(TreeHelper.GetVisualChildByName(comboBox, "EditableText"));
				VERIFY_IS_NOT_NULL(textBox);

				button1.Focus(FocusState.Programmatic);
			});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Initial focus should be on Button 1.");
		await VerifyFocusedElement(button1);

		LOG_OUTPUT("Pressing Tab should move focus to the ComboBox.");
		await TestServices.KeyboardHelper.Tab();
		await VerifyFocusedElement(comboBox);

		LOG_OUTPUT("Pressing Tab again should move focus to Button 2.");
		await TestServices.KeyboardHelper.Tab();
		await VerifyFocusedElement(button2);

		LOG_OUTPUT("Pressing Shift + Tab should move focus back to the ComboBox.");
		await TestServices.KeyboardHelper.ShiftTab();
		await VerifyFocusedElement(comboBox);

		LOG_OUTPUT("Pressing Shift + Tab once more should move focus back to Button1.");
		await TestServices.KeyboardHelper.ShiftTab();
		await VerifyFocusedElement(button1);

		await RunOnUIThread(() =>

			{
				comboBox.IsEditable = true;
			});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Initial focus should be on Button 1.");
		await VerifyFocusedElement(button1);

		LOG_OUTPUT("Pressing Tab should move focus to the TextBox inside the ComboBox.");
		await TestServices.KeyboardHelper.Tab();
		await VerifyFocusedElement(textBox);

		LOG_OUTPUT("Pressing Tab again should move focus to Button 2.");
		await TestServices.KeyboardHelper.Tab();
		await VerifyFocusedElement(button2);

		LOG_OUTPUT("Pressing Shift + Tab should move focus back to the TextBox inside the ComboBox.");
		await TestServices.KeyboardHelper.ShiftTab();
		await VerifyFocusedElement(textBox);

		LOG_OUTPUT("Pressing Shift + Tab once more should move focus back to Button1.");
		await TestServices.KeyboardHelper.ShiftTab();
		await VerifyFocusedElement(button1);
	}

	private async Task VerifyFocusedElement(object expectedFocusedElement)
	{
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>

			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(expectedFocusedElement));
			});
	}

	private async Task InjectKeySequence(List<string> keySequence, int timeBetweenKeyPressesInMs)
	{
		for (int i = 0; i < keySequence.Count; i++)
		{
			LOG_OUTPUT("ComboBox: Injecting key sequence");
			LOG_OUTPUT("%s", keySequence[i]);

			await TestServices.KeyboardHelper.PressKeySequence(keySequence[i]);

			await Task.Delay(timeBetweenKeyPressesInMs);
		}
	}

	private async Task EnsureEditableTextBoxHasFocus(ComboBox comboBox)
	{
		TextBox textBox = null;
		await RunOnUIThread(() =>
			{
				textBox = (TextBox)(TreeHelper.GetVisualChildByName(comboBox, "EditableText"));
				VERIFY_IS_NOT_NULL(textBox);
			});
		await FocusTestHelper.EnsureFocus(textBox, FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task CanRaiseTextSubmittedEventComboBoxClosed()
	{
		await CanRaiseTextSubmittedEventComboBox(false);
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task CanRaiseTextSubmittedEventComboBoxOpened()
	{
		await CanRaiseTextSubmittedEventComboBox(true);
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task ValidateItemAutoMatchOnTextSubmitted()
	{
		await CanRaiseTextSubmittedEventComboBox(false, true);
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task ValidateTextSubmittedHandledProperty()
	{
		await CanRaiseTextSubmittedEventComboBox(false, false, true);
	}

	private async Task CanRaiseTextSubmittedEventComboBox(bool open, bool addToItemSource = false, bool setHandled = false)
	{
		var comboBox = await SetupBasicComboBoxTest(5, true, true);
		ComboBoxItem comboBoxItem = null;

		var comboBoxTextSubmittedEvent = new Event();
		var textSubmittedRegistration = CreateSafeEventRegistration<ComboBox, TypedEventHandler<ComboBox, ComboBoxTextSubmittedEventArgs>>("TextSubmitted");

		textSubmittedRegistration.Attach(comboBox, async (s, args) =>
		{
			VERIFY_ARE_EQUAL(args.Text, "Custom Value");

			if (addToItemSource)
			{
				{
					comboBoxItem = new ComboBoxItem();
					comboBoxItem.Content = "Custom Value";

					comboBox.Items.Add(comboBoxItem);
				}
			}

			if (setHandled)
			{
				args.Handled = true;
			}

			comboBoxTextSubmittedEvent.Set();
		});

		await RunOnUIThread(() =>

			{
				if (setHandled)
				{
					// Set selection to something different than -1 so we can compare it stays the same.
					comboBox.SelectedIndex = 1;
				}

				if (open)
				{
					LOG_OUTPUT("OpenComboBox");
					comboBox.IsDropDownOpen = true; ;
				}
			});
		await TestServices.WindowHelper.WaitForIdle();

		await EnsureEditableTextBoxHasFocus(comboBox);

		await TestServices.WindowHelper.WaitForIdle();

		var keySequence = new List<string>(); ;
		keySequence.Add("C");
		keySequence.Add("u");
		keySequence.Add("s");
		keySequence.Add("t");
		keySequence.Add("o");
		keySequence.Add("m");
		keySequence.Add(" ");
		keySequence.Add("V");
		keySequence.Add("a");
		keySequence.Add("l");
		keySequence.Add("u");
		keySequence.Add("e");

		await InjectKeySequence(keySequence, 100);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for ComboBox TextSubmitted Event");
		await comboBoxTextSubmittedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				textSubmittedRegistration.Detach();
				if (addToItemSource)
				{
					VERIFY_ARE_EQUAL((ComboBoxItem)(comboBox.SelectedItem), comboBoxItem);
					VERIFY_ARE_EQUAL((ComboBoxItem)(comboBox.SelectedValue), comboBoxItem);
					VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 5);
				}
				else if (setHandled)
				{
					var testItem = comboBox.Items[1];
					VERIFY_ARE_EQUAL(comboBox.SelectedItem, testItem);
					VERIFY_ARE_EQUAL(comboBox.SelectedValue, testItem);
					VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 1);
				}
				else
				{
					VERIFY_ARE_EQUAL((comboBox.SelectedItem), "Custom Value");
					VERIFY_ARE_EQUAL((comboBox.SelectedValue), "Custom Value");
					VERIFY_ARE_EQUAL(comboBox.SelectedIndex, -1);
				}
			});
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task ValidateEditableModeSearchAndSelectionComboBoxClosed()
	{
		await ValidateEditableModeSearchAndSelection(false);
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task ValidateEditableModeSearchAndSelectionComboBoxOpened()
	{
		await ValidateEditableModeSearchAndSelection(true);
	}

	private async Task ValidateEditableModeSearchAndSelection(bool open)
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = null;

		await RunOnUIThread(() =>

			{
				var rootPanel = (Grid)(XamlReader.Load(
					""""
					<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
					  <ComboBox x:Name='comboBox' IsEditable='true'>
					      <ComboBoxItem Content='One' />
					      <ComboBoxItem Content='Two' />
					      <ComboBoxItem Content='Three' />
					      <ComboBoxItem Content='Four' />
					  </ComboBox>
					</Grid>
					""""));

				comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
				VERIFY_IS_NOT_NULL(comboBox);

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await EnsureEditableTextBoxHasFocus(comboBox);

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				if (open)
				{
					LOG_OUTPUT("OpenComboBox");
					comboBox.IsDropDownOpen = true; ;
				}
			});

		await TestServices.WindowHelper.WaitForIdle();

		var keySequence = new List<string>(); ;
		keySequence.Add("T");
		keySequence.Add("h");

		await InjectKeySequence(keySequence, 200);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				var testItem = comboBox.Items[2];
				VERIFY_ARE_EQUAL(comboBox.SelectedItem, testItem);
				VERIFY_ARE_EQUAL(comboBox.SelectedValue, testItem);
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 2);
			});
	}

	[TestMethod]
	public async Task ValidateEditableModeSelectedItemAndValueAreSetToNull()
	{


		var comboBox = await SetupBasicComboBoxTest(5, true, true);

		await RunOnUIThread(() =>

			{
				comboBox.Focus(FocusState.Keyboard);
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				comboBox.Text = "Custom Value";
			});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL((comboBox.SelectedItem), "Custom Value");
				VERIFY_ARE_EQUAL((comboBox.SelectedValue), "Custom Value");
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, -1);

				comboBox.IsEditable = false;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBox.SelectedItem, null);
				VERIFY_ARE_EQUAL(comboBox.SelectedValue, null);
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, -1);
			});
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__
	[Ignore("Focus does not behave correctly in this case because we move it asynchronously in TextBox.ProcessFocusChanged #17988")]
#endif
	public async Task ValidateEditableModeGamePadInteraction()
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = null;
		TextBox textBox = null;
		ComboBoxItem comboBoxItem1 = null;
		ComboBoxItem comboBoxItem2 = null;

		await RunOnUIThread(() =>

			{
				var rootPanel = (Grid)(XamlReader.Load(
					""""
					<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
					  <ComboBox x:Name='comboBox' IsEditable='true'>
					      <ComboBoxItem x:Name='cbi1' Content='One' />
					      <ComboBoxItem x:Name='cbi2' Content='Two' />
					      <ComboBoxItem x:Name='cbi3' Content='Three' />
					      <ComboBoxItem x:Name='cbi4' Content='Four' />
					  </ComboBox>
					</Grid>
					""""));

				comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
				VERIFY_IS_NOT_NULL(comboBox);

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				textBox = (TextBox)(TreeHelper.GetVisualChildByName(comboBox, "EditableText"));
				VERIFY_IS_NOT_NULL(textBox);

				// Programmatic/Keyboard/Pointer focus in Editable ComboBox moves the focus to the internal TextBox after the ComboBox is focused.
				// We later simulate a GamepadB press to return the focus to the ComboBox and open it.
				comboBox.Focus(FocusState.Programmatic);
			});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.GamepadB();
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.GamepadA();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				comboBoxItem1 = (ComboBoxItem)(TreeHelper.GetVisualChildByNameFromOpenPopups("cbi1", comboBox));
				comboBoxItem2 = (ComboBoxItem)(TreeHelper.GetVisualChildByNameFromOpenPopups("cbi2", textBox));
				VERIFY_IS_NOT_NULL(comboBoxItem1);
				VERIFY_IS_NOT_NULL(comboBoxItem2);
			});

		await TestServices.WindowHelper.WaitForIdle();

		await CommonInputHelper.Down(InputDevice.Gamepad);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBoxItem1.FocusState, FocusState.Keyboard);
			});

		await CommonInputHelper.Up(InputDevice.Gamepad);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				// Validate Focus can return to TextBox after moving up beyond first ComboBoxItem.
				VERIFY_ARE_EQUAL(textBox.FocusState, FocusState.Keyboard);
			});

		await CommonInputHelper.Down(InputDevice.Gamepad);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBoxItem2.FocusState, FocusState.Keyboard);
			});

		await TestServices.KeyboardHelper.GamepadA();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 1);
			});
	}

	[TestMethod]
	[RequiresFullWindow]
	[Ignore("We are now layouting ComboBox Popup differently than WinUI. #17988")]
	public async Task ValidateEditableModePopupOpensUp()
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = null;

		await RunOnUIThread(() =>

			{
				var rootPanel = (Grid)(XamlReader.Load(
					""""
					<Grid x:Name='root' VerticalAlignment="Stretch" xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
					  <ComboBox x:Name='comboBox' IsEditable='true' VerticalAlignment="Bottom">
					      <ComboBoxItem Content='One' />
					      <ComboBoxItem Content='Two' />
					      <ComboBoxItem Content='Three' />
					  </ComboBox>
					</Grid>
					""""));

				comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
				VERIFY_IS_NOT_NULL(comboBox);

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				comboBox.Focus(FocusState.Keyboard);
				comboBox.IsDropDownOpen = true;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				// Verify popup verticalOffset
				var popup = TreeHelper.GetVisualChildByType<Popup>(comboBox);
				VERIFY_ARE_EQUAL(popup.VerticalOffset, -105);

				// Verify correct margin is applied to canvas top.
				var popupBorder = TreeHelper.GetVisualChildByNameFromOpenPopups("PopupBorder", comboBox);
				VERIFY_IS_NOT_NULL(popupBorder);
				VERIFY_ARE_EQUAL(Canvas.GetTop(popupBorder), 1);
			});
	}

	[TestMethod]
	[Ignore("We don't yet handle ScrollIntoView in Selector scenarios correctly #17988")]
	public async Task ValidateEditableModeComboBoxCanScrollToNewItems()
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = null;

		await RunOnUIThread(() =>

			{
				// Non latin characters are needed as this consistently virtualizes the last items.
				var rootPanel = (Grid)(XamlReader.Load(
					""""
					<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
					  <ComboBox x:Name='comboBox' IsEditable='true'>
					      <ComboBox.Items>
					          <TextBlock>Text 1</TextBlock>
					          <TextBlock>Text 2</TextBlock>
					          <TextBlock>Text 3</TextBlock>
					          <TextBlock>SuuuuuuperUltraLargeText</TextBlock>
					          <TextBlock>Text 4</TextBlock>
					          <TextBlock>Text 5</TextBlock>
					          <TextBlock>Text 6</TextBlock>
					          <TextBlock>SuuuuuuperUltraLargeText2</TextBlock>
					          <TextBlock>Text 7</TextBlock>
					          <TextBlock>Text 8</TextBlock>
					          <TextBlock>Text 9</TextBlock>
					          <TextBlock>????????????</TextBlock>
					          <TextBlock>?????????</TextBlock>
					          <TextBlock>??????</TextBlock>
					          <TextBlock>???</TextBlock>
					          <TextBlock>??????</TextBlock>
					          <TextBlock>SuuuuuuperUltraLargeText3</TextBlock>
					      </ComboBox.Items>
					  </ComboBox>
					</Grid>
					""""));

				comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
				VERIFY_IS_NOT_NULL(comboBox);

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

		await TestServices.WindowHelper.WaitForIdle();

		var comboBoxTextSubmittedEvent = new Event();
		var textSubmittedRegistration = CreateSafeEventRegistration<ComboBox, TypedEventHandler<ComboBox, ComboBoxTextSubmittedEventArgs>>("TextSubmitted");

		textSubmittedRegistration.Attach(comboBox, async (s, args) =>
		{
			VERIFY_ARE_EQUAL(args.Text, "Value");

			await RunOnUIThread(() =>

				{
					// Add custom value.
					var comboBoxItem = new ComboBoxItem();
					comboBoxItem.Content = "Value";

					comboBox.Items.Add(comboBoxItem);
				});

			comboBoxTextSubmittedEvent.Set();
		});

		await RunOnUIThread(() =>

			{
				comboBox.Focus(FocusState.Keyboard);
				comboBox.Text = "Value";
			});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for ComboBox TextSubmitted Event");
		await comboBoxTextSubmittedEvent.WaitForDefault();

		await RunOnUIThread(() =>

			{
				textSubmittedRegistration.Detach();
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 17);

				LOG_OUTPUT("OpenComboBox");
				comboBox.IsDropDownOpen = true;
			});

		await TestServices.WindowHelper.WaitForIdle();

		// Selects first item of the list.
		await TestServices.KeyboardHelper.PressKeySequence("T");
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				comboBox.IsDropDownOpen = true;
			});

		await TestServices.WindowHelper.WaitForIdle();

		// Typing the first character of the custom value, this should trigger a scroll to the searched item.
		await TestServices.KeyboardHelper.PressKeySequence("V");
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				var popup = TreeHelper.GetVisualChildByType<Popup>(comboBox);
				var popupChild = (FrameworkElement)(popup.Child);
				var scrollViewer = TreeHelper.GetVisualChildByType<ScrollViewer>(popupChild);

				VERIFY_IS_NOT_NULL(scrollViewer);

				// Verify we have scrolled enough elements to have the last item visible. (Collection Size "18" - size of items visible in popup "15" = 3)
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 3);
			});
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task ValidateSettingSelectedIndexUpdatesTextBoxText()
	{
		var comboBox = await SetupBasicComboBoxTest(5, true, true);

		TextBox textBox = null;

		await RunOnUIThread(() =>

			{
				textBox = (TextBox)(TreeHelper.GetVisualChildByName(comboBox, "EditableText"));
				VERIFY_IS_NOT_NULL(textBox);
			});

		await EnsureEditableTextBoxHasFocus(comboBox);

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("C");
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(textBox.Text, "ComboBox Item 0");

				comboBox.SelectedIndex = 2;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(textBox.Text, "ComboBox Item 2");
			});
	}

	[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
	[Ignore("We cannot simulate keyboard input into focused TextBox on Android, iOS, and WASM #17220")]
#endif
	public async Task ValidateSelectionTriggerAlwaysCanSetCustomValueAgain()
	{
		Size size = new(400, 400);
		TestServices.WindowHelper.SetWindowSizeOverride(size);

		ComboBox comboBox = null;

		await RunOnUIThread(() =>

			{
				var rootPanel = (Grid)(XamlReader.Load(
					""""
					<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
					  <ComboBox x:Name='comboBox' IsEditable='true' SelectionChangedTrigger='Always'>
					      <ComboBoxItem Content='One' />
					      <ComboBoxItem Content='Two' />
					      <ComboBoxItem Content='Three' />
					      <ComboBoxItem Content='Four' />
					  </ComboBox>
					</Grid>
					""""));

				comboBox = (ComboBox)(rootPanel.FindName("comboBox"));
				VERIFY_IS_NOT_NULL(comboBox);

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await EnsureEditableTextBoxHasFocus(comboBox);

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("Hello");
		await TestServices.WindowHelper.WaitForIdle();

		// Set Hello as the active custom value.
		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL((comboBox.SelectedItem), "Hello");
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, -1);

				comboBox.IsDropDownOpen = true;
			});

		await TestServices.WindowHelper.WaitForIdle();

		// Arrow down through the ComboBox values, this will cause the SelectedIndex to change.
		await TestServices.KeyboardHelper.Down();
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 1);
			});

		await TestServices.KeyboardHelper.PressKeySequence("Hello");
		await TestServices.WindowHelper.WaitForIdle();

		// Try to commit Hello again.
		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL((comboBox.SelectedItem), "Hello");
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, -1);
			});
	}

	[TestMethod]
#if !__SKIA__ && !__WASM__
	[Ignore("We currently only support InputHelper.Tap properly on input injector targets. #17988")]
#endif
	public async Task ValidateDropDownArrowClosesPopupOnEditableComboBox()
	{
		var comboBox = await SetupBasicComboBoxTest(5, true, true);

		Border dropDownOverlay = null;

		await RunOnUIThread(() =>

			{
				dropDownOverlay = (Border)(TreeHelper.GetVisualChildByName(comboBox, "DropDownOverlay"));
				VERIFY_IS_NOT_NULL(dropDownOverlay);

				comboBox.Focus(FocusState.Keyboard);
				comboBox.IsDropDownOpen = true; ;
			});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBox.IsDropDownOpen, true);
			});

		await TestServices.KeyboardHelper.Down();
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(dropDownOverlay);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				VERIFY_ARE_EQUAL(comboBox.IsDropDownOpen, false);
				VERIFY_ARE_EQUAL(comboBox.SelectedIndex, 1);
			});
	}

	//[TestMethod]
	//public async Task ValidateDropDownOverlayVisuals()
	//{
	//	await ValidateDropDownOverlayVisualsHelper(ElementTheme.Dark, false /*useHighContrast*/, "Dark");
	//	await ValidateDropDownOverlayVisualsHelper(ElementTheme.Light, false /*useHighContrast*/, "Light");
	//	await ValidateDropDownOverlayVisualsHelper(ElementTheme.Light, true /*useHighContrast*/, "HC");
	//}

	//private async Task ValidateDropDownOverlayVisualsHelper(ElementTheme theme, bool useHighContrast, string variation)
	//{
	//	Size size = new(400, 400);
	//	TestServices.WindowHelper.SetWindowSizeOverride(size);

	//	ComboBox comboBox1 = null;
	//	ComboBox comboBox2 = null;
	//	ComboBox comboBox3 = null;

	//	var validationRules = new Platform.String(
	//		LR"(<?xml version='1.0' encoding='UTF-8'?>
	//			< Rules >



	//				< Rule Applicability =\"//Element[@Type='Microsoft.UI.Xaml.Controls.ComboBox']\" Inclusion='Blacklist'>
	//					< Property Name = 'FocusState' />



	//					< Property Name = 'IsSelectionBoxHighlighted' />



	//				</ Rule >



	//			</ Rules >)");



	//	await RunOnUIThread(() =>

	//	{
	//		var rootPanel = (Grid)(XamlReader.Load(
	//			"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"



	//			"  <StackPanel>"



	//			"      <ComboBox x:Name='comboBox1' Width='200' IsEditable='true' PlaceholderText='Select Option' />"



	//			"      <ComboBox x:Name='comboBox2' Width='200' IsEditable='true' PlaceholderText='Select Option' />"



	//			"      <ComboBox x:Name='comboBox3' Width='200' IsEditable='true' PlaceholderText='Select Option' />"



	//			"  </StackPanel>"



	//			"</Grid>"));

	//		comboBox1 = (ComboBox)(rootPanel.FindName("comboBox1"));
	//		comboBox2 = (ComboBox)(rootPanel.FindName("comboBox2"));
	//		comboBox3 = (ComboBox)(rootPanel.FindName("comboBox3"));

	//		rootPanel.RequestedTheme = theme;

	//		if (useHighContrast)
	//		{
	//			TestServices.ThemingHelper.HighContrastTheme = HighContrastTheme.Black;
	//		}
	//		else
	//		{
	//			TestServices.ThemingHelper.HighContrastTheme = HighContrastTheme.None;
	//		}

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox1.Focus(FocusState.Keyboard);
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//		{
	//			VisualStateManager.GoToState(comboBox2, "TextBoxOverlayPointerOver", false);
	//			VisualStateManager.GoToState(comboBox3, "TextBoxOverlayPressed", false);
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.Utilities.VerifyUIElementTreeWithRulesInline(variation + "1", validationRules);

	//	await RunOnUIThread(() =>

	//		{
	//			VisualStateManager.GoToState(comboBox1, "TextBoxFocusedOverlayPointerOver", false);
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.Utilities.VerifyUIElementTreeWithRulesInline(variation + "2", validationRules);

	//	await RunOnUIThread(() =>

	//		{
	//			VisualStateManager.GoToState(comboBox1, "TextBoxFocusedOverlayPressed", false);
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.Utilities.VerifyUIElementTreeWithRulesInline(variation + "3", validationRules);
	//}

	//void LightDismissLayerOnIslands()
	//{
	//	WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree);

	//	// Validate the ComboBox DComp output when ComboBox is opened with the mouse input
	//	// after opened/closed the ComboBox with the touch input.
	//	var comboBox = await SetupBasicComboBoxTest(2 /* numberOfItems */, false /* adjustMargin */);

	//	await RunOnUIThread(() =>

	//		{
	//			comboBox.Height = 50;
	//			comboBox.VerticalAlignment = VerticalAlignment.Center;
	//		});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Mouse);

	//	await RunOnUIThread(() =>

	//		{
	//			LOG_OUTPUT("> There should be exactly one popup open...");
	//			XamlRoot ^ xamlRoot = comboBox.XamlRoot;
	//			var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);
	//			VERIFY_ARE_EQUAL(openPopups.Size, 1u);

	//			LOG_OUTPUT("> ...with a Canvas as its Popup.Child...");
	//			var popup = openPopups.GetAt(0.0;
	//			var canvas = Canvas ^> (popup.Child);
	//			VERIFY_IS_NOT_NULL(canvas);

	//			LOG_OUTPUT("> ...and that Canvas should have only a Border inside it and no light dismiss children.");
	//			VERIFY_ARE_EQUAL(canvas.Children.Size, 1u);
	//			var border = Border ^> (canvas.Children[0]);
	//			VERIFY_IS_NOT_NULL(border);
	//		});

	//	await ComboBoxHelper.CloseComboBox(comboBox);
	//}

	//	[TestMethod]
	//	public async Task CanDisableShadow()
	//	{


	//		var comboBox = await SetupBasicComboBoxTest(5, true, true);

	//		await RunOnUIThread(() =>

	//			{
	//				LOG_OUTPUT("Open the ComboBox");
	//				comboBox.IsDropDownOpen = true;
	//			});

	//		await TestServices.WindowHelper.WaitForIdle();

	//		var findShadow = [&]()


	//		{
	//			var shadowTarget = TreeHelper.GetVisualChildByNameFromOpenPopups("PopupBorder", comboBox);
	//			return shadowTarget.Shadow;
	//		};

	//		await RunOnUIThread(() =>

	//			{
	//				LOG_OUTPUT("Make sure it has a shadow");
	//				var shadow = findShadow();
	//				VERIFY_IS_NOT_NULL(shadow);

	//				LOG_OUTPUT("Close the ComboBox");
	//				comboBox.IsDropDownOpen = false;
	//			});

	//		await TestServices.WindowHelper.WaitForIdle();

	//		await RunOnUIThread(() =>

	//			{
	//			var dictionary = ResourceDictionary ^> (XamlReader.Load(
	//				LR"(<ResourceDictionary



	//						xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"



	//						xmlns: x = "http://schemas.microsoft.com/winfx/2006/xaml" >



	//					< x:Boolean x:Key = "IsDefaultShadowEnabled" > False </ x:Boolean >



	//				</ ResourceDictionary >)"));



	//			Application.Current.Resources.MergedDictionaries.Add(dictionary);
	//	});

	//// To remove the resource dictionary we added above
	//var resourceCleanup = wil.scope_exit([]()
	//        {

	//	RunOnUIThread([]()
	//            {

	//		var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
	//	mergedDictionaries.RemoveAt(mergedDictionaries.Size - 1);
	//            });
	//        });

	//await RunOnUIThread(() =>

	//		{
	//			LOG_OUTPUT("Open the ComboBox");
	//			comboBox.IsDropDownOpen = true;
	//		});

	//await TestServices.WindowHelper.WaitForIdle();

	//await RunOnUIThread(() =>

	//		{
	//			LOG_OUTPUT("Make sure it does not have a shadow");
	//			var shadow = findShadow();
	//			VERIFY_IS_NULL(shadow);
	//		});

	//await ComboBoxHelper.CloseComboBox(comboBox);
	//    }

	[TestMethod]
	public async Task ValidateRestoreOnCancelIndexResetOnClose()
	{
		var comboBox = await SetupBasicComboBoxTest();

		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownOpened");
		var closedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
		var selectionChangedEvent = new Event();
		var selectionChangedRegistration = CreateSafeEventRegistration<ComboBox, SelectionChangedEventHandler>("SelectionChanged");

		openedRegistration.Attach(comboBox, (s, e) =>
		{
			LOG_OUTPUT("ComboBox opened.");
			openedEvent.Set();
		});

		closedRegistration.Attach(comboBox, (s, e) =>
		{
			LOG_OUTPUT("ComboBox closed.");
			closedEvent.Set();
		});

		selectionChangedRegistration.Attach(comboBox, (s, e) =>
		{
			LOG_OUTPUT("ComboBox selection changed. Selection is now %d.", comboBox.SelectedIndex);
			selectionChangedEvent.Set();
		});

		int originalSelectedIndex = 0;

		await RunOnUIThread(() =>

			{
				comboBox.SelectedIndex = originalSelectedIndex;
			});

		await selectionChangedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FocusTestHelper.EnsureFocus(comboBox, FocusState.Keyboard);

		LOG_OUTPUT("Pressing space to open the ComboBox.");
		await TestServices.KeyboardHelper.Space();

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Pressing down and space to select a new item and close the ComboBox.");
		await TestServices.KeyboardHelper.Down();
		await TestServices.KeyboardHelper.Space();

		await selectionChangedEvent.WaitForDefault();
		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, originalSelectedIndex + 1);

		selectionChangedEvent.Reset();

		LOG_OUTPUT("Pressing space to open the ComboBox again.");
		await TestServices.KeyboardHelper.Space();

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Pressing escape to close the ComboBox without selecting anything.");
		await TestServices.KeyboardHelper.Escape();

		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		VERIFY_IS_FALSE(selectionChangedEvent.HasFired());
		await ComboBoxHelper.VerifySelectedIndex(comboBox, originalSelectedIndex + 1);
	}
}
#endif
