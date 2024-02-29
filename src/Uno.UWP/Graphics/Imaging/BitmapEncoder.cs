using System;
using System.Collections.Generic;

#if __ANDROID__
using Android.Graphics;
#elif __IOS__
using Foundation;
using UIKit;
#elif __MACOS__
using Foundation;
using AppKit;
#elif __SKIA__
using SkiaSharp;
#endif

namespace Windows.Graphics.Imaging;

partial class BitmapEncoder
{
	public static Guid BmpEncoderId { get; }
		= new(0x69be8bb4, 0xd66d, 0x47c8, 0x86, 0x5a, 0xed, 0x15, 0x89, 0x43, 0x37, 0x82);

	public static Guid GifEncoderId { get; }
		= new(0x114f5598, 0x0b22, 0x40a0, 0x86, 0xa1, 0xc8, 0x3e, 0xa4, 0x95, 0xad, 0xbd);

	public static Guid JpegEncoderId { get; }
		= new(0x1a34f5c1, 0x4a5a, 0x46dc, 0xb6, 0x44, 0x1f, 0x45, 0x67, 0xe7, 0xa6, 0x76);

	public static Guid JpegXREncoderId { get; }
		= new(0xac4ce3cb, 0xe1c1, 0x44cd, 0x82, 0x15, 0x5a, 0x16, 0x65, 0x50, 0x9e, 0xc2);

	public static Guid PngEncoderId { get; }
		= new(0x27949969, 0x876a, 0x41d7, 0x94, 0x47, 0x56, 0x8f, 0x6a, 0x35, 0xa4, 0xdc);

	public static Guid HeifEncoderId { get; }
		= new(0x0dbecec1, 0x9eb3, 0x4860, 0x9c, 0x6f, 0xdd, 0xbe, 0x86, 0x63, 0x45, 0x75);

	public static Guid TiffEncoderId { get; }
		= new(0x0131be10, 0x2001, 0x4c5f, 0xa9, 0xb0, 0xcc, 0x88, 0xfa, 0xb6, 0x4c, 0xe8);

	// _encoderMap is defined here to  make sure they are initialized after the encoder ID Guids above.
	// Static field initializers are executed in textual order. When dealing with partial classes, the order is undefined.

#if __ANDROID__
	private static readonly IDictionary<Guid, Bitmap.CompressFormat> _encoderMap =
		new Dictionary<Guid, Bitmap.CompressFormat>()
		{
			{JpegEncoderId, Bitmap.CompressFormat.Jpeg!},
			{PngEncoderId, Bitmap.CompressFormat.Png!},
		};
#elif __IOS__
	private static readonly Dictionary<Guid, Func<UIImage, NSData>> _encoderMap =
		new()
		{
			{JpegEncoderId, AsJPEG},
			{PngEncoderId, AsPNG},
		};
#elif __MACOS__
	private static readonly Dictionary<Guid, Func<NSImage, NSData>> _encoderMap =
		new()
		{
			{JpegEncoderId, AsJPEG},
			{PngEncoderId, AsPNG},
			{GifEncoderId, AsGIF},
			{TiffEncoderId, AsTIFF},
		};
#elif __SKIA__
	private static readonly IDictionary<Guid, SKEncodedImageFormat> _encoderMap =
		new Dictionary<Guid, SKEncodedImageFormat>()
		{
			{BmpEncoderId, SKEncodedImageFormat.Bmp},
			{GifEncoderId, SKEncodedImageFormat.Gif},
			{JpegEncoderId, SKEncodedImageFormat.Jpeg},
			{PngEncoderId, SKEncodedImageFormat.Png},
			{HeifEncoderId, SKEncodedImageFormat.Heif},
		};
#endif
}
