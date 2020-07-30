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

		public CompositionClip Clip
		{
			get;
			set;
		}
	}
}
#endif
