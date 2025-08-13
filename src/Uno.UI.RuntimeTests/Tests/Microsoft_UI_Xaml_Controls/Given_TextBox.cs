using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

using Color = Windows.UI.Color;

#if HAS_UNO_WINUI || WINAPPSDK || WINUI
using Colors = Microsoft.UI.Colors;
#else
using Colors = Windows.UI.Colors;
#endif

using static Private.Infrastructure.TestServices;
using SamplesApp.UITests;
using Windows.UI.Input.Preview.Injection;
using Windows.Foundation;
using System.Collections.Generic;
using Uno.Extensions;
using Windows.UI.ViewManagement;
using Private.Infrastructure;
using Combinatorial.MSTest;


#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#else
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_TextBox
	{
		[TestMethod]
		[DataRow(UpdateSourceTrigger.Default, false)]
		[DataRow(UpdateSourceTrigger.PropertyChanged, false)]
		[DataRow(UpdateSourceTrigger.Explicit, false)]
		[DataRow(UpdateSourceTrigger.LostFocus, false)]
		[DataRow(UpdateSourceTrigger.Default, true)]
		[DataRow(UpdateSourceTrigger.LostFocus, true)]
		public async Task When_TwoWay_Text_Binding(UpdateSourceTrigger trigger, bool xBind)
		{
			var SUT = new When_TwoWay_Text_Binding();
			var tb = (trigger, xBind) switch
			{
				(UpdateSourceTrigger.Default, false) => SUT.tbTwoWay_triggerDefault,
				(UpdateSourceTrigger.PropertyChanged, false) => SUT.tbTwoWay_triggerPropertyChanged,
				(UpdateSourceTrigger.Explicit, false) => SUT.tbTwoWay_triggerExplicit,
				(UpdateSourceTrigger.LostFocus, false) => SUT.tbTwoWay_triggerLostFocus,
				(UpdateSourceTrigger.Default, true) => SUT.tbTwoWay_triggerDefault_xBind,
				(UpdateSourceTrigger.LostFocus, true) => SUT.tbTwoWay_triggerLostFocus_xBind,
				_ => throw new Exception("Should not happen."),
			};
			var expectedSetCount = 0;

			await UITestHelper.Load(SUT);

			var vm = xBind ? SUT.VMForXBind : (When_TwoWay_Text_Binding.VM)tb.DataContext;

			Assert.AreNotEqual(tb, FocusManager.GetFocusedElement(SUT.XamlRoot));

			Assert.AreEqual(expectedSetCount, vm.SetCount);
			Assert.AreEqual("", tb.Text);

			// Change text while not focused
			tb.Text = "Hello";
			if (trigger is UpdateSourceTrigger.PropertyChanged || (trigger is not UpdateSourceTrigger.Explicit && !xBind))
			{
				expectedSetCount++;
			}

			Assert.AreEqual(expectedSetCount, vm.SetCount);
			Assert.AreEqual("Hello", tb.Text);

			tb.Focus(FocusState.Programmatic);
			Assert.AreEqual(tb, FocusManager.GetFocusedElement(SUT.XamlRoot));

			// Change text while focused
			tb.Text = "Hello2";
			if (trigger is UpdateSourceTrigger.PropertyChanged)
			{
				expectedSetCount++;
			}
			Assert.AreEqual(expectedSetCount, vm.SetCount);
			Assert.AreEqual("Hello2", tb.Text);

			// To unfocus TextBox.
			SUT.dummyButton.Focus(FocusState.Programmatic);
			Assert.AreEqual(SUT.dummyButton, FocusManager.GetFocusedElement(SUT.XamlRoot));
			if (trigger is UpdateSourceTrigger.Default or UpdateSourceTrigger.LostFocus)
			{
				expectedSetCount++;
			}

			// In WinUI, a WaitForIdle is required.
			// In Uno, it's not at the time of writing the test.
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(expectedSetCount, vm.SetCount);
			Assert.AreEqual("Hello2", tb.Text);
		}

		[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_BorderThickness_Zero()
		{
			var grid = new Grid
			{
				Width = 120,
				Height = 120,
				Background = new SolidColorBrush(Colors.Yellow)
			};

			var textBox = new TextBox
			{
				Text = "",
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				Width = 100,
				Height = 100
			};

			grid.Children.Add(textBox);

			await UITestHelper.Load(grid);

			var borderThicknessZero = await UITestHelper.ScreenShot(grid);

			textBox.Visibility = Visibility.Collapsed;

			var opacityZero = await UITestHelper.ScreenShot(grid);

			await ImageAssert.AreEqualAsync(opacityZero, borderThicknessZero);
		}

		[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsEnabled_With_Text_Changes()
		{
			var grid = new Grid
			{
				Background = new SolidColorBrush(Colors.Yellow)
			};

			var textBox = new TextBox
			{
				Text = "Hello!",
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Padding = new Thickness(100),
			};

			grid.Children.Add(textBox);

			await UITestHelper.Load(grid);

			// Get element marked "ContentElement" from template of textBox
			var contentElement = textBox.FindFirstChild<ScrollViewer>(tb => tb.Name == "ContentElement");

			var enabledScreenshot = await UITestHelper.ScreenShot(contentElement);

			textBox.IsEnabled = false;

			var disabledScreenshot = await UITestHelper.ScreenShot(contentElement);

			await ImageAssert.AreNotEqualAsync(enabledScreenshot, disabledScreenshot);
		}

#if __ANDROID__
		[TestMethod]
		public void When_InputScope_Null_And_ImeOptions()
		{
			var tb = new TextBox();
			tb.InputScope = null;
			tb.ImeOptions = Android.Views.InputMethods.ImeAction.Search;
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_Template_Recycled()
		{
			try
			{
				var textBox = new TextBox
				{
					Text = "Test"
				};

				WindowHelper.WindowContent = textBox;
				await WindowHelper.WaitForLoaded(textBox);

				FocusManager.GettingFocus += OnGettingFocus;
				textBox.OnTemplateRecycled();
			}
			finally
			{
				FocusManager.GettingFocus -= OnGettingFocus;
			}

			static void OnGettingFocus(object sender, GettingFocusEventArgs args)
			{
				Assert.Fail("Focus should not move");
			}
		}
#endif

		[TestMethod]
		public async Task When_TB_Fluent_And_Theme_Changed()
		{
			var textBox = new TextBox
			{
				PlaceholderText = "Enter..."
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			var placeholderTextContentPresenter = textBox.FindFirstChild<TextBlock>(tb => tb.Name == "PlaceholderTextContentPresenter");
			Assert.IsNotNull(placeholderTextContentPresenter);

			var lightThemeForeground = TestsColorHelper.ToColor("#9E000000");
			var darkThemeForeground = TestsColorHelper.ToColor("#C5FFFFFF");

			Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);

			using (ThemeHelper.UseDarkTheme())
			{
				Assert.AreEqual(darkThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
			}

			Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
		}

		[TestMethod]
		public async Task When_BeforeTextChanging()
		{
			var textBox = new TextBox
			{
				Text = "Test"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.BeforeTextChanging += (s, e) =>
			{
				Assert.AreEqual("Test", textBox.Text);
			};

			textBox.Text = "Something";
		}

		[TestMethod]
		public async Task When_Calling_Select_With_Negative_Values()
		{
			var textBox = new TextBox();
			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			Assert.ThrowsExactly<ArgumentException>(() => textBox.Select(0, -1));
			Assert.ThrowsExactly<ArgumentException>(() => textBox.Select(-1, 0));
		}

		[TestMethod]
		public async Task When_Calling_Select_With_In_Range_Values()
		{
			var textBox = new TextBox
			{
				Text = "0123456789"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);
			textBox.Focus(FocusState.Programmatic);
			// On WinUI, TextBoxes start their selection at 0
			Assert.AreEqual(
#if __SKIA__
				!FeatureConfiguration.TextBox.UseOverlayOnSkia ? 0 :
#endif
					textBox.Text.Length,
				textBox.SelectionStart);
			Assert.AreEqual(0, textBox.SelectionLength);
			textBox.Select(1, 7);
			Assert.AreEqual(1, textBox.SelectionStart);
			Assert.AreEqual(7, textBox.SelectionLength);
		}

		[TestMethod]
		public async Task When_Calling_Select_With_Out_Of_Range_Length()
		{
			var textBox = new TextBox
			{
				Text = "0123456789"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);
			textBox.Focus(FocusState.Programmatic);
			// On WinUI, TextBoxes start their selection at 0
			Assert.AreEqual(
#if __SKIA__
				!FeatureConfiguration.TextBox.UseOverlayOnSkia ? 0 :
#endif
					textBox.Text.Length,
				textBox.SelectionStart);
			Assert.AreEqual(0, textBox.SelectionLength);
			textBox.Select(1, 20);
			Assert.AreEqual(1, textBox.SelectionStart);
			Assert.AreEqual(9, textBox.SelectionLength);
		}

		[TestMethod]
		public async Task When_Calling_Select_With_Out_Of_Range_Start()
		{
			var textBox = new TextBox
			{
				Text = "0123456789"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);
			textBox.Focus(FocusState.Programmatic);
			// On WinUI, TextBoxes start their selection at 0
			Assert.AreEqual(
#if __SKIA__
				!FeatureConfiguration.TextBox.UseOverlayOnSkia ? 0 :
#endif
					textBox.Text.Length,
				textBox.SelectionStart);
			Assert.AreEqual(0, textBox.SelectionLength);
			textBox.Select(20, 5);
			Assert.AreEqual(10, textBox.SelectionStart);
			Assert.AreEqual(0, textBox.SelectionLength);
		}

#if __APPLE_UIKIT__
		[Ignore("Disabled as not working properly. See https://github.com/unoplatform/uno/issues/8016")]
#endif
		[TestMethod]
		public async Task When_SelectionStart_Set()
		{
			var textBox = new TextBox
			{
				Text = "0123456789"
			};

			var button = new Button()
			{
				Content = "Some button"
			};

			var stackPanel = new StackPanel()
			{
				Children =
				{
					textBox,
					button
				}
			};

			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(textBox);

			button.Focus(FocusState.Programmatic);

			await WindowHelper.WaitForIdle();

			textBox.SelectionStart = 3;

			textBox.Focus(FocusState.Programmatic);
			Assert.AreEqual(3, textBox.SelectionStart);
		}

#if __APPLE_UIKIT__
		[Ignore("Disabled as not working properly. See https://github.com/unoplatform/uno/issues/8016")]
#endif
		[TestMethod]
		public async Task When_Focus_Changes_SelectionStart_Preserved()
		{
			var textBox = new TextBox
			{
				Text = "0123456789"
			};

			var button = new Button()
			{
				Content = "Some button"
			};

			var stackPanel = new StackPanel()
			{
				Children =
				{
					textBox,
					button
				}
			};

			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.Focus(FocusState.Programmatic);

			await WindowHelper.WaitForIdle();

			textBox.SelectionStart = 3;

			button.Focus(FocusState.Programmatic);
			Assert.AreEqual(3, textBox.SelectionStart);

			textBox.Focus(FocusState.Programmatic);
			Assert.AreEqual(3, textBox.SelectionStart);
		}

		[TestMethod]
		public async Task When_IsEnabled_Set()
		{
			var foregroundColor = new SolidColorBrush(Colors.Red);
			var disabledColor = new SolidColorBrush(Colors.Blue);

			var textbox = new TextBox
			{
				Text = "Original Text",
				Foreground = foregroundColor,
				Style = TestsResourceHelper.GetResource<Style>("MaterialOutlinedTextBoxStyle"),
				IsEnabled = false
			};

			var stackPanel = new StackPanel()
			{
				Children = { textbox }
			};


			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(textbox);


			var contentPresenter = (ScrollViewer)textbox.FindName("ContentElement");

			await WindowHelper.WaitForIdle();

			Assert.IsFalse(textbox.IsEnabled);
			Assert.AreEqual(contentPresenter.Foreground, disabledColor);

			textbox.IsEnabled = true;

			Assert.IsTrue(textbox.IsEnabled);
			Assert.AreEqual(contentPresenter.Foreground, foregroundColor);
		}

#if __ANDROID__
		[TestMethod]
		public async Task When_Text_IsWrapping_Set()
		{
			var textbox = new TextBox();

			textbox.Width = 100;
			StackPanel panel = new() { Orientation = Orientation.Vertical };
			panel.Children.Add(textbox);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(textbox);
			var originalActualHeigth = textbox.ActualHeight;
			textbox.Text = "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremaaaaaaaaaaaaaaaaaa";
			textbox.TextWrapping = TextWrapping.Wrap;
			await WindowHelper.WaitForIdle();
			textbox.ActualHeight.Should().BeGreaterThan(originalActualHeigth);
		}


		[TestMethod]
		public async Task When_Text_IsNoWrap_Set()
		{
			var textbox = new TextBox();

			textbox.Width = 100;
			StackPanel panel = new() { Orientation = Orientation.Vertical };
			panel.Children.Add(textbox);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(textbox);
			var originalActualHeigth = textbox.ActualHeight;
			textbox.Text = "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremaaaaaaaaaaaaaaaaaa";
			textbox.TextWrapping = TextWrapping.NoWrap;
			await WindowHelper.WaitForIdle();
			textbox.ActualHeight.Should().Be(originalActualHeigth);
		}


		[TestMethod]
		public async Task When_AcceptsReturn_Set()
		{
			var textbox = new TextBox();

			textbox.Width = 100;
			StackPanel panel = new() { Orientation = Orientation.Vertical };
			panel.Children.Add(textbox);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(textbox);
			var originalActualHeigth = textbox.ActualHeight;
			textbox.AcceptsReturn = true;
			textbox.Text = "Sed ut perspiciatis unde omnis iste natus \nerror sit voluptatem accusantium doloremaaaaaaaaaaaaaaaaaa";
			await WindowHelper.WaitForIdle();
			textbox.ActualHeight.Should().BeGreaterThan(originalActualHeigth * 2);
		}

		[TestMethod]
		public async Task When_AcceptsReturn_Set_On_Init()
		{
			var textbox = new TextBox();
			textbox.AcceptsReturn = true;
			textbox.Text = "Sed ut perspiciatis unde omnis iste natus \nerror sit voluptatem accusantium doloremaaaaaaaaaaaaaaaaaa";
			textbox.Width = 100;

			StackPanel panel = new() { Orientation = Orientation.Vertical };
			panel.Children.Add(textbox);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(textbox);
			var originalActualHeigth = textbox.ActualHeight;
			textbox.AcceptsReturn = false;
			await WindowHelper.WaitForIdle();
			textbox.ActualHeight.Should().BeLessThan(originalActualHeigth / 2);
		}
#endif

		[TestMethod]
		public async Task When_SelectedText_StartZero()
		{
			var textBox = new TextBox
			{
				Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.Focus(FocusState.Programmatic);

			textBox.SelectionStart = 0;
			textBox.SelectionLength = 0;
			textBox.SelectedText = "1234";

			Assert.AreEqual("1234ABCDEFGHIJKLMNOPQRSTUVWXYZ", textBox.Text);
#if __SKIA__
			Xaml.Core.VisualTree.GetFocusManagerForElement(textBox)!.FindAndSetNextFocus(FocusNavigationDirection.Next); // move focus to dismiss the overlay
#endif
		}

		[TestMethod]
		public async Task When_SelectedText_EndOfText()
		{
			var textBox = new TextBox
			{
				Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.Focus(FocusState.Programmatic);

			textBox.SelectionStart = 26;
			textBox.SelectedText = "1234";

			Assert.AreEqual("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234", textBox.Text);
		}

		[TestMethod]
		public async Task When_SelectedText_MiddleOfText()
		{
			var textBox = new TextBox
			{
				Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.Focus(FocusState.Programmatic);

			textBox.SelectionStart = 2;
			textBox.SelectionLength = 22;
			textBox.SelectedText = "1234";

			Assert.AreEqual("AB1234YZ", textBox.Text);
		}

		[TestMethod]
		public async Task When_SelectedText_AllTextToEmpty()
		{
			var textBox = new TextBox
			{
				Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.Focus(FocusState.Programmatic);

			textBox.SelectionStart = 0;
			textBox.SelectionLength = 26;
			textBox.SelectedText = String.Empty;

			Assert.AreEqual(String.Empty, textBox.Text);
			Assert.AreEqual(0, textBox.SelectionStart);
			Assert.AreEqual(0, textBox.SelectionLength);
		}

#if __SKIA__
		[TestMethod]
		public async Task When_Text_Set_On_Initial_Load()
		{
			var initialText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			var textBox = new TextBox
			{
				Text = initialText
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			var contentControl = VisualTreeUtils.FindVisualChildByType<ContentControl>(textBox);
			Assert.IsInstanceOfType(contentControl.Content, typeof(TextBlock));
			Assert.AreEqual(initialText, ((TextBlock)contentControl.Content).Text);
		}
#endif

		[TestMethod]
		public async Task When_Changes_In_TextChanged()
		{
			int update = 0;
			var initialText = "Text";

			var textBox = new TextBox
			{
				Text = "Waiting"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			// make sure the initial (''->'Waiting') TextChanged event had time to be dispatched
			await WindowHelper.WaitForIdle();

			int textChangedInvokeCount = 0;
			int textChangingInvokeCount = 0;

			bool failedCheck = false;
			bool finished = false;

			string GetCurrentText() => initialText + update;
			string GetNewText() => initialText + ++update;
			async void OnTextChanged(object sender, TextChangedEventArgs e)
			{
				textChangedInvokeCount++;
				if (GetCurrentText() != textBox.Text)
				{
					failedCheck = true;
					finished = true;
				}
				else if (update <= 4)
				{
					textBox.Text = GetNewText();
				}
				else
				{
					finished = true;
				}
			}

			void OnTextChanging(object sender, TextBoxTextChangingEventArgs e) =>
				textChangingInvokeCount++;

			textBox.TextChanged += OnTextChanged;
			textBox.TextChanging += OnTextChanging;

			textBox.Text = GetNewText();

			await WindowHelper.WaitFor(() => finished);

			if (failedCheck)
			{
				Assert.Fail("Text changed to incorrect value. Expected: " + GetCurrentText() + ", Actual: " + textBox.Text);
			}

			Assert.AreEqual(5, textChangedInvokeCount);
			Assert.AreEqual(5, textChangingInvokeCount);
			Assert.AreEqual(initialText + "5", textBox.Text);
		}

#if __ANDROID__
		[TestMethod]
		public async Task When_ReadOnly_TextBoxView()
		{
			var textBox = new TextBox
			{
				Text = "Text",
				IsReadOnly = true
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			Assert.IsTrue(textBox.TextBoxView.Focusable);
			Assert.IsTrue(textBox.TextBoxView.FocusableInTouchMode);
			Assert.IsTrue(textBox.TextBoxView.Clickable);
			Assert.IsTrue(textBox.TextBoxView.LongClickable);
		}

		[TestMethod]
		public async Task When_NotTabStop_TextBoxView()
		{
			var textBox = new TextBox
			{
				Text = "Text",
				IsTabStop = false
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			Assert.IsFalse(textBox.TextBoxView.Focusable);
			Assert.IsFalse(textBox.TextBoxView.FocusableInTouchMode);
			Assert.IsFalse(textBox.TextBoxView.Clickable);
			Assert.IsFalse(textBox.TextBoxView.LongClickable);
		}

		[TestMethod]
		public async Task When_ReadOnly_NotTabStop_TextBoxView()
		{
			var textBox = new TextBox
			{
				Text = "Text",
				IsTabStop = false,
				IsReadOnly = true
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			Assert.IsFalse(textBox.TextBoxView.Focusable);
			Assert.IsFalse(textBox.TextBoxView.FocusableInTouchMode);
			Assert.IsFalse(textBox.TextBoxView.Clickable);
			Assert.IsFalse(textBox.TextBoxView.LongClickable);
		}


		[TestMethod]
		public async Task When_ReadOnly_Update_Text_Native_View()
		{
			var textBox = new TextBox
			{
				Text = "Text",
				IsReadOnly = true,
				IsTabStop = false
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.Text = "Something";

			Assert.AreEqual("Something", textBox.TextBoxView.Text);
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_Default_Foreground_TextBoxView(bool useDarkTheme)
		{
			using var _ = useDarkTheme ? ThemeHelper.UseDarkTheme() : default;

			var SUT = new TextBox { Text = "Asd" };

			await UITestHelper.Load(SUT);
			var expected = GetBrushColor(SUT.Foreground);
			var forwarded = GetBrushColor(SUT.TextBoxView.Foreground);
			var native = new Color((uint)SUT.TextBoxView.CurrentTextColor);

			Assert.AreEqual(expected, forwarded);
			Assert.AreEqual(expected, native);

			Color? GetBrushColor(Brush brush) => (brush as SolidColorBrush)?.Color;
		}
#endif

		[TestMethod]
		public async Task When_ReadOnly_Update_Text()
		{
			var textBox = new TextBox
			{
				Text = "Text",
				IsReadOnly = true,
				IsTabStop = false
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			textBox.Text = "Something";

			Assert.AreEqual("Something", textBox.Text);
		}

		[TestMethod]
		public async Task When_ReadOnly_Toggled_Repeatedly()
		{
			var textBox = new TextBox
			{
				Text = "Text",
				IsReadOnly = true,
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);
			textBox.Focus(FocusState.Programmatic);

			textBox.Text = "Something";

			textBox.IsReadOnly = false;

			var updatedText = "Something else";
			textBox.Text = updatedText;

			textBox.IsReadOnly = true;

			textBox.SelectAll();

			Assert.AreEqual(updatedText.Length, textBox.SelectionLength);
		}

#if __ANDROID__
		[TestMethod]
		public async Task When_TextBox_ImeAction_Enter()
		{
			var textBox = new TextBox
			{
				Text = "Text",
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			var act = () => textBox.OnEditorAction(textBox.TextBoxView, Android.Views.InputMethods.ImeAction.Next, null);
			act.Should().NotThrow();
		}
#endif

		[TestMethod]
#if __SKIA__
		[Ignore("Fails on Skia")]
#endif
		public async Task When_TextBox_Wrap_Custom_Style()
		{
			var page = new TextBox_Wrapping();
			await UITestHelper.Load(page);

			page.SUT.Text = "Short";
			await WindowHelper.WaitForIdle();
			var height1 = page.SUT.ActualHeight;

			page.SUT.Text = "This is a very very very much longer text. This TextBox should now wrap and have a larger height.";
			await WindowHelper.WaitForIdle();
			var height2 = page.SUT.ActualHeight;

			page.SUT.Text = "Short";
			await WindowHelper.WaitForIdle();
			var height3 = page.SUT.ActualHeight;

			Assert.AreEqual(height1, height3);
			height2.Should().BeGreaterThan(height1);
		}

		[TestMethod]
#if __SKIA__ || __APPLE_UIKIT__
		[Ignore("Fails on Skia and iOS")]
		// On iOS, the failure is: AssertFailedException: Expected value to be greater than 1199.0, but found 1199.0.
		// Since the number is large, it looks like the TextBox is taking the full height.
#endif
		public async Task When_TextBox_Wrap_Fluent()
		{
			var SUT = new TextBox()
			{
				Width = 200,
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true,
			};

			await UITestHelper.Load(SUT);

			SUT.Text = "Short";
			await WindowHelper.WaitForIdle();
			var height1 = SUT.ActualHeight;

			SUT.Text = "This is a very very very much longer text. This TextBox should now wrap and have a larger height.";
			await WindowHelper.WaitForIdle();
			var height2 = SUT.ActualHeight;

			SUT.Text = "Short";
			await WindowHelper.WaitForIdle();
			var height3 = SUT.ActualHeight;

			Assert.AreEqual(height1, height3);
			height2.Should().BeGreaterThan(height1);
		}

		// Clipboard is currently not available on skia-WASM
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Paste()
		{
#if __SKIA__
			if (OperatingSystem.IsBrowser())
			{
				// TODO: Investigate what happens on Wasm Skia when running this test.
				Assert.Inconclusive("Fails on Wasm Skia for unknown reason");
			}
#endif
			var SUT = new TextBox();

			var pasteCount = 0;
			SUT.Paste += (_, _) => pasteCount++;

			await UITestHelper.Load(SUT);

			var dataPackage = new DataPackage();
			dataPackage.SetText("a");
			Clipboard.SetContent(dataPackage);

			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, pasteCount);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__
		[Ignore("https://github.com/unoplatform/uno/issues/15457")]
#endif
		public async Task When_GotFocus_BringIntoView()
		{
			var tb = new TextBox();
			var ts = new ToggleSwitch();
			var SUT = new ScrollViewer
			{
				Content = new StackPanel
				{
					Spacing = 1200,
					Children =
					{
						tb,
						ts
					}
				}
			};

			await UITestHelper.Load(SUT);

			Assert.AreEqual(0, SUT.VerticalOffset);

			ts.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
#if __WASM__ // wasm needs an additional delay for some reason, probably because of smooth scrolling?
			await Task.Delay(2000);
#endif

			Assert.AreEqual(0, SUT.VerticalOffset);
			SUT.ScrollToVerticalOffset(99999);

			await WindowHelper.WaitForIdle();
#if __WASM__ // wasm needs an additional delay for some reason, probably because of smooth scrolling?
			await Task.Delay(2000);
#endif

			tb.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
#if __WASM__ // wasm needs an additional delay for some reason, probably because of smooth scrolling?
			await Task.Delay(2000);
#endif

			Assert.AreEqual(0, SUT.VerticalOffset);
		}

		[TestMethod]
		public async Task When_VerticalContentAlignment_Is_Changed()
		{
			// This test ensures that setting VerticalContentAlignment on the TextBox doesn't flow to:
			// 1) PlaceholderTextContentPresenter.VerticalContentAlignment
			// 2) ContentElement.VerticalContentAlignment
			// 3) PlaceholderTextContentPresenter.VerticalAlignment
			// 4) ContentElement.VerticalAlignment
			// This matches the behavior observed on Windows
			var xaml = """
				    <Grid>
				        <Grid.Resources>
				            <Style x:Key="TextBoxAlignmentTestStyle" TargetType="TextBox">
				                <Setter Property="Template">
				                    <Setter.Value>
				                        <ControlTemplate TargetType="TextBox">
				                            <Grid x:Name="RootGrid">
				                                <Grid VerticalAlignment="Stretch">
				                                    <ContentControl x:Name="PlaceholderTextContentPresenter" />
				                                    <ContentControl x:Name="ContentElement" />
				                                </Grid>
				                            </Grid>
				                        </ControlTemplate>
				                    </Setter.Value>
				                </Setter>
				            </Style>
				        </Grid.Resources>

				        <TextBox Style="{StaticResource TextBoxAlignmentTestStyle}" />
				    </Grid>
				""";
			var grid = XamlHelper.LoadXaml<Grid>(xaml);
			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForLoaded(grid);
			var textBox = (TextBox)grid.Children.Single();

			Assert.AreEqual(VerticalAlignment.Center, textBox.VerticalContentAlignment);
			textBox.VerticalContentAlignment = VerticalAlignment.Bottom;

#if WINAPPSDK
			var getTemplateChild = typeof(Control).GetMethod("GetTemplateChild", BindingFlags.Instance | BindingFlags.NonPublic);
			var placeHolder = (ContentControl)getTemplateChild.Invoke(textBox, new object[] { "PlaceholderTextContentPresenter" });
			var contentElement = (ContentControl)getTemplateChild.Invoke(textBox, new object[] { "ContentElement" });
#else
			var placeHolder = (ContentControl)textBox.GetTemplateChild("PlaceholderTextContentPresenter");
			var contentElement = (ContentControl)textBox.GetTemplateChild("ContentElement");
#endif
			Assert.AreEqual(VerticalAlignment.Top, placeHolder.VerticalContentAlignment);
			Assert.AreEqual(VerticalAlignment.Top, contentElement.VerticalContentAlignment);
			Assert.AreEqual(VerticalAlignment.Stretch, placeHolder.VerticalAlignment);
			Assert.AreEqual(VerticalAlignment.Stretch, contentElement.VerticalAlignment);
		}

		[TestMethod]
		public async Task When_Size_Zero_Fluent_Default()
		{
			var textBox = await LoadZeroSizeTextBoxAsync(null);

			textBox.ActualWidth.Should().BeApproximately(textBox.MinWidth, 0.1);
			textBox.ActualHeight.Should().BeApproximately(textBox.MinHeight, 0.1);
		}

#if HAS_UNO
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.Wasm | RuntimeTestPlatforms.Android)]
		public async Task When_Focus_Immediately()
		{
			var inputPaneShown = false;
			void Given_TextBox_Showing(InputPane sender, InputPaneVisibilityEventArgs args)
			{
				inputPaneShown = !args.OccludedRect.IsEmpty;
			}
			try
			{
				InputPane.GetForCurrentView().Showing += Given_TextBox_Showing;
				var textBox = new TextBox();
				textBox.MaxLength = 10;
				TestServices.WindowHelper.WindowContent = textBox;
				textBox.Focus(FocusState.Pointer);
				await WindowHelper.WaitForIdle();
				await WindowHelper.WaitFor(() => inputPaneShown);
			}
			finally
			{
				InputPane.GetForCurrentView().Showing -= Given_TextBox_Showing;
			}
		}
#endif

		private void Given_TextBox_Showing(InputPane sender, InputPaneVisibilityEventArgs args) => throw new NotImplementedException();

		[TestMethod]
		public async Task When_Size_Zero_Default()
		{
			using var uwpStyles = StyleHelper.UseUwpStyles();
			var textBox = await LoadZeroSizeTextBoxAsync(null);

			textBox.ActualWidth.Should().Be(0);
			textBox.ActualHeight.Should().Be(0);
		}

		[TestMethod]
		public async Task When_Size_Zero_Fluent_ComboBoxTextBoxStyle()
		{
			var style = Application.Current.Resources["ComboBoxTextBoxStyle"] as Style;

			var textBox = await LoadZeroSizeTextBoxAsync(style);

			textBox.ActualWidth.Should().Be(0);
			textBox.ActualHeight.Should().Be(0);
		}

#if HAS_UNO
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18040")]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Clicking_Outside_ContentElement_Should_Focus()
		{
			var tb1 = new TextBox() { Tag = "First" };
			var tb2 = new TextBox() { Tag = "Second" };
			var stackPanel = new StackPanel
			{
				Children = { tb1, tb2 },
			};

			await UITestHelper.Load(stackPanel);

			var list = new List<string>();

			FocusManager.GotFocus += FocusManager_GotFocus;

			var scp1 = GetSCP(tb1);
			var scp2 = GetSCP(tb2);

			var tb1Bounds = tb1.GetAbsoluteBounds();
			var tb2Bounds = tb2.GetAbsoluteBounds();
			var scp1Bounds = scp1.GetAbsoluteBounds();
			var scp2Bounds = scp2.GetAbsoluteBounds();

			Assert.IsTrue(tb1Bounds.X < scp1Bounds.X);
			Assert.IsTrue(tb2Bounds.X < scp2Bounds.X);

			var clickPosition1 = new Point((tb1Bounds.X + scp1Bounds.X) / 2, (tb1Bounds.Top + tb1Bounds.Bottom) / 2);
			var clickPosition2 = new Point((tb2Bounds.X + scp2Bounds.X) / 2, (tb2Bounds.Top + tb2Bounds.Bottom) / 2);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(clickPosition2);
			Assert.AreEqual(0, list.Count);
			mouse.Press(clickPosition2);
			await WindowHelper.WaitForIdle();
			mouse.Release();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("Second", list[0]);

			mouse.MoveTo(clickPosition1);
			Assert.AreEqual(1, list.Count);
			mouse.Press(clickPosition1);
			await WindowHelper.WaitForIdle();
			mouse.Release();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("First", list[1]);

			FocusManager.GotFocus -= FocusManager_GotFocus;

			void FocusManager_GotFocus(object sender, FocusManagerGotFocusEventArgs e)
				=> list.Add((e.NewFocusedElement as TextBox)?.Tag?.ToString() ?? e.NewFocusedElement?.ToString() ?? "null");

			static FrameworkElement GetSCP(TextBox tb)
			{
				var grid = (Grid)VisualTreeHelper.GetChild(tb, 0);
				foreach (var child in grid.Children)
				{
					if (child is ScrollViewer { Name: "ContentElement" } sv)
					{
						return sv.Content as FrameworkElement;
					}
				}

				Assert.Fail("Cannot find SCP inside TextBox");
				return null;
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18790")]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Clicked_In_Popup()
		{
			TextBox tb;
			var btn = new Button
			{
				Flyout = new Flyout
				{
					Content = tb = new TextBox()
				}
			};

			await UITestHelper.Load(btn);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.Press(btn.GetAbsoluteBoundsRect().GetCenter());
			await UITestHelper.WaitForIdle();
			mouse.Release();
			await UITestHelper.WaitForIdle();

			Assert.IsTrue(btn.Flyout.IsOpen);

			mouse.Press(tb.GetAbsoluteBoundsRect().GetCenter());
			await UITestHelper.WaitForIdle();
			mouse.Release();
			await UITestHelper.WaitForIdle();

			Assert.AreEqual(tb, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		}
#endif

		private static async Task<TextBox> LoadZeroSizeTextBoxAsync(Style style)
		{
			var loaded = false;
			var grid = new Grid()
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			var textBox = new TextBox
			{
				Text = "",
				Width = 0,
				Height = 0
			};
			if (style is not null)
			{
				textBox.Style = style;
			}

			grid.Children.Add(textBox);
			textBox.Loaded += (s, e) => loaded = true;

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitFor(() => loaded);
			await WindowHelper.WaitForIdle(); // Needed to account for lifecycle differences on mobile
			return textBox;
		}
	}
}
