using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// This class wraps native popups and make them accessible through VisualTreeHelper.GetOpenPopups(Window)
/// </summary>
/// <typeparam name="T"></typeparam>
internal class NativePopupAdapter<T> : IPopup
{
	private readonly T _nativePopup;
	private readonly Action<T> _open;
	private readonly Action<T> _close;

	private bool _isOpen;

	public event EventHandler<object> Opened;
	public event EventHandler<object> Closed;

	public NativePopupAdapter(
		T nativePopup,
		Action<T> open,
		Action<T> close,
		Action<T, EventHandler> opened = null,
		Action<T, EventHandler> closed = null
	)
	{
		_nativePopup = nativePopup;
		_open = open;
		_close = close;

		opened?.Invoke(_nativePopup, (_, __) => { IsOpen = true; });
		closed?.Invoke(_nativePopup, (_, __) => { IsOpen = false; });
	}

	public T NativePopup => _nativePopup;

	public bool IsOpen
	{
		get => _isOpen;
		set
		{
			if (_isOpen == value)
			{
				// Make sure to not raise invalid events
				return;
			}

			_isOpen = value;
			if (value)
			{
				_open(_nativePopup);
				Opened?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				_close(_nativePopup);
				Closed?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public UIElement Child { get; set; }
}
