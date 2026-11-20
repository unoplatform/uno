using Windows.Foundation;
using Uno.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public sealed partial class TappedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly bool _hasPosition;
		private readonly Point _globalPoint;

		public TappedRoutedEventArgs() { }

		/// <param name="originalSource">The original source of the PointerUp event causing the Tapped event (i.e. the top-most element that hit-tests positively).</param>
		internal TappedRoutedEventArgs(UIElement originalSource, TappedEventArgs args)
			: base(originalSource)
		{
			_hasPosition = true;
			_globalPoint = args.RawPosition;
			PointerDeviceType = (PointerDeviceType)args.PointerDeviceType;
			PointerId = args.PointerId;
		}

		internal uint PointerId { get; }

		public bool Handled { get; set; }

		public PointerDeviceType PointerDeviceType { get; }

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
