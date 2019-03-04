using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Uno.Foundation;

namespace Windows.UI.Core
{
	public partial class CoreWindow 
	{
		private CoreCursor _pointerCursor;

		public global::Windows.UI.Core.CoreCursor PointerCursor
		{
			get => _pointerCursor;
			set
			{
				_pointerCursor = value;
				Internal_SetPointerCursor();
			}

		}

		private void Internal_SetPointerCursor()
		{
			string cssCoreCursorType = GetCssCursor(_pointerCursor.Type);
			WebAssemblyRuntime.InvokeJS($"Uno.UI.WindowManager.current.setCursor(\"{cssCoreCursorType}\");");
		}

		string GetCssCursor(CoreCursorType coreCursorType)
		{
			//best matches between 
			// https://docs.microsoft.com/id-id/uwp/api/windows.ui.core.corecursortype
			//and 
			//https://developer.mozilla.org/fr/docs/Web/CSS/cursor
			// no support for Custom Cursor for now

			switch (coreCursorType)
			{
				case CoreCursorType.Custom:
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
		[global::Uno.NotImplemented]
		public global::Windows.UI.Core.CoreVirtualKeyStates GetKeyState(global::Windows.System.VirtualKey virtualKey)
		{
			return CoreVirtualKeyStates.None;
		}
	}
}
