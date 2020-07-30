using System;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.UI.Core
{
	public interface ICoreWindowExtension
	{

	}

	public partial class CoreWindow : ICoreWindowEvents
	{
		private ICoreWindowExtension _coreWindowExtension;

		public event TypedEventHandler<CoreWindow, PointerEventArgs> PointerEntered;
		public event TypedEventHandler<CoreWindow, PointerEventArgs> PointerExited;
		public event TypedEventHandler<CoreWindow, PointerEventArgs> PointerMoved;
		public event TypedEventHandler<CoreWindow, PointerEventArgs> PointerPressed;
		public event TypedEventHandler<CoreWindow, PointerEventArgs> PointerReleased;
		public event TypedEventHandler<CoreWindow, PointerEventArgs> PointerWheelChanged;

		partial void InitializePartial()
		{
			if(!ApiExtensibility.CreateInstance(this, out _coreWindowExtension))
			{
				throw new InvalidOperationException($"Unable to find ICoreWindowExtension extension");
			}
		}

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
	}

	public interface ICoreWindowEvents
	{
		void RaisePointerEntered(PointerEventArgs args);
		void RaisePointerExited(PointerEventArgs args);
		void RaisePointerMoved(PointerEventArgs args);
		void RaisePointerPressed(PointerEventArgs args);
		void RaisePointerReleased(PointerEventArgs args);
		void RaisePointerWheelChanged(PointerEventArgs args);
	}
}
