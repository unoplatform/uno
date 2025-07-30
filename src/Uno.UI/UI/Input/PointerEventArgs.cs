#if HAS_UNO_WINUI
using System.Collections.Generic;
using Uno;
using Windows.System;

namespace Microsoft.UI.Input
{
	public sealed partial class PointerEventArgs
	{
		internal PointerEventArgs(PointerPoint currentPoint)
		{
			CurrentPoint = currentPoint;
		}

		public PointerPoint CurrentPoint { get; }

		public bool Handled
		{
			get;
			set;
		}

		public VirtualKeyModifiers KeyModifiers { get; }

		[NotImplemented]
		public IList<PointerPoint> GetIntermediatePoints()
			=> new List<PointerPoint>(0);

		[NotImplemented]
		public IList<PointerPoint> GetIntermediateTransformedPoints(IPointerPointTransform transform)
			=> new List<PointerPoint>(0);
	}
}
#endif
