using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Core;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Tests.Uno_UI_Core
{
	[TestClass]
	public class Given_KeyboardStateTracker
	{
		[TestMethod]
		public void When_CleanSlate()
		{
			var state = KeyboardStateTracker.GetKeyState(VirtualKey.A);
			Assert.AreEqual(CoreVirtualKeyStates.None, state);
		}

		[TestMethod]
		public void When_Initial_Press_Locks()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.B);

			var state = KeyboardStateTracker.GetKeyState(VirtualKey.B);
			Assert.AreEqual(CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked, state);
		}

		[TestMethod]
		public void When_Release_Keeps_Lock()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.C);
			KeyboardStateTracker.OnKeyUp(VirtualKey.C);

			var state = KeyboardStateTracker.GetKeyState(VirtualKey.C);
			Assert.AreEqual(CoreVirtualKeyStates.None | CoreVirtualKeyStates.Locked, state);
		}

		[TestMethod]
		public void When_Second_Down_Unlocks()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.D);
			KeyboardStateTracker.OnKeyUp(VirtualKey.D);
			KeyboardStateTracker.OnKeyDown(VirtualKey.D);

			var state = KeyboardStateTracker.GetKeyState(VirtualKey.D);
			Assert.AreEqual(CoreVirtualKeyStates.Down, state);
		}

		[TestMethod]
		public void When_Second_Release_Keeps_Unlocked()
		{
			KeyboardStateTracker.OnKeyDown(VirtualKey.E);
			KeyboardStateTracker.OnKeyUp(VirtualKey.E);
			KeyboardStateTracker.OnKeyDown(VirtualKey.E);
			KeyboardStateTracker.OnKeyUp(VirtualKey.E);

			var state = KeyboardStateTracker.GetKeyState(VirtualKey.E);
			Assert.AreEqual(CoreVirtualKeyStates.None, state);
		}

		[TestMethod]
		public void When_Even_KeyDown()
		{
			for (int i = 0; i < 10; i++)
			{
				// Odd sequence unlocks
				KeyboardStateTracker.OnKeyDown(VirtualKey.F);
				KeyboardStateTracker.OnKeyUp(VirtualKey.F);

				// Every even down should set lock
				KeyboardStateTracker.OnKeyDown(VirtualKey.F);

				var state = KeyboardStateTracker.GetKeyState(VirtualKey.F);
				Assert.AreEqual(CoreVirtualKeyStates.Down, state);

				KeyboardStateTracker.OnKeyUp(VirtualKey.F);

				state = KeyboardStateTracker.GetKeyState(VirtualKey.F);
				Assert.AreEqual(CoreVirtualKeyStates.None, state);
			}
		}

		[TestMethod]
		public void When_Odd_KeyDown()
		{
			for (int i = 0; i < 10; i++)
			{
				// Every odd down should not have lock
				KeyboardStateTracker.OnKeyDown(VirtualKey.G);

				var state = KeyboardStateTracker.GetKeyState(VirtualKey.G);
				Assert.AreEqual(CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked, state);

				KeyboardStateTracker.OnKeyUp(VirtualKey.G);

				state = KeyboardStateTracker.GetKeyState(VirtualKey.G);
				Assert.AreEqual(CoreVirtualKeyStates.None | CoreVirtualKeyStates.Locked, state);

				// Unlock sequence
				KeyboardStateTracker.OnKeyDown(VirtualKey.G);
				KeyboardStateTracker.OnKeyUp(VirtualKey.G);
			}
		}
	}
}
