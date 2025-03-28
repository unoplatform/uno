using Uno.UI.Xaml.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Input
{
	public partial class HoldingRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly UIElement _originalSource;
		private readonly Point _position;

		public HoldingRoutedEventArgs() { }

		internal HoldingRoutedEventArgs(UIElement originalSource, HoldingEventArgs args)
			: base(originalSource)
		{
			_originalSource = originalSource;
			_position = args.Position;
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
