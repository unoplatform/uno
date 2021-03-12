using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Uno;
using Uno.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class PointerRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs, CoreWindow.IPointerEventArgs, IDragEventSource
	{
#if UNO_HAS_MANAGED_POINTERS
		internal const bool PlatformSupportsNativeBubbling = false;
#else
		internal const bool PlatformSupportsNativeBubbling = true;
#endif

		public PointerRoutedEventArgs()
		{
			// This is acceptable as all ctors of this class are internal
			CoreWindow.GetForCurrentThread().LastPointerEvent = this;

			CanBubbleNatively = PlatformSupportsNativeBubbling;
		}

		/// <inheritdoc />
		PointerPoint CoreWindow.IPointerEventArgs.GetLocation(object relativeTo)
			=> GetCurrentPoint(relativeTo as UIElement);

		public IList<PointerPoint> GetIntermediatePoints(UIElement relativeTo)
			=> new List<PointerPoint>(1) {GetCurrentPoint(relativeTo)};

		internal uint FrameId { get; }

		internal bool CanceledByDirectManipulation { get; set; }

		public bool IsGenerated { get; } = false; // Generated events are not supported by UNO

		public bool Handled { get; set; }

		public VirtualKeyModifiers KeyModifiers { get; }

		public Pointer Pointer { get; }

		/// <inheritdoc />
		public override string ToString()
			=> $"PointerRoutedEventArgs({Pointer}@{GetCurrentPoint(null).Position})";

		long IDragEventSource.Id => Pointer.UniqueId;
		uint IDragEventSource.FrameId => FrameId;

		Point IDragEventSource.GetPosition(object relativeTo)
		{
			if (relativeTo is null || relativeTo is UIElement)
			{
				return GetCurrentPoint(relativeTo as UIElement).Position;
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(relativeTo), "The relative element must be a UIElement.");
			}
		}

		(Point location, DragDropModifiers modifier) IDragEventSource.GetState()
		{
			var point = GetCurrentPoint(null);
			var mods = DragDropModifiers.None;

			var props = point.Properties;
			if (props.IsLeftButtonPressed)
			{
				mods |= DragDropModifiers.LeftButton;
			}
			if (props.IsMiddleButtonPressed)
			{
				mods |= DragDropModifiers.MiddleButton;
			}
			if (props.IsRightButtonPressed)
			{
				mods |= DragDropModifiers.RightButton;
			}

			var window = Window.Current.CoreWindow;
			if (window.GetAsyncKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down)
			{
				mods |= DragDropModifiers.Shift;
			}
			if (window.GetAsyncKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down)
			{
				mods |= DragDropModifiers.Control;
			}
			if (window.GetAsyncKeyState(VirtualKey.Menu) == CoreVirtualKeyStates.Down)
			{
				mods |= DragDropModifiers.Alt;
			}

			return (point.Position, mods);
		}
	}
}
