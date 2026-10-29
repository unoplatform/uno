using Windows.Foundation;
using Uno.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class RightTappedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly bool _hasPosition;
		private readonly Point _globalPoint;

		public RightTappedRoutedEventArgs() { }

		/// <param name="originalSource">
		/// The original source of the PointerUp event causing the RightTapped event
		/// (i.e. the top-most element that hit-tests positively).
		/// </param>
		internal RightTappedRoutedEventArgs(UIElement originalSource, RightTappedEventArgs args)
			: base(originalSource)
		{
			_hasPosition = true;
			_globalPoint = args.RawPosition;
			PointerDeviceType = args.PointerDeviceType;
			PointerId = args.PointerId;
		}

		public bool Handled { get; set; }

		public PointerDeviceType PointerDeviceType { get; }

		internal uint PointerId { get; }

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
