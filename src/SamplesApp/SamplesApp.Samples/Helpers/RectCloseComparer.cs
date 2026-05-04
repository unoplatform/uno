using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Uno.UI.Samples.UITests.Helpers
{
	internal class RectCloseComparer : IEqualityComparer<Rect>
	{
		private readonly double _epsilon;

		public static RectCloseComparer Default { get; } = new RectCloseComparer(double.Epsilon);

		public static RectCloseComparer UI { get; } = new RectCloseComparer(.5);

		public RectCloseComparer(double epsilon)
		{
			_epsilon = epsilon;
		}

		/// <inheritdoc />
		public bool Equals(Rect left, Rect right)
			=> Math.Abs(left.X - right.X) < _epsilon
			&& Math.Abs(left.Y - right.Y) < _epsilon
			&& Math.Abs(left.Width - right.Width) < _epsilon
			&& Math.Abs(left.Height - right.Height) < _epsilon;

		/// <inheritdoc />
		public int GetHashCode(Rect obj)
			=> ((int)obj.Width)
				^ ((int)obj.Height);
	}
}
