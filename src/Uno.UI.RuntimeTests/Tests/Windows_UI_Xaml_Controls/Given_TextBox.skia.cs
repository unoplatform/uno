using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	/// <summary>
	/// This partial is for testing the skia-based TextBox implementation.
	/// Most tests here should set UseOverlayOnSkia to false and HideCaret
	/// to true and then set them back at the end of the test.
	/// </summary>
	public partial class Given_TextBox
	{
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

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Control));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(6, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Control));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(13, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Control));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(15, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Control));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(16, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.Control));
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
			Assert.IsTrue(sv.HorizontalOffset > 0);
			Assert.AreEqual(sv.ScrollableWidth, sv.HorizontalOffset);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.None));
			Assert.IsTrue(sv.ScrollableWidth > 0);
			Assert.AreEqual(0, sv.HorizontalOffset);
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
			Assert.IsTrue(LayoutInformation.GetLayoutSlot(SUT).Right - LayoutInformation.GetLayoutSlot(sv).Right > 10);

			btn.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(LayoutInformation.GetLayoutSlot(SUT).Right - LayoutInformation.GetLayoutSlot(sv).Right < 10);
		}

		[TestMethod]
		public async Task When_Scrolling_Updates_With_Movement()
		{
			using var _ = new TextBoxFeatureConfigDisposable();

			var SUT = new TextBox
			{
				Width = 40,
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
			Assert.AreEqual(sv.ScrollableWidth, sv.HorizontalOffset);

			// The TextBox should fit roughly 7 characters
			for (var i = 0; i < 6; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(sv.ScrollableWidth, sv.HorizontalOffset);
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Left, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(sv.HorizontalOffset < sv.ScrollableWidth);

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Home, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, sv.HorizontalOffset);

			for (var i = 0; i < 6; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, sv.HorizontalOffset);
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Right, VirtualKeyModifiers.None));
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(sv.HorizontalOffset > 0);
		}

		[TestMethod]
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
			Assert.IsTrue(LayoutInformation.GetLayoutSlot(SUT).Right - LayoutInformation.GetLayoutSlot(sv).Right > 10);

			var svRight = LayoutInformation.GetLayoutSlot(SUT).Right;
			for (var i = 0; i < 5; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.None));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(svRight, LayoutInformation.GetLayoutSlot(SUT).Right);
			}

			for (var i = 0; i < 10; i++)
			{
				SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Back, VirtualKeyModifiers.Control));
			}
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, sv.ScrollableWidth);
		}

		[TestMethod]
		public async Task When_Pointer_Tap()
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

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Pointer_Hold_Drag()
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

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(-50, 0);

			Assert.AreEqual(1, SUT.SelectionStart);
			Assert.AreEqual(8, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_Pointer_Hold_Drag_OutOfBounds()
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

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(0, 50);
			mouse.MoveBy(-150, 0);

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);
		}

		[TestMethod]
		public async Task When_LongText_Pointer_Hold_Drag_OutOfBounds()
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

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			mouse.MoveBy(0, 50);
			mouse.MoveBy(-150, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStart);
			Assert.AreEqual(9, SUT.SelectionLength);

			mouse.MoveBy(0, 50);
			mouse.MoveBy(600, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(SUT.Text.Length - 9, SUT.SelectionLength);
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
