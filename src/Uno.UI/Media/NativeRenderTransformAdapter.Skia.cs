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
		}

		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
			if (isSizeChanged)
			{
				// As we use the 'transform-origin', the transform matrix is independent of the size of the control
			}
			else if (isOriginChanged)
			{
				Owner.Visual.CenterPoint = new Vector3((float)CurrentOrigin.X, (float)CurrentOrigin.Y, 0);
				return;
			}
			else
			{
				Owner.Visual.TransformMatrix = new Matrix4x4(Transform.MatrixCore);
			}
		}

		partial void Cleanup()
		{
			Owner.Visual.TransformMatrix = Matrix4x4.Identity;
		}
	}
}
