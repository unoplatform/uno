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
using Private.Infrastructure;
using Windows.Storage;
using static Private.Infrastructure.TestServices;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging
{
	[TestClass]
	public class Given_BitmapSource
	{
		[TestMethod]
		public async Task When_SetSource_Then_ImageOpened_Event_Fires()
		{
			var count = 0;
			var sut = new BitmapImage();
			sut.ImageOpened += SUT_ImageOpened;

			var memoryStream = new MemoryStream(Convert.FromBase64String("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"));
			Assert.AreEqual(0, count);
			await sut.SetSourceAsync(memoryStream.AsRandomAccessStream());
			Assert.AreEqual(1, count);

			void SUT_ImageOpened(object sender, RoutedEventArgs e) => count++;
		}


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SetSource_Then_StreamClonedSynchronously()
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
				sut.SetSourceAsync(raStream);
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

#if !WINDOWS_UWP
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SetSource_Stream_Then_StreamClonedSynchronously()
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
		public async Task When_SetSourceAsync_Stream_Then_StreamClonedSynchronously()
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
			public override bool CanWrite { get; } = false;
			public override long Length { get; } = 1024;
			public override long Position { get; set; } = 0;
		}
	}
}
