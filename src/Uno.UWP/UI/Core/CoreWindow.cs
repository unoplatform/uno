#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Input;

namespace Windows.UI.Core
{
	public partial class CoreWindow
	{
		[ThreadStatic]
		private static CoreWindow? _current;

		public static CoreWindow? GetForCurrentThread()
			=> _current; // UWP returns 'null' if on a BG thread

		private static Action? _invalidateRender;

		internal static void SetInvalidateRender(Action invalidateRender)
			=> _invalidateRender = invalidateRender;

		internal static void QueueInvalidateRender()
			=> _invalidateRender?.Invoke();

		public event TypedEventHandler<CoreWindow, WindowSizeChangedEventArgs>? SizeChanged;

		private Point? _pointerPosition;

		internal CoreWindow()
		{
			_current = this;
			Main ??= this;

			InitializePartial();
		}

		internal static CoreWindow Main { get; private set; }

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		internal IPointerEventArgs? LastPointerEvent { get; set; }

		public Point PointerPosition
		{
			get => _pointerPosition ?? LastPointerEvent?.GetLocation(null).Position ?? new Point();
			set => _pointerPosition = value;
		}

#if !__WASM__ && !__MACOS__
		[Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public CoreCursor PointerCursor { get; set; } = new CoreCursor(CoreCursorType.Arrow, 0);
#endif

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetAsyncKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;

		partial void InitializePartial();

		internal void OnSizeChanged(WindowSizeChangedEventArgs windowSizeChangedEventArgs)
			=> SizeChanged?.Invoke(this, windowSizeChangedEventArgs);

		internal interface IPointerEventArgs
		{
			PointerPoint GetLocation(object? relativeTo);
		}
	}
}
