using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
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
	}
}
