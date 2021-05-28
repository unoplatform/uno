#nullable enable

#if !__IOS__
using System.Numerics;
using System;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		internal virtual void Render(SKSurface surface, SKImageInfo info)
		{

		}

		public CompositionClip? Clip
		{
			get;
			set;
		}

		// Backing for scroll offsets
		private Vector2 _anchorPoint = Vector2.Zero;
		public Vector2 AnchorPoint
		{
			get => _anchorPoint;
			set
			{
				_anchorPoint = value;
				Compositor.InvalidateRender();
			}
		}
	}
}
#endif
