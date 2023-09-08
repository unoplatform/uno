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
			// Apply the transform as soon as its been declared
			Update();

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
				FlowDirectionTransform = Owner.GetFlowDirectionTransform();

				if (Transform is null)
				{
					Owner.SetNativeTransform(FlowDirectionTransform);
				}
				else
				{
					Owner.SetNativeTransform(Transform.MatrixCore * FlowDirectionTransform);
				}
			}
		}

		partial void Cleanup()
		{
			FlowDirectionTransform = Matrix3x2.Identity;
			Owner.ResetStyle("transform");
		}
	}
}
