#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IRenderData"/> holding a native <c>SKPicture</c> handle.</summary>
internal sealed class SkiaRenderData : IRenderData
{
	public SkiaRenderData(IntPtr picture) => Picture = picture;

	/// <summary>The native <c>SKPicture</c> handle (may be <see cref="IntPtr.Zero"/> if nothing was recorded).</summary>
	public IntPtr Picture { get; private set; }

	public void Dispose()
	{
		if (Picture != IntPtr.Zero)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(Picture);
			Picture = IntPtr.Zero;
		}
	}
}
