using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdk;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.GTK.Extensions
{
	internal static class CursorExtensions
	{
		public static CoreCursor ToCoreCursor(this Cursor cursor)
		{
			CoreCursorType GetCoreCursorType(CursorType? type) =>
				type switch
				{
					CursorType.Arrow => CoreCursorType.Arrow,
					CursorType.Cross => CoreCursorType.Cross,
					CursorType.Hand1 => CoreCursorType.Hand,
					CursorType.Hand2 => CoreCursorType.Hand,
					CursorType.QuestionArrow => CoreCursorType.Help,
					CursorType.Sizing => CoreCursorType.SizeAll,
					CursorType.Watch => CoreCursorType.Wait,
					_ => CoreCursorType.Arrow,
				};
			
			return new CoreCursor(GetCoreCursorType(cursor?.CursorType), 0);
		}

		public static Cursor ToCursor(this CoreCursor coreCursor)
		{
			CursorType GetCursorType(CoreCursorType? coreCursorType) =>
				coreCursorType switch
				{
					CoreCursorType.Arrow => CursorType.Arrow,
					CoreCursorType.Cross => CursorType.Cross,
					CoreCursorType.Hand => CursorType.Hand1,
					CoreCursorType.Help => CursorType.QuestionArrow,
					CoreCursorType.SizeAll => CursorType.Sizing,
					CoreCursorType.Wait => CursorType.Watch,
					_ => CursorType.Arrow,
				};

			return new Cursor(GetCursorType(coreCursor?.Type));
		}
			
	}
}
