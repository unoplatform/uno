using System.Windows.Input;
using Windows.UI.Core;

namespace Uno.UI.Skia.Platform.Extensions
{
	internal static class CursorExtensions
	{
		public static CoreCursor ToCoreCursor(this Cursor cursor)
		{
			var cursorType = CoreCursorType.Arrow;
			if (cursor == Cursors.Wait)
			{
				cursorType = CoreCursorType.Wait;
			}
			else if (cursor == Cursors.Hand)
			{
				cursorType = CoreCursorType.Hand;
			}
			else if (cursor == Cursors.Cross)
			{
				cursorType = CoreCursorType.Cross;
			}
			else if (cursor == Cursors.Help)
			{
				cursorType = CoreCursorType.Help;
			}
			else if (cursor == Cursors.IBeam)
			{
				cursorType = CoreCursorType.IBeam;
			}
			else if (cursor == Cursors.No)
			{
				cursorType = CoreCursorType.UniversalNo;
			}
			else if (cursor == Cursors.UpArrow)
			{
				cursorType = CoreCursorType.UpArrow;
			}
			else if (cursor == Cursors.SizeNESW)
			{
				cursorType = CoreCursorType.SizeNortheastSouthwest;
			}
			else if (cursor == Cursors.SizeNS)
			{
				cursorType = CoreCursorType.SizeNorthSouth;
			}
			else if (cursor == Cursors.SizeNWSE)
			{
				cursorType = CoreCursorType.SizeNorthwestSoutheast;
			}
			else if (cursor == Cursors.SizeWE)
			{
				cursorType = CoreCursorType.SizeWestEast;
			}
			return new CoreCursor(cursorType, 0);
		}

		public static Cursor ToCursor(this CoreCursor coreCursor) =>
			coreCursor?.Type switch
			{
				CoreCursorType.Arrow => Cursors.Arrow,
				CoreCursorType.Cross => Cursors.Cross,
				CoreCursorType.Hand => Cursors.Hand,
				CoreCursorType.Help => Cursors.Help,
				CoreCursorType.IBeam => Cursors.IBeam,
				CoreCursorType.SizeAll => Cursors.SizeAll,
				CoreCursorType.SizeNortheastSouthwest => Cursors.SizeNESW,
				CoreCursorType.SizeNorthSouth => Cursors.SizeNS,
				CoreCursorType.SizeNorthwestSoutheast => Cursors.SizeNWSE,
				CoreCursorType.SizeWestEast => Cursors.SizeWE,
				CoreCursorType.UniversalNo => Cursors.No,
				CoreCursorType.UpArrow => Cursors.UpArrow,
				CoreCursorType.Wait => Cursors.Wait,
				_ => Cursors.Arrow,
			};
	}
}
