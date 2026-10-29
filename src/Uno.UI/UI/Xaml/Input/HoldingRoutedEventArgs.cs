using Uno.UI.Xaml.Input;
using Windows.Foundation;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class HoldingRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly bool _hasPosition;
		private readonly Point _globalPoint;

		public HoldingRoutedEventArgs() { }

		/// <param name="originalSource">
		/// The original source of the last pointer event before the Holding event, which a timer can also raise
		/// (i.e. the top-most element that hit-tests positively).
		/// </param>
		internal HoldingRoutedEventArgs(UIElement originalSource, HoldingEventArgs args)
			: base(originalSource)
		{
			_hasPosition = true;
			_globalPoint = args.RawPosition;
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
			if (!_hasPosition)
			{
				return default; // Required for the default public ctor ...
			}

			// As on WinUI, the press point is resolved when asked, so the position doesn't follow the element.
			return InputPointEventArgsHelper.ConvertGlobalPointToRelativePoint(_globalPoint, relativeTo);
		}
	}
}
