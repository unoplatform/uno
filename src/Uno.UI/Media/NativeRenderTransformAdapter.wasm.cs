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
				Owner.SetNativeTransform(Transform.MatrixCore);
			}
		}

		partial void Cleanup()
		{
			Owner.ResetStyle("transform");
		}
	}
}
