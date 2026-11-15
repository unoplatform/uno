#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
using WinPoint = Windows.Foundation.Point;

namespace Microsoft.Windows.Apps.Test.Foundation;

/// <summary>
/// Single InputInjector (and pointer state) shared by every shim entry point, in window coordinates.
/// </summary>
internal static class InteractionInput
{
	internal const uint DefaultHoldDuration = 1000;
	internal const uint DefaultDragDuration = 100;
	private const uint MoveSteps = 10;

	private static InputInjector? _injector;
	private static Mouse? _mouse;
	private static Finger? _finger;

	internal static InputInjector Injector
		=> _injector ??= InputInjector.TryCreate() ?? throw new InvalidOperationException("Input injection is not supported on this platform.");

	internal static Mouse Mouse => _mouse ??= Injector.GetMouse();

	internal static Finger Finger => _finger ??= Injector.GetFinger();

	internal static void Reset()
	{
		_finger?.Release();
		_mouse?.ReleaseAny();
		TextInput.ReleaseHeldKeys();
	}

	internal static void MoveMouse(WinPoint point) => Mouse.MoveTo(point);

	internal static void Click(WinPoint point, PointerButtons button)
	{
		Mouse.MoveTo(point);
		Press(button);
		Release(button);
	}

	internal static void DoubleClick(WinPoint point, PointerButtons button)
	{
		Click(point, button);
		Click(point, button);
	}

	internal static void Press(PointerButtons button)
	{
		switch (button)
		{
			case PointerButtons.Secondary or PointerButtons.PhysicalRight:
				Mouse.PressRight();
				break;
			case PointerButtons.Middle:
				Mouse.PressMiddle();
				break;
			default:
				Mouse.Press();
				break;
		}
	}

	internal static void Release(PointerButtons button)
	{
		switch (button)
		{
			case PointerButtons.Secondary or PointerButtons.PhysicalRight:
				Mouse.ReleaseRight();
				break;
			case PointerButtons.Middle:
				Mouse.ReleaseMiddle();
				break;
			default:
				Mouse.Release();
				break;
		}
	}

	internal static void MouseDrag(WinPoint from, WinPoint to, PointerButtons button, uint durationMs = DefaultDragDuration)
	{
		Mouse.MoveTo(from);
		Press(button);
		Mouse.MoveTo(to, MoveSteps, Math.Max(1, durationMs / MoveSteps));
		Release(button);
	}

	internal static void Wheel(WinPoint point, int delta)
	{
		Mouse.MoveTo(point);
		Mouse.Wheel(delta);
	}

	internal static void Tap(WinPoint point)
	{
		Finger.Press(point);
		Finger.Release();
	}

	internal static void TouchDown(WinPoint point) => Finger.Press(point);

	internal static void TouchMove(WinPoint point, uint durationMs) => Finger.MoveTo(point, MoveSteps, Math.Max(1, durationMs / MoveSteps));

	internal static void TouchUp() => Finger.Release();

	internal static void TapAndHold(WinPoint point, uint durationMs)
	{
		Finger.Press(point);
		Finger.MoveTo(point, steps: 1, stepOffsetInMilliseconds: durationMs);
		Finger.Release();
	}

	internal static void Pan(WinPoint from, WinPoint to, uint holdDuration, uint dragDuration)
	{
		Finger.Press(from);
		if (holdDuration > 0)
		{
			Finger.MoveTo(from, steps: 1, stepOffsetInMilliseconds: holdDuration);
		}

		Finger.MoveTo(to, MoveSteps, Math.Max(1, dragDuration / MoveSteps));
		Finger.Release();
	}

	internal static void InjectKeys(IEnumerable<InjectedInputKeyboardInfo> keys) => Injector.InjectKeyboardInput(keys);
}
