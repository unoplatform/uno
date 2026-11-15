#nullable enable

using System;
using DrawingPoint = System.Drawing.Point;
using WinPoint = Windows.Foundation.Point;

namespace Microsoft.Windows.Apps.Test.Foundation;

public static class PointerInput
{
	public static void Move(DrawingPoint point) => InteractionInput.MoveMouse(ToWin(point));

	public static void Press(PointerButtons button) => InteractionInput.Press(button);

	public static void Release(PointerButtons button) => InteractionInput.Release(button);

	public static void Click(PointerButtons button) => InteractionInput.Click(InteractionInput.Mouse.Position(), button);

	public static void ClickDrag(DrawingPoint end, PointerButtons button, uint duration)
		=> InteractionInput.MouseDrag(InteractionInput.Mouse.Position(), ToWin(end), button, duration);

	internal static WinPoint ToWin(DrawingPoint point) => new(point.X, point.Y);
}

public static class MouseWheelInput
{
	public static void RotateWheel(UIObject uiObject, int mouseWheelDelta)
	{
		var point = uiObject.GetClickablePoint();
		InteractionInput.Wheel(PointerInput.ToWin(point), mouseWheelDelta);
	}

	public static void RotateWheel(int mouseWheelDelta)
		=> InteractionInput.Wheel(InteractionInput.Mouse.Position(), mouseWheelDelta);
}

public class SinglePointGesture
{
	private WinPoint _position;

	private SinglePointGesture()
	{
	}

	public static SinglePointGesture Current { get; } = new();

	public void Move(DrawingPoint point) => _position = PointerInput.ToWin(point);

	public void Tap() => InteractionInput.Tap(_position);

	public void PressAndDrag(DrawingPoint end, uint dragDuration, uint holdDuration)
		=> InteractionInput.Pan(_position, PointerInput.ToWin(end), holdDuration, dragDuration);

	public void Pan(DrawingPoint end, uint holdDuration, float acceleration)
		=> InteractionInput.Pan(_position, PointerInput.ToWin(end), holdDuration, PanDuration(_position, PointerInput.ToWin(end), acceleration));

	public void ClickDrag(DrawingPoint end, PointerButtons button, uint duration)
		=> InteractionInput.Pan(_position, PointerInput.ToWin(end), holdDuration: 0, duration);

	// MITA accelerates the contact from rest, so distance = a * t^2 / 2 (pixels and milliseconds).
	private static uint PanDuration(WinPoint start, WinPoint end, float acceleration)
	{
		var distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
		return acceleration > 0 ? (uint)Math.Max(1, Math.Sqrt(2 * distance / acceleration)) : InteractionInput.DefaultDragDuration;
	}
}

public static class MultiPointGesture
{
	public static void Pinch(UIObject uiObject) => throw NotSupported();

	public static void Pinch(UIObject uiObject, uint distance, float direction, bool pivot) => throw NotSupported();

	public static void Stretch(UIObject uiObject) => throw NotSupported();

	public static void Stretch(UIObject uiObject, uint distance, float direction, bool pivot) => throw NotSupported();

	private static NotSupportedException NotSupported() => new("Multi-touch gestures are not supported by the in-process MITA shim.");
}

internal static class MousePositionExtensions
{
	internal static WinPoint Position(this Uno.UI.DevTools.Input.Mouse mouse)
#if HAS_UNO
		=> InteractionInput.Injector.Mouse.Position;
#else
		=> throw new NotSupportedException("The injected mouse position is only tracked on Uno.");
#endif
}
