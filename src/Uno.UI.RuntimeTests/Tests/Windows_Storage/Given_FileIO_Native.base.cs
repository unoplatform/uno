#nullable enable

using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	public abstract class Given_FileIO_Native_Base
    {
		protected abstract Task<StorageFolder> GetRootFolderAsync();

		protected virtual Task CleanupRootFolderAsync() => Task.CompletedTask;

		[TestMethod]
		public async Task When_WriteTextAsyncNoEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(targetFile, contents);

				var realContents = await FileIO.ReadTextAsync(targetFile);
				Assert.AreEqual(contents, realContents);
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_WriteLinesAsyncNoEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var lines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteLinesAsync(targetFile, lines);

				var realContents = await FileIO.ReadLinesAsync(targetFile);
				CollectionAssert.AreEqual(lines, realContents.ToArray());
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_WriteTextAsyncWithEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(targetFile, contents, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);

				var realContents = await FileIO.ReadTextAsync(targetFile, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);
				Assert.AreEqual(contents, realContents);
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_WriteLinesAsyncWithEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var lines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteLinesAsync(targetFile, lines, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);

				var realContents = await FileIO.ReadLinesAsync(targetFile, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
				CollectionAssert.AreEqual(lines, realContents.ToArray());
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_AppendTextAsyncNoEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var originalContent = "First line3527535205šěššýétžščžíé";
				var appendedContent = "New line33251164535205věšřěšřčžčšžž";

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(targetFile, originalContent);
				await FileIO.AppendTextAsync(targetFile, appendedContent);

				var realContents = await FileIO.ReadTextAsync(targetFile);
				Assert.AreEqual(originalContent + appendedContent, realContents);
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_AppendTextAsyncRecognizesEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var originalContent = "First line3527535205šěššýétžščžíé";
				var appendedContent = "New line33251164535205věšřěšřčžčšžž";

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(targetFile, originalContent, Windows.Storage.Streams.UnicodeEncoding.Utf8);
				await FileIO.AppendTextAsync(targetFile, appendedContent);

				var realContents = await FileIO.ReadTextAsync(targetFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);
				Assert.AreEqual(originalContent + appendedContent, realContents);
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_AppendTextAsyncWithEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var originalContent = "First line3527535205šěššýétžščžíé";
				var appendedContent = "New line33251164535205věšřěšřčžčšžž";

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(targetFile, originalContent, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);
				await FileIO.AppendTextAsync(targetFile, appendedContent, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);

				var realContents = await FileIO.ReadTextAsync(targetFile, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);
				Assert.AreEqual(originalContent + appendedContent, realContents);
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_AppendLinesAsyncNoEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var firstLines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var appendedLines = new[]
				{
					"New line",
					"33251164535205",
					"věšřěšřčžčšžž"
				};

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteLinesAsync(targetFile, firstLines);
				await FileIO.AppendLinesAsync(targetFile, appendedLines);

				var realContents = await FileIO.ReadLinesAsync(targetFile);
				CollectionAssert.AreEqual(firstLines.Concat(appendedLines).ToArray(), realContents.ToArray());
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_AppendLinesAsyncRecognizesEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var firstLines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var appendedLines = new[]
				{
					"New line",
					"33251164535205",
					"věšřěšřčžčšžž"
				};

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteLinesAsync(targetFile, firstLines, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);				
				await FileIO.AppendLinesAsync(targetFile, appendedLines);

				var realContents = await FileIO.ReadLinesAsync(targetFile, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);

				CollectionAssert.AreEqual(firstLines.Concat(appendedLines).ToArray(), realContents.ToArray());
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_AppendLinesAsyncWithEncoding()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var firstLines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var appendedLines = new[]
				{
					"New line",
					"33251164535205",
					"věšřěšřčžčšžž"
				};

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteLinesAsync(targetFile, firstLines, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
				await FileIO.AppendLinesAsync(targetFile, appendedLines, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);

				var realContents = await FileIO.ReadLinesAsync(targetFile, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
				CollectionAssert.AreEqual(firstLines.Concat(appendedLines).ToArray(), realContents.ToArray());
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_WriteBytesAsync()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var bytes = Encoding.UTF8.GetBytes(contents);

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteBytesAsync(targetFile, bytes);

				var realContents = await FileIO.ReadBufferAsync(targetFile);
				var realBytes = realContents.ToArray();

				CollectionAssert.AreEqual(bytes, realBytes);
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_WriteBufferAsync()
		{
			var rootFolder = await GetRootFolderAsync();
			StorageFile? targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var bytes = Encoding.UTF8.GetBytes(contents);
				var buffer = bytes.AsBuffer();

				targetFile = await rootFolder.CreateFileAsync(GetRandomTextFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteBufferAsync(targetFile, buffer);

				var realContents = await FileIO.ReadBufferAsync(targetFile);
				var realBytes = realContents.ToArray();

				CollectionAssert.AreEqual(bytes, realBytes);
			}
			finally
			{
				await DeleteIfNotNullAsync(targetFile);
				await CleanupRootFolderAsync();
			}
		}

		private string GetRandomTextFileName() => Guid.NewGuid().ToString() + ".txt";

		private Task DeleteIfNotNullAsync(IStorageItem? item) => item != null ? item.DeleteAsync().AsTask() : Task.CompletedTask;
	}
}
