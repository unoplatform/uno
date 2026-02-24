using Uno.UI.Xaml.Input;
using Windows.Foundation;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class HoldingRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly UIElement _originalSource;
		private readonly Point _position;

		public HoldingRoutedEventArgs() { }

		/// <param name="originalSource">The original source of the PointerUp event causing the Tapped event (i.e. the top-most element that hit-tests positively).</param>
		/// <param name="gestureRecognizerOwner">The element that subscribes to the Tapped event and initiates then propagates the event. This element is the owner of the GestureRecognizer that recognizes this Tap event.</param>
		internal HoldingRoutedEventArgs(UIElement originalSource, HoldingEventArgs args, UIElement gestureRecognizerOwner)
			: base(originalSource)
		{
			_originalSource = originalSource;
			// The HoldingEventArgs position is relative to the GestureRecognizer owner, not the original source of the pointer event.
			_position = gestureRecognizerOwner.GetPosition(args.Position, originalSource);
			PointerId = args.PointerId;
			PointerDeviceType = args.PointerDeviceType;
			HoldingState = args.HoldingState;
		}

		/// <summary>
		/// Used internally to prevent multiple holding events due to routed event bubbling for the same pointer
		/// </summary>
		internal uint PointerId { get; }

		public bool Handled { get; set; }

		bool IHandleableRoutedEventArgs.Handled
		{
			get => Handled;
			set => Handled = value;
		}

		public PointerDeviceType PointerDeviceType { get; }

		public HoldingState HoldingState { get; }

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
