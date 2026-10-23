using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices.WindowsRuntime;
using Private.Infrastructure;
using Windows.Storage;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Windows.Storage.Streams;
using System.Diagnostics;
#if __SKIA__
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;
using IImageEncoderDecoder = Uno.UI.Composition.Drawing.IImageEncoderDecoder;
using ImageEncoderDecoder = Uno.UI.Composition.Drawing.ImageEncoderDecoder;
#endif
#if UNO_DRAWING_SKIA
using SkiaSharp;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_BitmapSource
	{
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_SetSource_Then_StreamClonedSynchronously()
		{
			var sut = new BitmapImage();
			var stream = new Given_BitmapSource_Stream();
			var raStream = stream.AsRandomAccessStream();

			var success = false;
			try
			{
				sut.SetSource(raStream);
			}
			catch (Given_BitmapSource_Exception ex) when (ex.Caller is nameof(Given_BitmapSource_Stream.Read))
			{
				success = true;
			}

			Assert.IsTrue(success);
		}

		[TestMethod]
		public async Task When_SetSourceAsync_Then_StreamClonedSynchronously()
		{
			var sut = new BitmapImage();
			var stream = new Given_BitmapSource_Stream();
			var raStream = stream.AsRandomAccessStream();

			var success = false;
			try
			{
				await sut.SetSourceAsync(raStream);
			}
			catch (Given_BitmapSource_Exception ex) when (ex.Caller is nameof(Given_BitmapSource_Stream.Read))
			{
				success = true;
			}

			Assert.IsTrue(success);
		}

#if __SKIA__
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaTvOS)]
		public async Task When_LocalResource_ScaleQualifier()
		{
			var path = new Uri("ms-appx:///Assets/scale-test.png");
			var resolved = await BitmapImage.TryResolveLocalResource(path, scaleOverride: Windows.Graphics.Display.ResolutionScale.Scale400Percent);

			Assert.Contains("scale-400", resolved.PathAndQuery, $"Resolved asset path did not contain the expected .scale-400 qualifier: {resolved}");
		}
#endif

#if __SKIA__ // Not yet supported on the other platforms (https://github.com/unoplatform/uno/issues/8909)
		[TestMethod]
		// The ms-appdata load never completes on Skia Linux, and the awaited TCS has no timeout, so the job hangs to its limit (#23967).
		// Android goes silent the same way: first seen under NativeAOT (#24271), then on plain Android Skia
		// once #24003 fixed the shard partition and the test actually started running there.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaFrameBuffer | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaAndroid)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23967")]
		// Backstop for the storage calls below, which the inner WaitAsync does not cover.
		[Timeout(60000)]
		public async Task When_MsAppData()
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ingredient3.png"));
			await file.CopyAsync(ApplicationData.Current.LocalFolder, "ingredient3.png", NameCollisionOption.ReplaceExisting);

			var image = new Image();
			WindowHelper.WindowContent = image;

			var sut = new BitmapImage();
			sut.UriSource = new Uri("ms-appdata:///local/ingredient3.png");

			image.Source = sut;

			TaskCompletionSource<bool> tcs = new();
			sut.ImageOpened += (s, e) => tcs.TrySetResult(true);
			sut.ImageFailed += (s, e) => tcs.TrySetException(new Exception($"Image failed to load: {e.ErrorMessage}"));

			// Neither event fires on some Skia legs (#23967). Without a timeout the bare await
			// hangs until the 60-minute job cap, which loses the results file for the whole suite.
			await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
		}
#endif

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_BundleImages_Loaded_StreamsAreDisposed()
		{
			var panel = new StackPanel { Orientation = Orientation.Horizontal };
			WindowHelper.WindowContent = panel;

			var imageUris = new[]
			{
				"ms-appx:///Assets/ingredient3.png",
				"ms-appx:///Assets/BlueSquare.png",
				"ms-appx:///Assets/StoreLogo.png"
			};

			var loadTasks = new List<Task>();
			var images = new List<Image>();
			var bitmapImages = new List<BitmapImage>();

			foreach (var uri in imageUris)
			{
				var image = new Image { Width = 50, Height = 50 };
				var bitmapImage = new BitmapImage();

				var tcs = new TaskCompletionSource<bool>();
				bitmapImage.ImageOpened += (s, e) => tcs.TrySetResult(true);
				bitmapImage.ImageFailed += (s, e) => tcs.TrySetException(new Exception($"Failed to load {uri}: {e.ErrorMessage}"));

				bitmapImage.UriSource = new Uri(uri);
				image.Source = bitmapImage;
				panel.Children.Add(image);

				images.Add(image);
				bitmapImages.Add(bitmapImage);
				loadTasks.Add(tcs.Task);
			}

			await Task.WhenAll(loadTasks);

			foreach (var bitmapImage in bitmapImages)
			{
				Assert.IsGreaterThan(0, bitmapImage.PixelWidth, "Image should have valid pixel dimensions after loading");
			}

			foreach (var image in images)
			{
				image.Source = null;
			}
			foreach (var bitmapImage in bitmapImages)
			{
				bitmapImage.UriSource = null;
			}
			panel.Children.Clear();

			await WindowHelper.WaitForIdle();
			await Task.Delay(100);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Assert.HasCount(imageUris.Length, images, "All images were processed");
		}

		[TestMethod]
		// Same Skia Linux hang as When_MsAppData (#23967): never reached in CI, but it awaits the same unbounded TCS.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaFrameBuffer | RuntimeTestPlatforms.SkiaX11)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23967")]
		public async Task When_MsAppData_StreamDisposed_FileNotLocked()
		{
			var fileName = $"test_stream_disposed_{Guid.NewGuid()}.png";

			var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ingredient3.png"));
			var copiedFile = await sourceFile.CopyAsync(ApplicationData.Current.LocalFolder, fileName, NameCollisionOption.ReplaceExisting);
			var filePath = copiedFile.Path;

			try
			{
				var image = new Image();
				WindowHelper.WindowContent = image;

				var bitmapImage = new BitmapImage();

				TaskCompletionSource<bool> imageOpenedTcs = new();
				bitmapImage.ImageOpened += (s, e) => imageOpenedTcs.TrySetResult(true);
				bitmapImage.ImageFailed += (s, e) => imageOpenedTcs.TrySetException(new Exception($"Image failed to load: {e.ErrorMessage}"));

				bitmapImage.UriSource = new Uri($"ms-appdata:///local/{fileName}");
				image.Source = bitmapImage;

				await imageOpenedTcs.Task;

				await Task.Delay(100);

				image.Source = null;
				bitmapImage.UriSource = null;

				await WindowHelper.WaitForIdle();
				await Task.Delay(100);

				Exception lockException = null;
				try
				{
					using var exclusiveStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
					exclusiveStream.WriteByte(0);
				}
				catch (IOException ex)
				{
					lockException = ex;
				}

				Assert.IsNull(lockException, $"File is still locked after image load completed. This indicates the stream was not properly disposed. Exception: {lockException?.Message}");
			}
			finally
			{
				try
				{
					File.Delete(filePath);
				}
				catch (Exception deleteEx)
				{
					// Best-effort cleanup: log and ignore failures when deleting the temporary test file.
					Debug.WriteLine($"Failed to delete temporary test file '{filePath}': {deleteEx}");
				}
			}
		}

#if !WINAPPSDK
		[TestMethod]
		public void When_SetSource_Stream_Then_StreamClonedSynchronously()
		{
			var sut = new BitmapImage();
			var stream = new Given_BitmapSource_Stream();

			var success = false;
			try
			{
				sut.SetSource(stream);
			}
			catch (Given_BitmapSource_Exception ex) when (ex.Caller is nameof(Given_BitmapSource_Stream.Read))
			{
				success = true;
			}

			Assert.IsTrue(success);
		}

		[TestMethod]
		public void When_SetSourceAsync_Stream_Then_StreamClonedSynchronously()
		{
			var sut = new BitmapImage();
			var stream = new Given_BitmapSource_Stream();

			var success = false;
			try
			{
				sut.SetSourceAsync(stream); // Note: We do not await the task here. It has to fail within the method itself!
			}
			catch (Given_BitmapSource_Exception ex) when (ex.Caller is nameof(Given_BitmapSource_Stream.Read))
			{
				success = true;
			}

			Assert.IsTrue(success);
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_SetSource_Cloned_InMemoryRandomAccessStream()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ingredient3.png"));
			using var stream = await file.OpenStream(CancellationToken.None, FileAccessMode.Read, StorageOpenOptions.AllowOnlyReaders);

			var byteArray = new byte[stream.Length];
			await stream.ReadExactlyAsync(byteArray, 0, byteArray.Length);

			var stream2 = new InMemoryRandomAccessStream();
			await stream2.WriteAsync(byteArray.AsBuffer());
			stream2.Seek(0);

			var success = true;
			try
			{
				var source = new BitmapImage();
				source.SetSource(stream2);
				var image = new Image();
				await UITestHelper.Load(image, image1 => image1.IsLoaded);
				image.Source = source;
			}
			catch (Exception)
			{
				success = false;
			}

			Assert.IsTrue(success);
		}
#endif

#if __SKIA__
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		[DataRow("Skia", "Formats/uno-overalls.jpg", 800, 600)]
		[DataRow("Skia", "Formats/uno-overalls.png", 800, 600)]
		[DataRow("Skia", "testimage_exif_rotated_different_dimensions.jpg", 400, 250)] // Stored 250x400, EXIF orientation 6.
		[DataRow("Skia", "Formats/animated.gif", 480, 480)]
		[DataRow("Skia", "Formats/uno-overalls.webp", 800, 600)]
		[DataRow("Skia", "Formats/uno-overalls.bmp", 800, 600)]
		public async Task When_SetSource_Then_PixelSize_Is_Read_With_TryGetSize(string codecName, string asset, int width, int height)
		{
			var bytes = await ReadAssetAsync(asset);
			var codec = new SizeReadingCountingImageCodec(CreateCodec(codecName), bytes.Length);

			var sut = SetSource(codec, bytes);

			Assert.AreEqual(width, sut.PixelWidth);
			Assert.AreEqual(height, sut.PixelHeight);
			Assert.AreEqual(0, codec.Decodes, "SetSource should read the size with TryGetSize, not TryDecode.");
		}

#if UNO_DRAWING_SKIA
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		[DataRow("LargeWisteria.jpg")]
		[DataRow("test_image_1252_836.png")]
		[DataRow("Formats/uno-overalls.bmp")]
		[DataRow("Formats/uno-overalls.gif")]
		public async Task When_SetSource_Then_Skia_Reads_Only_The_Header(string asset)
		{
			var bytes = await ReadAssetAsync(asset);
			var codec = new SizeReadingCountingImageCodec(CreateCodec("Skia"), bytes.Length);

			var sut = SetSource(codec, bytes);

			Assert.IsGreaterThan(0, sut.PixelWidth);
			Assert.IsLessThan((long)bytes.Length / 4, codec.SizeBytesRead, $"Reading the size should read the header, not the {bytes.Length} bytes of the file.");
		}
#endif

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		[DataRow("Skia", 1)]
		[DataRow("Skia", 2)]
		[DataRow("Skia", 3)]
		[DataRow("Skia", 4)]
		[DataRow("Skia", 5)]
		[DataRow("Skia", 6)]
		[DataRow("Skia", 7)]
		[DataRow("Skia", 8)]
		public void When_SetSource_Exif_Oriented_Then_PixelSize_Matches_Decode(string codecName, int orientation)
		{
			var inner = CreateCodec(codecName);
			var bytes = CreateOrientedJpeg(inner, orientation);

			var sut = SetSource(new SizeReadingCountingImageCodec(inner, bytes.Length), bytes);

			// Orientations 5 to 8 turn the 3x2 image by a quarter.
			var (width, height) = orientation >= 5 ? (2, 3) : (3, 2);
			Assert.AreEqual(width, sut.PixelWidth);
			Assert.AreEqual(height, sut.PixelHeight);
			Assert.IsTrue(inner.TryDecode(new MemoryStream(bytes), null, null, out var frames));
			using (frames)
			{
				Assert.AreEqual(width, frames.Frames[0].PixelWidth, "The decode should agree with the size read.");
				Assert.AreEqual(height, frames.Frames[0].PixelHeight, "The decode should agree with the size read.");
			}
		}

#if UNO_DRAWING_SKIA
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		[DataRow("Skia", "VP8L", 3, 2)]
		[DataRow("Skia", "VP8", 3, 2)]
		[DataRow("Skia", "VP8X", 3, 2)]
		public void When_SetSource_Webp_Then_PixelSize_Matches_Decode(string codecName, string container, int width, int height)
		{
			var inner = CreateCodec(codecName);
			var lossless = container == "VP8L";
			using var bitmap = new SKBitmap(new SKImageInfo(3, 2, SKColorType.Bgra8888, SKAlphaType.Premul));
			bitmap.Erase(container == "VP8X" ? SKColors.Red.WithAlpha(0x80) : SKColors.Red);
			using var pixmap = bitmap.PeekPixels();
			using var data = pixmap.Encode(new SKWebpEncoderOptions(lossless ? SKWebpEncoderCompression.Lossless : SKWebpEncoderCompression.Lossy, 90));
			var bytes = data.ToArray();
			Assert.AreEqual(container, Encoding.ASCII.GetString(bytes, 12, 4).TrimEnd());
			var codec = new SizeReadingCountingImageCodec(inner, bytes.Length);

			var sut = SetSource(codec, bytes);

			Assert.AreEqual(width, sut.PixelWidth);
			Assert.AreEqual(height, sut.PixelHeight);
			Assert.AreEqual(0, codec.Decodes);
			var decoded = inner.TryDecode(new MemoryStream(bytes), null, null, out var frames);
			using (frames)
			{
				// A width of 0 is a row for a file the codec declines to decode.
				Assert.AreEqual(width != 0, decoded, "The size should be read for the images the codec decodes.");
				Assert.AreEqual(width, decoded ? frames.Frames[0].PixelWidth : 0);
				Assert.AreEqual(height, decoded ? frames.Frames[0].PixelHeight : 0);
			}
		}
#endif

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		[DataRow("Skia", "InvalidImage.jpg", -1)]
		[DataRow("Skia", "Formats/uno-overalls.png", 20)] // Cut inside the IHDR chunk.
		public async Task When_SetSource_Undecodable_Then_PixelSize_Is_Zero(string codecName, string asset, int length)
		{
			var bytes = await ReadAssetAsync(asset);
			if (length >= 0)
			{
				bytes = bytes[..length];
			}

			var sut = SetSource(new SizeReadingCountingImageCodec(CreateCodec(codecName), bytes.Length), bytes);

			Assert.AreEqual(0, sut.PixelWidth);
			Assert.AreEqual(0, sut.PixelHeight);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		public async Task When_SetSource_With_Codec_Without_TryGetSize_Then_PixelSize_Is_Read_From_Decode()
		{
			var bytes = await ReadAssetAsync("testimage_exif_rotated_different_dimensions.jpg");
			var codec = new CountingImageCodec(ImageEncoderDecoder.Current, bytes.Length);

			var sut = SetSource(codec, bytes);

			Assert.AreEqual(400, sut.PixelWidth);
			Assert.AreEqual(250, sut.PixelHeight);
			Assert.AreEqual(1, codec.Decodes, "The default TryGetSize should read the size from a decode.");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		public async Task When_SetSourceAsync_On_Opened_Source_Then_Completes_Once_New_Source_Opens()
		{
			var sut = new BitmapImage();
			var opened = 0;
			sut.ImageOpened += (_, _) => opened++;
			var image = new Image { Width = 50, Height = 50, Source = sut };
			WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);

			await sut.SetSourceAsync(new MemoryStream(await ReadAssetAsync("Formats/uno-overalls.png")).AsRandomAccessStream()).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
			Assert.AreEqual(1, opened);

			await sut.SetSourceAsync(new MemoryStream(await ReadAssetAsync("ingredient3.png")).AsRandomAccessStream()).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
			Assert.AreEqual(2, opened, "SetSourceAsync should complete once the new source opens, not with the previous one.");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		public async Task When_SetSource_On_Shown_Source_Then_Image_Shows_New_Source()
		{
			var sut = new BitmapImage();
			var opened = 0;
			sut.ImageOpened += (_, _) => opened++;
			var image = new Image { Source = sut };
			WindowHelper.WindowContent = new Canvas { Children = { image } };
			await WindowHelper.WaitForLoaded(image, isLoaded: e => e.IsLoaded);

			sut.SetSource(new MemoryStream(await ReadAssetAsync("Formats/uno-overalls.png")).AsRandomAccessStream());
			await WindowHelper.WaitFor(() => opened == 1, timeoutMS: 5000, message: "The first source should open for the Image showing the bitmap.");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(800, image.ActualWidth, 1);

			sut.SetSource(new MemoryStream(await ReadAssetAsync("ingredient3.png")).AsRandomAccessStream());
			await WindowHelper.WaitFor(() => opened == 2, timeoutMS: 5000, message: "The second source should replace the first in the Image.");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(147, image.ActualWidth, 1);
			Assert.AreEqual(147, sut.PixelWidth);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		public async Task When_SetSource_On_Opened_Source_Then_Image_Shown_Later_Gets_New_Source()
		{
			var sut = new BitmapImage();
			var opened = 0;
			sut.ImageOpened += (_, _) => opened++;
			await sut.SetSourceAsync(new MemoryStream(await ReadAssetAsync("Formats/uno-overalls.png")).AsRandomAccessStream()).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
			Assert.AreEqual(1, opened);

			sut.SetSource(new MemoryStream(await ReadAssetAsync("ingredient3.png")).AsRandomAccessStream());
			var image = new Image { Source = sut };
			WindowHelper.WindowContent = new Canvas { Children = { image } };
			await WindowHelper.WaitForLoaded(image, isLoaded: e => e.IsLoaded);

			await WindowHelper.WaitFor(() => opened == 2, timeoutMS: 5000, message: "The Image should get the new source, not the image opened from the previous one.");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(147, image.ActualWidth, 1);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
		public async Task When_SetSource_While_Previous_Source_Opens_Unseen_Then_Its_Result_Is_Dropped()
		{
			var large = await ReadAssetAsync("LargeWisteria.jpg"); // 2400x1400
			var small = await ReadAssetAsync("ingredient3.png"); // 147x147
			var sut = new BitmapImage();
			var openedWidths = new List<int>();
			sut.ImageOpened += (_, _) => openedWidths.Add(sut.PixelWidth);
			sut.SetSource(new MemoryStream(large).AsRandomAccessStream());
			var image = new Image();
			WindowHelper.WindowContent = new Canvas { Children = { image } };
			await WindowHelper.WaitForLoaded(image, isLoaded: e => e.IsLoaded);

			// Showing the bitmap opens it; once nothing shows it, that open is still in flight when the source changes.
			image.Source = sut;
			image.Source = null;
			sut.SetSource(new MemoryStream(small).AsRandomAccessStream());
			await Task.Delay(1000);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(147, sut.PixelWidth, "The previous source's late result shouldn't set the size.");
			CollectionAssert.DoesNotContain(openedWidths, 2400, "The previous source's open shouldn't raise ImageOpened.");

			image.Source = sut;
			await WindowHelper.WaitFor(() => openedWidths.Contains(147), timeoutMS: 5000);
			CollectionAssert.DoesNotContain(openedWidths, 2400);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(147, image.ActualWidth, 1);
		}

		private static BitmapImage SetSource(CountingImageCodec codec, byte[] bytes)
		{
			var sut = new BitmapImage();
			using (codec.Register())
			{
				sut.SetSource(new MemoryStream(bytes).AsRandomAccessStream());
			}

			return sut;
		}

		private static async Task<byte[]> ReadAssetAsync(string asset)
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + asset));
			return (await FileIO.ReadBufferAsync(file)).ToArray();
		}

		// A codec parameter, though only Skia's has rows here: another backend's rows can be added next to them.
		private static IImageEncoderDecoder CreateCodec(string name) => name switch
		{
#if UNO_DRAWING_SKIA
			"Skia" => global::Uno.UI.Composition.Skia.SkiaBackend.CreateImageDecoder(),
#endif
			_ => throw new AssertInconclusiveException($"The {name} codec isn't in this build."),
		};

		// A 3x2 JPEG with an EXIF orientation.
		private static byte[] CreateOrientedJpeg(IImageEncoderDecoder codec, int orientation)
		{
			using var encoded = new MemoryStream();
			var pixels = Enumerable.Repeat((byte)0xFF, 3 * 2 * 4).ToArray();
			codec.Encode(encoded, pixels, 3, 2, Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, global::Uno.UI.Composition.Drawing.BitmapEncoderFormat.Jpeg, 90);
			var jpeg = encoded.ToArray();

			// APP1 right after SOI: "Exif\0\0", a big-endian TIFF header, and an IFD with one entry, Orientation (a SHORT).
			byte[] app1 =
			[
				0xFF, 0xE1, 0x00, 0x22,
				(byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
				(byte)'M', (byte)'M', 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
				0x00, 0x01,
				0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, (byte)orientation, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
			];
			return [.. jpeg[..2], .. app1, .. jpeg[2..]];
		}
#endif

		[TestMethod]
		public async Task When_ImageBrush_Source_Changes()
		{
			var imageBrush = new ImageBrush();
			var bitmapImage = new BitmapImage();
			bitmapImage.UriSource = new Uri("ms-appx:///Assets/BlueSquare.png");
			imageBrush.ImageSource = bitmapImage;

			var stackPanel = new Border()
			{
				Width = 100,
				Height = 100,
				Background = imageBrush,
			};

			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(stackPanel);


			imageBrush.ImageOpened += ImageBrush_ImageOpened;
			var imageOpened = false;
			void ImageBrush_ImageOpened(object sender, RoutedEventArgs e) => imageOpened = true;

			bitmapImage.UriSource = new Uri("ms-appx:///Assets/test_image_100_150.png");
			await WindowHelper.WaitFor(() => imageOpened);
		}

#if __SKIA__
		[Ignore("Flaky for skia targets: https://github.com/unoplatform/uno/issues/9080")]
#endif
		[TestMethod]
		public async Task When_Uri_Nullified()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive("Taking screenshots is not possible on this target platform.");
			}

			var image = new Image();
			var bitmapImage = new BitmapImage();
			image.Source = bitmapImage;
			var border = new Border()
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Colors.Red),
			};
			border.Child = image;
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);

			var initialScreenshot = await UITestHelper.ScreenShot(border);

			bitmapImage.UriSource = new Uri("ms-appx:///Assets/BlueSquare.png");

			await WindowHelper.WaitForIdle();

#if __SKIA__ // Flaky on native
			await WindowHelper.WaitForOpened(bitmapImage);
#endif

			var screenshotWithImage = await UITestHelper.ScreenShot(border);

			await ImageAssert.AreNotEqualAsync(initialScreenshot, screenshotWithImage);

			bitmapImage.UriSource = null;

			var screenshotWithoutImage = await UITestHelper.ScreenShot(border);

			var sw = Stopwatch.StartNew();
			while (
				!await ImageAssert.AreRenderTargetBitmapsEqualAsync(screenshotWithoutImage.Bitmap, initialScreenshot.Bitmap)
				&& sw.ElapsedMilliseconds < 5000
			)
			{
				await WindowHelper.WaitForIdle();
				await Task.Delay(100);

				screenshotWithoutImage = await UITestHelper.ScreenShot(border);
			}

			await ImageAssert.AreEqualAsync(screenshotWithoutImage, initialScreenshot);
		}

#if __SKIA__
		[TestMethod]
		public async Task When_DecodePixelWidth_Only()
		{
			// test_image_200_300.png is 200x300. Setting DecodePixelWidth=100 should
			// decode at 100x150, preserving the 2:3 aspect ratio.
			var bitmapImage = new BitmapImage();
			bitmapImage.DecodePixelWidth = 100;

			var image = new Image { Width = 200, Height = 300 };
			WindowHelper.WindowContent = image;

			var openedTask = WindowHelper.WaitForOpened(bitmapImage);
			bitmapImage.UriSource = new Uri("ms-appx:///Assets/test_image_200_300.png");
			image.Source = bitmapImage;

			await openedTask;

			Assert.AreEqual(100, bitmapImage.PixelWidth, "PixelWidth should match DecodePixelWidth");
			Assert.AreEqual(150, bitmapImage.PixelHeight, "PixelHeight should preserve aspect ratio");
		}

		[TestMethod]
		public async Task When_DecodePixelHeight_Only()
		{
			// test_image_200_300.png is 200x300. Setting DecodePixelHeight=150 should
			// decode at 100x150, preserving the 2:3 aspect ratio.
			var bitmapImage = new BitmapImage();
			bitmapImage.DecodePixelHeight = 150;

			var image = new Image { Width = 200, Height = 300 };
			WindowHelper.WindowContent = image;

			var openedTask = WindowHelper.WaitForOpened(bitmapImage);
			bitmapImage.UriSource = new Uri("ms-appx:///Assets/test_image_200_300.png");
			image.Source = bitmapImage;

			await openedTask;

			Assert.AreEqual(100, bitmapImage.PixelWidth, "PixelWidth should preserve aspect ratio");
			Assert.AreEqual(150, bitmapImage.PixelHeight, "PixelHeight should match DecodePixelHeight");
		}

		[TestMethod]
		public async Task When_DecodePixelWidth_And_Height()
		{
			// When both are set, they are used as-is with no aspect ratio enforcement.
			var bitmapImage = new BitmapImage();
			bitmapImage.DecodePixelWidth = 80;
			bitmapImage.DecodePixelHeight = 60;

			var image = new Image { Width = 200, Height = 300 };
			WindowHelper.WindowContent = image;

			var openedTask = WindowHelper.WaitForOpened(bitmapImage);
			bitmapImage.UriSource = new Uri("ms-appx:///Assets/test_image_200_300.png");
			image.Source = bitmapImage;

			await openedTask;

			Assert.AreEqual(80, bitmapImage.PixelWidth, "PixelWidth should match DecodePixelWidth");
			Assert.AreEqual(60, bitmapImage.PixelHeight, "PixelHeight should match DecodePixelHeight");
		}

		[TestMethod]
		public async Task When_No_DecodePixel_Set()
		{
			// Regression: full resolution when no decode pixel is set.
			var bitmapImage = new BitmapImage();

			var image = new Image { Width = 200, Height = 300 };
			WindowHelper.WindowContent = image;

			var openedTask = WindowHelper.WaitForOpened(bitmapImage);
			bitmapImage.UriSource = new Uri("ms-appx:///Assets/test_image_200_300.png");
			image.Source = bitmapImage;

			await openedTask;

			Assert.AreEqual(200, bitmapImage.PixelWidth, "PixelWidth should be full resolution");
			Assert.AreEqual(300, bitmapImage.PixelHeight, "PixelHeight should be full resolution");
		}

		[TestMethod]
		public async Task When_DecodePixelWidth_With_Stream()
		{
			// Tests stream-based loading with decode pixel.
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/test_image_200_300.png"));
			using var stream = await file.OpenReadAsync();

			var bitmapImage = new BitmapImage();
			bitmapImage.DecodePixelWidth = 100;

			var image = new Image { Width = 200, Height = 300 };
			WindowHelper.WindowContent = image;

			var openedTask = WindowHelper.WaitForOpened(bitmapImage);
			bitmapImage.SetSource(stream);
			image.Source = bitmapImage;

			await openedTask;

			Assert.AreEqual(100, bitmapImage.PixelWidth, "PixelWidth should match DecodePixelWidth");
			Assert.AreEqual(150, bitmapImage.PixelHeight, "PixelHeight should preserve aspect ratio");
		}

#if UNO_DRAWING_SKIA
		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Browser_Workers_Are_Reused()
		{
			// The workers are called directly because a failed browser decode falls back to SKCodec, so the image would
			// still open. One more decode than there are workers makes at least one of them decode again after its reset.
			var workers = int.Parse(WasmSemanticDomHelper.InvokeBrowserJs("String(navigator.hardwareConcurrency)"));
			var images = new (byte[] Png, int Width, int Height, (int R, int G, int B, int A) PremultipliedFirstPixel)[]
			{
				(CreatePng(300, 200, new SKColor(200, 100, 50, 128)), 300, 200, (100, 50, 25, 128)),
				(CreatePng(2, 3, SKColors.Blue), 2, 3, (0, 0, 255, 255)),
				(CreatePng(5, 4, SKColors.Red), 5, 4, (255, 0, 0, 255)),
			};
			var batch = Enumerable.Range(0, workers + 1).Select(i => images[i % images.Length]).ToArray();

			var results = await DecodeInBrowser(batch.Select(image => image.Png).ToArray());

			for (var i = 0; i < batch.Length; i++)
			{
				AssertDecoded(results[i], batch[i].Width, batch[i].Height, batch[i].PremultipliedFirstPixel);
			}

			var invalid = await DecodeInBrowser(new byte[] { 1, 2, 3, 4 });
			Assert.IsTrue(invalid[0].StartsWith("error:", StringComparison.Ordinal), $"An invalid image should report an error, got '{invalid[0]}'.");
			AssertDecoded((await DecodeInBrowser(images[0].Png))[0], images[0].Width, images[0].Height, images[0].PremultipliedFirstPixel);
		}

		private static byte[] CreatePng(int width, int height, SKColor color)
		{
			using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(color);
			using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
			return data.ToArray();
		}

		private static void AssertDecoded(string result, int width, int height, (int R, int G, int B, int A) premultipliedFirstPixel)
		{
			Assert.IsFalse(result.StartsWith("error:", StringComparison.Ordinal) || result.StartsWith("exception:", StringComparison.Ordinal), $"The browser worker failed to decode the image: {result}");
			var values = result.Split(',').Select(int.Parse).ToArray();

			Assert.AreEqual(width, values[0], $"Width ({result})");
			Assert.AreEqual(height, values[1], $"Height ({result})");
			Assert.AreEqual(width * height * 4, values[2], $"Byte count ({result})");
			var (r, g, b, a) = premultipliedFirstPixel;
			Assert.IsTrue(
				Math.Abs(values[3] - r) <= 2 && Math.Abs(values[4] - g) <= 2 && Math.Abs(values[5] - b) <= 2 && Math.Abs(values[6] - a) <= 2,
				$"First pixel is RGBA {values[3]},{values[4]},{values[5]},{values[6]}, expected {r},{g},{b},{a}.");
		}

		private static int _browserDecodeCalls;

		// Decodes the images concurrently and returns, for each, "width,height,byteCount,r,g,b,a", "error: <reason>" or
		// "exception: <reason>".
		private static async Task<string[]> DecodeInBrowser(params byte[][] images)
		{
			var key = $"__unoBrowserDecode{++_browserDecodeCalls}";
			var encoded = string.Join(",", images.Select(image => $"'{Convert.ToBase64String(image)}'"));
			WasmSemanticDomHelper.InvokeBrowserJs(
				"(function(){"
				+ $"var images=[{encoded}].map(function(s){{return Uint8Array.from(atob(s),function(c){{return c.charCodeAt(0);}});}});"
				+ "var clean=function(e){return String(e).replace(/\\s+/g,' ');};"
				+ "Promise.all(images.map(function(bytes){return Uno.UI.Runtime.ImageLoader.loadFromArray(bytes).then(function(r){"
				+ "return r.error?'error: '+clean(r.error):[r.width,r.height,r.bytes.length,r.bytes[0],r.bytes[1],r.bytes[2],r.bytes[3]].join(',');"
				+ "},function(e){return 'exception: '+clean(e);});}))"
				+ $".then(function(all){{globalThis['{key}']=all.join('\\n');}});return '';}})()");

			try
			{
				var result = "";
				await WindowHelper.WaitFor(
					() => (result = WasmSemanticDomHelper.InvokeBrowserJs($"globalThis['{key}']")) != "",
					timeoutMS: 10000,
					message: $"The browser decode of {images.Length} image(s) did not complete.");
				return result.Split('\n');
			}
			finally
			{
				WasmSemanticDomHelper.InvokeBrowserJs($"delete globalThis['{key}']");
			}
		}
#endif
#endif

		private class Given_BitmapSource_Exception : Exception
		{
			public string Caller { get; }

			public Given_BitmapSource_Exception([CallerMemberName] string caller = null)
			{
				Caller = caller;
			}
		}

		public class Given_BitmapSource_Stream : Stream
		{
			public override void Flush() => throw new Given_BitmapSource_Exception();
			public override int Read(byte[] buffer, int offset, int count) => throw new Given_BitmapSource_Exception();
			public override long Seek(long offset, SeekOrigin origin) => throw new Given_BitmapSource_Exception();
			public override void SetLength(long value) => throw new Given_BitmapSource_Exception();
			public override void Write(byte[] buffer, int offset, int count) => throw new Given_BitmapSource_Exception();

			public override bool CanRead { get; } = true;
			public override bool CanSeek { get; } = true;
			public override bool CanWrite { get; }
			public override long Length { get; } = 1024;
			public override long Position { get; set; }
		}
	}
}
