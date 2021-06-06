using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdk;
using Windows.UI.Core;
using Uno.UI.Runtime.Skia.GTK.UI.Core;

namespace Uno.UI.Runtime.Skia.GTK.Extensions
{
	internal static class CursorExtensions
	{
		public static CoreCursor ToCoreCursor(this Cursor cursor)
		{
			CoreCursorType cursorType;
			if (cursor == Cursors.Wait)
			{
				cursorType = CoreCursorType.Wait;
			}
			else if (cursor == Cursors.Pointer)
			{
				cursorType = CoreCursorType.Hand;
			}
			else if (cursor == Cursors.Crosshair)
			{
				cursorType = CoreCursorType.Cross;
			}
			else if (cursor == Cursors.Help)
			{
				cursorType = CoreCursorType.Help;
			}
			else if (cursor == Cursors.Text)
			{
				cursorType = CoreCursorType.IBeam;
			}
			else if (cursor == Cursors.NotAllowed)
			{
				cursorType = CoreCursorType.UniversalNo;
			}
			else if (cursor == Cursors.UpArrow)
			{
				cursorType = CoreCursorType.UpArrow;
			}
			else if (cursor == Cursors.NeswResize)
			{
				cursorType = CoreCursorType.SizeNortheastSouthwest;
			}
			else if (cursor == Cursors.NsResize)
			{
				cursorType = CoreCursorType.SizeNorthSouth;
			}
			else if (cursor == Cursors.NwseResize)
			{
				cursorType = CoreCursorType.SizeNorthwestSoutheast;
			}
			else if (cursor == Cursors.EwResize)
			{
				cursorType = CoreCursorType.SizeWestEast;
			}
			else
			{
				CoreCursorType GetCoreCursorType(CursorType? type) =>
					type switch
					{
						CursorType.Arrow => CoreCursorType.Arrow,
						CursorType.Cross => CoreCursorType.Cross,
						CursorType.Fleur => CoreCursorType.SizeAll,
						CursorType.Hand1 => CoreCursorType.Hand,
						CursorType.Hand2 => CoreCursorType.Hand,
						CursorType.QuestionArrow => CoreCursorType.Help,
						CursorType.SbUpArrow => CoreCursorType.UpArrow,
						CursorType.SbHDoubleArrow => CoreCursorType.SizeWestEast,
						CursorType.SbVDoubleArrow => CoreCursorType.SizeNorthSouth,
						CursorType.Sizing => CoreCursorType.SizeAll,
						CursorType.Watch => CoreCursorType.Wait,
						CursorType.Xterm => CoreCursorType.IBeam,
						_ => CoreCursorType.Arrow,
					};
				cursorType = GetCoreCursorType(cursor?.CursorType);

			}
			return new CoreCursor(cursorType, 0);
		}

		public static Cursor ToCursor(this CoreCursor coreCursor) =>
			coreCursor?.Type switch
			{
				CoreCursorType.Arrow => Cursors.Default,
				CoreCursorType.Cross => Cursors.Crosshair,
				CoreCursorType.Hand => Cursors.Pointer,
				CoreCursorType.Help => Cursors.Help,
				CoreCursorType.IBeam => Cursors.Text,
				CoreCursorType.SizeAll => Cursors.AllScroll,
				CoreCursorType.SizeNortheastSouthwest => Cursors.NeswResize,
				CoreCursorType.SizeNorthSouth => Cursors.NsResize,
				CoreCursorType.SizeNorthwestSoutheast => Cursors.NwseResize,
				CoreCursorType.SizeWestEast => Cursors.EwResize,
				CoreCursorType.UniversalNo => Cursors.NotAllowed,
				CoreCursorType.UpArrow => Cursors.UpArrow,
				CoreCursorType.Wait => Cursors.Wait,
				_ => Cursors.Default
			};
	}
}
