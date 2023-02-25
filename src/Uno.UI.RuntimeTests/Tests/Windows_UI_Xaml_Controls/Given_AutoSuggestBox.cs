using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_AutoSuggestBox
	{
#if !WINDOWS_UWP // GetTemplateChild is protected in UWP while public in Uno.
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
#endif

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
	}
}
