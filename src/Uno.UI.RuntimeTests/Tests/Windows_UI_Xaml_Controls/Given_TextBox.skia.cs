using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Combinatorial.MSTest;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using SamplesApp.UITests;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;
using Color = Windows.UI.Color;
using Point = Windows.Foundation.Point;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	/// <summary>
	/// This partial is for testing the skia-based TextBox implementation.
	/// Most tests here should set UseOverlayOnSkia to false and HideCaret
	/// to true and then set them back at the end of the test.
	/// </summary>
	public partial class Given_TextBox
	{
		// most macOS keyboard shortcuts uses Command (mapped as Window) and not Control (Ctrl)
		private readonly VirtualKeyModifiers _platformCtrlKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;

		[TestMethod]
		public async Task When_Basic_Input()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var text = "Hello world";
			foreach (var c in text)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			await WindowHelper.WaitForIdle();
			Assert.AreEqual(text, SUT.Text);
			Assert.AreEqual(11, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Basic_Input_Event_Sequence()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var eventLog = "";
			SUT.KeyDown += (_, _) => eventLog += $"KeyDown Text={SUT.Text} SelectionStart={SUT.SelectionStart} SelectionLength={SUT.SelectionLength}\n";
			SUT.KeyUp += (_, _) => eventLog += $"KeyUp Text={SUT.Text} SelectionStart={SUT.SelectionStart} SelectionLength={SUT.SelectionLength}\n";
			SUT.PreviewKeyDown += (_, _) => eventLog += $"PreviewKeyDown Text={SUT.Text} SelectionStart={SUT.SelectionStart} SelectionLength={SUT.SelectionLength}\n";
			SUT.PreviewKeyUp += (_, _) => eventLog += $"PreviewKeyUpKeyUp Text={SUT.Text} SelectionStart={SUT.SelectionStart} SelectionLength={SUT.SelectionLength}\n";
			SUT.SelectionChanging += (_, _) => eventLog += $"SelectionChanging Text={SUT.Text} SelectionStart={SUT.SelectionStart} SelectionLength={SUT.SelectionLength}\n";
			SUT.SelectionChanged += (_, _) => eventLog += $"SelectionChanged Text={SUT.Text} SelectionStart={SUT.SelectionStart} SelectionLength={SUT.SelectionLength}\n";
			SUT.TextChanged += (_, _) => eventLog += $"TextChanged Text={SUT.Text} SelectionStart={SUT.SelectionStart} SelectionLength={SUT.SelectionLength}\n";

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: 'a'));
			await Task.Delay(10);
			SUT.SafeRaiseEvent(UIElement.KeyUpEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: 'a'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(
				"""
                PreviewKeyDown Text= SelectionStart=0 SelectionLength=0
                KeyDown Text= SelectionStart=0 SelectionLength=0
                SelectionChanging Text= SelectionStart=0 SelectionLength=0
                SelectionChanged Text=a SelectionStart=1 SelectionLength=0
                TextChanged Text=a SelectionStart=1 SelectionLength=0
                PreviewKeyUpKeyUp Text=a SelectionStart=1 SelectionLength=0
                KeyUp Text=a SelectionStart=1 SelectionLength=0
                
                """, eventLog);
		}

		[TestMethod]
		public async Task When_Basic_Input_With_ArrowKeys()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			foreach (var c in "world")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			await WindowHelper.WaitForIdle();
			Assert.AreEqual(5, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			for (int i = 1; i <= 5; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(5 - i, SUT.SelectionStart);
				Assert.AreEqual(0, SUT.SelectionLength);
			}

			foreach (var c in "Hello ")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Hello world", SUT.Text);
			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Basic_Input_With_Home_End()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			foreach (var c in "world")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			foreach (var c in "Hello ")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Hello world", SUT.Text);
			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.End, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(11, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Selection_With_Keyboard_NoMod_And_Shift()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			foreach (var c in "Hello world")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			await WindowHelper.WaitForIdle();

			for (var i = 1; i <= 11; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(11 - i, SUT.SelectionStart);
				Assert.AreEqual(i, SUT.SelectionLength);
			}

			for (var i = 1; i <= 5; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(i, SUT.SelectionStart);
				Assert.AreEqual(11 - i, SUT.SelectionLength);
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(5, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.End, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(5, SUT.SelectionStart);
			Assert.AreEqual(6, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(11, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(11, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(11, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Keyboard_Selection_Backwards_ScrollViewer_Offset()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox { Width = 150 };

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			foreach (var c in "some text that is longer than the width of the text box")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}
			await WindowHelper.WaitForIdle();

			await Task.Delay(1000); // Allow the ScrollViewer to update its offset

			Assert.AreNotEqual(0, ((ScrollViewer)SUT.ContentElement).HorizontalOffset);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();

			await Task.Delay(1000); // Allow the ScrollViewer to update its offset

			Assert.AreEqual(0, ((ScrollViewer)SUT.ContentElement).HorizontalOffset);

			for (int i = 0; i < 5; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift));
				await WindowHelper.WaitForIdle();
			}

			await Task.Delay(1000); // Allow the ScrollViewer to update its offset

			// The ScrollViewer shouldn't move as long as the caret is still in view.
			Assert.AreEqual(0, ((ScrollViewer)SUT.ContentElement).HorizontalOffset);
		}

		[TestMethod]
		public async Task When_Ctrl_End_ScrollViewer_Vertical_Offset()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 400,
				Height = 90,
				TextWrapping = TextWrapping.Wrap,
				Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Gravida dictum fusce ut placerat orci nulla. Luctus venenatis lectus magna fringilla urna porttitor rhoncus. Faucibus vitae aliquet nec ullamcorper. Sem fringilla ut morbi tincidunt. Imperdiet proin fermentum leo vel orci. Velit aliquet sagittis id consectetur. Faucibus et molestie ac feugiat sed lectus vestibulum. Morbi enim nunc faucibus a pellentesque sit amet porttitor. Elementum sagittis vitae et leo duis ut diam. Pulvinar pellentesque habitant morbi tristique senectus et netus et malesuada. Id porta nibh venenatis cras sed felis eget velit aliquet. Feugiat pretium nibh ipsum consequat nisl. Adipiscing diam donec adipiscing tristique risus nec feugiat. Consequat semper viverra nam libero justo laoreet sit. Non tellus orci ac auctor augue mauris augue neque. Dolor purus non enim praesent."
			};

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, ((ScrollViewer)SUT.ContentElement).VerticalOffset);

			// on macOS moving to the end of the document is done with `Command` + `Down`
			var macOS = OperatingSystem.IsMacOS();
			var key = macOS ? VirtualKey.Down : VirtualKey.End;
			var mod = macOS ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, key, mod));
			await WindowHelper.WaitForIdle();

			await Task.Delay(1000); // Allow the ScrollViewer to update its offset

			((ScrollViewer)SUT.ContentElement).VerticalOffset.Should().BeApproximately(((ScrollViewer)SUT.ContentElement).ScrollableHeight, 1.0);
		}

		[TestMethod]
		public async Task When_Ctrl_A()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "hello world"
			};

			var keyDownCount = 0;
			SUT.KeyDown += (_, _) => keyDownCount++;

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.A, _platformCtrlKey, unicodeKey: 'a'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, keyDownCount);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.A, _platformCtrlKey, unicodeKey: 'a'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, keyDownCount);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Shift()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "hello world"
			};

			var keyDownCount = 0;
			SUT.KeyDown += (_, _) => keyDownCount++;

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Shift, VirtualKeyModifiers.None, unicodeKey: '\0'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello world", SUT.Text);
		}

		[TestMethod]
		public async Task When_Ctrl_Home_End()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true,
				Text = "lorem\nipsum\r\ndolor"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var key = OperatingSystem.IsMacOS() ? VirtualKey.Down : VirtualKey.End;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, key, _platformCtrlKey));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			key = OperatingSystem.IsMacOS() ? VirtualKey.Up : VirtualKey.Home;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, key, _platformCtrlKey));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Ctrl_Delete()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "lorem ipsum dolor"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			// on macOS it's option (menu/alt) and backspace to delete a word
			var mod = OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.Control;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, mod));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("ipsum dolor", SUT.Text);
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, mod));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("dolor", SUT.Text);
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Ctrl_Backspace()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "lorem ipsum dolor"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(SUT.Text.Length, 0);
			await WindowHelper.WaitForIdle();

			// on macOS it's option (menu/alt) and backspace to delete a word
			var mod = OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.Control;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, mod));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("lorem ipsum ", SUT.Text);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, mod));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("lorem ", SUT.Text);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Enter_But_Not_Multiline()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(2, 0);
			await WindowHelper.WaitForIdle();

			var size = SUT.ActualSize;

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Enter, VirtualKeyModifiers.None, unicodeKey: '\r'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello world", SUT.Text);
			Assert.AreEqual(2, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
			Assert.AreEqual(size, SUT.ActualSize);
		}

		[TestMethod]
		public async Task When_Selection_With_Keyboard_NoMod_Ctrl_And_Shift()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			foreach (var c in "Hello &(%&^( w0.rld")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();

			// on macOS you use `option` (alt/menu) and `right` to move to the next work
			var mod = OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.Control;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(13, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(15, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(16, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(19, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Text_Bigger_Than_TextBox()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			foreach (var c in "This should be a lot longer than the width of the TextBox.")
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}
			await WindowHelper.WaitForIdle();

			var sv = SUT.FindVisualChildByType<ScrollViewer>();

			await Task.Delay(1000); // Allow the ScrollViewer to update its offset
			sv.HorizontalOffset.Should().BeGreaterThan(0);

			var isiOS = OperatingSystem.IsIOS();
			if (!isiOS)
			{
				//TODO: this is flaky on iOS. Fails on CI but passes locally.
				Assert.AreEqual(sv.ScrollableWidth, sv.HorizontalOffset, "HorizontalOffset should be equal to ScrollableWidth after typing long text");
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.None));
			await Task.Delay(1000); // Allow the ScrollViewer to update its offset
			sv.ScrollableWidth.Should().BeGreaterThan(0);
			Assert.AreEqual(0, sv.HorizontalOffset, "HorizontalOffset should be 0 after Home key press");
		}

		[TestMethod]
		public async Task When_KeyDown_Bubbles_Out()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 100,
				Text = "Hello world"
			};

			var keyDownCount = 0;
			SUT.KeyDown += (_, _) => keyDownCount++;

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.End, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.End, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, keyDownCount);


			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.Select(2, 0);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(4, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(4, keyDownCount);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(5, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(6, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.Select(0, 0);
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(7, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(7, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.Select(SUT.Text.Length, 0);
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(7, keyDownCount);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.A, VirtualKeyModifiers.None, unicodeKey: 'A'));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(8, keyDownCount);
		}

		[TestMethod]
		public async Task When_Selection_Initial_Then_Text_Changed()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40,
				Text = "Initial"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(SUT.Text.Length, 0);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Text = "Changed";

			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_ReadOnly()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40,
				Text = "Initial",
				IsReadOnly = true
			};

			var keyDownCount = 0;
			SUT.KeyDown += (_, _) => keyDownCount++;

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.A, VirtualKeyModifiers.None, unicodeKey: 'A'));

			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Initial", SUT.Text);
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
			Assert.AreEqual(1, keyDownCount);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));

			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
			Assert.AreEqual(2, keyDownCount);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));

			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
			Assert.AreEqual(2, keyDownCount);
		}

		[TestMethod]
		public async Task When_Long_Text_Unfocused()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 80,
				Text = "This should be a lot longer than the width of the TextBox."
			};

			var btn = new Button();

			var sp = new StackPanel
			{
				Children =
				{
					SUT,
					btn
				}
			};


			WindowHelper.WindowContent = sp;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			var sv = SUT.FindVisualChildByType<ScrollViewer>();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(SUT.Text.Length, 0);
			await WindowHelper.WaitForIdle();
			// DeleteButton Takes space to the right of sv
			LayoutInformation.GetLayoutSlot(SUT).Right.Should().BeGreaterThan(LayoutInformation.GetLayoutSlot(sv).Right + 10);

			btn.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			LayoutInformation.GetLayoutSlot(SUT).Right.Should().BeLessThan(LayoutInformation.GetLayoutSlot(sv).Right + 10);
		}

		[TestMethod]
		public async Task When_Scrolling_Updates_With_Movement()
		{
			if (OperatingSystem.IsLinux() || OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("There are small differences in fonts between Linux and other platforms, so the numbers aren't exactly the same.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 64, // == TextControlThemeMinWidth for UWP styles
				Text = "This should be a lot longer than the width of the TextBox."
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			var sv = SUT.FindVisualChildByType<ScrollViewer>();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(SUT.Text.Length, 0);
			await WindowHelper.WaitForIdle();
			await Task.Delay(600); // Allow the ScrollViewer to update its offset
			Assert.AreEqual(sv.ScrollableWidth, sv.HorizontalOffset, "sv.ScrollableWidth is not equal to sv.HorizontalOffset");

			for (var i = 0; i < 6; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
				await WindowHelper.WaitForIdle();
				await Task.Delay(200); // Allow the ScrollViewer to update its offset
				Assert.AreEqual(sv.ScrollableWidth, sv.HorizontalOffset, $"Index: {i} sv.ScrollableWidth is not equal to sv.HorizontalOffset");
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			await Task.Delay(600); // Allow the ScrollViewer to update its offset
			sv.HorizontalOffset.Should().BeLessThan(sv.ScrollableWidth);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			await Task.Delay(600); // Allow the ScrollViewer to update its offset
			Assert.AreEqual(0, sv.HorizontalOffset);

			for (var i = 0; i < 6; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
				await WindowHelper.WaitForIdle();
				await Task.Delay(600); // Allow the ScrollViewer to update its offset
				Assert.AreEqual(0, sv.HorizontalOffset, $"Index: {i} sv.HorizontalOffset is not 0");
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			await Task.Delay(600); // Allow the ScrollViewer to update its offset
			sv.HorizontalOffset.Should().BeGreaterThan(0);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Scrolling_Updates_After_Backspace()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "This should be a lot longer than the width of the TextBox."
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			var sv = SUT.FindVisualChildByType<ScrollViewer>();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(SUT.Text.Length, 0);
			await WindowHelper.WaitForIdle();
			// DeleteButton Takes space to the right of sv
			LayoutInformation.GetLayoutSlot(SUT).Right.Should().BeGreaterThan(LayoutInformation.GetLayoutSlot(sv).Right + 10);

			var svRight = LayoutInformation.GetLayoutSlot(SUT).Right;
			for (var i = 0; i < 5; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(svRight, LayoutInformation.GetLayoutSlot(SUT).Right);
			}

			// on macOS we use `option` (menu/alt) + `delete` to remove word at the left
			var mod = OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.Control;
			for (var i = 0; i < 10; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, mod));
			}
			await WindowHelper.WaitForIdle();

			// Accounting for font difference on Wasm Skia, until we unify with Open Sans.
			Assert.AreEqual(OperatingSystem.IsBrowser() ? 4 : 0, sv.ScrollableWidth);
		}

		// Clipboard is currently not available on skia-WASM
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Scrolling_Updates_After_Pasting_Long_Text()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			var text = "This should be a lot longer than the width of the TextBox.";
			dp.SetText(text);
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();

#if HAS_UNO
			// The animation may take some time to finish

			await WindowHelper.WaitFor(() =>
			{
				if (SUT.ContentElement is ScrollViewer sv)
				{
					return Math.Abs(sv.HorizontalOffset - sv.ScrollableWidth) < 1.0;
				}

				return false;
			}, 5000);
#endif

			((ScrollViewer)SUT.ContentElement).HorizontalOffset.Should().BeApproximately(((ScrollViewer)SUT.ContentElement).ScrollableWidth, 5.0);
		}

		[TestMethod]
		public async Task When_Pointer_Tap()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Pointer_Tap_After_Ending_Spaces()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 350,
				Text = "Hello world          ",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(new Point(bounds.Right - 30, bounds.GetMidY()));
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Pointer_Shift_Tap()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 130,
				Text = "Hello world",
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var selectionEnd = SUT.SelectionStart;

			mouse.MoveBy(-20, 0);
			mouse.Press(VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(selectionEnd, SUT.SelectionStart + SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Pointer_RightClick_No_Selection()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.PressRight();
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit)] // Fails in Skia UIKit CI - https://github.com/unoplatform/uno-private/issues/808
		public async Task When_Pointer_RightClick_Selection()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = new FontFamily("ms-appx:///Assets/Fonts/OpenSans/OpenSans.ttf")
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(2, 2);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			// Right tapping should move the caret to the current pointer location and open the context menu
			mouse.PressRight();
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-100, 0); // click out
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			// clicking inside the TextBox to dismiss the context menu should NOT move the caret
			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Pointer_Hold_Drag()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-51, 0);

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Pointer_Hold_Drag_OutOfBounds()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(0, 50);
			mouse.MoveBy(-150, 0);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(10, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_LongText_Pointer_Hold_Drag_OutOfBounds()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "This should be a lot longer than the width of the TextBox.",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(0, 50);
			mouse.MoveBy(-150, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(10, SUT.SelectionLength);

			mouse.MoveBy(0, 50);
			mouse.MoveBy(600, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length - 10, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Chunk_DoubleTapped()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(5, SUT.SelectionLength);

			// the selection should start on the left
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(4, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Chunk_DoubleTapHeld()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(5, SUT.SelectionLength);

			mouse.MoveBy(-40, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionLength);

			mouse.Release();
			await WindowHelper.WaitForIdle();

			// the selection should start on the right
			Assert.IsTrue(SUT.IsBackwardSelection);
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length - 1, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Chunk_TripleTapped()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionLength);

			// the selection should start on the left
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length - 1, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Typing_While_Pointer_Held()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-51, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Hello world", SUT.Text);
			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);
		}

		[TestMethod]
		[DataRow(VirtualKey.Left, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.Right, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.Up, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.Down, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.Home, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.End, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.Back, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.Delete, VirtualKeyModifiers.None)]
		[DataRow(VirtualKey.A, VirtualKeyModifiers.Control)]
		public async Task When_Move_Caret_While_Pointer_Held(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			var handled = false;
			SUT.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) => handled = e.Handled), true);

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-51, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, key, modifiers));
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(handled);
			Assert.AreEqual("Hello world", SUT.Text);
			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Cut_While_Pointer_Held()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-51, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.X, _platformCtrlKey));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Hd", SUT.Text);

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(10, -1);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(1, SUT.SelectionLength);
		}

		[TestMethod]
#if __SKIA__
		[Ignore("Disabled due to https://github.com/unoplatform/uno-private/issues/878")]
#endif
		public async Task When_Paste_While_Pointer_Held()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Clipboard can't be read in managed code for security reasons.
				// An actual attempt to paste will work, because the native HTML
				// input is what will receive the key event, and the browser will be
				// responsible for changing the text.
				Assert.Inconclusive("Skipped on Wasm Skia due to clipboard-related issues.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var dp = new DataPackage();
			var text = "copied content";
			dp.SetText(text);
			Clipboard.SetContent(dp);

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-51, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.V, _platformCtrlKey));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Hcopied contentd", SUT.Text);
			Assert.AreEqual(15, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(0, -1); // nudge the mouse a bit to recalculate selection, this is the behaviour on WinUI as well
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.SelectionStart);
			Assert.AreEqual(13, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Escape_While_Pointer_Held()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-51, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Escape, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();

			mouse.MoveBy(-10, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);

			// We're pretty much "not pressed" at all at this point, even if we're technically still holding the mouse
			// so we can actually type stuff in!

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.A, VirtualKeyModifiers.None, unicodeKey: 'a'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Had", SUT.Text);
			Assert.AreEqual(2, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_NonAscii_Characters()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var text = "صباح الخير";
			foreach (var c in text)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			}

			await WindowHelper.WaitForIdle();
			Assert.AreEqual(text, SUT.Text);
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_Copy_Paste(bool useInsert)
		{
			if (useInsert && OperatingSystem.IsMacOS())
			{
				Assert.Inconclusive("There's no `Insert` key on Mac keyboards");
				// it's replaced by the `fn` key, which is a modifier
			}
			if (OperatingSystem.IsBrowser())
			{
				// Clipboard can't be read in managed code for security reasons.
				// An actual attempt to paste will work, because the native HTML
				// input is what will receive the key event, and the browser will be
				// responsible for changing the text.
				Assert.Inconclusive("Skipped on Wasm Skia due to clipboard-related issues.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150
			};

			var handled = false;
			SUT.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, args) => handled |= args.Handled), true);

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			var dp = new DataPackage();
			var text = "copied content";
			dp.SetText(text);
			Clipboard.SetContent(dp);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			void Paste()
			{
				if (useInsert)
				{
					SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Insert, VirtualKeyModifiers.Shift));
				}
				else
				{
					SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.V, _platformCtrlKey, unicodeKey: 'v'));
				}
			}

			void Copy()
			{
				if (useInsert)
				{
					SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Insert, VirtualKeyModifiers.Control));
				}
				else
				{
					SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.C, _platformCtrlKey, unicodeKey: 'c'));
				}
			}

			Paste();
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(handled);
			Assert.AreEqual(text, SUT.Text);

			SUT.Select(2, 4);
			await WindowHelper.WaitForIdle();
			Copy();
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(handled);
			Assert.AreEqual(SUT.Text.Substring(2, 4), await Clipboard.GetContent()!.GetTextAsync());
			Assert.AreEqual(2, SUT.SelectionStart);
			Assert.AreEqual(4, SUT.SelectionLength);

			SUT.Select(SUT.Text.Length - 1, 0);
			await WindowHelper.WaitForIdle();
			Paste();
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(handled);
			Assert.AreEqual("copied contenpiedt", SUT.Text);
			Assert.AreEqual(SUT.Text.Length - 1, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(6, 3);
			await WindowHelper.WaitForIdle();
			Paste();
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(handled);
			Assert.AreEqual("copiedpiedntenpiedt", SUT.Text);
			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Cut_Paste()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Clipboard can't be read in managed code for security reasons.
				// An actual attempt to paste will work, because the native HTML
				// input is what will receive the key event, and the browser will be
				// responsible for changing the text.
				Assert.Inconclusive("Skipped on Wasm Skia due to clipboard-related issues.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "Hello world"
			};

			var handled = false;
			SUT.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, args) => handled |= args.Handled), true);

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Select(2, 4);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.X, _platformCtrlKey, unicodeKey: 'x'));
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(handled);
			Assert.AreEqual("llo ", await Clipboard.GetContent()!.GetTextAsync());
			Assert.AreEqual("Heworld", SUT.Text);
			Assert.AreEqual(2, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(SUT.Text.Length - 1, 0);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.V, _platformCtrlKey, unicodeKey: 'v'));
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(handled);
			Assert.AreEqual("Heworlllo d", SUT.Text);
			Assert.AreEqual(SUT.Text.Length - 1, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(6, 3);
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.V, _platformCtrlKey, unicodeKey: 'v'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Heworlllo  d", SUT.Text);
			Assert.AreEqual(10, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		// Clipboard is currently not available on skia-WASM
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Paste_History_Remains_Intact()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "initial"
			};

			WindowHelper.WindowContent = SUT;

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			var text = "copied content";
			dp.SetText(text);
			Clipboard.SetContent(dp);

			// This actually matches WinUI. text comes before "initial" and text2 comes after text

			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(text + "initial", SUT.Text);

			var dp2 = new DataPackage();
			var text2 = "copied content 2";
			dp2.SetText(text2);
			Clipboard.SetContent(dp2);

			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(text + text2 + "initial", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(text + "initial", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("initial", SUT.Text);
		}

		// Clipboard is currently not available on skia-WASM
		// Newline handling is different on Skia.UIKit targets due to native input sync #788
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Paste_The_Same_Text()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true,
				Text = "copied\r\ncontent"
			};

			WindowHelper.WindowContent = SUT;

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			var text = "copied\r\ncontent";
			dp.SetText(text);
			Clipboard.SetContent(dp);

			SUT.SelectAll();
			await WindowHelper.WaitForIdle();
			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("copied\rcontent".Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_Simple()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Text = "hello";
			await WindowHelper.WaitForIdle();

			var height = SUT.ActualHeight;

			SUT.Text = "hello\rworld";
			await WindowHelper.WaitForIdle();

			SUT.ActualHeight.Should().BeGreaterThan(height * 1.2);
		}

		[TestMethod]
		public async Task When_Multiline_LineFeed()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Text = "lorem\nipsum\r\ndolor";

			await WindowHelper.WaitForIdle();
			Assert.AreEqual("lorem\ripsum\rdolor", SUT.Text);
		}

		[TestMethod]
		public async Task When_Multiline_Return_Selected()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Text = "hello\rworld";
			SUT.Select(4, 0);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("o\r", SUT.SelectedText);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("o\rw", SUT.SelectedText);
		}

		[TestMethod]
		public async Task When_Up_WithWithout_Shift()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "Lorem ipsum"
			};

			var keyDownCount = 0;
			SUT.KeyDown += (_, _) => keyDownCount++;

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Select(4, 0);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(4, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
			Assert.AreEqual(1, keyDownCount);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(4, SUT.SelectionLength);
			Assert.AreEqual(1, keyDownCount);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Multiline_NewLine_UpDown()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true,
				Text = "Lorem ipsum\rdolor sit\ramet consectetur\radipiscing"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(17, 0);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			// Accounting for font difference on Wasm Skia, until we unify with Open Sans.
			Assert.AreEqual(OperatingSystem.IsBrowser() ? 5 : 4, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(17, SUT.SelectionStart); // notice how up -> down -> up doesn't necessarily end up back where it started, this is correct
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			// Accounting for font difference on Wasm Skia, until we unify with Open Sans.
			Assert.AreEqual(OperatingSystem.IsBrowser() ? 5 : 4, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(17, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_UpDown_Caret_Position_Preserved()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true,
				Text = "abcdef\rabc\rabcdefghi"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select("abcdef".Length, 0);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("abcdef".Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("abcdef\rabc".Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("abcdef".Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("abcdef\rabc\rabcdef".Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			// hitting down on the last line goes to the end BUT doesn't change the logical caret column position.
			Assert.AreEqual("abcdef\rabc\rabcdefghi".Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("abcdef\rabc".Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Multiline_Wrapping_UpDown()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 120,
				TextWrapping = TextWrapping.Wrap,
				Text = "Lorem ipsum dolor sit amet consectetur adipiscing"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(17, 0);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			// Accounting for font difference on Wasm Skia, until we unify with Open Sans.
			Assert.AreEqual(OperatingSystem.IsBrowser() ? 5 : 4, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			// Accounting for font difference on Wasm Skia, until we unify with Open Sans.
			Assert.AreEqual(17, SUT.SelectionStart); // notice how up -> down -> up doesn't necessarily end up back where it started, this is correct
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Up, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			// Accounting for font difference on Wasm Skia, until we unify with Open Sans.
			Assert.AreEqual(OperatingSystem.IsBrowser() ? 5 : 4, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Down, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			// Accounting for font difference on Wasm Skia, until we unify with Open Sans.
			Assert.AreEqual(17, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_NewLine_LeftRight()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				AcceptsReturn = true,
				Text = "Lorem ipsum\rdolor sit\ramet consectetur\radipiscing"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(11, 0);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(12, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_Wrapping_LeftRight()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 120,
				TextWrapping = TextWrapping.Wrap,
				Text = "Lorem ipsum dolor sit amet consectetur adipiscing"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.Select(11, 0);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(12, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_Keyboard_Chunking()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true,
				Text =
				"""
				Lorem 
				     
				
				ipsum

				&&^
				    
				
				
				"""
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			// on macOS selecting the next word is `shift` + `option` (alt/menu) + `right`
			var mod = VirtualKeyModifiers.Shift | (OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.Control);
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(6, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(7, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(12, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(13, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(14, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(19, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(20, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(21, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(24, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(25, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(29, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(30, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, mod));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(31, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_Text_Ends_In_Return()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				AcceptsReturn = true,
				Text = "hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var height = SUT.ActualHeight;

			SUT.Text += "\r";

			SUT.ActualHeight.Should().BeGreaterThan(height * 1.2);
		}

		[TestMethod]
		public async Task When_Multiline_Wrapping_Text_Ends_In_Too_Many_Spaces()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				Text = "hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var height = SUT.ActualHeight;

			SUT.Text = "mmmmmmmmm               ";

			// Trailing space shouldn't wrap
			Assert.AreEqual(height, SUT.ActualHeight);
		}

		[TestMethod]
		public async Task When_Text_Changed_Events()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "hello world"
			};

			var output = "";
			SUT.TextChanged += (o, _) => output += $"TextChanged {((TextBox)o).Text}\n";
			SUT.SelectionChanged += (o, _) => output += $"SelectionChanged {((TextBox)o).Text}\n";

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.A, VirtualKeyModifiers.Shift, unicodeKey: 'a'));
			await WindowHelper.WaitForIdle();

			var expected =
			"""
			TextChanged hello world
			SelectionChanged ahello world
			TextChanged ahello world
			
			""";

			Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18371")]
		public async Task When_BeforeTextChanging_Resets_Selection_Direction()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "adasgasg"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(SUT.Text.Length, 0);
			await WindowHelper.WaitForIdle();

			// Select from the end to right after the first character
			for (int i = 0; i < SUT.Text.Length - 1; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift));
			}

			SUT.BeforeTextChanging += (_, args) => args.Cancel = args.NewText == "as";

			var selectionChangedCount = 0;
			SUT.SelectionChanged += (_, _) => selectionChangedCount++;

			await KeyboardHelper.InputText("s");
			Assert.AreEqual(0, selectionChangedCount);

			// when we press Shift+Left now, the selection "end" is on the right, so the selection shrinks.
			Assert.AreEqual(SUT.Text.Length - 1, SUT.SelectionLength);
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift));
			Assert.AreEqual(SUT.Text.Length - 2, SUT.SelectionLength);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit)] // Fails in Skia UIKit CI - https://github.com/unoplatform/uno-private/issues/808
		public async Task When_Multiline_Pointer_Tap()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Temporarily: Wasm Skia can't use Arial so the coordinates being pressed are not what we expect.
				// In future when we have Open Sans by default, we'll need to remove the use of Arial and maybe
				// adjust the coordinates so that they do what we want. Then the test will become stable on Skia Desktop and Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 250,
				AcceptsReturn = true,
				Text = "Lorem\ripsum dolor sit\ramet consectetur\radipiscing",
				FontFamily = "Arial" // no Segoe UI on Linux, so we set something common
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(38, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveTo(bounds.GetCenter() - new Point(40, 10));
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(17, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_Pointer_DoubleTap()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 250,
				AcceptsReturn = true,
				Text = "Lorem\ripsum dolor sit\ramet consectetur\radipiscing"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			mouse.MoveTo(bounds.GetCenter() - new Point(40, 10));
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(12, SUT.SelectionStart);
			Assert.AreEqual(6, SUT.SelectionLength);

			mouse.MoveBy(-41, 10);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(12, SUT.SelectionStart);
			Assert.AreEqual(15, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift));

			Assert.AreEqual(12, SUT.SelectionStart);
			Assert.AreEqual(14, SUT.SelectionLength);

			mouse.Press();
			mouse.Release();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(22, SUT.SelectionStart);
			Assert.AreEqual(5, SUT.SelectionLength);

			mouse.MoveBy(41, -10);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(12, SUT.SelectionStart);
			Assert.AreEqual(15, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift));

			Assert.AreEqual(13, SUT.SelectionStart);
			Assert.AreEqual(14, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_Wrapping_Pointer_DoubleTap()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				Text = "first line\rsecond longlonglongworddddddddddddddd"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			mouse.Press();
			mouse.Release();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			// clicking at the end of a wrapping line should select starting from the wrapped part of the line (i.e. the continuing line after)
			Assert.AreEqual(18, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length - 18, SUT.SelectionLength);
		}

		// Clipboard is currently not available on skia-WASM
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SurrogatePair_Copy()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Clipboard can't be read for security reasons.
				Assert.Inconclusive("Skipped on Wasm Skia due to clipboard-related issues.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Text = "🚫 Hello world"
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			SUT.CopySelectionToClipboard();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Hello ", await Clipboard.GetContent()!.GetTextAsync());
		}

		[TestMethod]
		public async Task When_Multiline_Pointer_TripleTap()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 250,
				AcceptsReturn = true,
				Text = "elit aliquam\rullamcorper\rcommodoprimis\rornare himenaeos"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(25, SUT.SelectionStart);
			Assert.AreEqual(14, SUT.SelectionLength);

			mouse.MoveBy(0, 20);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(25, SUT.SelectionStart);
			Assert.AreEqual(30, SUT.SelectionLength);

			mouse.MoveBy(0, -30);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(13, SUT.SelectionStart);
			Assert.AreEqual(26, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Multiline_Pointer_TripleTap_With_Wrapping()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 250,
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				FontSize = 12,
				Text =
					"""
					Lorem ipsum dolor sit amet consectetur adipiscing, elit aliquam u
					llamcorper commodo primis ornare himenaeos, inceptos tellus accumsan praesent laoreet. Pharetra semper ullamcorper neque mollis vestibulum luctus gravida facilisi rhoncus, rutrum massa bibendum vitae imp
					erdiet quisque fames dignissim, varius curae erat risus platea orci quis scelerisque. Auctor erat vestibulum enim sodales sapien nam litora rhoncus condimentum praesent, platea dui odio eros integer id gravida turpis semper nisi maecenas, nascetur dictumst sed arcu aenean varius dis leo habitant.
					"""
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(270, SUT.SelectionStart);
			Assert.AreEqual(297, SUT.SelectionLength);

			mouse.MoveBy(0, -50);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(66, SUT.SelectionStart);
			Assert.AreEqual(501, SUT.SelectionLength);

			mouse.MoveBy(0, -100);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Text_Cleared_No_Paint()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 250,
				Text = "Hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SelectAll();
			await WindowHelper.WaitForIdle();
			await Task.Delay(100);

			var canvas = SUT.FindVisualChildByType<ScrollViewer>();
			var initial = await UITestHelper.ScreenShot(canvas);
			ImageAssert.HasColorInRectangle(initial, new Rectangle(System.Drawing.Point.Empty, initial.Size), SUT.SelectionHighlightColor.Color);

			SUT.Text = "";
			await WindowHelper.WaitForIdle();
			await Task.Delay(100);

			// No residual colors on canvas
			var cleared = await UITestHelper.ScreenShot(canvas);
			ImageAssert.DoesNotHaveColorInRectangle(cleared, new Rectangle(System.Drawing.Point.Empty, cleared.Size), SUT.SelectionHighlightColor.Color);
		}

		[TestMethod]
		public async Task When_Undo_Redo_Basic()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Undo_Redo_Keyboard_Basic()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Z, _platformCtrlKey, unicodeKey: 'z'));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Y, _platformCtrlKey, unicodeKey: 'z'));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Typing_with_Backspace_Undo_Redo()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Typing_Over_Selection_Undo_Redo()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Select(6, 5);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello hello", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello world", SUT.Text);
			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(5, SUT.SelectionLength);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello hello", SUT.Text);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Undo_Redo_ContextMenu_Basic()
		{
			using var _ = new TextBoxFeatureConfigDisposable();
			using var __ = new DisposableAction(() => (VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot)).ForEach((_, p) => p.IsOpen = false));

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.PressRight();
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			var flyoutItems = (VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)[0].Child as FrameworkElement).FindChildren<MenuFlyoutItem>().ToList();
			Assert.AreEqual(3, flyoutItems.Count);

			mouse.MoveTo(flyoutItems[1].GetAbsoluteBounds().GetCenter());
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.PressRight();
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			flyoutItems = (VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)[0].Child as FrameworkElement).FindChildren<MenuFlyoutItem>().ToList();
			Assert.AreEqual(3, flyoutItems.Count);

			mouse.MoveTo(flyoutItems[1].GetAbsoluteBounds().GetCenter());
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Right_Tap_Selection_Persists()
		{
			if (OperatingSystem.IsIOS())
			{
				Assert.Inconclusive("Currently failing on iOS");
				return;
			}

			using var _ = new TextBoxFeatureConfigDisposable();
			using var __ = new DisposableAction(() => (VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot)).ForEach((_, p) => p.IsOpen = false));

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().Location + new Point(10, 10));
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.MoveBy(8, 0);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var selection = (SUT.SelectionStart, SUT.SelectionLength);

			mouse.MoveBy(-4, 0);
			mouse.PressRight();
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(selection, (SUT.SelectionStart, SUT.SelectionLength));
		}

		[TestMethod]
		public async Task When_Text_Changed_History_Cleared()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			SUT.Text = "Changed";
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Changed", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Changed", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Changed", SUT.Text);
		}

		[TestMethod]
		public async Task When_ClearUndoRedoHistory()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			SUT.ClearUndoRedoHistory();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Typing_Nothing()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None)); // break typing run
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("he", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("he", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
		}

		[TestMethod]
		[DataRow(VirtualKey.Y)] // redo
		[DataRow(VirtualKey.C)] // copy
		public async Task When_Redo_Copy_DoesNot_Break_Typing(VirtualKey key)
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, key, VirtualKeyModifiers.Control));
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
		}

		[TestMethod]
		[DataRow(VirtualKey.X)] // cut
		[DataRow(VirtualKey.V)] // paste
		public async Task When_Cut_Paste_Breaks_Typing(VirtualKey key)
		{
			if (OperatingSystem.IsBrowser())
			{
				// Clipboard can't be read in managed code for security reasons.
				// An actual attempt to paste will work, because the native HTML
				// input is what will receive the key event, and the browser will be
				// responsible for changing the text.
				Assert.Inconclusive("Skipped on Wasm Skia due to clipboard-related issues.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var dataPackage = new DataPackage();
			dataPackage.SetText("");
			Clipboard.SetContent(dataPackage); // even with nothing to paste, typing still breaks

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, key, _platformCtrlKey));
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("he", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("he", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
		}

		[TestMethod]
		public async Task When_CanRedo_CanUndo()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.CanRedo);
			Assert.IsFalse(SUT.CanUndo);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None)); // break typing run
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hellohello", SUT.Text);

			Assert.IsFalse(SUT.CanRedo);
			Assert.IsTrue(SUT.CanUndo);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);

			Assert.IsTrue(SUT.CanRedo);
			Assert.IsTrue(SUT.CanUndo);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);

			Assert.IsTrue(SUT.CanRedo);
			Assert.IsFalse(SUT.CanUndo);

			SUT.Redo();

			Assert.IsTrue(SUT.CanRedo);
			Assert.IsTrue(SUT.CanUndo);
		}

		[TestMethod]
		public async Task When_Pointer_Clicked_Typing_Ends()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hellohello", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hellohello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Pointer_Pressed_Undo()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			// no release
			await WindowHelper.WaitForIdle();

			// Shouldn't be able to undo
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);

			mouse.Release();
			await WindowHelper.WaitForIdle();

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Pointer_Pressed_Redo()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			// no release
			await WindowHelper.WaitForIdle();

			// Shouldn't be able to redo
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);

			mouse.Release();
			await WindowHelper.WaitForIdle();

			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
		}

		[TestMethod]
		public async Task When_Unfocused_Typing_Ends()
		{
			using var _ = new TextBoxFeatureConfigDisposable();
			var SUT = new TextBox
			{
				Width = 150
			};

			var sp = new StackPanel()
			{
				SUT,
				new TextBox() { Text="focus dummy" }
			};

			WindowHelper.WindowContent = sp;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			VisualTree.GetFocusManagerForElement(SUT)!.TryMoveFocusInstance(FocusNavigationDirection.Next);
			await WindowHelper.WaitForIdle();

			await Task.Delay(5000);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hellohello", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hellohello", SUT.Text);
		}

		[TestMethod]
		public async Task When_Caret_Moves_Typing_Ends()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.H, VirtualKeyModifiers.None, unicodeKey: 'h'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.E, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.O, VirtualKeyModifiers.None, unicodeKey: 'o'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", SUT.Text);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.L, VirtualKeyModifiers.None, unicodeKey: 'l'));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("hellllo", SUT.Text);

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hellllo", SUT.Text);
		}

		[TestMethod]
		public async Task When_Repeated_Delete_Undo_Redo()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "hello"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("lo", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("llo", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("ello", SUT.Text);
			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("ello", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("llo", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("lo", SUT.Text);
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("o", SUT.Text);
		}

		[TestMethod]
		public async Task When_Ctrl_Delete_Undo_Redo()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "hello world"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// on macOS it's option (menu/alt) and backspace to delete a word
			var mod = OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.Control;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, mod));
			await WindowHelper.WaitForIdle();

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("hello world", SUT.Text);
			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(6, SUT.SelectionLength); // When Ctrl-delete is undone, we select what was (un)deleted!!!
			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("world", SUT.Text);
		}

		[TestMethod]
		public async Task When_Paste_Does_Not_Change_Text()
		{
			if (OperatingSystem.IsBrowser())
			{
				// TODO: Investigate what goes wrong here on Wasm Skia.
				Assert.Inconclusive("Not working on Wasm Skia, unknown issue.");
			}

			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "t"
			};

			WindowHelper.WindowContent = SUT;

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.End, VirtualKeyModifiers.None));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift));
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			var text = "t";
			dp.SetText(text);
			Clipboard.SetContent(dp);

			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Variable_Width_Tab()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 150,
				Text = "\tabc"
			};

			WindowHelper.WindowContent = SUT;

			await UITestHelper.Load(SUT);
			var bitmap = await UITestHelper.ScreenShot(SUT);

			SUT.Text = "a\tabc";
			await WindowHelper.WaitForIdle();
			var bitmap2 = await UITestHelper.ScreenShot(SUT);

			for (var x = 20; x < bitmap.Width; x++)
			{
				for (var y = 0; y < bitmap.Height; y++)
				{
					Assert.AreEqual(bitmap.GetPixel(x, y), bitmap2.GetPixel(x, y));
				}
			}
		}

		[TestMethod]
		public async Task When_Tab_Forces_NewLine_When_Not_Enough_Width()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			// WinUI actually wraps when width <= 114, not <= 113 like we have here.
			// But when width == 114, WinUI has a bug where it wraps, but it doesn't
			// increase the height of the TextBox, so most of the new line (due to wrapping)
			// is out of view :/.
			var sp = new StackPanel
			{
				Children =
				{
					new TextBox
					{
						Width = 114,
						TextWrapping = TextWrapping.Wrap,
						Text = "\t\t",
						FontFamily = new FontFamily("ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/Roboto-Regular.ttf")
					},
					new TextBox
					{
						Width = 113,
						TextWrapping = TextWrapping.Wrap,
						Text = "\t\t",
						FontFamily = new FontFamily("ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/Roboto-Regular.ttf")
					},
				}
			};

			await UITestHelper.Load(sp);

			Assert.AreNotEqual(sp.Children[0].ActualSize.Y, sp.Children[1].ActualSize.Y);
		}

		[TestMethod]
		public async Task When_FeatureConfiguration_Changes()
		{
			var useOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
			using var _ = Disposable.Create(() => FeatureConfiguration.TextBox.UseOverlayOnSkia = useOverlay);

			FeatureConfiguration.TextBox.UseOverlayOnSkia = true;

			var SUT = new TextBox
			{
				Width = 150,
				Text = "hello world"
			};

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.TextBoxView.DisplayBlock.Opacity);

			FeatureConfiguration.TextBox.UseOverlayOnSkia = false;

			await UITestHelper.Load(new Button()); // a random control to unload SUT

			Assert.AreEqual(1, SUT.TextBoxView.DisplayBlock.Opacity);
		}

		[TestMethod]
		public async Task When_Caret_Color_DarkMode()
		{
			var useOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
			using var _1 = Disposable.Create(() => FeatureConfiguration.TextBox.UseOverlayOnSkia = useOverlay);

			// The TextBox is purposefully empty. We want the only content pixels to come from the caret.
			var SUT = new TextBox
			{
				Width = 150
			};

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var random = new Random();
			var i = 0;
			for (; i < 20; i++)
			{
				await Task.Delay(random.Next(75, 126));
				var screenshot = await UITestHelper.ScreenShot(SUT);
				// For some reason, the caret sometimes appears black, and sometimes as very dark grey (#FF030303), so we check for both
				Color[] blacks = [Colors.Black, Colors.FromARGB(0xFF, 0x03, 0x03, 0x03)];
				if (blacks.Any(b => HasColorInRectangle(screenshot, new Rectangle(0, 0, screenshot.Width / 2, screenshot.Height), b)) &&
					blacks.All(b => !HasColorInRectangle(screenshot, new Rectangle(screenshot.Width / 2, 0, screenshot.Width / 2, screenshot.Height), Colors.Black)))
				{
					break;
				}
			}

			Assert.IsTrue(i < 20);

			using var _2 = ThemeHelper.UseDarkTheme();
			await WindowHelper.WaitForIdle();

			for (; i < 20; i++)
			{
				await Task.Delay(random.Next(75, 126));
				var screenshot = await UITestHelper.ScreenShot(SUT);
				if (HasColorInRectangle(screenshot, new Rectangle(0, 0, screenshot.Width / 2, screenshot.Height), Colors.White) &&
					!HasColorInRectangle(screenshot, new Rectangle(screenshot.Width / 2, 0, screenshot.Width / 2, screenshot.Height), Colors.White))
				{
					break;
				}
			}

			Assert.IsTrue(i < 20);
		}

		[TestMethod]
		public async Task When_PasswordBox_TextRevealed()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Overlay isn't supported on Wasm Skia.
				Assert.Inconclusive("Not supported on Wasm Skia.");
			}

			var useOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
			using var _1 = Disposable.Create(() => FeatureConfiguration.TextBox.UseOverlayOnSkia = useOverlay);

			var SUT = new PasswordBox()
			{
				Width = 150
			};

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: 't'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: 'e'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: 's'));
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: 't'));
			await WindowHelper.WaitForIdle();

#if !HAS_UNO
			char defaultPasswordBoxChar = '\u25CF';
#else
			char defaultPasswordBoxChar = PasswordBox.DefaultPasswordChar[0];
#endif

			Assert.AreEqual(new string(defaultPasswordBoxChar, 4), SUT.TextBoxView.DisplayBlock.Text);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var revealButton = (FrameworkElement)SUT.FindName("RevealButton");
			mouse.MoveTo(revealButton.GetAbsoluteBoundsRect().GetCenter());
			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("test", SUT.TextBoxView.DisplayBlock.Text);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno.chefs/issues/1472")]
		public async Task When_PasswordBox_Focus_Changes()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var stackPanel = new StackPanel()
			{
				Padding = new Thickness(10),
				Spacing = 8
			};
			var button = new Button()
			{
				Content = "Focus"
			};
			var passwordBox = new PasswordBox
			{
				IsPasswordRevealButtonEnabled = false,
				Width = 150
			};

			stackPanel.Children.Add(passwordBox);
			stackPanel.Children.Add(button);

			await UITestHelper.Load(stackPanel);

			passwordBox.Focus(FocusState.Pointer);
			await WindowHelper.WaitForIdle();

			var screenshotEmpty = await UITestHelper.ScreenShot(passwordBox);

			passwordBox.Password = "1234567890";
			await WindowHelper.WaitForIdle();

			var screenshotFilled = await UITestHelper.ScreenShot(passwordBox);

			await ImageAssert.AreNotEqualAsync(screenshotEmpty, screenshotFilled);

			button.Focus(FocusState.Pointer);
			await WindowHelper.WaitForIdle();

			// Re-focus
			passwordBox.Focus(FocusState.Pointer);
			await WindowHelper.WaitForIdle();
			var screenshotRefocused = await UITestHelper.ScreenShot(passwordBox);

			await ImageAssert.AreEqualAsync(screenshotRefocused, screenshotFilled);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno-private/issues/753")]
		public async Task When_TextBox_Touch_Tapped_At_End()
		{
			var SUT = new TextBox
			{
				Width = 400,
				Text = "Some Text"
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(SUT.GetAbsoluteBoundsRect().GetCenter());
			finger.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionLength);
			Assert.AreEqual(SUT.Text.Length, SUT.SelectionStart);
		}

		[TestMethod]
		public async Task When_First_Second_Tap_Caret_Thumb_Shows()
		{
			var SUT = new TextBox
			{
				Width = 400,
				Text = "Some Text"
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(SUT.GetAbsoluteBoundsRect().GetCenter());
			finger.Release();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.CaretMode is TextBox.CaretDisplayMode.ThumblessCaretHidden or TextBox.CaretDisplayMode.ThumblessCaretShowing);

			finger.Press(SUT.GetAbsoluteBoundsRect().GetCenter());
			finger.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextBox.CaretDisplayMode.CaretWithThumbsOnlyEndShowing, SUT.CaretMode);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno-private/issues/753")]
		public async Task When_Touch_Focused_Then_Scrolled_Away()
		{
			var SUT = new TextBox
			{
				Width = 400,
				Text = "Some Text"
			};

			var sv = new ScrollViewer()
			{
				Height = 100,
				Content = new StackPanel()
				{
					Children =
					{
						new Microsoft.UI.Xaml.Shapes.Rectangle()
						{
							Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
							Width = 100,
							Height = 500
						},
						SUT,
						new Microsoft.UI.Xaml.Shapes.Rectangle()
						{
							Fill = new SolidColorBrush(Microsoft.UI.Colors.Blue),
							Width = 100,
							Height = 500
						}
					}
				}
			};

			await UITestHelper.Load(sv);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			SUT.StartBringIntoView();
			await UITestHelper.WaitForIdle(true);

			// make the textbox touch knob appear
			finger.Press(SUT.GetAbsoluteBoundsRect().GetCenter());
			finger.Release();
			await UITestHelper.WaitForIdle(true);
			finger.Press(SUT.GetAbsoluteBoundsRect().GetCenter());
			finger.Release();
			await UITestHelper.WaitForIdle(true);

			// scroll
			finger.Press(sv.GetAbsoluteBoundsRect().GetCenter());
			await UITestHelper.WaitForIdle(true);
			finger.MoveBy(0, 300, stepOffsetInMilliseconds: 20);
			await UITestHelper.WaitForIdle(true);
			finger.Release();
			await UITestHelper.WaitForIdle(true);

			await Task.Delay(TimeSpan.FromSeconds(2));

			SUT.GetAbsoluteBoundsRect().Bottom.Should().BeApproximately(sv.GetAbsoluteBoundsRect().Bottom, 5);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno-private/issues/1199")]
		public async Task When_TextBox_TextChange_Not_Trigger_Selection_Change_To_Start()
		{
			var SUT = new TextBox
			{
				Width = 400,
				Text = "Some Text"
			};

			await UITestHelper.Load(SUT);

			var selectionChangedToStart = false;

			var displayBlock = SUT.TextBoxView.DisplayBlock;
			displayBlock.SelectionChanged += (s, e) =>
			{
				if (displayBlock.SelectionStart.Offset == 0)
				{
					selectionChangedToStart = true;
				}
			};

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(SUT.GetAbsoluteBoundsRect().GetCenter());
			finger.Release();
			await WindowHelper.WaitForIdle();

			SUT.Text = "Some Text 2";

			await WindowHelper.WaitForIdle();
			Assert.IsFalse(selectionChangedToStart, "SelectionChanged event should not be triggered when TextBox text is changed.");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19327")]
		public async Task When_Setting_Short_Text_And_Previous_Selection_Is_OutOfBounds()
		{
			var useOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
			using var _ = Disposable.Create(() => FeatureConfiguration.TextBox.UseOverlayOnSkia = useOverlay);

			var SUT = new TextBox
			{
				Width = 150,
				Text = "longer text",
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true
			};

			SUT.KeyUp += (_, e) =>
			{
				SUT.Text = "shorter";
				e.Handled = true;
			};

			await UITestHelper.Load(SUT);

			SUT.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			SUT.Select(SUT.Text.Length, 0);
			await WindowHelper.WaitForIdle();

			SUT.RaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Escape, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			SUT.RaiseEvent(UIElement.KeyUpEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Escape, VirtualKeyModifiers.None));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)] // needs paste permission
		public async Task When_MaxLine_Paste()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				MaxLength = 10,
				Text = "0123456789",
				SelectionStart = 4,
				SelectionLength = 2
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			var text = "abcdefgh";
			dp.SetText(text);
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.PasteFromClipboard();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("0123ab6789", SUT.Text);
			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20857")]
		public async Task When_Rearranged_Without_Remeasuring()
		{
			var SUT1 = new TextBox { Text = "text", TextAlignment = TextAlignment.End };
			var btn1 = new Button { Content = "button" };
			var grid1 = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLengthHelper.OneStar },
					new ColumnDefinition { Width = GridLengthHelper.Auto }
				},
				Children =
				{
					SUT1,
					FluentExtensions.Apply(btn1, btn => Grid.SetColumn(btn, 1))
				}
			};
			await UITestHelper.Load(grid1);

			var screenshot1 = await UITestHelper.ScreenShot(SUT1);

			var SUT2 = new TextBox { Text = "text", TextAlignment = TextAlignment.End };
			var btn2 = new Button { Content = "button" };
			btn2.Visibility = Visibility.Collapsed; // difference here
			var grid2 = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLengthHelper.OneStar },
					new ColumnDefinition { Width = GridLengthHelper.Auto }
				},
				Children =
				{
					SUT2,
					FluentExtensions.Apply(btn2, btn => Grid.SetColumn(btn, 1))
				}
			};
			await UITestHelper.Load(grid2);
			btn2.Visibility = Visibility.Visible;
			await UITestHelper.WaitForIdle();

			var screenshot2 = await UITestHelper.ScreenShot(SUT2);
			await ImageAssert.AreEqualAsync(screenshot1, screenshot2);
		}

		private static bool HasColorInRectangle(RawBitmap screenshot, Rectangle rect, Color expectedColor)
		{
			for (var x = rect.Left; x < rect.Right; x++)
			{
				for (var y = rect.Top; y < rect.Bottom; y++)
				{
					var pixel = screenshot.GetPixel(x, y);
					if (expectedColor == pixel)
					{
						return true;
					}
				}
			}

			return false;
		}

		private class TextBoxFeatureConfigDisposable : IDisposable
		{
			private bool _useOverlay;
			private bool _hideCaret;

			public TextBoxFeatureConfigDisposable()
			{
				_useOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
				_hideCaret = FeatureConfiguration.TextBox.HideCaret;

				FeatureConfiguration.TextBox.UseOverlayOnSkia = false;
				FeatureConfiguration.TextBox.HideCaret = true;
			}

			public void Dispose()
			{
				FeatureConfiguration.TextBox.UseOverlayOnSkia = _useOverlay;
				FeatureConfiguration.TextBox.HideCaret = _hideCaret;
			}
		}
	}
}
