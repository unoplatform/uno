using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class DoubleTappedRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
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
		}

		public bool Handled { get; set; }

		public PointerDeviceType PointerDeviceType { get; }

		public Point GetPosition(UIElement relativeTo)
		{
			if (_originalSource == null)
			{
				return default; // Required for the default public ctor ...
			}
			else if(relativeTo == _originalSource)
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
