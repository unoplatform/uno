using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class TappedRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly Point _position;

		public TappedRoutedEventArgs() { }
		
		internal TappedRoutedEventArgs(object originalSource, TappedEventArgs args)
			: base(originalSource)
		{
			_position = args.Position;
			PointerDeviceType = args.PointerDeviceType;
		}

		public bool Handled { get; set; }

		public Point GetPosition() => _position;

		public PointerDeviceType PointerDeviceType { get; internal set; }

		public Point GetPosition(UIElement relativeTo)
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
