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
using static Microsoft.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Combinatorial.MSTest;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_AutoSuggestBox
	{
#if !WINAPPSDK
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15662")]
		public async Task When_SymbolIcon_Verify_Size()
		{
			var SUT = new AutoSuggestBox()
			{
				QueryIcon = new SymbolIcon(Symbol.Home),
			};

			await UITestHelper.Load(SUT);

			var expectedGlyph = SymbolIcon.ConvertSymbolValueToGlyph((int)Symbol.Home);

#if __SKIA__ || __WASM__
			var tb = SUT.FindChildren<TextBlock>().Single(tb => tb.Text.Length == 1 && tb.Text[0] == expectedGlyph);
#else
			var tb = (TextBlock)SUT.EnumerateAllChildren().SingleOrDefault(c => c is TextBlock textBlock && textBlock.Text.Length == 1 && textBlock.Text[0] == expectedGlyph);
#endif

			Assert.AreEqual(12, tb.FontSize);

			var tbBounds = tb.GetAbsoluteBounds();

#if __WASM__
			Assert.AreEqual(new Size(13, 12), new Size(tbBounds.Width, tbBounds.Height));
#elif __ANDROID__
			Assert.AreEqual(new Size(12, 14), new Size(tbBounds.Width, tbBounds.Height));
#else
			// 12, 12 is the right behavior here.
			Assert.AreEqual(new Size(12, 12), new Size(tbBounds.Width, tbBounds.Height));
#endif
		}
#endif

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
			SUT.IsSuggestionListOpen.Should().BeFalse();
			await WindowHelper.WaitForIdle();
			SUT.IsSuggestionListOpen.Should().BeTrue();
		}

		[TestMethod]
#if __SKIA__
		[CombinatorialData]
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
			await KeyboardHelper.PressKeySequence("$d$_a#$u$_a");

			await WindowHelper.WaitForIdle();
			Assert.IsTrue(eventRaised);
			Assert.IsTrue(popup.IsOpen);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, reason);
		}

		[TestMethod]
#if !__SKIA__
		[Ignore("The test is not playing nicely with KeyboardHelper on non-skia.")]
#endif
		public async Task When_Keyboard_Navigation_Scrolls_SuggestionsList()
		{
			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = Enumerable.Range(0, 20).Select(i => $"a{i}").ToList();

			await UITestHelper.Load(SUT);

			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			var popup = (Popup)SUT.GetTemplateChild("SuggestionsPopup");

			textBox.Focus(FocusState.Programmatic);
			await KeyboardHelper.InputText("a");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(popup.IsOpen);

			var sv = popup.Child.FindVisualChildByType<ScrollViewer>();
			Assert.AreEqual(0, sv.VerticalOffset);
			for (int i = 0; i < 10; i++)
			{
				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
			}
			Assert.AreNotEqual(0, sv.VerticalOffset);
		}

		[TestMethod]
		public async Task When_Size_Changes_SuggestionsList_Size_Also_Changes()
		{
			var SUT = new AutoSuggestBox();
			SUT.ItemsSource = Enumerable.Range(0, 20).Select(i => $"a{i}").ToList();
			var border = new Border
			{
				Child = SUT,
				Width = 40
			};

			await UITestHelper.Load(border);

			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			var popup = (Popup)SUT.GetTemplateChild("SuggestionsPopup");

			textBox.Focus(FocusState.Programmatic);
#if __SKIA__
			await KeyboardHelper.InputText("a");
#else
			textBox.ProcessTextInput("a");
#endif
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(popup.IsOpen);

			var sv = popup.Child.FindVisualChildByType<ScrollViewer>();
			var initialWidth = sv.ActualWidth;

			border.Width = 100;
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(initialWidth < sv.ActualWidth);
		}

		[TestMethod]
		// Clipboard is currently not available on skia-WASM
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
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
			await KeyboardHelper.PressKeySequence("$d$_a#$u$_a");

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
		// Clipboard is currently not available on skia-WASM
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
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
			await KeyboardHelper.PressKeySequence("$d$_a#$u$_a");

			var dataPackage = new DataPackage();
			dataPackage.SetText("a");
			Clipboard.SetContent(dataPackage);

			await KeyboardHelper.PressKeySequence("$d$_a#$u$_a");
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.Escape(); // close the popup
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(popup.IsOpen);

			textBox.Undo();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(eventRaised);
			Assert.IsTrue(popup.IsOpen);
			Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, reason);

			eventRaised = default;
			reason = default;

			await KeyboardHelper.Escape(); // close the popup
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
		[CombinatorialData]
		public async Task When_Text_Changed_Sequence(bool waitBetweenActions)
		{
			var SUT = new AutoSuggestBox()
			{
				ItemsSource = new List<string>() { "ab", "abc", "abcde" }
			};
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");

			var expectations = new List<AutoSuggestionBoxTextChangeReason>();
			var reasons = new List<AutoSuggestionBoxTextChangeReason>();
			SUT.TextChanged += (s, e) =>
			{
				reasons.Add(e.Reason);
			};

			expectations.Add(SuggestionChosen);
			SUT.Focus(FocusState.Programmatic);
			SUT.ChoseItem("ab");
			await Wait();

			expectations.Add(ProgrammaticChange);
			SUT.Text = "other";
			await Wait();

			expectations.Add(UserInput);
			SUT.Focus(FocusState.Programmatic);
#if __SKIA__
			await KeyboardHelper.InputText("manual");
#else
			textBox.ProcessTextInput("manual");
#endif
			await Wait();

			expectations.Add(SuggestionChosen);
			SUT.ChoseItem("ab");
			await Wait();

			expectations.Add(UserInput);
			SUT.Focus(FocusState.Programmatic);
#if __SKIA__ // We want to test the behaviour of "typing individual characters in sequence", not setting the Text in one shot. The behaviour is currently only accurate on skia.
			await KeyboardHelper.InputText("manual");
#else
			textBox.ProcessTextInput("manual");
#endif
			await Wait();

			expectations.Add(ProgrammaticChange);
			SUT.Focus(FocusState.Programmatic);
			SUT.Text = "other";
			await Wait();

			expectations.Add(SuggestionChosen);
			SUT.ChoseItem("ab");
			await Wait();

			await WindowHelper.WaitForIdle();

			// We want to test the behaviour of "typing individual characters in sequence", not setting the Text in one shot. The behaviour is currently only accurate on skia.
			if (!waitBetweenActions)
			{
				// skia is closer to what happens on WinUI. On WinUI, if there is no delay between changes,
				// AutoSuggestBox.TextChanged is fired once (but TextBox.TextChanged fires everytime)
				expectations = new() { SuggestionChosen };
			}

			// remove repeating UserInputs in a sequence as a result of typing individual characters. WinUI has a timer
			// that will only fire an event with UserInput once it has waited a bit and found no new characters coming
			reasons = reasons
				.Where((reason, i) => i == 0 || !(reason == UserInput && reasons[i - 1] == UserInput))
				.ToList();

			CollectionAssert.AreEquivalent(expectations, reasons, string.Join("; ",
				$"expectations[{expectations.Count}]: {string.Join(",", expectations)}",
				$"actual[{reasons.Count}]: {string.Join(",", reasons)}"
			));

			async Task Wait()
			{
				if (waitBetweenActions)
				{
					await WindowHelper.WaitForIdle();
				}
			}
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

				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(1, keyDownHandled);
				Assert.AreEqual(0, keyDownNotHandled);

				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(2, keyDownHandled);
				Assert.AreEqual(0, keyDownNotHandled);

				await KeyboardHelper.Right();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(2, keyDownHandled);
				Assert.AreEqual(1, keyDownNotHandled);

				await KeyboardHelper.Left();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(3, keyDownHandled); // actually handled in textbox
				Assert.AreEqual(1, keyDownNotHandled);

				await KeyboardHelper.Up();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(4, keyDownHandled);
				Assert.AreEqual(1, keyDownNotHandled);
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

				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(2, keyDownHandled);
				Assert.AreEqual(0, keyDownNotHandled);

				await KeyboardHelper.Enter();
				Assert.AreEqual(3, keyDownHandled);
				Assert.AreEqual(0, keyDownNotHandled);
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
		[CombinatorialData]
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

				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(2, keyDownHandled);
				Assert.AreEqual(0, keyDownNotHandled);

				if (escape)
				{
					await KeyboardHelper.Escape();
				}
				else
				{
					await KeyboardHelper.Enter();
				}
				Assert.AreEqual(3, keyDownHandled);
				Assert.AreEqual(0, keyDownNotHandled);
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

				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				await KeyboardHelper.Down();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, lv.SelectedIndex);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(tb.Text.Length, tb.SelectionStart);

				await KeyboardHelper.Up();
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

#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/ziidms-private/issues/54")]
#if ANDROID && IS_CI
		[Ignore("This test is failing on Android in CI only.")]
#endif
		public async Task When_Loaded_Unloaded()
		{
			var SUT = new AutoSuggestBox();
			var suggestions = new List<string> { "ab", "abc", "abcde" };
			SUT.ItemsSource = suggestions;

			SUT.TextChanged += (sender, args) =>
			{
				if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					var filteredSuggestions =
						suggestions.Where(s => s.StartsWith(sender.Text, StringComparison.OrdinalIgnoreCase)).ToList();
					sender.ItemsSource = filteredSuggestions;
				}
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);
			textBox.ProcessTextInput("a");

			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot).Count);
		}
#endif

#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		[TestMethod]
		public async Task When_Popup_Above_AutoSuggestBox_And_SuggestionsList_changes()
		{
			var SUT = new AutoSuggestBox()
			{
				VerticalAlignment = VerticalAlignment.Bottom
			};
			var suggestions = new List<string> { "ab1", "ab2", "ac" };
			SUT.ItemsSource = suggestions;

			SUT.TextChanged += (sender, args) =>
			{
				if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					var filteredSuggestions =
						suggestions.Where(s => s.StartsWith(sender.Text, StringComparison.OrdinalIgnoreCase)).ToList();
					sender.ItemsSource = filteredSuggestions;
				}
			};

			WindowHelper.WindowContent = new Border()
			{
				Height = WindowHelper.XamlRoot.Content.ActualSize.Y - 100,
				Child = SUT
			};
			await WindowHelper.WaitForIdle();

			var textBox = (TextBox)SUT.GetTemplateChild("TextBox");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			textBox.ProcessTextInput("a");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot).Count);
			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)[0];
			var oldPopupRect = (popup.Child as FrameworkElement).GetAbsoluteBoundsRect();

			textBox.ProcessTextInput("ab");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot).Count);
			var newPopupRect = (popup.Child as FrameworkElement).GetAbsoluteBoundsRect();

			oldPopupRect.Y.Should().BeLessThan(newPopupRect.Y);
		}
#endif
	}
}
