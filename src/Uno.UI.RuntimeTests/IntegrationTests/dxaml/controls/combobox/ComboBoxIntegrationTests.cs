using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
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

	[TestCleanup]
	public void TestCleanup()
	{
		TestServices.WindowHelper.VerifyTestCleanup();
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
			TestServices.WindowHelper.WindowContent = comboBox;

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

	//	void ValidateUIElementTree()
	//	{

	//		TestServices.WindowHelper.SetWindowSizeOverride(wf.Size(400, 600));

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
	//		ComboBoxItem ^ restUnselectedComboBoxItem = null;
	//		ComboBoxItem ^ pointerOverUnselectedComboBoxItem = null;
	//		ComboBoxItem ^ pressedUnselectedComboBoxItem = null;
	//		ComboBoxItem ^ disabledUnselectedComboBoxItem = null;

	//		ComboBoxItem ^ restSelectedComboBoxItem = null;
	//		ComboBoxItem ^ pointerOverSelectedComboBoxItem = null;
	//		ComboBoxItem ^ pressedSelectedComboBoxItem = null;
	//		ComboBoxItem ^ focusedSelectedComboBoxItem = null;
	//		ComboBoxItem ^ disabledSelectedComboBoxItem = null;

	//#if WI_IS_FEATURE_PRESENT(Feature_HeaderPlacement)
	//        ComboBox^ topHeaderComboBox = null;
	//        ComboBox^ leftHeaderComboBox = null;
	//#endif

	//		StackPanel ^ rootPanel = null;

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
	//			openedComboBox.Margin = ThicknessHelper.FromLengths(0, 350, 0, 0.0;
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
	//				rootPanel.Background = new xaml_media.SolidColorBrush(Microsoft.UI.Colors.White);
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
	//				rootPanel.Background = new xaml_media.SolidColorBrush(mu.Colors.Black);
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
		// Regression coverage for MSFT:1577374 "ComboBox crashes Apps if text in them is wider than the screen resolution for mobile phones"
		// We were hitting an issue where a ComboBox that contained an item that was too wide to fit on the screen caused a layout cycle exception.
		// To test this we just open a ComboBox containing a very wide ComboBox item and verify that no exceptions get thrown.

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
	public async Task ValidateOpenComboBoxWithAllItemsLongWidth()
	{


		// This is a regression coverage for bug#6274763 "ComboBox hits a layout cycle crash with having long item width in the all items.
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


	//	var comboBox = SetupBasicComboBoxTest();

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
	//		var comboBox = SetupBasicComboBoxTest();
	//		var comboBoxClosedEvent = new Event();
	//		var closedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
	//		var comboBoxParentKeyDownRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyDownEvent);
	//		var comboBoxParentKeyUpRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyUpEvent);

	//		await RunOnUIThread(() =>

	//		{
	//			parent = (UIElement ^)(xaml_media.VisualTreeHelper.GetParent(comboBox));
	//		});

	//		await TestServices.WindowHelper.WaitForIdle();

	//		await RunOnUIThread(() =>

	//		{
	//			if (verifyKeyDown)
	//			{
	//				comboBoxParentKeyDownRegistration.Attach(parent,
	//					new xaml_input.KeyEventHandler([expectedKeyDownHandledValue](Platform.Object ^ sender, xaml_input.KeyRoutedEventArgs ^ e)

	//				{
	//					VERIFY_ARE_EQUAL(e.Handled, expectedKeyDownHandledValue);
	//				}));
	//	}

	//			if (verifyKeyUp)
	//			{
	//				comboBoxParentKeyUpRegistration.Attach(parent,
	//					new xaml_input.KeyEventHandler([expectedKeyUpHandledValue](Platform.Object ^ sender, xaml_input.KeyRoutedEventArgs ^ e)

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

	//TestServices.KeyboardHelper.PressKeySequence(keySequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//comboBoxClosedEvent.WaitFor(std.chrono.milliseconds(8000)); //TFS #3636501 is tracking an issue where input injection is a taking too long

	//await RunOnUIThread(() =>

	//{
	//if (verifyKeyDown)
	//{
	//	comboBoxParentKeyDownRegistration.Detach();
	//	comboBoxParentKeyDownRegistration.Attach(parent,
	//				new xaml_input.KeyEventHandler([](Platform.Object ^ sender, xaml_input.KeyRoutedEventArgs ^ e)


	//		{
	//					VERIFY_ARE_EQUAL(e.Handled, false);
	//		}));
	//			}

	//			if (verifyKeyUp)
	//{
	//	comboBoxParentKeyUpRegistration.Detach();
	//	comboBoxParentKeyUpRegistration.Attach(parent,
	//		new xaml_input.KeyEventHandler([](Platform.Object ^ sender, xaml_input.KeyRoutedEventArgs ^ e)


	//	{
	//					VERIFY_ARE_EQUAL(e.Handled, false);
	//	}));
	//}
	//		});

	//LOG_OUTPUT("ComboBox: Injecting key sequence again to make sure no events are handled with the drop-down closed.");
	//LOG_OUTPUT("%s", keySequence.Data());

	//TestServices.KeyboardHelper.PressKeySequence(keySequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//	}

	//	void EventsContinuallyHandledOnPressAndHoldGamepadB()
	//{
	//	EventsContinuallyHandledOnPressAndHold("$d$_GamepadB", "$u$_GamepadB");
	//}

	//void EventsContinuallyHandledOnPressAndHold(Platform.String^ keyDownSequence, Platform.String^ keyUpSequence)
	//{


	//	UIElement ^ parent = null;
	//	var comboBox = SetupBasicComboBoxTest();
	//	var comboBoxClosedEvent = new Event();
	//	var comboBoxKeyDownRepeatedEvent = new Event();
	//	var closedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
	//	var comboBoxParentKeyDownRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyDownEvent);
	//	var comboBoxParentKeyUpRegistration = CreateSafeEventRegistrationForHandledEvents(Microsoft.UI.Xaml.UIElement, KeyUpEvent);

	//	int keyDownCount = 0;

	//	await RunOnUIThread(() =>

	//	{
	//		parent = (UIElement ^)(xaml_media.VisualTreeHelper.GetParent(comboBox));
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	{
	//		comboBoxParentKeyDownRegistration.Attach(parent,
	//			new xaml_input.KeyEventHandler([&keyDownCount, comboBoxKeyDownRepeatedEvent](Platform.Object ^ sender, xaml_input.KeyRoutedEventArgs ^ e)


	//		{
	//				VERIFY_ARE_EQUAL(e.Handled, true);

	//				keyDownCount++;



	//			if (keyDownCount >= 2)
	//		{
	//			comboBoxKeyDownRepeatedEvent.Set();
	//		}
	//	}));

	//	comboBoxParentKeyUpRegistration.Attach(parent,
	//		new xaml_input.KeyEventHandler([](Platform.Object ^ sender, xaml_input.KeyRoutedEventArgs ^ e)


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

	//RunOnUIThread([&]()

	//		{
	//	comboBox.Focus(FocusState.Keyboard);
	//});

	//await TestServices.WindowHelper.WaitForIdle();

	//LOG_OUTPUT("Injecting key-down sequence.");
	//LOG_OUTPUT("%s", keyDownSequence.Data());

	//TestServices.KeyboardHelper.PressKeySequence(keyDownSequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//comboBoxClosedEvent.WaitFor(std.chrono.milliseconds(8000)); //TFS #3636501 is tracking an issue where input injection is a taking too long

	//LOG_OUTPUT("Injecting key-down sequence again to simulate the situation where the user is holding down the key.");
	//LOG_OUTPUT("%s", keyDownSequence.Data());

	//TestServices.KeyboardHelper.PressKeySequence(keyDownSequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//comboBoxKeyDownRepeatedEvent.WaitFor(std.chrono.milliseconds(8000));

	//LOG_OUTPUT("Injecting key-up sequence.");
	//LOG_OUTPUT("%s", keyUpSequence.Data());

	//TestServices.KeyboardHelper.PressKeySequence(keyUpSequence);

	//await TestServices.WindowHelper.WaitForIdle();
	//    }

	//void ValidateResettingSelectedIndexBringsBackPlaceholderText()
	//{


	//	var comboBox = SetupBasicComboBoxTest();
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
	//	ScrollViewer ^ scrollViewer;
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
	//	var popup = TreeHelper.GetVisualChildByType<xaml_primitives.Popup>(comboBox);
	//	var popupChild = FrameworkElement ^> (popup.Child);
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

	private async Task<ComboBox> SetupBasicComboBoxTest(uint numberOfItems = 5, bool adjustMargin = true, bool isEditable = false)
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

	//// For these purposes, an "Ascending combobox" is a box that has data in a triangular pattern.
	//// This forces us to resize the combobox to get bigger every time we navigate down since we realize wider items.
	//// It resembles the following:
	//// X
	//// XX
	//// XXX
	//// XXXX
	//ComboBox SetupAscendingComboBoxTest(uint numberOfItems)
	//{
	//	ComboBox comboBox = null;

	//	await RunOnUIThread(() =>

	//		{
	//			var rootPanel = new Grid();
	//			VERIFY_IS_NOT_NULL(rootPanel);

	//			comboBox = new ComboBox();
	//			VERIFY_IS_NOT_NULL(comboBox);

	//			rootPanel.Children.Add(comboBox);
	//			TestServices.WindowHelper.WindowContent = rootPanel;

	//			for (uint i = 0; i < numberOfItems; i++)
	//			{
	//				var item = new ComboBoxItem();

	//				var stringItem = "";
	//				for (uint j = 1; j <= i + 1; j++)
	//				{
	//					stringItem += "X";
	//				}

	//				item.Content = stringItem;
	//				comboBox.Items.Add(item);
	//			}
	//		});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	return comboBox;
	//}

	[TestMethod]
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
		TestServices.KeyboardHelper.PressKeySequence(" ");
		await TestServices.WindowHelper.WaitForIdle();
		await dropDownOpenedEvent.WaitForDefault();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);

		LOG_OUTPUT("Select the first item with the Down key");
		TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 0);

		LOG_OUTPUT("Change selection with the Down key");
		TestServices.KeyboardHelper.Down();
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
		TestServices.KeyboardHelper.Down();
		await comboBoxItem2gotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 2);

		LOG_OUTPUT("Change selection with the Up key");
		TestServices.KeyboardHelper.Up();
		await comboBoxItem2lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
		await ComboBoxHelper.VerifySelectedIndex(comboBox, 1);

		LOG_OUTPUT("Accept selection with the Space key");
		TestServices.KeyboardHelper.PressKeySequence(" ");
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
		FocusManager.LosingFocus += (s, e) => Debug.WriteLine("FocusManager.LosingFocus");
		TestServices.KeyboardHelper.PressKeySequence(keySequence);
		await TestServices.WindowHelper.WaitForIdle();
		await dropDownClosedEvent.WaitForDefault();

		await ComboBoxHelper.VerifySelectedIndex(comboBox, -1);
	}

}
