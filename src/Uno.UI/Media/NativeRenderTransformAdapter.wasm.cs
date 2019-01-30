using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Uno.Extensions;

namespace Uno.UI.Media
{
	partial class NativeRenderTransformAdapter
	{
		partial void Initialized()
		{
			// On WASM Transform are applied by default on the center on the view
			// so make sure to reset it when the transform is attached to the view
			Apply(isSizeChanged: false, isOriginChanged: true);
		}

		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
			if (isSizeChanged)
			{
				// As we use the 'transform-origin', the transform matrix is independent of the size of the control
			}
			else if (isOriginChanged)
			{
				// Note: On WASM Transform are applied by default on the center on the view

				FormattableString nativeOrigin = $"{(int)(CurrentOrigin.X * 100)}% {(int)(CurrentOrigin.Y * 100)}%";
				Owner.SetStyle("transform-origin", nativeOrigin.ToStringInvariant());

				return;
			}
			else
			{
				var matrix = Transform.MatrixCore;
				FormattableString native = $"matrix({matrix.M11},{matrix.M12},{matrix.M21},{matrix.M22},{matrix.M31},{matrix.M32})";

				Owner.SetStyle("transform", native.ToStringInvariant());
			}
		}

		partial void Cleanup()
		{
			Owner.ResetStyle("transform");
		}
	}
}
