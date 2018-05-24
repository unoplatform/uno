using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial class PointerPoint
	{
		public Point Position { get; }

		internal PointerPoint(Point position)
		{
			Position = position;
		}
	}
}
