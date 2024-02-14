using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml;
using Windows.UI;
using FluentAssertions;
using MUXControlsTestApp.Utilities;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

#if WINAPPSDK
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_TextBox
	{
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
		public async Task When_Fluent_And_Theme_Changed()
		{
			using (StyleHelper.UseFluentStyles())
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

			Assert.ThrowsException<ArgumentException>(() => textBox.Select(0, -1));
			Assert.ThrowsException<ArgumentException>(() => textBox.Select(-1, 0));
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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

#if __IOS__
		[Ignore("Disabled as not working properly. See https://github.com/unoplatform/uno/issues/8016")]
#endif
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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

#if __IOS__
		[Ignore("Disabled as not working properly. See https://github.com/unoplatform/uno/issues/8016")]
#endif
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
			if (FeatureConfiguration.TextBox.UseOverlayOnSkia)
			{
				Assert.IsInstanceOfType(contentControl.Content, typeof(TextBlock));
				Assert.AreEqual(initialText, ((TextBlock)contentControl.Content).Text);
			}
			else
			{
				Assert.IsInstanceOfType(contentControl.Content, typeof(Grid));
				Assert.AreEqual(initialText, contentControl.FindFirstChild<TextBlock>().Text);
			}
		}
#endif

		[TestMethod]
		public async Task When_Changes_In_TextChanged()
		{
			int update = 0;
			var initialText = "Text";
			string GetCurrentText() => initialText + update;
			string GetNewText() => initialText + ++update;

			var textBox = new TextBox
			{
				Text = "Waiting"
			};

			WindowHelper.WindowContent = textBox;
			await WindowHelper.WaitForLoaded(textBox);

			int textChangedInvokeCount = 0;
			int textChangingInvokeCount = 0;

			bool failedCheck = false;
			bool finished = false;

			void OnTextChanged(object sender, TextChangedEventArgs e)
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __SKIA__ || __IOS__
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

		[TestMethod]
#if __MACOS__
		[Ignore("Paste is not implemented on MacOS")]
#endif
		public async Task When_Paste()
		{
			var SUT = new TextBox();

			var pasteCount = 0;
			SUT.Paste += (_, _) => pasteCount++;

			await UITestHelper.Load(SUT);

			var dataPackage = new DataPackage();
			dataPackage.SetText("a");
			Clipboard.SetContent(dataPackage);

			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(pasteCount, 1);
		}

		[TestMethod]
		[RunsOnUIThread]
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
	}
}
