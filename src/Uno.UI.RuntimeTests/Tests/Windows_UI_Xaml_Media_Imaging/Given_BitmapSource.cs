using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices.WindowsRuntime;
using Private.Infrastructure;
using Windows.Storage;
using static Private.Infrastructure.TestServices;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using System.Linq;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging
{
	[TestClass]
	public class Given_BitmapSource
	{
		[TestMethod]
		[RunsOnUIThread]
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
		[RunsOnUIThread]
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

#if __SKIA__ // Not yet supported on the other platforms (https://github.com/unoplatform/uno/issues/8909)
		[TestMethod]
		[RunsOnUIThread]
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

			await tcs.Task;
		}
#endif

#if !WINAPPSDK
		[TestMethod]
		[RunsOnUIThread]
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
		[RunsOnUIThread]
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

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_WriteableBitmap_Assigned_With_Data_Present()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var wb = new WriteableBitmap(20, 20);

			var parent = new Border()
			{
				Width = 50,
				Height = 50,
				Background = new SolidColorBrush(Colors.Blue),
			};

			var rect = new Rectangle
			{
				Width = 20,
				Height = 20,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
			};

			parent.Child = rect;

			WindowHelper.WindowContent = parent;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(rect);

			var bgraPixelData = Enumerable.Repeat<byte>(255, (int)wb.PixelBuffer.Length).ToArray();

			using (Stream stream = wb.PixelBuffer.AsStream())
			{
				stream.Write(bgraPixelData, 0, bgraPixelData.Length);
			}

			rect.Fill = new ImageBrush
			{
				ImageSource = wb
			};

			await WindowHelper.WaitForIdle();

			var snapshot = await UITestHelper.ScreenShot(parent);
			var coords = parent.GetRelativeCoords(rect);
			await WindowHelper.WaitForIdle();

			ImageAssert.DoesNotHaveColorInRectangle(
				snapshot, new System.Drawing.Rectangle((int)coords.X, (int)coords.Y, (int)coords.Width, (int)coords.Height), Colors.Blue);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __WASM__
		[Ignore("https://github.com/unoplatform/uno/issues/12445")]
#endif
		public async Task When_WriteableBitmap_SetSource_Should_Update_PixelWidth_And_PixelHeight()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var writeableBitmap = new WriteableBitmap(1, 1);
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ingredient3.png"));
			using (var stream = await file.OpenReadAsync())
			{
				await writeableBitmap.SetSourceAsync(stream);
			}

			Assert.AreEqual(147, writeableBitmap.PixelWidth);
			Assert.AreEqual(147, writeableBitmap.PixelHeight);

			var parent = new Border()
			{
				Width = 147,
				Height = 147,
			};

			var rect = new Rectangle
			{
				Width = 147,
				Height = 147,
			};

			parent.Child = rect;

			WindowHelper.WindowContent = parent;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(rect);

			rect.Fill = new ImageBrush
			{
				ImageSource = writeableBitmap
			};

			await WindowHelper.WaitForIdle();

			var renderer = new RenderTargetBitmap();

			await renderer.RenderAsync(parent);
			var snapshot = await RawBitmap.From(renderer, rect);

#if !__IOS__ && !__MACOS__ // https://github.com/unoplatform/uno/issues/12705
			ImageAssert.HasColorAt(snapshot, 1, 1, Color.FromArgb(0xFF, 0xFA, 0xB8, 0x63), tolerance: 5);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
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
