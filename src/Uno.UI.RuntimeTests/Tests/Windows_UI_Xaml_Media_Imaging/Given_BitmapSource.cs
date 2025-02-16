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

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Uri_Nullified()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
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
			bool opened = false;
			bitmapImage.ImageOpened += (s, e) => opened = true;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitFor(() => opened);

			var screenshotWithImage = await UITestHelper.ScreenShot(border);

			await ImageAssert.AreNotEqualAsync(initialScreenshot, screenshotWithImage);

			bitmapImage.UriSource = null;
			await WindowHelper.WaitForIdle();

			var screenshotWithoutImage = await UITestHelper.ScreenShot(border);

			await ImageAssert.AreEqualAsync(screenshotWithoutImage, initialScreenshot);
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
