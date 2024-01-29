using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_AutoSuggestBox
	{
#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		[TestMethod]
		public async Task When_Text_Changed_Should_Reflect_In_DataTemplate_TextBox()
		{
			var SUT = new AutoSuggestBox();
			SUT.Text = "New text..";
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			textBox.Text.Should().Be("New text..");
		}

		[TestMethod]
		public async Task When_Text_Changed_And_Not_Focused_Should_Not_Open_Suggestion_List()
		{
			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			textBox.IsFocused.Should().BeFalse();
			SUT.Text = "a";
			SUT.IsSuggestionListOpen.Should().BeFalse();
		}

		[TestMethod]
		public async Task When_Text_Changed_And_Focused_Should_Open_Suggestion_List()
		{
			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);
			textBox.IsFocused.Should().BeTrue();
			SUT.Text = "a";
			SUT.IsSuggestionListOpen.Should().BeTrue();
		}

		[TestMethod]
#if __SKIA__
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_Text_Changed_UserInput(bool useTextBoxOverlay)
#else
		public async Task When_Text_Changed_UserInput()
#endif
		{
#if __SKIA__
			var oldUseOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
			FeatureConfiguration.TextBox.UseOverlayOnSkia = useTextBoxOverlay;
			using var _ = Disposable.Create(() => FeatureConfiguration.TextBox.UseOverlayOnSkia = oldUseOverlay);
#endif

			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			bool eventRaised = false;
			AutoSuggestionBoxTextChangeReason? reason = null;
			SUT.TextChanged += (s, e) =>
			{
				reason = e.Reason;
				eventRaised = true;
			};
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);
			textBox.ProcessTextInput("something");

			await WindowHelper.WaitFor(() => eventRaised);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, reason);
		}

		[TestMethod]
#if !__SKIA__
		[Ignore("This test specifically tests the skia-rendered TextBox")]
#endif
		public async Task When_Text_Changed_UserInput_Skia()
		{
			var oldUseOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
			FeatureConfiguration.TextBox.UseOverlayOnSkia = false;
			using var _ = Disposable.Create(() =>
			{
				FeatureConfiguration.TextBox.UseOverlayOnSkia = oldUseOverlay;
				VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).ForEach(p => p.IsOpen = false);
			});

			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			bool eventRaised = false;
			AutoSuggestionBoxTextChangeReason? reason = null;
			SUT.TextChanged += (s, e) =>
			{
				reason = e.Reason;
				eventRaised = true;
			};
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			var popup = (Popup)SUT.GetTemplateChild("SuggestionsPopup");
			textBox.Focus(FocusState.Programmatic);
			KeyboardHelper.PressKeySequence("$d$_a#$u$_a");

			await WindowHelper.WaitForIdle();
			Assert.IsTrue(eventRaised);
			Assert.IsTrue(popup.IsOpen);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, reason);
		}

		[TestMethod]
#if __WASM__
		[Ignore("WASM requires user confirmation to accept reading the clipboard.")]
#endif
		public async Task When_UserInput_Paste()
		{
			using var _ = Disposable.Create(() =>
			{
				VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).ForEach(p => p.IsOpen = false);
			});

			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			bool eventRaised = false;
			AutoSuggestionBoxTextChangeReason? reason = null;
			SUT.TextChanged += (s, e) =>
			{
				reason = e.Reason;
				eventRaised = true;
			};
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			var popup = (Popup)SUT.GetTemplateChild("SuggestionsPopup");
			textBox.Focus(FocusState.Programmatic);
			KeyboardHelper.PressKeySequence("$d$_a#$u$_a");

			var dataPackage = new DataPackage();
			dataPackage.SetText("a");
			Clipboard.SetContent(dataPackage);

			textBox.PasteFromClipboard();

			await WindowHelper.WaitForIdle();
			Assert.IsTrue(eventRaised);
			Assert.IsTrue(popup.IsOpen);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, reason);
		}

		[TestMethod]
#if !__SKIA__
		[Ignore("This test specifically tests the skia-rendered TextBox")]
#endif
		public async Task When_UserInput_Undo_Redo()
		{
			var oldUseOverlay = FeatureConfiguration.TextBox.UseOverlayOnSkia;
			FeatureConfiguration.TextBox.UseOverlayOnSkia = false;
			using var _ = Disposable.Create(() =>
			{
				FeatureConfiguration.TextBox.UseOverlayOnSkia = oldUseOverlay;
				VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).ForEach(p => p.IsOpen = false);
			});

			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			bool eventRaised = false;
			AutoSuggestionBoxTextChangeReason? reason = null;
			SUT.TextChanged += (s, e) =>
			{
				reason = e.Reason;
				eventRaised = true;
			};
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			var popup = (Popup)SUT.GetTemplateChild("SuggestionsPopup");
			textBox.Focus(FocusState.Programmatic);
			KeyboardHelper.PressKeySequence("$d$_a#$u$_a");

			var dataPackage = new DataPackage();
			dataPackage.SetText("a");
			Clipboard.SetContent(dataPackage);

			KeyboardHelper.PressKeySequence("$d$_a#$u$_a");
			await WindowHelper.WaitForIdle();

			KeyboardHelper.Escape(); // close the popup
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(popup.IsOpen);

			textBox.Undo();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(eventRaised);
			Assert.IsTrue(popup.IsOpen);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, reason);

			eventRaised = default;
			reason = default;

			KeyboardHelper.Escape(); // close the popup
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(popup.IsOpen);

			textBox.Redo();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(eventRaised);
			Assert.IsTrue(popup.IsOpen);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, reason);
		}

		[TestMethod]
		public async Task When_Text_Changed_Programmatic()
		{
			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			bool eventRaised = false;
			AutoSuggestionBoxTextChangeReason? reason = null;
			SUT.TextChanged += (s, e) =>
			{
				reason = e.Reason;
				eventRaised = true;
			};
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);
			textBox.Text = "stuff";

			await WindowHelper.WaitFor(() => eventRaised);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.ProgrammaticChange, reason);
		}

		[TestMethod]
		public async Task When_Text_Changed_SuggestionChosen()
		{
			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			bool eventRaised = false;
			AutoSuggestionBoxTextChangeReason? reason = null;
			SUT.TextChanged += (s, e) =>
			{
				reason = e.Reason;
				eventRaised = true;
			};
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);
			SUT.ChoseItem("ab");

			await WindowHelper.WaitFor(() => eventRaised);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.SuggestionChosen, reason);
		}


		[TestMethod]
		public async Task When_Text_Changed_Sequence()
		{
			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			bool eventRaised = false;
			var reasons = new List<AutoSuggestionBoxTextChangeReason>();
			var counter = 0;
			SUT.TextChanged += (s, e) =>
			{
				reasons.Add(e.Reason);
				if (++counter == 7)
				{
					eventRaised = true;
				}
			};
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);
			SUT.ChoseItem("ab");
			SUT.Text = "other";
			textBox.ProcessTextInput("manual");
			SUT.ChoseItem("ab");
			textBox.ProcessTextInput("manual");
			SUT.Text = "other";
			SUT.ChoseItem("ab");

			await WindowHelper.WaitFor(() => eventRaised);
			CollectionAssert.AreEquivalent(
				new[] {
					AutoSuggestionBoxTextChangeReason.SuggestionChosen,
					AutoSuggestionBoxTextChangeReason.ProgrammaticChange,
					AutoSuggestionBoxTextChangeReason.UserInput,
					AutoSuggestionBoxTextChangeReason.SuggestionChosen,
					AutoSuggestionBoxTextChangeReason.UserInput,
					AutoSuggestionBoxTextChangeReason.ProgrammaticChange,
					AutoSuggestionBoxTextChangeReason.SuggestionChosen
				},
				reasons);
		}

		[TestMethod]
		public async Task When_Selecting_Suggest_With_UpDown_Key()
		{
			AutoSuggestBox SUT = new AutoSuggestBox();
			string[] suggestions = { "a1", "a2", "b1", "b2" };
			bool eventRaised = false;
			SUT.TextChanged += (s, e) =>
			{
				eventRaised = true;
				if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					s.ItemsSource = suggestions.Where(i => i.StartsWith(s.Text));
				}
			};
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			Type type = typeof(AutoSuggestBox);
			MethodInfo HandleUpDownKeys = type.GetMethod("HandleUpDownKeys", BindingFlags.NonPublic | BindingFlags.Instance);
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);

			eventRaised = false;
			textBox.ProcessTextInput("a");
			await WindowHelper.WaitFor(() => eventRaised);
			_ = HandleUpDownKeys.Invoke(SUT, new object[] { new KeyRoutedEventArgs(SUT, Windows.System.VirtualKey.Down, VirtualKeyModifiers.None) });
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("a1", SUT.Text);
			_ = HandleUpDownKeys.Invoke(SUT, new object[] { new KeyRoutedEventArgs(SUT, Windows.System.VirtualKey.Down, VirtualKeyModifiers.None) });
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("a2", SUT.Text);

			eventRaised = false;
			textBox.ProcessTextInput("b");
			await WindowHelper.WaitFor(() => eventRaised);
			_ = HandleUpDownKeys.Invoke(SUT, new object[] { new KeyRoutedEventArgs(SUT, Windows.System.VirtualKey.Down, VirtualKeyModifiers.None) });
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("b1", SUT.Text);
			_ = HandleUpDownKeys.Invoke(SUT, new object[] { new KeyRoutedEventArgs(SUT, Windows.System.VirtualKey.Down, VirtualKeyModifiers.None) });
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("b2", SUT.Text);
		}

		[TestMethod]
		public async Task When_Submitting_After_Typing_Text()
		{
			var SUT = new AutoSuggestBox();
			SUT.QuerySubmitted += (s, e) =>
			{
				Assert.IsNull(e.ChosenSuggestion);
			};
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
			await WindowHelper.WaitForIdle();
			SUT.Focus(FocusState.Programmatic);
			SUT.Text = "abc";
			await WindowHelper.WaitForIdle();
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task When_Using_Custom_ItemContainerStyle()
		{
			AutoSuggestBox SUT = new AutoSuggestBox
			{
				ItemContainerStyle = new Style(typeof(ListViewItem))
				{
					Setters =
								{
									new Setter(Control.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
								},
				}
			};
			string[] suggestions = { "a1", "a2", "b1", "b2" };
			SUT.TextChanged += (s, e) =>
			{
				if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					s.ItemsSource = suggestions.Where(i => i.StartsWith(s.Text));
				}
			};
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			var listView = (ListView)SUT.GetTemplateChild("SuggestionsList");
			SUT.Focus(FocusState.Programmatic);
			textBox.ProcessTextInput("a");
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitFor(() => SUT.IsSuggestionListOpen);
			Assert.AreEqual(2, listView.Items.Count);
			Assert.AreEqual("a1", listView.Items[0].ToString());
			Assert.AreEqual("a2", listView.Items[1].ToString());
#if __WASM__
			//ItemsPanelRoot.Children works only on wasm
			Assert.AreEqual(2, listView.ItemsPanelRoot.Children.Count);
			Assert.AreEqual("a1", (listView.ItemsPanelRoot.Children[0] as ContentControl).Content.ToString());
			Assert.AreEqual("a2", (listView.ItemsPanelRoot.Children[1] as ContentControl).Content.ToString());
#endif
		}
#endif

		[TestMethod]
#if WINAPPSDK
		[Ignore("KeyboardHelper doesn't work on Windows")]
#endif
		public async Task When_Keyboard_Handled()
		{
			var SUT = new AutoSuggestBox();

			static void AutoSuggestBox_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs e)
			{
				var items = new List<string>
				{
					"A0",
					"A1",
					"A2",
					"B0",
					"B1",
					"B2",
					"C0",
					"C1",
					"C2"
				};

				if (e.Reason != AutoSuggestionBoxTextChangeReason.SuggestionChosen)
				{
					s.ItemsSource = items.Where(a => a.StartsWith(s.Text.Trim())).ToArray();
				}
			}

			Button button = null;
			try
			{
				button = new Button();
				var stack = new StackPanel()
				{
					Children =
					{
						button,
						SUT
					}
				};

				var keyDownHandled = 0;
				var keyDownNotHandled = 0;
				SUT.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, a) =>
				{
					if (a.Handled)
					{
						keyDownHandled++;
					}
					else
					{
						keyDownNotHandled++;
					}
				}), true);

				SUT.TextChanged += AutoSuggestBox_TextChanged;
				WindowHelper.WindowContent = stack;
				await WindowHelper.WaitForIdle();

				var tb = SUT.FindVisualChildByType<TextBox>();
				var popup = SUT.FindVisualChildByType<Popup>();

				SUT.Focus(FocusState.Programmatic);
				SUT.Text = "A";
				await WindowHelper.WaitForIdle();
				tb.Select(1, 0);
				await WindowHelper.WaitForIdle();

				var lv = popup.Child.FindVisualChildByType<ListView>();

				Assert.AreEqual(-1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);

				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(keyDownHandled, 1);
				Assert.AreEqual(keyDownNotHandled, 0);

				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(keyDownHandled, 2);
				Assert.AreEqual(keyDownNotHandled, 0);

				KeyboardHelper.Right();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(keyDownHandled, 2);
				Assert.AreEqual(keyDownNotHandled, 1);

				KeyboardHelper.Left();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(keyDownHandled, 3); // actually handled in textbox
				Assert.AreEqual(keyDownNotHandled, 1);

				KeyboardHelper.Up();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(keyDownHandled, 4);
				Assert.AreEqual(keyDownNotHandled, 1);
			}
			finally
			{
				button?.Focus(FocusState.Programmatic); // Unfocus the AutoSuggestBox to ensure popup is closed.
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
#if WINAPPSDK
		[Ignore("KeyboardHelper doesn't work on Windows")]
#endif
		public async Task When_ArrowKeys_Handled()
		{
			var SUT = new AutoSuggestBox();

			static void AutoSuggestBox_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs e)
			{
				var items = new List<string>
				{
					"A0",
					"A1",
					"A2",
					"B0",
					"B1",
					"B2",
					"C0",
					"C1",
					"C2"
				};

				if (e.Reason != AutoSuggestionBoxTextChangeReason.SuggestionChosen)
				{
					s.ItemsSource = items.Where(a => a.StartsWith(s.Text.Trim())).ToArray();
				}
			}

			Button button = null;
			try
			{
				button = new Button();
				var stack = new StackPanel()
				{
					Children =
					{
						button,
						SUT
					}
				};

				var keyDownHandled = 0;
				var keyDownNotHandled = 0;
				SUT.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, a) =>
				{
					if (a.Handled)
					{
						keyDownHandled++;
					}
					else
					{
						keyDownNotHandled++;
					}
				}), true);

				SUT.TextChanged += AutoSuggestBox_TextChanged;
				WindowHelper.WindowContent = stack;
				await WindowHelper.WaitForIdle();

				var tb = SUT.FindVisualChildByType<TextBox>();
				var popup = SUT.FindVisualChildByType<Popup>();

				SUT.Focus(FocusState.Programmatic);
				SUT.Text = "A";
				await WindowHelper.WaitForIdle();
				tb.Select(1, 0);
				await WindowHelper.WaitForIdle();

				var lv = popup.Child.FindVisualChildByType<ListView>();

				Assert.AreEqual(-1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);

				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(keyDownHandled, 2);
				Assert.AreEqual(keyDownNotHandled, 0);

				KeyboardHelper.Enter();
				Assert.AreEqual(keyDownHandled, 3);
				Assert.AreEqual(keyDownNotHandled, 0);
			}
			finally
			{
				button?.Focus(FocusState.Programmatic); // Unfocus the AutoSuggestBox to ensure popup is closed.
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
#if WINAPPSDK
		[Ignore("KeyboardHelper doesn't work on Windows")]
#endif
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Enter_Escape_Handled(bool escape)
		{
			var SUT = new AutoSuggestBox();

			static void AutoSuggestBox_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs e)
			{
				var items = new List<string>
				{
					"A0",
					"A1",
					"A2",
					"B0",
					"B1",
					"B2",
					"C0",
					"C1",
					"C2"
				};

				if (e.Reason != AutoSuggestionBoxTextChangeReason.SuggestionChosen)
				{
					s.ItemsSource = items.Where(a => a.StartsWith(s.Text.Trim())).ToArray();
				}
			}

			Button button = null;
			try
			{
				button = new Button();
				var stack = new StackPanel()
				{
					Children =
					{
						button,
						SUT
					}
				};

				var keyDownHandled = 0;
				var keyDownNotHandled = 0;
				SUT.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, a) =>
				{
					if (a.Handled)
					{
						keyDownHandled++;
					}
					else
					{
						keyDownNotHandled++;
					}
				}), true);

				SUT.TextChanged += AutoSuggestBox_TextChanged;
				WindowHelper.WindowContent = stack;
				await WindowHelper.WaitForIdle();

				var tb = SUT.FindVisualChildByType<TextBox>();
				var popup = SUT.FindVisualChildByType<Popup>();

				SUT.Focus(FocusState.Programmatic);
				SUT.Text = "A";
				await WindowHelper.WaitForIdle();
				tb.Select(1, 0);
				await WindowHelper.WaitForIdle();

				var lv = popup.Child.FindVisualChildByType<ListView>();

				Assert.AreEqual(-1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);

				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(keyDownHandled, 2);
				Assert.AreEqual(keyDownNotHandled, 0);

				if (escape)
				{
					KeyboardHelper.Escape();
				}
				else
				{
					KeyboardHelper.Enter();
				}
				Assert.AreEqual(keyDownHandled, 3);
				Assert.AreEqual(keyDownNotHandled, 0);
			}
			finally
			{
				button?.Focus(FocusState.Programmatic); // Unfocus the AutoSuggestBox to ensure popup is closed.
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
#if WINAPPSDK
		[Ignore("KeyboardHelper doesn't work on Windows")]
#endif
		public async Task When_SuggestionChosen_TextBox_Moves_To_The_End()
		{
			var SUT = new AutoSuggestBox();

			static void AutoSuggestBox_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs e)
			{
				var items = new List<string>
				{
					"0",
					"01",
					"012",
					"0123",
					"01234",
					"012345",
					"0123456"
				};

				if (e.Reason != AutoSuggestionBoxTextChangeReason.SuggestionChosen)
				{
					s.ItemsSource = items.Where(a => a.StartsWith(s.Text.Trim())).ToArray();
				}
			}

			Button button = null;
			try
			{
				button = new Button();
				var stack = new StackPanel()
				{
					Children =
					{
						button,
						SUT
					}
				};

				SUT.TextChanged += AutoSuggestBox_TextChanged;
				WindowHelper.WindowContent = stack;
				await WindowHelper.WaitForIdle();

				var tb = SUT.FindVisualChildByType<TextBox>();
				var popup = SUT.FindVisualChildByType<Popup>();

				SUT.Focus(FocusState.Programmatic);
				SUT.Text = "0";
				await WindowHelper.WaitForIdle();
				tb.Select(1, 0);
				await WindowHelper.WaitForIdle();

				var lv = popup.Child.FindVisualChildByType<ListView>();

				Assert.AreEqual(-1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);

				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(tb.Text.Length, tb.SelectionStart);

				KeyboardHelper.Up();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(tb.Text.Length, tb.SelectionStart);
			}
			finally
			{
				button?.Focus(FocusState.Programmatic); // Unfocus the AutoSuggestBox to ensure popup is closed.
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		public async Task When_Typing_Should_Keep_Focus()
		{
			static void GettingFocus(object sender, GettingFocusEventArgs e)
			{
				if (e.NewFocusedElement is Popup)
				{
					Assert.Fail();
				}
			}
			Button button = null;
			try
			{
				var SUT = new AutoSuggestBox();
				button = new Button();
				var stack = new StackPanel()
				{
					Children =
					{
						button,
						SUT
					}
				};
				SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
				WindowHelper.WindowContent = stack;
				await WindowHelper.WaitForIdle();

				SUT.Focus(FocusState.Programmatic);
				FocusManager.GettingFocus += GettingFocus;
				SUT.Text = "a";
				await WindowHelper.WaitForIdle();
			}
			finally
			{
				FocusManager.GettingFocus -= GettingFocus;
				button?.Focus(FocusState.Programmatic); // Unfocus the AutoSuggestBox to ensure popup is closed.
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		public async Task When_Choose_Selection()
		{
			var SUT = new AutoSuggestBox();

			static void QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
			{
				if (args.ChosenSuggestion is null)
				{
					Assert.Fail();
				}
			}
			Button button = null;
			try
			{


				button = new Button();
				var stack = new StackPanel()
				{
					Children =
					{
						button,
						SUT
					}
				};

				SUT.QuerySubmitted += QuerySubmitted;
				SUT.ItemsSource = new List<string>() { "ab", "abc", "abcde" };
				WindowHelper.WindowContent = stack;
				await WindowHelper.WaitForIdle();


				SUT.Focus(FocusState.Programmatic);
				SUT.Text = "ab";
				await WindowHelper.WaitForIdle();
			}
			finally
			{
				button?.Focus(FocusState.Programmatic); // Unfocus the AutoSuggestBox to ensure popup is closed.
				await WindowHelper.WaitForIdle();
			}
		}

#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Popup_Above()
		{
			await When_Popup_Position(VerticalAlignment.Bottom, (SUT, popup) =>
			{
				var popupPoint = popup.Child.TransformToVisual(WindowHelper.WindowContent).TransformPoint(default);
				var suggestBoxPoint = SUT.TransformToVisual(WindowHelper.WindowContent).TransformPoint(default);
				Assert.IsTrue(popupPoint.Y + popup.Child.ActualSize.Y <= suggestBoxPoint.Y + 1); // Added 1 to adjust for border on Windows
			});
		}

#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Popup_Below()
		{
			await When_Popup_Position(VerticalAlignment.Top, (SUT, popup) =>
			{
				var popupPoint = popup.Child.TransformToVisual(WindowHelper.WindowContent).TransformPoint(default);
				var suggestBoxPoint = SUT.TransformToVisual(WindowHelper.WindowContent).TransformPoint(default);
				Assert.IsTrue(popupPoint.Y + 1 >= suggestBoxPoint.Y + SUT.ActualHeight); // Added 1 to adjust for border on Windows
			});
		}

		private async Task When_Popup_Position(VerticalAlignment verticalAlignment, Action<AutoSuggestBox, Popup> assert)
		{
			var SUT = new AutoSuggestBox();

			Button button = null;
			try
			{
				button = new Button();
				var rootGrid = new Grid();
				var stack = new StackPanel()
				{
					Children =
					{
						button,
						SUT
					},
					VerticalAlignment = verticalAlignment
				};

				SUT.ItemsSource = Enumerable.Range(0, 10).ToArray();
				rootGrid.Children.Add(stack);
				WindowHelper.WindowContent = rootGrid;
				await WindowHelper.WaitForIdle();

				SUT.Focus(FocusState.Programmatic);
				SUT.Text = "1";
				await WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot);
				var popup = popups[0];

				Assert.IsNotNull(popup);
				var child = popup.Child;
				Assert.IsNotNull(child);
				await WindowHelper.WaitFor(() => child.ActualSize.Y > 0);

				assert(SUT, popup);
			}
			finally
			{
				button?.Focus(FocusState.Programmatic); // Unfocus the AutoSuggestBox to ensure popup is closed.
				await WindowHelper.WaitForIdle();
			}
		}
	}
}
