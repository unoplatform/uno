#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using CoreGraphics;

namespace Windows.Foundation
{
    public partial struct Rect
    {
		/// <summary>
		/// Converts this <see cref="Rect"/> to a <see cref="CGRect"/>.
		/// </summary>
		public CGRect ToCGRect()
		{
			return new CGRect(X, Y, Width, Height);
		}

		public static implicit operator Rect(CGRect rect) => new Rect(rect.X, rect.Y, rect.Width, rect.Height);

		public static implicit operator CGRect(Rect rect) => new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
	}
}
