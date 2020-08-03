using System.Collections.Generic;
using Windows.System;
using Windows.UI.Input;

namespace Windows.UI.Core
{
	public partial class PointerEventArgs : ICoreWindowEventArgs
	{
		internal PointerEventArgs(PointerPoint currentPoint, VirtualKeyModifiers keyModifiers)
		{
			CurrentPoint = currentPoint;
			KeyModifiers = keyModifiers;
		}

		public bool Handled { get; set; }

		public PointerPoint CurrentPoint { get; }

		public VirtualKeyModifiers KeyModifiers { get; }

		public IList<PointerPoint> GetIntermediatePoints()
			=> new List<PointerPoint> {CurrentPoint};
	}
}
