using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Uno.Extensions;
using Uno.Extensions.ValueType;

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
			Owner.Visual.TransformMatrix = new Matrix4x4(Transform.ToMatrix(new Windows.Foundation.Point(CurrentOrigin.X * CurrentSize.Width, CurrentOrigin.Y * CurrentSize.Height)));
		}

		partial void Cleanup()
		{
			Owner.Visual.TransformMatrix = Matrix4x4.Identity;
		}
	}
}
