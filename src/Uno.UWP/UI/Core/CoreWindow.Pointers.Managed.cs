#if UNO_HAS_MANAGED_POINTERS
using System;
using Windows.Devices.Input;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Windows.UI.Input;

namespace Windows.UI.Core;

public partial class CoreWindow : ICoreWindowEvents
{
	private ICoreWindowExtension? _coreWindowExtension;

	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<CoreWindow, PointerEventArgs>? PointerCancelled;
	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyUp;

	public CoreCursor PointerCursor
	{
		get => _coreWindowExtension?.PointerCursor ?? new CoreCursor(CoreCursorType.Arrow, 0);
		set
		{
			if (_coreWindowExtension != null)
			{
				_coreWindowExtension.PointerCursor = value;
			}
		}
	}

	partial void InitializePartial()
	{
		if (!ApiExtensibility.CreateInstance(this, out _coreWindowExtension))
		{
			throw new InvalidOperationException($"Unable to find ICoreWindowExtension extension");
		}
	}

	public void SetPointerCapture()
		=> _coreWindowExtension?.SetPointerCapture(LastPointerEvent?.Pointer ?? default);
	internal void SetPointerCapture(PointerIdentifier pointer)
		=> _coreWindowExtension?.SetPointerCapture(pointer);

	public void ReleasePointerCapture()
		=> _coreWindowExtension?.ReleasePointerCapture(LastPointerEvent?.Pointer ?? default);
	internal void ReleasePointerCapture(PointerIdentifier pointer)
		=> _coreWindowExtension?.ReleasePointerCapture(pointer);

	void ICoreWindowEvents.RaisePointerEntered(PointerEventArgs args)
		=> PointerEntered?.Invoke(this, args);

	void ICoreWindowEvents.RaisePointerExited(PointerEventArgs args)
		=> PointerExited?.Invoke(this, args);

	void ICoreWindowEvents.RaisePointerMoved(PointerEventArgs args)
		=> PointerMoved?.Invoke(this, args);

	void ICoreWindowEvents.RaisePointerPressed(PointerEventArgs args)
		=> PointerPressed?.Invoke(this, args);

	void ICoreWindowEvents.RaisePointerReleased(PointerEventArgs args)
		=> PointerReleased?.Invoke(this, args);

	void ICoreWindowEvents.RaisePointerWheelChanged(PointerEventArgs args)
		=> PointerWheelChanged?.Invoke(this, args);

	public void RaisePointerCancelled(PointerEventArgs args)
		=> PointerCancelled?.Invoke(this, args);

	void ICoreWindowEvents.RaiseKeyUp(KeyEventArgs args)
		=> KeyUp?.Invoke(this, args);

	void ICoreWindowEvents.RaiseKeyDown(KeyEventArgs args)
		=> KeyDown?.Invoke(this, args);

	internal void InjectPointerAdded(PointerEventArgs args)
		=> PointerEntered?.Invoke(this, args);

	internal void InjectPointerRemoved(PointerEventArgs args)
		=> PointerExited?.Invoke(this, args);

	internal void InjectPointerUpdated(PointerEventArgs args)
	{
		var kind = args.CurrentPoint.Properties.PointerUpdateKind;

		if (args.CurrentPoint.Properties.IsCanceled)
		{
			PointerCancelled?.Invoke(this, args);
		}
		else if (args.CurrentPoint.Properties.MouseWheelDelta is not 0)
		{
			PointerWheelChanged?.Invoke(this, args);
		}
		else if (kind is PointerUpdateKind.Other)
		{
			PointerMoved?.Invoke(this, args);
		}
		else if (((int)kind & 1) == 1)
		{
			PointerPressed?.Invoke(this, args);
		}
		else
		{
			PointerReleased?.Invoke(this, args);
		}
	}
}
#endif
