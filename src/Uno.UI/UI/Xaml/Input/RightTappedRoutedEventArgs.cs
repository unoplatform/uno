using Windows.Foundation;
using Uno.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class RightTappedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly UIElement _originalSource;
		private readonly Point _position;

		public RightTappedRoutedEventArgs() { }

		/// <param name="originalSource">The original source of the PointerUp event causing the Tapped event (i.e. the top-most element that hit-tests positively).</param>
		/// <param name="gestureRecognizerOwner">The element that subscribes to the Tapped event and initiates then propagates the event. This element is the owner of the GestureRecognizer that recognizes this Tap event.</param>
		internal RightTappedRoutedEventArgs(UIElement originalSource, RightTappedEventArgs args, UIElement gestureRecognizerOwner)
			: base(originalSource)
		{
			_originalSource = originalSource;
			// The RightTappedEventArgs position is relative to the GestureRecognizer owner, not the original source of the pointer event.
			_position = gestureRecognizerOwner.GetPosition(args.Position, originalSource);
			PointerDeviceType = args.PointerDeviceType;
			PointerId = args.PointerId;
		}

		public bool Handled { get; set; }

		public PointerDeviceType PointerDeviceType { get; }

		internal uint PointerId { get; }

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
