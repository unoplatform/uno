#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.UI.Core
{
	public interface ICoreWindowExtension
	{
		public CoreCursor PointerCursor { get; set; }

		void ReleasePointerCapture();

		void SetPointerCapture();
	}

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
			=> _coreWindowExtension?.SetPointerCapture();

		public void ReleasePointerCapture()
			=> _coreWindowExtension?.ReleasePointerCapture();

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
	}

	public interface ICoreWindowEvents
	{
		void RaisePointerEntered(PointerEventArgs args);
		void RaisePointerExited(PointerEventArgs args);
		void RaisePointerMoved(PointerEventArgs args);
		void RaisePointerPressed(PointerEventArgs args);
		void RaisePointerReleased(PointerEventArgs args);
		void RaisePointerWheelChanged(PointerEventArgs args);
		void RaisePointerCancelled(PointerEventArgs args);

		void RaiseKeyUp(KeyEventArgs args);
		void RaiseKeyDown(KeyEventArgs args);
	}
}
#endif
