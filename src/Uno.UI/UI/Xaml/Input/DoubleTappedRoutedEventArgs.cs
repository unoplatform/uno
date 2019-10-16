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
		private readonly Point _position;

		public DoubleTappedRoutedEventArgs() { }

		internal DoubleTappedRoutedEventArgs(object originalSource, TappedEventArgs args)
			: base(originalSource)
		{
			PointerDeviceType = args.PointerDeviceType;
			_position = args.Position;
		}

		public Point GetPosition()
			=> _position;

		public bool Handled { get; set; }

		public PointerDeviceType PointerDeviceType { get; internal set; }

		public Point GetPosition(UIElement relativeTo)
		{
			if (relativeTo == null)
			{
				return _position;
			}
			else
			{
				return relativeTo.GetPosition(_position, relativeTo);
			}
		}
	}
}
