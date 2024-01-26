using Gdk;
using System;
using Uno.Extensions;
using Uno.UI.Runtime.Skia;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Core
{
	internal static class Cursors
	{
		private static Cursor _default;
		public static Cursor Default => _default;
		private static Cursor _crosshair;
		public static Cursor Crosshair => _crosshair;
		private static Cursor _pointer;
		public static Cursor Pointer => _pointer;
		private static Cursor _help;
		public static Cursor Help => _help;
		private static Cursor _text;
		public static Cursor Text => _text;
		private static Cursor _allScroll;
		public static Cursor AllScroll => _allScroll;
		private static Cursor _neswResize;
		public static Cursor NeswResize => _neswResize;
		private static Cursor _nsResize;
		public static Cursor NsResize => _nsResize;
		private static Cursor _nwseResize;
		public static Cursor NwseResize => _nwseResize;
		private static Cursor _ewResize;
		public static Cursor EwResize => _ewResize;
		private static Cursor _notAllowed;
		public static Cursor NotAllowed => _notAllowed;
		private static Cursor _upArrow;
		public static Cursor UpArrow => _upArrow;
		private static Cursor _wait;
		public static Cursor Wait => _wait;

		public static void EnsureLoaded()
		{
			if (_default is not null)
			{
				return;
			}
			// Name based on this: https://developer.gnome.org/gdk3/stable/gdk3-Cursors.html#gdk-cursor-new-from-name
			// Fallback based on this list: https://developer.gnome.org/gdk3/stable/gdk3-Cursors.html#GdkCursorType
			Set(ref _default, "default", CursorType.Arrow);
			Set(ref _crosshair, "crosshair", CursorType.Cross);
			Set(ref _pointer, "pointer", CursorType.Hand1);
			Set(ref _help, "help", CursorType.QuestionArrow);
			Set(ref _text, "text", CursorType.Xterm);
			Set(ref _allScroll, "all-scroll", CursorType.Fleur);
			Set(ref _neswResize, "nesw-resize");
			Set(ref _nsResize, "ns-resize", CursorType.SbVDoubleArrow);
			Set(ref _nwseResize, "nwse-resize");
			Set(ref _ewResize, "ew-resize", CursorType.SbHDoubleArrow);
			Set(ref _notAllowed, "not-allowed");
			Set(ref _upArrow, CursorType.SbUpArrow);
			Set(ref _wait, "wait", CursorType.Watch);
		}

		private static void Set(ref Cursor cursor, string name, CursorType fallback = CursorType.Arrow)
		{
			try
			{
				var display = GtkHost.Current.InitialWindow.Display;
				if (display != null)
				{
					cursor = new Cursor(display, name);
					// Library funtion returns null if theme does not contain cursor with name.
					if (cursor.Handle != IntPtr.Zero)
					{
						return;
					}
				}
			}
			catch (Exception exception)
			{
				if (typeof(Cursors).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Cursors).Log().Error($"Unexpected exception while loading cursor \"{name}\" from theme.", exception);
				}
			}
			cursor = new Cursor(fallback);
		}

		private static void Set(ref Cursor cursor, CursorType type)
		{
			cursor = new Cursor(type);
		}
	}
}
