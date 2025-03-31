using System.ComponentModel;
using Windows.UI.Core;

namespace Microsoft.UI.Input;

internal static partial class InputSystemCursorShapeExtensions
{
	internal static InputSystemCursorShape ToInputSystemCursorShape(this CoreCursorType shape)
	{
		switch (shape)
		{
			case CoreCursorType.Arrow:
				return InputSystemCursorShape.Arrow;
			case CoreCursorType.Cross:
				return InputSystemCursorShape.Cross;
			case CoreCursorType.Custom: // no direct mapping
				return InputSystemCursorShape.Arrow;
			case CoreCursorType.Hand:
				return InputSystemCursorShape.Hand;
			case CoreCursorType.Help:
				return InputSystemCursorShape.Help;
			case CoreCursorType.IBeam:
				return InputSystemCursorShape.IBeam;
			case CoreCursorType.SizeAll:
				return InputSystemCursorShape.SizeAll;
			case CoreCursorType.SizeNortheastSouthwest:
				return InputSystemCursorShape.SizeNortheastSouthwest;
			case CoreCursorType.SizeNorthSouth:
				return InputSystemCursorShape.SizeNorthSouth;
			case CoreCursorType.SizeNorthwestSoutheast:
				return InputSystemCursorShape.SizeNorthwestSoutheast;
			case CoreCursorType.SizeWestEast:
				return InputSystemCursorShape.SizeWestEast;
			case CoreCursorType.UniversalNo:
				return InputSystemCursorShape.UniversalNo;
			case CoreCursorType.UpArrow:
				return InputSystemCursorShape.UpArrow;
			case CoreCursorType.Wait:
				return InputSystemCursorShape.Wait;
			case CoreCursorType.Pin:
				return InputSystemCursorShape.Pin;
			case CoreCursorType.Person:
				return InputSystemCursorShape.Person;
			default:
				throw new InvalidEnumArgumentException(nameof(shape), (int)shape, typeof(CoreCursorType));
		}
	}

	internal static CoreCursorType ToCoreCursorType(this InputSystemCursorShape shape)
	{
		switch (shape)
		{
			case InputSystemCursorShape.Arrow:
				return CoreCursorType.Arrow;
			case InputSystemCursorShape.Cross:
				return CoreCursorType.Cross;
			case InputSystemCursorShape.Hand:
				return CoreCursorType.Hand;
			case InputSystemCursorShape.Help:
				return CoreCursorType.Help;
			case InputSystemCursorShape.IBeam:
				return CoreCursorType.IBeam;
			case InputSystemCursorShape.SizeAll:
				return CoreCursorType.SizeAll;
			case InputSystemCursorShape.SizeNortheastSouthwest:
				return CoreCursorType.SizeNortheastSouthwest;
			case InputSystemCursorShape.SizeNorthSouth:
				return CoreCursorType.SizeNorthSouth;
			case InputSystemCursorShape.SizeNorthwestSoutheast:
				return CoreCursorType.SizeNorthwestSoutheast;
			case InputSystemCursorShape.SizeWestEast:
				return CoreCursorType.SizeWestEast;
			case InputSystemCursorShape.UniversalNo:
				return CoreCursorType.UniversalNo;
			case InputSystemCursorShape.UpArrow:
				return CoreCursorType.UpArrow;
			case InputSystemCursorShape.Wait:
				return CoreCursorType.Wait;
			case InputSystemCursorShape.Pin:
				return CoreCursorType.Pin;
			case InputSystemCursorShape.Person:
				return CoreCursorType.Person;
			case InputSystemCursorShape.AppStarting: // no direct mapping
				return CoreCursorType.Arrow;
			default:
				throw new InvalidEnumArgumentException(nameof(shape), (int)shape, typeof(InputSystemCursorShape));
		}
	}
}
