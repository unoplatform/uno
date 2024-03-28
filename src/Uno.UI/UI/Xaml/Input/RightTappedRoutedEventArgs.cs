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
	public partial class RightTappedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly UIElement _originalSource;
		private readonly Point _position;

		public RightTappedRoutedEventArgs() { }
		internal RightTappedRoutedEventArgs(UIElement originalSource, RightTappedEventArgs args)
			: base(originalSource)
		{
			_originalSource = originalSource;
			_position = args.Position;
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
