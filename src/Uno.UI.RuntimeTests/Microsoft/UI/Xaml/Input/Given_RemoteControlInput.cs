#nullable enable

// RemoteControlInputTranslator is the Uno-internal bridge used by TV remotes (tvOS Siri Remote),
// so the whole fixture is Uno-only.
#if HAS_UNO

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Input;

[TestClass]
[RunsOnUIThread]
public class Given_RemoteControlInput
{
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_Down_Then_Focus_Moves_Down(bool isSwipe)
	{
		var top = new Button { Content = "Top" };
		var bottom = new Button { Content = "Bottom" };
		var panel = new StackPanel { XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled, Children = { top, bottom } };

		try
		{
			await UITestHelper.Load(panel);
			top.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			var translator = CreateTranslator(out _);
			if (isSwipe)
			{
				translator.OnSwipe(RemoteControlButton.Down);
			}
			else
			{
				Assert.IsTrue(translator.OnButtonDown(RemoteControlButton.Down));
				translator.OnButtonUp(RemoteControlButton.Down);
			}
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(bottom, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
			Assert.AreEqual(FocusState.Keyboard, bottom.FocusState);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Select_Then_Focused_Button_Clicked()
	{
		var button = new Button { Content = "Button" };
		var clicks = 0;
		button.Click += (_, _) => clicks++;

		try
		{
			await UITestHelper.Load(button);
			button.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			var translator = CreateTranslator(out var backRequests);
			Assert.IsTrue(translator.OnButtonDown(RemoteControlButton.Select));
			Assert.IsTrue(translator.OnButtonUp(RemoteControlButton.Select));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, clicks);
			Assert.AreEqual(0, backRequests.Count);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Menu_With_Open_Flyout_Then_Flyout_Closed_Without_Back_Request()
	{
		var flyoutContent = new Button { Content = "Inside" };
		var flyout = new Flyout { Content = flyoutContent };
		var owner = new Button { Content = "Owner", Flyout = flyout };

		try
		{
			await UITestHelper.Load(owner);
			flyout.ShowAt(owner);
			await WindowHelper.WaitForLoaded(flyoutContent);
			flyoutContent.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			var translator = CreateTranslator(out var backRequests);
			Assert.IsTrue(translator.OnButtonDown(RemoteControlButton.Menu));
			translator.OnButtonUp(RemoteControlButton.Menu);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(flyout.IsOpen);
			Assert.AreEqual(0, backRequests.Count);
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Menu_Unhandled_Then_BackRequested_Raised()
	{
		var button = new Button { Content = "Button" };
		var backRequested = 0;
		void OnBackRequested(object? sender, BackRequestedEventArgs e)
		{
			backRequested++;
			e.Handled = true;
		}

		var navigationManager = SystemNavigationManager.GetForCurrentView();
		navigationManager.BackRequested += OnBackRequested;
		try
		{
			await UITestHelper.Load(button);
			button.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			var translator = new RemoteControlInputTranslator(RaiseKey, navigationManager.RequestBack, Rethrow);
			Assert.IsTrue(translator.OnButtonDown(RemoteControlButton.Menu));
			Assert.IsTrue(translator.OnButtonUp(RemoteControlButton.Menu));

			Assert.AreEqual(1, backRequested);
		}
		finally
		{
			navigationManager.BackRequested -= OnBackRequested;
			WindowHelper.WindowContent = null;
		}
	}

	private static RemoteControlInputTranslator CreateTranslator(out BackRequestCounter backRequests)
	{
		var counter = backRequests = new BackRequestCounter();
		return new RemoteControlInputTranslator(RaiseKey, () =>
		{
			counter.Count++;
			return true;
		}, Rethrow);
	}

	private static void Rethrow(Exception e) => ExceptionDispatchInfo.Capture(e).Throw();

	// Goes through the InputManager, so the key is routed from the focused element like a real remote press.
	private static bool RaiseKey(VirtualKey key, bool isDown)
	{
		var args = new KeyEventArgs("test", key, VirtualKeyModifiers.None, new CorePhysicalKeyStatus());
		WindowHelper.XamlRoot!.VisualTree.ContentRoot.InputManager.Keyboard.OnKeyTestingOnly(args, isDown);
		return args.Handled;
	}

	private sealed class BackRequestCounter
	{
		public int Count { get; set; }
	}
}
#endif
