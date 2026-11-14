// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InteractionTests/RadioButtonsTests.cs

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls
{
	// Upstream drives the RadioButtons test page through UI Automation. Here the page is rebuilt in code
	// (RadioButtonsTestPage below) and its readouts are read directly. Keyboard input is raised by
	// KeyboardHelper, which is a no-op on WinAppSDK, so the keyboard tests skip NativeWinUI.
	[TestClass]
	[RunsOnUIThread]
	public class RadioButtonsInteractionTests
	{
		private RadioButtonsTestPage elements;

		public enum RadioButtonsSourceType
		{
			String,
			RadioButton
		}

		public enum RadioButtonsSourceLocation
		{
			Items,
			ItemSource
		}

		[TestCleanup]
		public void TestCleanup()
		{
			elements?.Dispose();
			elements = null;
			WindowHelper.WindowContent = null;
		}

		[TestMethod]
		public async Task SelectionTest()
		{
			elements = await RadioButtonsTestPage.Create();
			await SetItemType(RadioButtonsSourceType.RadioButton);
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);

				await SelectByIndex(1);
				VerifySelectedIndex(1);
				RadioButton item1 = elements.FindItemByName("Radio Button 1");
				Verify.IsTrue(item1.IsChecked == true);

				await SelectByItem(3);
				VerifySelectedIndex(3);
				RadioButton item3 = elements.FindItemByName("Radio Button 3");
				Verify.IsTrue(item3.IsChecked == true);
				Verify.IsFalse(item1.IsChecked == true);
			}
		}

		[TestMethod]
		public async Task SelectByItem()
		{
			elements = await RadioButtonsTestPage.Create();
			await SetItemType(RadioButtonsSourceType.RadioButton);
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					if (location == RadioButtonsSourceLocation.Items && type == RadioButtonsSourceType.String)
					{
						// This is broken due to issue #1674. The items collection does not have a way to compare
						// boxed strings for equality.
						continue;
					}

					await SetItemType(type);

					await SelectByItem(1);
					VerifySelectedIndex(1);

					await SelectByItem(3);
					VerifySelectedIndex(3);
				}
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task FocusComingFromAnotherRepeaterTest()
		{
			var radioButtons1 = new RadioButtons { Header = "TestRadioButtons", ItemsSource = new[] { "Item 1", "Item 2", "Item 3" } };
			var radioButtons2 = new RadioButtons { Header = "TestRadioButtons2", ItemsSource = new[] { "Item 1", "Item 2", "Item 3" } };
			var focus1 = new FocusTracker(radioButtons1);
			var focus2 = new FocusTracker(radioButtons2);

			// The two groups are adjacent in tab order, as on the upstream focus page.
			var root = new StackPanel { Children = { radioButtons1, radioButtons2 } };
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);
			await WindowHelper.WaitForIdle();

			// Select items
			radioButtons1.SelectedIndex = 1;
			radioButtons2.SelectedIndex = 2;
			await WindowHelper.WaitForIdle();

			// Verify selection
			Verify.AreEqual(1, radioButtons1.SelectedIndex);
			Verify.AreEqual(2, radioButtons2.SelectedIndex);

			// Upstream starts with focus in the first group; put it there with the keyboard.
			radioButtons1.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();
			Verify.IsTrue(focus1.HasFocus);

			// Move focus to second RadioButtons control by pressing TAB
			await KeyboardHelper.Tab();

			// Verify that
			// - the first RadioButtons control no longer has focus and
			// - the third radio button of the second RadioButtons control now has focus
			Verify.IsFalse(focus1.HasFocus);
			Verify.AreEqual(2, radioButtons2.SelectedIndex);
			Verify.AreEqual(2, focus2.FocusedIndex, "RadioButton with index 2 should have focus.");

			// Move focus to the first RadioButtons control by pressing TAB again
			await KeyboardHelper.ShiftTab();

			// Verify that the second radio button is still selected and now has focus
			Verify.AreEqual(1, radioButtons1.SelectedIndex);
			Verify.AreEqual(1, focus1.FocusedIndex, "RadioButton with index 1 should have focus.");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task BasicKeyboardTest()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					bool useBackup = type == RadioButtonsSourceType.String;
					await SetItemType(type);
					await TapOnItem(3, useBackup);
					VerifySelectedFocusedIndex(3);
					await KeyboardHelper.Down();
					VerifySelectedFocusedIndex(4);
					await KeyboardHelper.Up();
					VerifySelectedFocusedIndex(3);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(3);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(3);

					await TapOnItem(0, useBackup);
					VerifySelectedFocusedIndex(0);
					await KeyboardHelper.Up();
					VerifySelectedFocusedIndex(0);

					await TapOnItem(9, useBackup);
					VerifySelectedFocusedIndex(9);
					await KeyboardHelper.Down();
					VerifySelectedFocusedIndex(9);
				}
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task MultiColumnKeyboardTest()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					await SetItemType(type);
					bool useBackup = type == RadioButtonsSourceType.String;
					await SetNumberOfColumns(3);
					await TapOnItem(3, useBackup);
					VerifySelectedFocusedIndex(3);
					await KeyboardHelper.Down();
					VerifySelectedFocusedIndex(4);
					await KeyboardHelper.Up();
					VerifySelectedFocusedIndex(3);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(3);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(6);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(9);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(9);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(6);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(2);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(2);

					await TapOnItem(0, useBackup);
					VerifySelectedFocusedIndex(0);
					await KeyboardHelper.Up();
					VerifySelectedFocusedIndex(0);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(0);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(4);

					await TapOnItem(9, useBackup);
					VerifySelectedFocusedIndex(9);
					await KeyboardHelper.Down();
					VerifySelectedFocusedIndex(9);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(9);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(6);
				}
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task SingleRowKeyboardTest()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					await SetItemType(type);
					bool useBackup = type == RadioButtonsSourceType.String;
					await SetNumberOfColumns(3);
					await SetNumberOfItems(3);
					await TapOnItem(0, useBackup);
					VerifySelectedFocusedIndex(0);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(1);
					await KeyboardHelper.Right();
					VerifySelectedFocusedIndex(2);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(1);
					await KeyboardHelper.Left();
					VerifySelectedFocusedIndex(0);
					await KeyboardHelper.Down();
					VerifySelectedFocusedIndex(1);
					await KeyboardHelper.Down();
					VerifySelectedFocusedIndex(2);
					await KeyboardHelper.Up();
					VerifySelectedFocusedIndex(1);
					await KeyboardHelper.Up();
					VerifySelectedFocusedIndex(0);
				}
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task DisabledItemsKeyboardTest()
		{
			elements = await RadioButtonsTestPage.Create();
			await SetItemType(RadioButtonsSourceType.RadioButton);
			await SetNumberOfColumns(3);
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);

				await SetNumberOfItems(10);
				await InsertDisabledRadioButton(10);
				await TapOnItem(7);
				VerifySelectedFocusedIndex(7);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(9);
				await KeyboardHelper.Left();
				VerifySelectedFocusedIndex(5);
				await KeyboardHelper.Down();
				VerifySelectedFocusedIndex(6);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(9);
				//There is a known bug here where pressing down or right will put focus
				//On the first item because RadioButton handles the event.
				//RadioButtons doesn't catch this because it only catches when the
				//Last item is focused, but that item is disabled here...
				//Bug #1654
				//KeyboardHelper.PressKey(Key.Down);
				//VerifySelectedFocusedIndex(9);
				//KeyboardHelper.PressKey(Key.Right);
				//VerifySelectedFocusedIndex(9);

				await InsertDisabledRadioButton(6);
				await InsertDisabledRadioButton(6);

				await TapOnItem(1);
				VerifySelectedFocusedIndex(1);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(10);
				await TapOnItem(2);
				VerifySelectedFocusedIndex(2);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(11);
				await TapOnItem(5);
				await KeyboardHelper.Up();
				VerifySelectedFocusedIndex(4);
				await KeyboardHelper.Down();
				VerifySelectedFocusedIndex(5);
				await KeyboardHelper.Down();
				VerifySelectedFocusedIndex(8);
				await TapOnItem(8);
				VerifySelectedFocusedIndex(10);
				await KeyboardHelper.Left();
				VerifySelectedFocusedIndex(1);
				await TapOnItem(9);
				VerifySelectedFocusedIndex(11);
				await KeyboardHelper.Left();
				VerifySelectedFocusedIndex(2);
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task DisabledItemsAtTopOfColumnKeyboardTest()
		{
			elements = await RadioButtonsTestPage.Create();
			await SetItemType(RadioButtonsSourceType.RadioButton);
			await SetNumberOfColumns(3);
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);

				await SetNumberOfItems(10);
				await InsertDisabledRadioButton(5);
				await InsertDisabledRadioButton(5);
				await InsertDisabledRadioButton(5);

				await TapOnItem(0);
				VerifySelectedFocusedIndex(0);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(9);
				await TapOnItem(1);
				VerifySelectedFocusedIndex(1);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(10);
				await TapOnItem(2);
				VerifySelectedFocusedIndex(2);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(11);
				await TapOnItem(3);
				VerifySelectedFocusedIndex(3);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(8);
				await TapOnItem(4);
				VerifySelectedFocusedIndex(4);
				await KeyboardHelper.Right();
				VerifySelectedFocusedIndex(8);
				await TapOnItem(6);
				VerifySelectedFocusedIndex(9);
				await KeyboardHelper.Left();
				VerifySelectedFocusedIndex(0);
				await TapOnItem(7);
				VerifySelectedFocusedIndex(10);
				await KeyboardHelper.Left();
				VerifySelectedFocusedIndex(1);
				await TapOnItem(8);
				VerifySelectedFocusedIndex(11);
				await KeyboardHelper.Left();
				VerifySelectedFocusedIndex(2);
				await TapOnItem(9);
				VerifySelectedFocusedIndex(12);
				await KeyboardHelper.Left();
				VerifySelectedFocusedIndex(8);
			}
		}

		[TestMethod]
		[Ignore("Disabled upstream due to #4576: Unreliable test")]
		public async Task GamepadCanEscapeAndDoesNotSelectWithFocus()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					await SetItemType(type);
					bool useBackup = type == RadioButtonsSourceType.String;
					await SetNumberOfColumns(3);

					await TapOnItem(9, useBackup);
					VerifySelectedFocusedIndex(9);
					await KeyboardHelper.GamepadDpadDown();
					VerifyRadioButtonsHasFocus(false);

					await TapOnItem(0, useBackup);
					VerifySelectedFocusedIndex(0);
					await KeyboardHelper.GamepadDpadUp();
					VerifyRadioButtonsHasFocus(false);

					await TapOnItem(7, useBackup);
					VerifySelectedFocusedIndex(7);
					await KeyboardHelper.GamepadDpadRight();
					VerifyRadioButtonsHasFocus(false);

					await TapOnItem(7, useBackup);
					VerifySelectedFocusedIndex(7);
					await KeyboardHelper.GamepadDpadDown();
					VerifySelectedIndex(7);
					VerifyFocusedIndex(8);

					await KeyboardHelper.GamepadDpadLeft();
					VerifySelectedIndex(7);
					VerifyFocusedIndex(5);
				}
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task ControlKeyKeyboardTest()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					await SetItemType(type);
					bool useBackup = type == RadioButtonsSourceType.String;
					await TapOnItem(3, useBackup);
					VerifySelectedFocusedIndex(3);
					await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_down#$u$_down#$u$_ctrl");
					VerifySelectedIndex(3);
					VerifyFocusedIndex(4);
				}
			}
		}

		[TestMethod]
		public async Task ColumnsTest()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					await SetItemType(type);
					await SetNumberOfColumns(1);
					await SetNumberOfItems(10);
					await SetBorderWidthToInf();

					VerifyLayoutData(10, 1, 0);
					await SetNumberOfColumns(3);
					VerifyLayoutData(3, 3, 1);
					await SetNumberOfColumns(5);
					VerifyLayoutData(2, 5, 0);
					await SetNumberOfColumns(7);
					VerifyLayoutData(1, 7, 3);
					await SetNumberOfColumns(10);
					VerifyLayoutData(1, 10, 0);
					await SetNumberOfColumns(20);
					VerifyLayoutData(1, 10, 0);

					await SetNumberOfItems(77);
					VerifyLayoutData(3, 20, 17);

					await SetBorderWidth(100);
					VerifyLayoutData(77, 1, 0);
					await SetBorderWidth(200);
					VerifyLayoutData(77, 1, 0);
					await SetBorderWidth(300);
					VerifyLayoutData(38, 2, 1);
					await SetBorderWidth(500);
					VerifyLayoutData(25, 3, 2);
					await SetBorderWidth(550);
					VerifyLayoutData(19, 4, 1);
				}
			}
		}

		[TestMethod]
		public async Task UIAProperties()
		{
			elements = await RadioButtonsTestPage.Create();
			await SetItemType(RadioButtonsSourceType.RadioButton);
			await SetSource(RadioButtonsSourceLocation.ItemSource);

			Log.Comment("Verify the header is set as the name for the RadioButtons control.");
			var groupElement = FrameworkElementAutomationPeer.CreatePeerForElement(elements.SecondTestRadioButton);
			Verify.AreEqual("More Radio Buttons", groupElement.GetName());
			Verify.IsTrue(groupElement.GetAutomationControlType() == AutomationControlType.Group);

			await SelectByIndex(1);
			VerifySelectedIndex(1);
			VerifySelectedPositionInSet(2);
			VerifySelectedSizeOfSet(10);

			await SetNumberOfColumns(3);
			VerifySelectedIndex(1);
			VerifySelectedPositionInSet(2);
			VerifySelectedSizeOfSet(10);

			await InsertEnabledRadioButton(0);
			VerifySelectedIndex(2);
			VerifySelectedPositionInSet(3);
			VerifySelectedSizeOfSet(11);

			await InsertEnabledRadioButton(10);
			VerifySelectedIndex(2);
			VerifySelectedPositionInSet(3);
			VerifySelectedSizeOfSet(12);

			await SelectByIndex(10);
			VerifySelectedIndex(10);
			VerifySelectedPositionInSet(11);
			VerifySelectedSizeOfSet(12);

			await SetNumberOfItems(17);
			await SelectByIndex(16);
			VerifySelectedIndex(16);
			VerifySelectedPositionInSet(17);
			VerifySelectedSizeOfSet(17);
		}

		[TestMethod]
		public async Task InsertedCheckedRadioButtonGetsSelection()
		{
			elements = await RadioButtonsTestPage.Create();
			await SetItemType(RadioButtonsSourceType.RadioButton);
			await SetSource(RadioButtonsSourceLocation.ItemSource);

			await SelectByIndex(3);
			VerifySelectedIndex(3);

			await InsertEnabledRadioButton(6, /*isChecked*/true);
			VerifySelectedIndex(6);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task ScrollViewerSettingSelectionDoesNotMoveFocus()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					await SetItemType(type);
					await SelectByIndex(3);
					VerifySelectedIndex(3);
					VerifyRadioButtonsHasFocus(false);

					InputHelper.LeftMouseClick(elements.ReproTextBlock);
					await WindowHelper.WaitForIdle();
					VerifySelectedIndex(3);
					// This behavior is probably wrong, it is probably more correct for focus to be on the selected item...
					VerifyFocusedIndex(0);

					await KeyboardHelper.Down();
					VerifySelectedFocusedIndex(1);
				}
			}
		}

		[TestMethod]
		[Ignore("UIElement.AccessKeyInvoked is not implemented, so Alt+R never reaches RadioButtons.OnAccessKeyInvoked")]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17130")]
		public async Task AccessKeys()
		{
			elements = await RadioButtonsTestPage.Create();
			foreach (RadioButtonsSourceLocation location in Enum.GetValues(typeof(RadioButtonsSourceLocation)))
			{
				await SetSource(location);
				foreach (RadioButtonsSourceType type in Enum.GetValues(typeof(RadioButtonsSourceType)))
				{
					await SetItemType(type);
					await SetNumberOfItems(10);

					VerifyRadioButtonsHasFocus(false);
					await UseAccessKey();
					VerifyFocusedIndex(0);
					VerifySelectedIndex(-1);

					await SelectByIndex(3);
					VerifySelectedIndex(3);
					VerifyFocusedIndex(-1);

					await KeyboardHelper.Tab();
					VerifyRadioButtonsHasFocus(false);
					await UseAccessKey();
					VerifySelectedFocusedIndex(3);
					await UseAccessKey();
					VerifySelectedFocusedIndex(3);
				}
			}
		}

		private async Task SetNumberOfColumns(int columns)
		{
			await elements.FocusOutside();
			elements.TestRadioButtons.MaxColumns = columns;
			await WindowHelper.WaitForIdle();
		}

		private async Task SetNumberOfItems(int items)
		{
			await elements.FocusOutside();
			elements.SetNumberOfItems(items);
			await WindowHelper.WaitForIdle();
		}

		private async Task SetBorderWidth(float width)
		{
			await elements.FocusOutside();
			elements.TestRadioButtonsBorder.Width = width;
			await WindowHelper.WaitForIdle();
		}

		private async Task SetBorderWidthToInf()
		{
			await elements.FocusOutside();
			elements.TestRadioButtonsBorder.Width = double.NaN;
			await WindowHelper.WaitForIdle();
		}

		private async Task SetSource(RadioButtonsSourceLocation location)
		{
			await elements.FocusOutside();
			elements.SourceLocation = location;
			await WindowHelper.WaitForIdle();
		}

		private async Task SetItemType(RadioButtonsSourceType type)
		{
			await elements.FocusOutside();
			elements.ItemType = type;
			await WindowHelper.WaitForIdle();
		}

		private Task InsertDisabledRadioButton(int index, bool isChecked = false, string content = "Custom")
			=> InsertRadioButton(index, true, isChecked, content);

		private Task InsertEnabledRadioButton(int index, bool isChecked = false, string content = "Custom")
			=> InsertRadioButton(index, false, isChecked, content);

		private async Task InsertRadioButton(int index, bool disabled, bool isChecked = false, string content = "Custom")
		{
			await elements.FocusOutside();
			elements.InsertRadioButton(index, disabled, isChecked, content);
			await WindowHelper.WaitForIdle();
		}

		private void VerifyLayoutData(int rows, int columns, int largerColumns)
		{
			Verify.AreEqual(rows, elements.LayoutNumberOfRows);
			Verify.AreEqual(columns, elements.LayoutNumberOfColumns);
			Verify.AreEqual(largerColumns, elements.LayoutNumberOfLargerColumns);
		}

		private async Task UseAccessKey()
			=> await KeyboardHelper.PressKeySequence("$d$_alt#$d$_r#$u$_r#$u$_alt");

		private async Task TapOnItem(int index, bool useBackup = false)
		{
			if (useBackup)
			{
				await BackupTap(index);
			}
			else
			{
				Log.Comment("Clicking on item 'Radio Button " + index + "'");
				RadioButton item = elements.FindItemByName("Radio Button " + index);
#if HAS_INPUT_INJECTOR
				InputHelper.LeftMouseClick(item);
#else
				item.IsChecked = true;
				item.Focus(FocusState.Pointer);
#endif
				await WindowHelper.WaitForIdle();
			}
		}

		private async Task BackupTap(int index)
		{
			await SelectByIndex(index);
			elements.FocusSelectedItem();
			await WindowHelper.WaitForIdle();
		}

		private async Task SelectByIndex(int index)
		{
			await elements.FocusOutside();
			elements.SelectByIndex(index);
			await WindowHelper.WaitForIdle();
		}

		private async Task SelectByItem(int index)
		{
			await elements.FocusOutside();
			elements.SelectByItem(index);
			await WindowHelper.WaitForIdle();
		}

		private void VerifySelectedPositionInSet(int index) => Verify.AreEqual(index, elements.SelectedPositionInSet);

		private void VerifySelectedSizeOfSet(int index) => Verify.AreEqual(index, elements.SelectedSizeOfSet);

		private void VerifySelectedFocusedIndex(int index)
		{
			VerifySelectedIndex(index);
			VerifyFocusedIndex(index);
		}

		private void VerifyRadioButtonsHasFocus(bool hasFocus) => Verify.AreEqual(hasFocus, elements.Focus.HasFocus);

		private void VerifySelectedIndex(int index) => Verify.AreEqual(index, elements.SelectedIndex);

		private void VerifyFocusedIndex(int index) => Verify.AreEqual(index, elements.Focus.FocusedIndex);

		// Mirrors the GotFocus/LostFocus readouts of the upstream test pages.
		private sealed class FocusTracker
		{
			private readonly RadioButtons _radioButtons;

			public FocusTracker(RadioButtons radioButtons)
			{
				_radioButtons = radioButtons;
				radioButtons.GotFocus += OnGotFocus;
				radioButtons.LostFocus += OnLostFocus;
			}

			public int FocusedIndex { get; private set; } = -1;

			public bool HasFocus { get; private set; }

			private void OnGotFocus(object sender, RoutedEventArgs e)
			{
				var stackPanel = VisualTreeHelper.GetChild(_radioButtons, 0);
				var repeater = (ItemsRepeater)VisualTreeHelper.GetChild(stackPanel, 1);
				FocusedIndex = repeater.GetElementIndex((UIElement)e.OriginalSource);
				HasFocus = true;
			}

			private void OnLostFocus(object sender, RoutedEventArgs e)
			{
				FocusedIndex = -1;
				HasFocus = false;
			}
		}

		// Port of TestUI/RadioButtonsPage.xaml(.cs), keeping only what the tests use. Upstream sets each input
		// through a control of the page, which takes focus; the helpers above move focus out the same way.
		private sealed class RadioButtonsTestPage : IDisposable
		{
			private readonly ObservableCollection<string> m_stringItemCollection = new();
			private readonly ObservableCollection<RadioButton> m_radioButtonItemCollection = new();
			// Stand-ins for the test page's input panel, which starts with focus and follows the RadioButtons in tab order.
			private readonly Button _outsideButton = new() { Content = "Outside" };
			private readonly Button _outsideButton2 = new() { Content = "Outside 2" };
			private RadioButtonsSourceLocation _sourceLocation = RadioButtonsSourceLocation.Items;
			private RadioButtonsSourceType _itemType = RadioButtonsSourceType.String;

			private RadioButtonsTestPage()
			{
				TestRadioButtons = new RadioButtons { AccessKey = "r", Header = "I'm the header" };
				AutomationProperties.SetName(TestRadioButtons, "TestRadioButtons");
				TestRadioButtons.SelectionChanged += TestRadioButtons_SelectionChanged;
				Focus = new FocusTracker(TestRadioButtons);

				TestRadioButtonsBorder = new Border
				{
					BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black),
					BorderThickness = new Thickness(1),
					Child = TestRadioButtons,
				};

				SecondTestRadioButton = new RadioButtons { Header = "More Radio Buttons" };
				SecondTestRadioButton.Items.Add(new RadioButton { Content = "I'm really a Radio Button" });
				SecondTestRadioButton.Items.Add("I'm just a string");
				SecondTestRadioButton.Items.Add(new Button { Content = "And I'm a Button!" });

				ReproTextBlock = new TextBlock
				{
					MaxWidth = 300,
					TextWrapping = TextWrapping.WrapWholeWords,
					Text = "Click me to have Scroll Viewer place focus on the first radio button. Selection shouldn't change but down will move selection and focus to the second radio button.",
				};

				Root = new Grid
				{
					Children =
					{
						new Grid
						{
							Margin = new Thickness(5),
							MinWidth = 500,
							MaxWidth = 800,
							Children =
							{
								new ScrollViewer
								{
									HorizontalScrollMode = ScrollMode.Enabled,
									HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
									Content = new StackPanel
									{
										Children = { TestRadioButtonsBorder, SecondTestRadioButton, ReproTextBlock },
									},
								},
							},
						},
					},
				};
				var outsidePanel = new StackPanel { Orientation = Orientation.Horizontal, Children = { _outsideButton, _outsideButton2 } };
				Grid.SetRow(outsidePanel, 1);
				Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
				Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				Root.Children.Add(outsidePanel);
			}

			public static async Task<RadioButtonsTestPage> Create()
			{
				var page = new RadioButtonsTestPage();
				WindowHelper.WindowContent = page.Root;
				await WindowHelper.WaitForLoaded(page.Root);

				// RadioButtonsPage_Loaded
				RadioButtonsTestHooks.SetTestHooksEnabled(page.TestRadioButtons, true);
				RadioButtonsTestHooks.LayoutChanged += page.RadioButtonsTestHooks_LayoutChanged;
				page.TestRadioButtons.MaxColumns = 1;
				page.SetNumberOfItems(10);
				page.UpdateRadioButtonsSource();
				await page.FocusOutside();
				return page;
			}

			public Grid Root { get; }

			public RadioButtons TestRadioButtons { get; }

			public Border TestRadioButtonsBorder { get; }

			public RadioButtons SecondTestRadioButton { get; }

			public TextBlock ReproTextBlock { get; }

			public FocusTracker Focus { get; }

			public int SelectedIndex { get; private set; } = -1;

			public int SelectedPositionInSet { get; private set; } = -1;

			public int SelectedSizeOfSet { get; private set; } = -1;

			public int LayoutNumberOfRows { get; private set; }

			public int LayoutNumberOfColumns { get; private set; }

			public int LayoutNumberOfLargerColumns { get; private set; }

			public RadioButtonsSourceLocation SourceLocation
			{
				get => _sourceLocation;
				set
				{
					_sourceLocation = value;
					UpdateRadioButtonsSource();
				}
			}

			public RadioButtonsSourceType ItemType
			{
				get => _itemType;
				set
				{
					_itemType = value;
					UpdateRadioButtonsSource();
				}
			}

			public void Dispose()
				=> RadioButtonsTestHooks.LayoutChanged -= RadioButtonsTestHooks_LayoutChanged;

			public RadioButton FindItemByName(string name)
			{
				foreach (var radioButton in m_radioButtonItemCollection)
				{
					if (radioButton.Name == name)
					{
						return radioButton;
					}
				}

				throw new InvalidOperationException($"No item named '{name}'.");
			}

			public async Task FocusOutside()
			{
				_outsideButton.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
			}

			private void RadioButtonsTestHooks_LayoutChanged(RadioButtons sender, object args)
			{
				LayoutNumberOfRows = RadioButtonsTestHooks.GetRows(sender);
				LayoutNumberOfColumns = RadioButtonsTestHooks.GetColumns(sender);
				LayoutNumberOfLargerColumns = RadioButtonsTestHooks.GetLargerColumns(sender);
			}

			private void TestRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				var index = TestRadioButtons.SelectedIndex;
				SelectedIndex = index;

				if (index > 0)
				{
					var radioButton = TestRadioButtons.ContainerFromIndex(index);
					if (radioButton != null)
					{
						SelectedPositionInSet = (int)radioButton.GetValue(AutomationProperties.PositionInSetProperty);
						SelectedSizeOfSet = (int)radioButton.GetValue(AutomationProperties.SizeOfSetProperty);
					}
				}
				else
				{
					SelectedPositionInSet = -1;
					SelectedSizeOfSet = -1;
				}
			}

			public void SetNumberOfItems(int value)
			{
				m_stringItemCollection.Clear();
				m_radioButtonItemCollection.Clear();
				for (int i = 0; i < value; i++)
				{
					m_stringItemCollection.Add(i.ToString());
					var radioButton = new RadioButton();
					radioButton.Content = i.ToString() + "Radio Button";
					radioButton.SetValue(AutomationProperties.NameProperty, "Radio Button " + i);
					radioButton.Name = "Radio Button " + i;
					m_radioButtonItemCollection.Add(radioButton);
				}

				if (SourceLocation == RadioButtonsSourceLocation.Items)
				{
					UpdateRadioButtonsSource();
				}
			}

			public void SelectByIndex(int index) => TestRadioButtons.SelectedIndex = index;

			public void SelectByItem(int index)
			{
				if (ItemType == RadioButtonsSourceType.String)
				{
					if (SourceLocation == RadioButtonsSourceLocation.ItemSource)
					{
						TestRadioButtons.SelectedItem = m_stringItemCollection[index];
					}
					else if (SourceLocation == RadioButtonsSourceLocation.Items)
					{
						TestRadioButtons.SelectedItem = TestRadioButtons.Items[index];
					}
				}
				else if (ItemType == RadioButtonsSourceType.RadioButton)
				{
					TestRadioButtons.SelectedItem = m_radioButtonItemCollection[index];
				}
			}

			public void InsertRadioButton(int index, bool disabled, bool isChecked, string content)
			{
				var radioButton = new RadioButton();
				radioButton.Content = content;
				radioButton.IsEnabled = !disabled;
				radioButton.IsChecked = isChecked;
				m_radioButtonItemCollection.Insert(index, radioButton);
				if (SourceLocation == RadioButtonsSourceLocation.Items)
				{
					UpdateRadioButtonsSource();
				}

				TestRadioButtons_SelectionChanged(null, null);
			}

			public void FocusSelectedItem()
			{
				var stackPanel = VisualTreeHelper.GetChild(TestRadioButtons, 0);
				var repeater = (ItemsRepeater)VisualTreeHelper.GetChild(stackPanel, 1);
				((Control)repeater.TryGetElement(TestRadioButtons.SelectedIndex)).Focus(FocusState.Keyboard);
			}

			private void UpdateRadioButtonsSource()
			{
				if (SourceLocation == RadioButtonsSourceLocation.Items)
				{
					TestRadioButtons.Items.Clear();
					TestRadioButtons.ItemsSource = null;
					foreach (var item in GetItemsCollection())
					{
						TestRadioButtons.Items.Add(item);
					}
				}
				else
				{
					TestRadioButtons.ItemsSource = GetItemsCollection();
					TestRadioButtons.Items.Clear();
				}
			}

			private System.Collections.IList GetItemsCollection()
				=> ItemType == RadioButtonsSourceType.String ? m_stringItemCollection : m_radioButtonItemCollection;
		}
	}
}
