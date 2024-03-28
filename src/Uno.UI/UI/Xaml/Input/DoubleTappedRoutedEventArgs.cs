using Windows.Foundation;
using Uno.UI.Xaml.Input;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Input
{
	public sealed partial class DoubleTappedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly UIElement _originalSource;
		private readonly Point _position;

		public DoubleTappedRoutedEventArgs() { }

		internal DoubleTappedRoutedEventArgs(UIElement originalSource, TappedEventArgs args)
			: base(originalSource)
		{
			_originalSource = originalSource;
			PointerDeviceType = args.PointerDeviceType;
			_position = args.Position;
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
