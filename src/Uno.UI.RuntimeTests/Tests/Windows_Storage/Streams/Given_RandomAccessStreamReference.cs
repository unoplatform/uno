using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
				catch (Exception)
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
}
