#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	public abstract class Given_StorageFile_Native_Base
	{
		protected abstract Task<StorageFolder> GetRootFolderAsync();

		protected virtual Task CleanupRootFolderAsync() => Task.CompletedTask;

		[TestMethod]
		public async Task When_CreateFile_Name_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual(fileName, createdFile.Name);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
#if WINAPPSDK
		[Ignore("On UWP, DisplayName sometimes returns the name with, and sometimes without the extension.")]
#endif
		public async Task When_CreateFile_DisplayName_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual(fileNameWithoutExtension, createdFile.DisplayName);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_Without_Extension()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = Path.GetFileNameWithoutExtension(GetRandomTextFileName());
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual(fileName, createdFile.Name);
				Assert.AreEqual(fileName, createdFile.DisplayName);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_ContentType_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual("text/plain", createdFile.ContentType);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_ContentType_For_Extension_Upper()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName().Replace(".txt", ".TXT");
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual("text/plain", createdFile.ContentType);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_FileType_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual(".txt", createdFile.FileType);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_OfType()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.IsTrue(createdFile.IsOfType(StorageItemTypes.File));
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_Provider_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual(rootFolder.Provider, createdFile.Provider);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_Delete_File()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			try
			{
				var createdFile = await rootFolder.CreateFileAsync(fileName);
				await createdFile.DeleteAsync();
				Assert.IsNull(await rootFolder.TryGetItemAsync(fileName));
			}
			finally
			{
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_GetBasicProperties_Size()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			var length = 3UL;
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await FileIO.WriteBytesAsync(createdFile, new byte[] { 65, 66, 67 });
				var basicProperties = await createdFile.GetBasicPropertiesAsync();
				Assert.AreEqual(length, basicProperties.Size);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
#if WINAPPSDK
		[Ignore("UWP is unable to handle going up a level for picked folders")]
#endif
		public async Task When_GetParent()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var file = await createdFolder.CreateFileAsync(GetRandomTextFileName());
				var parent = await file.GetParentAsync();
				Assert.AreEqual(folderName, parent?.Name);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFolder);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_ReadStreamFlush()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await FileIO.WriteTextAsync(createdFile, "Hello world!");

				var buffer = new byte[1024];

				using var stream = await createdFile.OpenStreamForReadAsync();

				while (await stream.ReadAsync(buffer, 0, 1024) > 0)
				{
				}

				try
				{
					stream.Flush();
				}
				catch (Exception ex)
				{
					Assert.Fail($"Read stream flush threw {ex}");
				}
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_InputStreamFlush()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await FileIO.WriteTextAsync(createdFile, "Hello world!");

				var buffer = new byte[1024];

				using var stream = await createdFile.OpenReadAsync();

				try
				{
					await stream.FlushAsync();
				}
				catch (Exception ex)
				{
					Assert.Fail($"Input stream flush threw {ex}");
				}
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_OpenStreamForReadAsync_Stream_Is_ReadOnly()
		{
			StorageFile? createdFile = null;
			try
			{
				var rootFolder = await GetRootFolderAsync();
				var fileName = GetRandomTextFileName();
				createdFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(createdFile, "Some content");

				using var stream = await createdFile.OpenStreamForReadAsync();
				Assert.IsTrue(stream.CanRead, "Read stream should be readable");
				Assert.IsFalse(stream.CanWrite, "Read stream should not be writable");
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_OpenStreamForWriteAsync_Stream_CanWrite()
		{
			StorageFile? createdFile = null;
			try
			{
				var rootFolder = await GetRootFolderAsync();
				var fileName = GetRandomTextFileName();
				createdFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

				using (var stream = await createdFile.OpenStreamForWriteAsync())
				{
					Assert.IsTrue(stream.CanWrite, "Write stream should be writable");

					using var writer = new StreamWriter(stream);
					await writer.WriteAsync("Test content");
					await writer.FlushAsync();
				}

				var content = await FileIO.ReadTextAsync(createdFile);
				Assert.AreEqual("Test content", content);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
#if WINAPPSDK
		[Ignore("Exercises Uno's StorageFile.OpenStreamAsync FileMode mapping; native WinUI's StorageFile isn't subject to this fix.")]
#endif
		public async Task When_OpenStreamForWriteAsync_FileDoesNotExist_Stream_CreatesFile()
		{
			StorageFile? createdFile = null;
			try
			{
				var rootFolder = await GetRootFolderAsync();
				var fileName = GetRandomTextFileName();

				// Hold a StorageFile reference, then delete the backing file to reproduce
				// the regression: OpenStreamForWriteAsync used to hard-code FileMode.Open
				// and threw FileNotFoundException when the file was missing. With the fix
				// it maps ReadWrite → FileMode.OpenOrCreate and re-creates the file.
				createdFile = await rootFolder.CreateFileAsync(fileName);
				File.Delete(createdFile.Path);
				Assert.IsFalse(File.Exists(createdFile.Path), "Pre-condition: backing file should be deleted");

				using (var stream = await createdFile.OpenStreamForWriteAsync())
				{
					using var writer = new StreamWriter(stream);
					await writer.WriteAsync("Recreated content");
					await writer.FlushAsync();
				}

				Assert.IsTrue(File.Exists(createdFile.Path), "OpenStreamForWriteAsync should have recreated the file");
				var content = await FileIO.ReadTextAsync(createdFile);
				Assert.AreEqual("Recreated content", content);
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
#if WINAPPSDK
		[Ignore("Exercises Uno's StorageFile.OpenStreamAsync FileMode mapping; native WinUI's StorageFile isn't subject to this fix.")]
#endif
		public async Task When_OpenStreamForReadAsync_FileDoesNotExist_Throws()
		{
			StorageFile? createdFile = null;
			try
			{
				var rootFolder = await GetRootFolderAsync();
				var fileName = GetRandomTextFileName();

				createdFile = await rootFolder.CreateFileAsync(fileName);
				File.Delete(createdFile.Path);
				Assert.IsFalse(File.Exists(createdFile.Path), "Pre-condition: backing file should be deleted");

				var fileRef = createdFile;
				createdFile = null;

				await Assert.ThrowsAsync<FileNotFoundException>(
					async () =>
					{
						using var _ = await fileRef.OpenStreamForReadAsync();
					});
			}
			finally
			{
				await DeleteIfNotNullAsync(createdFile);
				await CleanupRootFolderAsync();
			}
		}

		private string GetRandomFolderName() => Guid.NewGuid().ToString();

		private string GetRandomTextFileName() => Guid.NewGuid().ToString() + ".txt";

		private Task DeleteIfNotNullAsync(IStorageItem? item) => item != null ? item.DeleteAsync().AsTask() : Task.CompletedTask;
	}
}
