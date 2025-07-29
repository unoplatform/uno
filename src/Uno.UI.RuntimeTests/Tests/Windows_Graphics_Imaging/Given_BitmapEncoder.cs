using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Graphics.Imaging;

namespace Uno.UI.RuntimeTests.Tests.Windows_Graphics_Imaging
{
	[TestClass]
	public class Given_BitmapEncoder
	{
		[TestMethod]
		[DynamicData(nameof(GetEncoders))]
		public async Task When_CreateAsync_With(Guid encoderId, bool notImplementedException)
		{
			Exception excption = default;
			BitmapEncoder encoder = default;
			try
			{
				global::Windows.Storage.Streams.IRandomAccessStream stream = default;
				encoder = await BitmapEncoder.CreateAsync(encoderId, stream);
			}
			catch (Exception ex)
			{
				excption = ex;
			}
			if (notImplementedException)
			{
				Assert.IsNotNull(excption);
				Assert.IsNull(encoder);
				Assert.IsInstanceOfType(excption, typeof(NotImplementedException));
			}
			else
			{
				Assert.IsNull(excption);
				Assert.IsNotNull(encoder);
			}
		}

		public static IEnumerable<object[]> GetEncoders()
		{
#if WINAPPSDK
			yield return new object[] { BitmapEncoder.BmpEncoderId, false };
			yield return new object[] { BitmapEncoder.GifEncoderId, false };
			yield return new object[] { BitmapEncoder.HeifEncoderId, false };
			yield return new object[] { BitmapEncoder.JpegEncoderId, false };
			yield return new object[] { BitmapEncoder.JpegXREncoderId, false };
			yield return new object[] { BitmapEncoder.PngEncoderId, false };
			yield return new object[] { BitmapEncoder.TiffEncoderId, false };
#else
			if (OperatingSystem.IsAndroid())
			{
				yield return new object[] { BitmapEncoder.BmpEncoderId, true };
				yield return new object[] { BitmapEncoder.GifEncoderId, true };
				yield return new object[] { BitmapEncoder.HeifEncoderId, true };
				yield return new object[] { BitmapEncoder.JpegEncoderId, false };
				yield return new object[] { BitmapEncoder.JpegXREncoderId, true };
				yield return new object[] { BitmapEncoder.PngEncoderId, false };
				yield return new object[] { BitmapEncoder.TiffEncoderId, true };
			}
			else if (OperatingSystem.IsIOS())
			{
				yield return new object[] { BitmapEncoder.BmpEncoderId, true };
				yield return new object[] { BitmapEncoder.GifEncoderId, true };
				yield return new object[] { BitmapEncoder.HeifEncoderId, true };
				yield return new object[] { BitmapEncoder.JpegEncoderId, false };
				yield return new object[] { BitmapEncoder.JpegXREncoderId, true };
				yield return new object[] { BitmapEncoder.PngEncoderId, false };
				yield return new object[] { BitmapEncoder.TiffEncoderId, true };
			}
#if !__SKIA__
			else if (OperatingSystem.IsMacOS())
			{
				yield return new object[] { BitmapEncoder.BmpEncoderId, true };
				yield return new object[] { BitmapEncoder.GifEncoderId, false };
				yield return new object[] { BitmapEncoder.HeifEncoderId, true };
				yield return new object[] { BitmapEncoder.JpegEncoderId, false };
				yield return new object[] { BitmapEncoder.JpegXREncoderId, true };
				yield return new object[] { BitmapEncoder.PngEncoderId, false };
				yield return new object[] { BitmapEncoder.TiffEncoderId, false };
			}
#endif
			else if (OperatingSystem.IsBrowser())
			{
				yield return new object[] { BitmapEncoder.BmpEncoderId, true };
				yield return new object[] { BitmapEncoder.GifEncoderId, true };
				yield return new object[] { BitmapEncoder.HeifEncoderId, true };
				yield return new object[] { BitmapEncoder.JpegEncoderId, true };
				yield return new object[] { BitmapEncoder.JpegXREncoderId, true };
				yield return new object[] { BitmapEncoder.PngEncoderId, true };
				yield return new object[] { BitmapEncoder.TiffEncoderId, true };
			}
			else
			{
				yield return new object[] { BitmapEncoder.BmpEncoderId, false };
				yield return new object[] { BitmapEncoder.GifEncoderId, false };
				yield return new object[] { BitmapEncoder.HeifEncoderId, false };
				yield return new object[] { BitmapEncoder.JpegEncoderId, false };
				yield return new object[] { BitmapEncoder.JpegXREncoderId, true };
				yield return new object[] { BitmapEncoder.PngEncoderId, false };
				yield return new object[] { BitmapEncoder.TiffEncoderId, true };
			}
#endif
		}
	}
}
