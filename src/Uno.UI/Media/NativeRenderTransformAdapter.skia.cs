using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml;

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
			var flowDirectionTransform = Matrix4x4.Identity;

#if !__ANDROID__ && !__IOS__ && !__MACOS__
			UIElement uiElement = Owner;
#else
			if (Owner is UIElement uiElement)
#endif
			{
				flowDirectionTransform = uiElement.GetFlowDirectionTransform();
			}

			FlowDirectionTransform = flowDirectionTransform;

			if (Transform is null)
			{
				Owner.Visual.TransformMatrix = flowDirectionTransform;
			}
			else
			{
				Owner.Visual.TransformMatrix = new Matrix4x4(Transform.ToMatrix(CurrentOrigin, CurrentSize)) * flowDirectionTransform;
			}
		}

		partial void Cleanup()
		{
			FlowDirectionTransform = Matrix4x4.Identity;
			Owner.Visual.TransformMatrix = Matrix4x4.Identity;
		}
	}
}
