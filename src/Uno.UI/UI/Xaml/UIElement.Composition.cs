using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Windows.UI.Composition;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private ContainerVisual _visual;

		private void InitializeComposition()
		{
			InitializeCompositionPartial();
		}
		partial void InitializeCompositionPartial();

		internal ContainerVisual Visual
		{
			get
			{
				if (_visual == null)
				{
					_visual = UIContext.Compositor.CreateContainerVisual();
#if DEBUG
					_visual.Comment = $"Owner:{this.GetDebugName()}";
#endif
				}

				return _visual;
			}
		}

		internal void SizeVisual(Rect renderRect)
		{
			if (!Uno.CompositionConfiguration.UseVisual)
			{
				return;
			}

			var visual = Visual;
			var roundedRect = LayoutRound(renderRect);

			visual.Offset = new Vector3((float)roundedRect.X, (float)roundedRect.Y, 0);
			visual.Size = new Vector2((float)roundedRect.Width, (float)roundedRect.Height);
			visual.CenterPoint = new Vector3((float)RenderTransformOrigin.X, (float)RenderTransformOrigin.Y, 0);
			SizeVisualPartial(renderRect);
		}
		partial void SizeVisualPartial(Rect renderRect);

		private void ClipVisual(Rect clip)
		{
			if (!Uno.CompositionConfiguration.UseVisual)
			{
				return;
			}

#if __SKIA__
			if (ClippingIsSetByCornerRadius)
			{
				return; // already applied
			}
#endif

			if (clip.IsEmpty)
			{
				Visual.Clip = null;
				ClipVisualPartial(null);
			}
			else
			{
				var roundedClip = LayoutRound(clip);
				var visualClip = Visual.Clip is InsetClip insetClip
					? insetClip
					: (InsetClip)(Visual.Clip = Visual.Compositor.CreateInsetClip());

				visualClip.TopInset = (float)roundedClip.Top;
				visualClip.LeftInset = (float)roundedClip.Left;
				visualClip.BottomInset = (float)roundedClip.Bottom;
				visualClip.RightInset = (float)roundedClip.Right;
				ClipVisualPartial(roundedClip);
			}
		}
		partial void ClipVisualPartial(Rect? renderRect);

		internal void InvalidateRender()
		{
			//if (!Uno.CompositionConfiguration.UseVisual)
			//{
			//	return;
			//}

			//UIContext.Compositor.InvalidateRender();
		}
	}
}
