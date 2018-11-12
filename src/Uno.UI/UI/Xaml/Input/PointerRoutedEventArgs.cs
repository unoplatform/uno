using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Uno.UI.Xaml.Input;
using Windows.System;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class PointerRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly Point _point;

		internal PointerRoutedEventArgs()
		{
			InitializePartial();
		}

		internal PointerRoutedEventArgs(Point point) : this()
		{
			_point = point;
		}

		public Point GetCurrentPoint() => _point;

		[NotImplemented]
		public Point[] GetIntermediatePoints()
		{
			throw new NotImplementedException();
		}

		public bool Handled { get; set; }

		public VirtualKeyModifiers KeyModifiers { get; internal set; }
		public Pointer Pointer { get; internal set; }

		partial void InitializePartial();
	}
}
