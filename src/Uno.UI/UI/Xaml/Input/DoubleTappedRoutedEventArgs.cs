using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Windows.Foundation;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class DoubleTappedRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly Point _position;

		public DoubleTappedRoutedEventArgs() 
			: this(new Point())
		{
		}

		internal DoubleTappedRoutedEventArgs(Point position)
		{
			_position = position;
		}

		public Point GetPosition()
		{
			return _position;
		}

		public bool Handled { get; set; }

		public PointerDeviceType PointerDeviceType { get; internal set; }

		public global::Windows.Foundation.Point GetPosition(global::Windows.UI.Xaml.UIElement relativeTo)
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
