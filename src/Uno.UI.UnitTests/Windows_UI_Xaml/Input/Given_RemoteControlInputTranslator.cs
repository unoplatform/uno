#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Input;
using Windows.System;

namespace Uno.UI.Tests.Windows_UI_Xaml.Input;

[UITestClass]
public class Given_RemoteControlInputTranslator
{
	[TestMethod]
	// RemoteControlButton is internal, so the rows carry its name.
	[DataRow(nameof(RemoteControlButton.Up), VirtualKey.GamepadDPadUp)]
	[DataRow(nameof(RemoteControlButton.Down), VirtualKey.GamepadDPadDown)]
	[DataRow(nameof(RemoteControlButton.Left), VirtualKey.GamepadDPadLeft)]
	[DataRow(nameof(RemoteControlButton.Right), VirtualKey.GamepadDPadRight)]
	[DataRow(nameof(RemoteControlButton.Select), VirtualKey.GamepadA)]
	[DataRow(nameof(RemoteControlButton.Menu), VirtualKey.GamepadB)]
	[DataRow(nameof(RemoteControlButton.PlayPause), (VirtualKey)179)]
	public void When_Button_Then_Maps_To_Gamepad_Key(string button, VirtualKey expected)
		=> Assert.AreEqual(expected, RemoteControlInputTranslator.ToVirtualKey(Enum.Parse<RemoteControlButton>(button)));

	[TestMethod]
	public void When_Button_Pressed_Then_Raises_Down_And_Up()
	{
		var sut = new Sut(handleKeys: true);

		Assert.IsTrue(sut.Translator.OnButtonDown(RemoteControlButton.Select));
		Assert.IsTrue(sut.Translator.OnButtonUp(RemoteControlButton.Select));

		CollectionAssert.AreEqual(new[] { (VirtualKey.GamepadA, true), (VirtualKey.GamepadA, false) }, sut.Keys);
		Assert.AreEqual(0, sut.BackRequests);
	}

	[TestMethod]
	public void When_Swipe_Then_Raises_Directional_Down_And_Up()
	{
		var sut = new Sut(handleKeys: true);

		sut.Translator.OnSwipe(RemoteControlButton.Left);

		CollectionAssert.AreEqual(new[] { (VirtualKey.GamepadDPadLeft, true), (VirtualKey.GamepadDPadLeft, false) }, sut.Keys);
	}

	[TestMethod]
	public void When_Menu_Key_Handled_Then_No_Back_Request()
	{
		var sut = new Sut(handleKeys: true);

		Assert.IsTrue(sut.Translator.OnButtonDown(RemoteControlButton.Menu));
		Assert.IsTrue(sut.Translator.OnButtonUp(RemoteControlButton.Menu));

		Assert.AreEqual(0, sut.BackRequests);
	}

	[TestMethod]
	public void When_Menu_Key_Unhandled_Then_Back_Requested()
	{
		var sut = new Sut(handleKeys: false, handleBack: true);

		Assert.IsTrue(sut.Translator.OnButtonDown(RemoteControlButton.Menu));
		Assert.IsTrue(sut.Translator.OnButtonUp(RemoteControlButton.Menu));

		Assert.AreEqual(1, sut.BackRequests);
		CollectionAssert.AreEqual(new[] { (VirtualKey.GamepadB, true), (VirtualKey.GamepadB, false) }, sut.Keys);
	}

	[TestMethod]
	public void When_Menu_And_Back_Unhandled_Then_Press_And_Release_Go_To_System()
	{
		var sut = new Sut(handleKeys: false, handleBack: false);

		Assert.IsFalse(sut.Translator.OnButtonDown(RemoteControlButton.Menu));
		Assert.IsFalse(sut.Translator.OnButtonUp(RemoteControlButton.Menu));

		Assert.AreEqual(1, sut.BackRequests);
	}

	[TestMethod]
	public void When_Release_Forwarded_Then_Next_Handled_Press_Stays_In_App()
	{
		var sut = new Sut(handleKeys: false, handleBack: false);
		sut.Translator.OnButtonDown(RemoteControlButton.Menu);
		sut.Translator.OnButtonUp(RemoteControlButton.Menu);

		sut.HandleBack = true;

		Assert.IsTrue(sut.Translator.OnButtonDown(RemoteControlButton.Menu));
		Assert.IsTrue(sut.Translator.OnButtonUp(RemoteControlButton.Menu));
	}

	[TestMethod]
	public void When_Non_Menu_Key_Unhandled_Then_Goes_To_System_Without_Back_Request()
	{
		var sut = new Sut(handleKeys: false, handleBack: true);

		Assert.IsFalse(sut.Translator.OnButtonDown(RemoteControlButton.PlayPause));
		Assert.IsFalse(sut.Translator.OnButtonUp(RemoteControlButton.PlayPause));

		Assert.AreEqual(0, sut.BackRequests);
	}

	[TestMethod]
	public void When_Key_Handler_Throws_On_Press_Then_Press_And_Release_Go_To_System()
	{
		var sut = new Sut(handleKeys: true) { ThrowOnKey = _ => true };

		Assert.IsFalse(sut.Translator.OnButtonDown(RemoteControlButton.Select));
		Assert.IsFalse(sut.Translator.OnButtonUp(RemoteControlButton.Select));

		Assert.AreEqual(2, sut.Errors.Count);
	}

	[TestMethod]
	public void When_Key_Handler_Throws_On_Release_Then_Release_Stays_With_Its_Press()
	{
		var sut = new Sut(handleKeys: true) { ThrowOnKey = isDown => !isDown };

		Assert.IsTrue(sut.Translator.OnButtonDown(RemoteControlButton.Select));
		Assert.IsTrue(sut.Translator.OnButtonUp(RemoteControlButton.Select));

		Assert.AreEqual(1, sut.Errors.Count);
	}

	[TestMethod]
	public void When_Back_Handler_Throws_Then_Menu_Press_And_Release_Go_To_System()
	{
		var sut = new Sut(handleKeys: false) { ThrowOnBack = true };

		Assert.IsFalse(sut.Translator.OnButtonDown(RemoteControlButton.Menu));
		Assert.IsFalse(sut.Translator.OnButtonUp(RemoteControlButton.Menu));

		Assert.AreEqual(1, sut.Errors.Count);
	}

	[TestMethod]
	public void When_Key_Handler_Throws_On_Swipe_Then_Error_Reported()
	{
		var sut = new Sut(handleKeys: true) { ThrowOnKey = _ => true };

		sut.Translator.OnSwipe(RemoteControlButton.Up);

		Assert.AreEqual(2, sut.Errors.Count);
	}

	private sealed class Sut
	{
		public Sut(bool handleKeys, bool handleBack = false)
		{
			HandleBack = handleBack;
			Translator = new(
				(key, isDown) =>
				{
					Keys.Add((key, isDown));
					return ThrowOnKey?.Invoke(isDown) == true ? throw new InvalidOperationException("KeyDown handler") : handleKeys;
				},
				() =>
				{
					BackRequests++;
					return ThrowOnBack ? throw new InvalidOperationException("BackRequested handler") : HandleBack;
				},
				Errors.Add);
		}

		public Func<bool, bool>? ThrowOnKey { get; init; }

		public bool ThrowOnBack { get; init; }

		public List<Exception> Errors { get; } = new();

		public RemoteControlInputTranslator Translator { get; }

		public List<(VirtualKey Key, bool IsDown)> Keys { get; } = new();

		public int BackRequests { get; private set; }

		public bool HandleBack { get; set; }
	}
}
