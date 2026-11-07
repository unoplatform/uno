using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams
{
	[TestClass]
	public class Given_RandomAccessStreamReference
	{
		private const string _unoStaticTestFileContent = "https://platform.uno/\r\n";
		private static readonly Uri _unoStaticTestFileUri = new("https://uno-assets.platform.uno/uno-unit-tests.txt");
		private static readonly byte[] _streamContent = [0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19];

		[TestMethod]
		public async Task When_FromUri()
		{
			var sut = RandomAccessStreamReference.CreateFromUri(_unoStaticTestFileUri);

			try
			{
				var actual = await ReadToEnd(sut);

				Assert.AreEqual(_unoStaticTestFileContent, actual);
			}
			catch (HttpRequestException ex) when (ex.StatusCode is null)
			{
				// No status code means the request never reached the server, so the agent has no egress. On iOS that is
				// NSURLErrorNotConnectedToInternet from a freshly booted simulator, very likely the same infrastructure
				// symptom as the simulator-boot flakiness; a network readiness probe in the iOS CI bootstrap is the better
				// placed fix, tracked separately. A real 4xx/5xx carries a status code and must keep failing.
				Assert.Inconclusive($"Could not reach {_unoStaticTestFileUri}, the test agent appears to be offline: {ex.Message}");
			}
		}

		[TestMethod]
		public async Task When_FlushReadOnly()
		{
			var sut = RandomAccessStreamReference.CreateFromUri(_unoStaticTestFileUri);

			try
			{
				using var readStream = await sut.OpenReadAsync();

				try
				{
					await readStream.FlushAsync();
				}
				catch (Exception ex) when (ex is NotImplementedException or InvalidOperationException)
				{
					// UWP throws NotImplementedException
					// Uno throws InvalidOperationException with a description
				}
			}
			catch (HttpRequestException ex) when (ex.StatusCode is null)
			{
				// Same transport-only guard as When_FromUri: an offline agent is inconclusive, an HTTP error is a failure.
				Assert.Inconclusive($"Could not reach {_unoStaticTestFileUri}, the test agent appears to be offline: {ex.Message}");
			}
		}

#if !WINAPPSDK
		[TestMethod]
		public async Task When_FromFile()
		{
			var temp = await GetTempFile();
			var sut = RandomAccessStreamReference.CreateFromFile(temp); // We create the ref even before writing anything in it.

			var tempContent = Guid.NewGuid().ToString("N");
			var tempOutStream = await temp.OpenStream(CancellationToken.None, FileAccessMode.ReadWrite, StorageOpenOptions.AllowReadersAndWriters);
			using (var writer = new StreamWriter(tempOutStream))
			{
				writer.Write(tempContent);
			}

			var actual = await ReadToEnd(sut);

			Assert.AreEqual(tempContent, actual);
		}
#endif

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_FromStream()
		{
			var temp = new MemoryStream();
			var sut = RandomAccessStreamReference.CreateFromStream(temp.AsRandomAccessStream()); // We create the ref even before writing anything in it.

			var tempContent = Guid.NewGuid().ToString("N");
			var tempContentBytes = Encoding.UTF8.GetBytes(tempContent);
			temp.Write(tempContentBytes, 0, tempContentBytes.Length);
			temp.Position = 0;

			var actual = await ReadToEnd(sut);

			Assert.AreEqual(tempContent, actual);
		}

		[TestMethod]
		public async Task When_From_AppData_Should_Open_For_Read()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ingredient3.png"));
			await file.CopyAsync(ApplicationData.Current.LocalFolder, "ingredient3.png", NameCollisionOption.ReplaceExisting);

			var uri = new Uri("ms-appdata:///local/ingredient3.png");
			var stream = (await RandomAccessStreamReference.CreateFromUri(uri).OpenReadAsync()).AsStreamForRead().ReadAllBytes();
			var actual = (await file.OpenReadAsync()).AsStreamForRead().ReadAllBytes();
			Assert.IsTrue(stream.SequenceEqual(actual));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
		public async Task When_FromStream_Opened_Twice_Then_Opens_Are_Independent()
		{
			using var source = await CreateInMemoryStream(_streamContent);
			source.Seek(4);
			var sut = RandomAccessStreamReference.CreateFromStream(source);
			source.Seek(6);

			using var first = await sut.OpenReadAsync();
			using var second = await sut.OpenReadAsync();

			// As on WinUI, each open starts at 0 wherever the source is, and reading one moves neither the other nor the source.
			Assert.AreEqual(0ul, first.Position);
			Assert.AreEqual(0ul, second.Position);
			CollectionAssert.AreEqual(_streamContent[..3], await Read(first, 3));
			Assert.AreEqual(3ul, first.Position);
			Assert.AreEqual(0ul, second.Position);
			CollectionAssert.AreEqual(_streamContent, await ReadAll(second));
			Assert.AreEqual(6ul, source.Position);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_FromStream_Opened_Stream_Disposed_Then_Source_And_Other_Opens_Stay_Readable(bool disposeThroughAsStreamForRead)
		{
			using var source = await CreateInMemoryStream(_streamContent);
			var sut = RandomAccessStreamReference.CreateFromStream(source);

			var first = await sut.OpenReadAsync();
			using var second = await sut.OpenReadAsync();
			Assert.AreEqual((ulong)_streamContent.Length, first.Size);
			if (disposeThroughAsStreamForRead)
			{
				first.AsStreamForRead().Dispose();
			}
			else
			{
				first.Dispose();
			}

			CollectionAssert.AreEqual(_streamContent, await ReadAll(second));
			using var third = await sut.OpenReadAsync();
			Assert.AreEqual(0ul, third.Position);
			CollectionAssert.AreEqual(_streamContent, await ReadAll(third));
			source.Seek(0);
			CollectionAssert.AreEqual(_streamContent, await ReadAll(source));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
		public async Task When_FromStream_Source_Disposed_Then_Earlier_Opens_Stay_Readable_And_Later_Opens_Fail()
		{
			var source = await CreateInMemoryStream(_streamContent);
			var sut = RandomAccessStreamReference.CreateFromStream(source);
			using var opened = await sut.OpenReadAsync();

			source.Dispose();

			CollectionAssert.AreEqual(_streamContent, await ReadAll(opened));
			await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await sut.OpenReadAsync());
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
		public async Task When_FromStream_With_ContentType_Then_Opens_And_Clones_Keep_It()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ingredient3.png"));
			using var source = await file.OpenReadAsync();
			Assert.AreEqual("image/png", source.ContentType);
			var sut = RandomAccessStreamReference.CreateFromStream(source);

			using var opened = await sut.OpenReadAsync();
			using var openedClone = opened.CloneStream();

			Assert.AreEqual("image/png", opened.ContentType);
			Assert.AreEqual("image/png", Assert.IsInstanceOfType<IRandomAccessStreamWithContentType>(openedClone).ContentType);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
		// Uno only: WinUI sniffs the content type from the content.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_FromStream_Source_Clones_Have_No_ContentType_Then_Opens_Keep_The_Source_ContentType()
		{
			using var source = new TypedRandomAccessStream(await CreateInMemoryStream(_streamContent), "image/x-test");
			var sut = RandomAccessStreamReference.CreateFromStream(source);

			using var opened = await sut.OpenReadAsync();

			Assert.AreEqual("image/x-test", opened.ContentType);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
		// Uno only: WinUI's AsRandomAccessStream() wrapper can't be cloned, so every open fails there. Uno's can, and its own
		// Win32 clipboard and drag-and-drop bitmap references wrap a MemoryStream.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_FromStream_Over_MemoryStream_Then_Opens_Are_Independent_And_Survive_Dispose()
		{
			using var source = new MemoryStream(_streamContent).AsRandomAccessStream();
			source.Seek(4);
			var sut = RandomAccessStreamReference.CreateFromStream(source);

			var first = await sut.OpenReadAsync();
			using var second = await sut.OpenReadAsync();
			CollectionAssert.AreEqual(_streamContent[..3], await Read(first, 3));
			Assert.AreEqual(0ul, second.Position);
			first.Dispose();

			CollectionAssert.AreEqual(_streamContent, await ReadAll(second));
			using var third = await sut.OpenReadAsync();
			CollectionAssert.AreEqual(_streamContent, await ReadAll(third));
			Assert.AreEqual(4ul, source.Position);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
		public async Task When_FromStream_Source_Cannot_Be_Cloned_Then_Open_Fails()
		{
			using var source = new UncloneableRandomAccessStream(await CreateInMemoryStream(_streamContent));
			var sut = RandomAccessStreamReference.CreateFromStream(source);

			await Assert.ThrowsExactlyAsync<NotSupportedException>(async () => await sut.OpenReadAsync());
		}

		private static async Task<InMemoryRandomAccessStream> CreateInMemoryStream(byte[] content)
		{
			var stream = new InMemoryRandomAccessStream();
			await stream.WriteAsync(content.AsBuffer());
			stream.Seek(0);

			return stream;
		}

		private static async Task<byte[]> Read(IInputStream stream, uint count)
			=> (await stream.ReadAsync(new global::Windows.Storage.Streams.Buffer(count), count, InputStreamOptions.None)).ToArray();

		private static Task<byte[]> ReadAll(IRandomAccessStream stream)
			=> Read(stream, (uint)(stream.Size - stream.Position));

		private static async Task<StorageFile> GetTempFile()
			=> await ApplicationData.Current.LocalFolder.CreateFileAsync($"{Guid.NewGuid()}.txt", CreationCollisionOption.ReplaceExisting);

		private static async Task<string> ReadToEnd(IRandomAccessStreamReference streamRef)
		{
			var stream = await streamRef.OpenReadAsync();
			using var reader = new StreamReader(stream.AsStreamForRead(), Encoding.UTF8);
			var content = await reader.ReadToEndAsync();

			return content;
		}
	}

	// Passes everything to the wrapped stream.
	internal partial class DelegatingRandomAccessStream(IRandomAccessStream inner) : IRandomAccessStream
	{
		public bool CanRead => inner.CanRead;

		public bool CanWrite => inner.CanWrite;

		public ulong Position => inner.Position;

		public ulong Size
		{
			get => inner.Size;
			set => inner.Size = value;
		}

		public virtual IRandomAccessStream CloneStream() => inner.CloneStream();

		public IInputStream GetInputStreamAt(ulong position) => inner.GetInputStreamAt(position);

		public IOutputStream GetOutputStreamAt(ulong position) => inner.GetOutputStreamAt(position);

		public void Seek(ulong position) => inner.Seek(position);

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> inner.ReadAsync(buffer, count, options);

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => inner.WriteAsync(buffer);

		public IAsyncOperation<bool> FlushAsync() => inner.FlushAsync();

		public void Dispose() => inner.Dispose();
	}

	// A stream that can't be cloned, like WinUI's AsRandomAccessStream() wrapper of a .NET stream.
	internal sealed partial class UncloneableRandomAccessStream(IRandomAccessStream inner) : DelegatingRandomAccessStream(inner)
	{
		public override IRandomAccessStream CloneStream() => throw new NotSupportedException();
	}

	// A stream with a content type whose clones, made by the wrapped stream, have none.
	internal sealed partial class TypedRandomAccessStream(IRandomAccessStream inner, string contentType)
		: DelegatingRandomAccessStream(inner), IRandomAccessStreamWithContentType
	{
		public string ContentType => contentType;
	}
}
