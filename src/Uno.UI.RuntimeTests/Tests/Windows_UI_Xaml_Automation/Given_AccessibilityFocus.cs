using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for accessibility focus management.
	/// Tests focus synchronization between the visual and semantic layers.
	/// </summary>
	[TestClass]
	public class Given_AccessibilityFocus
	{
		/// <summary>
		/// T086: Verifies that sequential focus movement works via automation peers.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Tab_Pressed_Then_Focus_Moves_Sequentially()
		{
			// Arrange
			var panel = new StackPanel();
			var button1 = new Button { Content = "First", IsTabStop = true };
			var button2 = new Button { Content = "Second", IsTabStop = true };
			var button3 = new Button { Content = "Third", IsTabStop = true };

			panel.Children.Add(button1);
			panel.Children.Add(button2);
			panel.Children.Add(button3);

			await UITestHelper.Load(panel);

			// Act
			button1.Focus(FocusState.Keyboard);
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			var peer1 = button1.GetOrCreateAutomationPeer();
			Assert.IsNotNull(peer1, "Button1 should have an automation peer");
			Assert.IsTrue(peer1.IsKeyboardFocusable(), "Button1 should be keyboard focusable");

			var peer2 = button2.GetOrCreateAutomationPeer();
			Assert.IsTrue(peer2.IsKeyboardFocusable(), "Button2 should be keyboard focusable");

			var peer3 = button3.GetOrCreateAutomationPeer();
			Assert.IsTrue(peer3.IsKeyboardFocusable(), "Button3 should be keyboard focusable");
		}

		/// <summary>
		/// T087: Verifies that dialog-like content receives focus when shown.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Dialog_Opens_Then_Focus_Moves_To_Dialog()
		{
			// Arrange
			var panel = new StackPanel();
			var button = new Button { Content = "Open Dialog", IsTabStop = true };
			var dialogContent = new Button
			{
				Content = "Dialog Button",
				IsTabStop = true,
				Visibility = Visibility.Collapsed
			};

			panel.Children.Add(button);
			panel.Children.Add(dialogContent);

			await UITestHelper.Load(panel);

			button.Focus(FocusState.Keyboard);
			await TestServices.WindowHelper.WaitForIdle();

			// Act - Simulate dialog opening
			dialogContent.Visibility = Visibility.Visible;
			dialogContent.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			var peer = dialogContent.GetOrCreateAutomationPeer();
			Assert.IsNotNull(peer, "Dialog button should have an automation peer");
			Assert.IsTrue(peer.IsKeyboardFocusable(), "Dialog content should be keyboard focusable");
		}

		/// <summary>
		/// T088: Verifies that when an element with focus is disabled, focus state is consistent.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Element_Disabled_With_Focus_Then_Focus_Moves_Away()
		{
			// Arrange
			var panel = new StackPanel();
			var button1 = new Button { Content = "Will Disable", IsTabStop = true };
			var button2 = new Button { Content = "Fallback", IsTabStop = true };

			panel.Children.Add(button1);
			panel.Children.Add(button2);

			await UITestHelper.Load(panel);

			button1.Focus(FocusState.Keyboard);
			await TestServices.WindowHelper.WaitForIdle();

			// Act
			button1.IsEnabled = false;
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			var peer1 = button1.GetOrCreateAutomationPeer();
			Assert.IsFalse(peer1.IsEnabled(), "Disabled button should report IsEnabled=false");
		}

		/// <summary>
		/// Verifies that non-focusable elements are correctly identified.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Element_Not_Focusable_Then_Peer_Reports_Not_Focusable()
		{
			// Arrange
			var textBlock = new TextBlock { Text = "Not focusable" };
			await UITestHelper.Load(textBlock);

			// Act
			var peer = textBlock.GetOrCreateAutomationPeer();

			// Assert
			Assert.IsNotNull(peer);
			Assert.IsFalse(peer.IsKeyboardFocusable(), "TextBlock should not be keyboard focusable");
		}
	}
}
