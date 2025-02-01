namespace Microsoft.UI.Input;

internal static partial class InputSystemCursorShapeExtensions
{
	internal static string ToCssProtectedCursor(this InputSystemCursorShape inputSystemCursorShape)
	{
		// Best matches between 
		// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.input.inputsystemcursorshape
		// and 
		// https://developer.mozilla.org/fr/docs/Web/CSS/cursor
		// Note that a similar method exists in CoreCursorTypeExtensions.wasm.cs

		switch (inputSystemCursorShape)
		{
			case InputSystemCursorShape.AppStarting:
				return "wait";
			case InputSystemCursorShape.Arrow:
				return "auto";
			case InputSystemCursorShape.Cross:
				return "crosshair";
			case InputSystemCursorShape.Hand:
				return "pointer";
			case InputSystemCursorShape.Help:
				return "help";
			case InputSystemCursorShape.IBeam:
				return "text";
			case InputSystemCursorShape.SizeAll:
				return "move";
			case InputSystemCursorShape.SizeNortheastSouthwest:
				return "nesw-resize";
			case InputSystemCursorShape.SizeNorthSouth:
				return "ns-resize";
			case InputSystemCursorShape.SizeNorthwestSoutheast:
				return "nwse-resize";
			case InputSystemCursorShape.SizeWestEast:
				return "ew-resize";
			case InputSystemCursorShape.UniversalNo:
				return "not-allowed";
			case InputSystemCursorShape.UpArrow:
				return "n-resize";
			case InputSystemCursorShape.Wait:
				return "wait";

			case InputSystemCursorShape.Pin:
			case InputSystemCursorShape.Person:
				return "pointer";
			default:
				return "auto";
		}
	}
}
