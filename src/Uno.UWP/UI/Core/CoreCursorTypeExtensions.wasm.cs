using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Core
{
	internal static class CoreCursorTypeExtensions
	{
		internal static string ToCssCursor(this CoreCursorType coreCursorType)
		{
			//best matches between 
			// https://docs.microsoft.com/id-id/uwp/api/windows.ui.core.corecursortype
			//and 
			//https://developer.mozilla.org/fr/docs/Web/CSS/cursor
			// no support for Custom Cursor for now

			switch (coreCursorType)
			{
				case CoreCursorType.Custom:
					coreCursorType.Log().Warn("Cursor type 'Custom' not supported yet. Default cursor is used instead.");
					return "auto";
				case CoreCursorType.Arrow:
					return "auto";
				case CoreCursorType.Cross:
					return "crosshair";
				case CoreCursorType.Hand:
					return "pointer";
				case CoreCursorType.Help:
					return "help";
				case CoreCursorType.IBeam:
					return "text";
				case CoreCursorType.SizeAll:
					return "move";
				case CoreCursorType.SizeNortheastSouthwest:
					return "nesw-resize";
				case CoreCursorType.SizeNorthSouth:
					return "ns-resize";
				case CoreCursorType.SizeNorthwestSoutheast:
					return "nwse-resize";
				case CoreCursorType.SizeWestEast:
					return "ew-resize";
				case CoreCursorType.UniversalNo:
					return "not-allowed";
				case CoreCursorType.UpArrow:
					return "n-resize";
				case CoreCursorType.Wait:
					return "wait";

				case CoreCursorType.Pin:
				case CoreCursorType.Person:
					return "pointer";
				default:
					return "auto";
			}
		}
	}
}
