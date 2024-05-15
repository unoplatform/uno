#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Windows.UI.Input;

namespace Windows.UI.Core;

public partial class CoreWindow
{
	private IUnoCorePointerInputSource? _pointerSource;

	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerWheelChanged;
	internal event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerCancelled;

	internal void SetPointerInputSource(IUnoCorePointerInputSource source)
	{
		if (_pointerSource is not null)
		{
			return;
		}

		_pointerSource = source;
		_pointerSource.PointerEntered += (_, args) => PointerEntered?.Invoke(this, args);
		_pointerSource.PointerExited += (_, args) => PointerExited?.Invoke(this, args);
		_pointerSource.PointerMoved += (_, args) => PointerMoved?.Invoke(this, args);
		_pointerSource.PointerPressed += (_, args) => PointerPressed?.Invoke(this, args);
		_pointerSource.PointerReleased += (_, args) => PointerReleased?.Invoke(this, args);
		_pointerSource.PointerWheelChanged += (_, args) => PointerWheelChanged?.Invoke(this, args);
		_pointerSource.PointerCancelled += (_, args) => PointerCancelled?.Invoke(this, args);
	}

	internal IUnoCorePointerInputSource? PointersSource => _pointerSource;

	public CoreCursor PointerCursor
	{
		get => _pointerSource?.PointerCursor ?? new CoreCursor(CoreCursorType.Arrow, 0);
		set
		{
			if (_pointerSource is not null)
			{
				_pointerSource.PointerCursor = value;
			}
		}
	}

	public void SetPointerCapture()
		=> _pointerSource?.SetPointerCapture();

	public void ReleasePointerCapture()
		=> _pointerSource?.ReleasePointerCapture();
}
#endif
