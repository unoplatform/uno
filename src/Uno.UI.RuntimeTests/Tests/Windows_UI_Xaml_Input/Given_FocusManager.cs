using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input
{
	[TestClass]
	public class Given_FocusManager
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task GotLostFocus()
		{
			using var _ = new AssertionScope();
			try
			{
				var panel = new StackPanel();
				var wasEventRaised = false;

				var receivedGotFocus = new bool[4];
				var receivedLostFocus = new bool[4];

				var buttons = new Button[4];
				for (var i = 0; i < 4; i++)
				{
					var button = new Button
					{
						Content = $"Button {i}",
						Name = $"Button{i}"
					};

					panel.Children.Add(button);
					buttons[i] = button;
				}

				TestServices.WindowHelper.WindowContent = panel;

				await TestServices.WindowHelper.WaitForIdle();

				const int tries = 10;
				var initialSuccess = false;
				for (var i = 0; i < tries; i++)
				{
					initialSuccess = buttons[0].Focus(FocusState.Programmatic);
					if (initialSuccess)
					{
						break;
					}
					//await TestServices.WindowHelper.WaitForIdle(); //
					await Task.Delay(50); //
				}
				
				initialSuccess.Should().BeTrue("initialSuccess");
				AssertHasFocus(buttons[0]);
				await TestServices.WindowHelper.WaitForIdle();

				AssertHasFocus(buttons[0]);

				for (var i = 0; i < 4; i++)
				{
					var inner = i;
					buttons[i].GotFocus += (o, e) =>
					{
						receivedGotFocus[inner] = true;
						wasEventRaised = true;
						FocusManager.GetFocusedElement().Should().NotBeNull($"buttons[{i}].GotFocus");
					};

					buttons[i].LostFocus += (o, e) =>
					{
						receivedLostFocus[inner] = true;
						wasEventRaised = true;
						FocusManager.GetFocusedElement().Should().NotBeNull($"buttons[{i}].LostFocus");
					};
				}

				FocusManager.GotFocus += (o, e) =>
				{
					FocusManager.GetFocusedElement().Should().NotBeNull($"FocusManager.GotFocus - element");
					wasEventRaised = true;
				};
				FocusManager.LostFocus += (o, e) =>
				{
					FocusManager.GetFocusedElement().Should().NotBeNull($"FocusManager.LostFocus - element");
					wasEventRaised = true;
				};

				buttons[1].Focus(FocusState.Programmatic);
				buttons[3].Focus(FocusState.Programmatic);
				buttons[2].Focus(FocusState.Programmatic);

				AssertHasFocus(buttons[2]);
				wasEventRaised.Should().BeFalse("No event raised");
				await TestServices.WindowHelper.WaitForIdle();
				AssertHasFocus(buttons[2]);
				wasEventRaised.Should().BeTrue("event raised");


				receivedGotFocus[0].Should().BeFalse("receivedGotFocus[0]");
				receivedGotFocus[1].Should().BeTrue("receivedGotFocus[1]");
				receivedGotFocus[2].Should().BeTrue("receivedGotFocus[2]");
				receivedGotFocus[3].Should().BeTrue("receivedGotFocus[3]");

				receivedLostFocus[0].Should().BeTrue("receivedLostFocus[0]");
				receivedLostFocus[1].Should().BeTrue("receivedLostFocus[1]");
				receivedLostFocus[2].Should().BeFalse("receivedLostFocus[2]");
				receivedLostFocus[3].Should().BeTrue("receivedLostFocus[3]");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task IsTabStop_False_Check_Inner()
		{
			var outerControl = new ContentControl() { IsTabStop = false };
			var innerControl = new Button { Content = "Inner" };
			var contentRoot = new Border
			{
				Child = new Grid
				{
					Children =
					{
						new TextBlock {Text = "Spinach"},
						innerControl
					}
				}
			};
			outerControl.Content = contentRoot;

			TestServices.WindowHelper.WindowContent = outerControl;
			await TestServices.WindowHelper.WaitForIdle();

			outerControl.Focus(FocusState.Programmatic);
			AssertHasFocus(innerControl);
			Assert.AreEqual(FocusState.Unfocused, outerControl.FocusState);

			TestServices.WindowHelper.WindowContent = null;
		}

		private void AssertHasFocus(Control control)
		{
			control.Should().NotBeNull("control");
			var focused = FocusManager.GetFocusedElement();
			focused.Should().NotBeNull("focused element");
			focused.Should().BeSameAs(control, "must be same element");
			control.FocusState.Should().NotBe(FocusState.Unfocused);
		}
	}
}
