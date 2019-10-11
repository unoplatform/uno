using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public partial class CoreWindow
	{
		[ThreadStatic]
		private static CoreWindow _current;

		public static CoreWindow GetForCurrentThread()
			=> _current; // UWP returns 'null' if on a BG thread

		private Point? _pointerPosition;
		private IPointerEventArgs _lastPointerEventArgs;

		internal CoreWindow()
		{
			_current = this;
		}

		public CoreDispatcher Dispatcher
			=> CoreDispatcher.Main;

		public Point PointerPosition
		{
			get => _pointerPosition ?? _lastPointerEventArgs?.GetLocation() ?? new Point();
			set => _pointerPosition = value;
		}

		[Uno.NotImplemented]
		public CoreCursor PointerCursor { get; set; } = new CoreCursor(CoreCursorType.Arrow, 0);

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetAsyncKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;

		internal void SetLastPointerEvent(IPointerEventArgs args)
			=> _lastPointerEventArgs = args;

		internal interface IPointerEventArgs
		{
			Point GetLocation();
		}
	}
}
