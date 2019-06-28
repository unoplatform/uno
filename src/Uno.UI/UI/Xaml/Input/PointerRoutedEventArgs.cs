using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno;
using Uno.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class PointerRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{

		public IList<PointerPoint> GetIntermediatePoints(UIElement relativeTo)
			=> new List<PointerPoint>(1) {GetCurrentPoint(relativeTo)};

		public bool IsGenerated { get; } = false; // Generated events are not supported by UNO

		public bool Handled { get; set; }

		public VirtualKeyModifiers KeyModifiers { get; }

		public Pointer Pointer { get; }
	}
}
