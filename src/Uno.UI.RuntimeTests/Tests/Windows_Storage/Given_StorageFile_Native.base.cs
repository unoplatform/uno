#nullable enable

using System;
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
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_DisplayName_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				Assert.AreEqual(fileName, createdFile.DisplayName);
			}
			finally
			{
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
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
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
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
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
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
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
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
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
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
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
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
			var length = 14UL;
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await FileIO.WriteBytesAsync(createdFile, new byte[length]);
				var basicProperties = await createdFile.GetBasicPropertiesAsync();
				Assert.AreEqual(length, basicProperties.Size);
			}
			finally
			{
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
				await CleanupRootFolderAsync();
			}
		}

#if !WINDOWS_UWP // UWP is unable to handle going up a level for picked folders
		[TestMethod]
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
				if (createdFolder != null)
				{
					await createdFolder.DeleteAsync();
				}
				await CleanupRootFolderAsync();
			}
		}
#endif

		private string GetRandomFolderName() => Guid.NewGuid().ToString();

		private string GetRandomTextFileName() => Guid.NewGuid().ToString() + ".txt";
	}
}
