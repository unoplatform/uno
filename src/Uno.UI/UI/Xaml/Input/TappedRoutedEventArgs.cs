using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class TappedRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly Point _position;

		public TappedRoutedEventArgs() 
			: this(new Point())
		{
		}

		internal TappedRoutedEventArgs(Point position)
		{
			_position = position;
		}

		public bool Handled { get; set; }

		public Point GetPosition() => _position;

		public PointerDeviceType PointerDeviceType { get; internal set; }

		public global::Windows.Foundation.Point GetPosition(global::Windows.UI.Xaml.UIElement relativeTo)
		{
			if(relativeTo == null)
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
