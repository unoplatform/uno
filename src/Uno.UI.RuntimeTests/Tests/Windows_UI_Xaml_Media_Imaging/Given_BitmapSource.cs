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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging
{
	[TestClass]
	public class Given_BitmapSource
	{
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
