using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
	}
}
