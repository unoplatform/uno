using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Core;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Tests.Uno_UI_Core
{
	[TestClass]
	public class Given_KeyboardStateTracker
	{
		// The tracker is process-wide static state, so each test uses its own key.

		[TestMethod]
		public void When_CleanSlate()
		{
			var state = KeyboardStateTracker.GetKeyState(VirtualKey.A);
			Assert.AreEqual(CoreVirtualKeyStates.None, state);
		}

		[TestMethod]
		public void When_Press()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.B);

			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.B));
		}

		[TestMethod]
		public void When_Release()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.C);
			KeyboardStateTracker.OnKeyUp(VirtualKey.C);

			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.C));
		}

		[TestMethod]
		public void When_Press_Release_Repeatedly()
		{
			for (int i = 0; i < 10; i++)
			{
				KeyboardStateTracker.OnKeyDown(VirtualKey.D);
				Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.D));

				KeyboardStateTracker.OnKeyUp(VirtualKey.D);
				Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.D));
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17100")]
		public void When_KeyDown_Repeats_While_Held()
		{
			// A single physical press reaches the tracker several times: routed key events are raised
			// once per element while bubbling, and the OS repeats key downs for a key that is held.
			// Neither may change the reported state.
			KeyboardStateTracker.OnKeyDown(VirtualKey.E);

			for (int i = 0; i < 10; i++)
			{
				KeyboardStateTracker.OnKeyDown(VirtualKey.E);
				Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.E));
			}

			KeyboardStateTracker.OnKeyUp(VirtualKey.E);
			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.E));
		}

		[TestMethod]
		public void When_KeyUp_Repeats()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.F);
			KeyboardStateTracker.OnKeyUp(VirtualKey.F);
			KeyboardStateTracker.OnKeyUp(VirtualKey.F);

			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.F));
		}

		[TestMethod]
		public void When_Side_Key_Pressed_Then_Combined_Key_Follows()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.LeftControl);

			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.LeftControl));
			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.Control));

			KeyboardStateTracker.OnKeyUp(VirtualKey.LeftControl);

			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.LeftControl));
			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.Control));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17100")]
		public void When_Combined_Key_Released_Then_Side_Keys_Follow()
		{
			// X11 reports the side key while the browser reports the combined one. A release of the
			// combined key has to clear both sides, or a side key left down by the other path sticks.
			KeyboardStateTracker.OnKeyDown(VirtualKey.LeftMenu);
			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.LeftMenu));

			KeyboardStateTracker.OnKeyUp(VirtualKey.Menu);

			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.Menu));
			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.LeftMenu));
			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.RightMenu));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17100")]
		public void When_SyncModifierState_Overrides_Stuck_Down()
		{
			// Simulates a key down for a modifier with no matching key up - e.g. a mobile on-screen
			// keyboard's Shift during auto-capitalization - which otherwise leaves the key reported
			// as down forever. An input event carrying the real modifier state must correct it.
			KeyboardStateTracker.OnKeyDown(VirtualKey.Shift);
			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.Shift));

			KeyboardStateTracker.SyncModifierState(VirtualKey.Shift, isDown: false);

			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.Shift));
		}

		[TestMethod]
		public void When_SyncModifierState_Sets_Down()
		{
			KeyboardStateTracker.SyncModifierState(VirtualKey.RightControl, isDown: true);

			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.RightControl));
			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.Control));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17100")]
		public void When_SyncModifierState_Interleaves_With_A_Real_Press()
		{
			// The resync runs on every input event, including ones arriving between a genuine key down
			// and its key up. It must agree with the key events rather than fight them.
			KeyboardStateTracker.OnKeyDown(VirtualKey.RightShift);

			KeyboardStateTracker.SyncModifierState(VirtualKey.Shift, isDown: true);
			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.Shift));

			KeyboardStateTracker.OnKeyUp(VirtualKey.RightShift);
			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.Shift));
		}

		[TestMethod]
		public void When_Reset()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.G);
			Assert.AreEqual(CoreVirtualKeyStates.Down, KeyboardStateTracker.GetKeyState(VirtualKey.G));

			KeyboardStateTracker.Reset();

			Assert.AreEqual(CoreVirtualKeyStates.None, KeyboardStateTracker.GetKeyState(VirtualKey.G));
		}
	}
}
