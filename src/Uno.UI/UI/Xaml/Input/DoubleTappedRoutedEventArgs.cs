using Windows.Foundation;
using Uno.UI.Xaml.Input;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Input
{
	public sealed partial class DoubleTappedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly UIElement _originalSource;
		private readonly Point _position;

		public DoubleTappedRoutedEventArgs() { }

		/// <param name="originalSource">The original source of the PointerUp event causing the Tapped event (i.e. the top-most element that hit-tests positively).</param>
		/// <param name="gestureRecognizerOwner">The element that subscribes to the Tapped event and initiates then propagates the event. This element is the owner of the GestureRecognizer that recognizes this Tap event.</param>
		internal DoubleTappedRoutedEventArgs(UIElement originalSource, TappedEventArgs args, UIElement gestureRecognizerOwner)
			: base(originalSource)
		{
			_originalSource = originalSource;
			PointerDeviceType = args.PointerDeviceType;
			// The TappedEventArgs position is relative to the GestureRecognizer owner, not the original source of the pointer event.
			_position = gestureRecognizerOwner.GetPosition(args.Position, originalSource);
			PointerId = args.PointerId;
		}

		internal uint PointerId { get; }

		public bool Handled { get; set; }

		public PointerDeviceType PointerDeviceType { get; }

		public Point GetPosition(UIElement relativeTo)
		{
			if (_originalSource == null)
			{
				return default; // Required for the default public ctor ...
			}
			else if (relativeTo == _originalSource)
			{
				return _position;
			}
			else
			{
				return _originalSource.GetPosition(_position, relativeTo);
			}
		}
	}
}
