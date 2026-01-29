using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Uno.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

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
			FlowDirectionTransform = Owner.GetFlowDirectionTransform();

			// Get base 2D transform (RenderTransform + FlowDirection)
			Matrix3x2 transform2D;
			if (Transform is null)
			{
				transform2D = FlowDirectionTransform;
			}
			else
			{
				transform2D = Transform.ToMatrix(CurrentOrigin, CurrentSize) * FlowDirectionTransform;
			}

			// Convert to 4x4 matrix
			var finalMatrix = new Matrix4x4(transform2D);

			// Apply projection if set
			if (Owner is UIElement element && element.GetProjection() is Projection projection)
			{
				var projectionMatrix = projection.GetProjectionMatrix(CurrentSize);
				// Projection is applied after RenderTransform
				finalMatrix = finalMatrix * projectionMatrix;
			}

			Owner.Visual.TransformMatrix = finalMatrix;
		}

		partial void Cleanup()
		{
			FlowDirectionTransform = Matrix3x2.Identity;
			Owner.Visual.TransformMatrix = Matrix4x4.Identity;
		}
	}
}
