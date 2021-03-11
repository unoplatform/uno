#if __MACOS__
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AppKit;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Uno.Foundation.Extensibility;

namespace Windows.UI.Core
{
	partial class CoreWindow
	{
		private readonly NSWindow _window;

		public CoreWindow(NSWindow window)
			: this()
		{
			_window = window;
		}

		/// <summary>
		/// Gets a reference to the native macOS Window behind the <see cref="CoreWindow"/> abstraction.
		/// </summary>
		internal NSWindow NativeWindow => _window;

		internal void RefreshCursor()
		{
			if (_coreWindowExtension is { })
			{
				_coreWindowExtension.PointerCursor = _coreWindowExtension.PointerCursor;
			}
		}
	}


	internal partial class CoreWindowExtension : ICoreWindowExtension
	{
		private bool _cursorHidden = false;
		private CoreCursor _pointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);

		/// <inheritdoc />
		public CoreCursor PointerCursor
		{
			get => _pointerCursor;
			set
			{
				_pointerCursor = value;
				RefreshCursor();
			}
		}

		/// <inheritdoc />
		public void ReleasePointerCapture() { }

		/// <inheritdoc />
		public void SetPointerCapture() { }

		private void RefreshCursor()
		{
			if (PointerCursor == null)
			{
				if (!_cursorHidden)
				{
					NSCursor.Hide();
					_cursorHidden = true;
				}
			}
			else
			{
				if (_cursorHidden)
				{
					NSCursor.Unhide();
					_cursorHidden = false;
				}
				switch (_pointerCursor.Type)
				{
					case CoreCursorType.Arrow:
						NSCursor.ArrowCursor.Set();
						break;
					case CoreCursorType.Cross:
						NSCursor.CrosshairCursor.Set();
						break;
					case CoreCursorType.Hand:
						NSCursor.PointingHandCursor.Set();
						break;
					case CoreCursorType.IBeam:
						NSCursor.IBeamCursor.Set();
						break;
					case CoreCursorType.SizeNorthSouth:
						NSCursor.ResizeUpDownCursor.Set();
						break;
					case CoreCursorType.SizeWestEast:
						NSCursor.ResizeLeftRightCursor.Set();
						break;
					default:
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().LogWarning($"Cursor type '{_pointerCursor.Type}' is not supported on macOS. Default cursor is used instead.");
						}
						NSCursor.ArrowCursor.Set();
						break;
				}
			}
		}
	}
}
#endif
