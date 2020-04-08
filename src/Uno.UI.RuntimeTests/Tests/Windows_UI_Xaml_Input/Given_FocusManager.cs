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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input
{
	[TestClass]
	public class Given_FocusManager
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task GotLostFocus()
		{
			try
			{
				var panel = new StackPanel();
				var wasEventRaised = false;

				var receivedGotFocus = new bool[4];
				var receivedLostFocus = new bool[4];

				var buttons = new Button[4];
				for (int i = 0; i < 4; i++)
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
				for (int i = 0; i < tries; i++)
				{
					initialSuccess = buttons[0].Focus(FocusState.Programmatic);
					if (initialSuccess)
					{
						break;
					}
					//await TestServices.WindowHelper.WaitForIdle(); //
					await Task.Delay(50); //
				}
				
				Assert.IsTrue(initialSuccess);
				AssertHasFocus(buttons[0]);
				await TestServices.WindowHelper.WaitForIdle();

				AssertHasFocus(buttons[0]);

				for (int i = 0; i < 4; i++)
				{
					var inner = i;
					buttons[i].GotFocus += (o, e) =>
					{
						receivedGotFocus[inner] = true;
						wasEventRaised = true;
						Assert.IsNotNull(FocusManager.GetFocusedElement());
					};

					buttons[i].LostFocus += (o, e) =>
					{
						receivedLostFocus[inner] = true;
						wasEventRaised = true;
						Assert.IsNotNull(FocusManager.GetFocusedElement());
					};
				}

				FocusManager.GotFocus += (o, e) =>
				{
					Assert.IsNotNull(FocusManager.GetFocusedElement());
					wasEventRaised = true;
				};
				FocusManager.LostFocus += (o, e) =>
				{
					Assert.IsNotNull(FocusManager.GetFocusedElement());
					wasEventRaised = true;
				};

				buttons[1].Focus(FocusState.Programmatic);
				buttons[3].Focus(FocusState.Programmatic);
				buttons[2].Focus(FocusState.Programmatic);

				AssertHasFocus(buttons[2]);
				Assert.IsFalse(wasEventRaised);
				await TestServices.WindowHelper.WaitForIdle();
				AssertHasFocus(buttons[2]);
				Assert.IsTrue(wasEventRaised);

				Assert.IsFalse(receivedGotFocus[0]);
				Assert.IsTrue(receivedGotFocus[1]);
				Assert.IsTrue(receivedGotFocus[2]);
				Assert.IsTrue(receivedGotFocus[3]);

				Assert.IsTrue(receivedLostFocus[0]);
				Assert.IsTrue(receivedLostFocus[1]);
				Assert.IsFalse(receivedLostFocus[2]);
				Assert.IsTrue(receivedLostFocus[3]);
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
			Assert.IsNotNull(control);
			var focused = FocusManager.GetFocusedElement();
			Assert.IsNotNull(focused);
			Assert.AreEqual(focused, control);
			Assert.AreNotEqual(FocusState.Unfocused, control.FocusState);
		}
	}
}
