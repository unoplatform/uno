#nullable enable

using System;

namespace Windows.Graphics.Imaging
{
	partial class BitmapEncoder
	{
#if __NETSTD_REFERENCE__ || __WASM__
		private BitmapEncoder() { }
#endif

		/// <summary>
		/// Encodes a raw pixel buffer to compressed bytes. Assigned once, top-down, at startup by the image-codec
		/// registration in the composition layer (which sits above this assembly and hands its
		/// <c>IImageEncoderDecoder.Encode</c> down as a plain delegate) — so this assembly needs no reference to,
		/// and no reflection into, the codec. Signature: (pixels, width, height, pixelFormat, alphaMode, format, quality) → bytes.
		/// </summary>
		public static Func<byte[], int, int, BitmapPixelFormat, BitmapAlphaMode, BitmapEncoderFormat, int, byte[]>? Encode { get; set; }
	}
}
