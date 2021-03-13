#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	public abstract class Given_StorageFolder_Native_Base
	{
		protected abstract Task<StorageFolder> GetRootFolderAsync();

		protected virtual Task CleanupRootFolderAsync() => Task.CompletedTask;

		[TestMethod]
		public async Task When_CreateFolder_Name_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				Assert.AreEqual(folderName, createdFolder.Name);
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

		[TestMethod]
		public async Task When_CreateFolder_DisplayName_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				Assert.AreEqual(folderName, createdFolder.DisplayName);
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

		[TestMethod]
		public async Task When_CreateFolder_OfType()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				Assert.IsTrue(createdFolder.IsOfType(StorageItemTypes.Folder));
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

		[TestMethod]
		public async Task When_CreateFolder_Provider_Matches()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				Assert.AreEqual(rootFolder.Provider, createdFolder.Provider);
			}
			finally
			{
				if (createdFolder != null)
				{
					await createdFolder.DeleteAsync();
				}
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_Duplicate_Default()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				await Assert.ThrowsExceptionAsync<Exception>(
					async () => await rootFolder.CreateFolderAsync(folderName));
				Assert.AreEqual(folderName, createdFolder.Name);
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

		[TestMethod]
		public async Task When_CreateFolder_Duplicate_FailIfExists()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				await Assert.ThrowsExceptionAsync<Exception>(
					async () => await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.FailIfExists));
				Assert.AreEqual(folderName, createdFolder.Name);
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

		[TestMethod]
		public async Task When_CreateFolder_Duplicate_OpenIfExists()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var duplicate = await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
				Assert.AreEqual(folderName, duplicate.Name);
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

		[TestMethod]
		public async Task When_CreateFolder_Duplicate_ReplaceExisting()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				await createdFolder.CreateFileAsync(GetRandomTextFileName());
				var replaced = await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.ReplaceExisting);
				Assert.AreEqual(folderName, replaced.Name);
				var files = await replaced.GetFilesAsync();
				Assert.AreEqual(0, files.Count);
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

		[TestMethod]
		public async Task When_CreateFolder_Duplicate_File_ReplaceExisting()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFile? createdFile = null;
			StorageFolder? createdFolder = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(folderName);
				await Assert.ThrowsExceptionAsync<Exception>(
					async () => await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.ReplaceExisting));
			}
			finally
			{
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
				if (createdFolder != null)
				{
					await createdFolder.DeleteAsync();
				}
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_Duplicate_GenerateUniqueName()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);				
				var uniqueFolder = await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.GenerateUniqueName);
				Assert.AreEqual(folderName + " (2)", uniqueFolder.Name);
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

		[TestMethod]
		public async Task When_CreateFolder_Duplicate_File_GenerateUniqueName()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = Guid.NewGuid().ToString();
			StorageFile? createdFile = null;
			StorageFolder? createdFolder = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(folderName);
				createdFolder = await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.GenerateUniqueName);
				Assert.AreEqual(folderName + " (2)", createdFolder.Name);
			}
			finally
			{
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
				if (createdFolder != null)
				{
					await createdFolder.DeleteAsync();
				}
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_Duplicate_Default()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await Assert.ThrowsExceptionAsync<Exception>(
					async () => await rootFolder.CreateFileAsync(fileName));
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
		public async Task When_CreateFile_Duplicate_FailIfExists()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await Assert.ThrowsExceptionAsync<Exception>(
					async () => await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists));
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
		public async Task When_CreateFile_Duplicate_OpenIfExists()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await FileIO.WriteBytesAsync(createdFile, new byte[14]);
				var duplicate = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
				var info = await duplicate.GetBasicPropertiesAsync();
				Assert.AreEqual(14, info.Size);
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
		public async Task When_CreateFile_Duplicate_ReplaceExisting()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				await FileIO.WriteBytesAsync(createdFile, new byte[14]);
				var duplicate = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
				var info = await duplicate.GetBasicPropertiesAsync();
				Assert.AreEqual(0, info.Size);
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
		public async Task When_CreateFile_Duplicate_Folder_ReplaceExisting()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(fileName);
				await Assert.ThrowsExceptionAsync<Exception>(
					async () => await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting));
			}
			finally
			{
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
				if (createdFolder != null)
				{
					await createdFolder.DeleteAsync();
				}
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_CreateFile_Duplicate_GenerateUniqueName()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				var uniqueFolder = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
				Assert.AreEqual(fileName + " (2)", uniqueFolder.Name);
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
		public async Task When_CreateFile_Duplicate_File_GenerateUniqueName()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(fileName);
				createdFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
				Assert.AreEqual(fileName + " (2)", createdFile.Name);
			}
			finally
			{
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
				if (createdFile != null)
				{
					await createdFile.DeleteAsync();
				}
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_GetFile_File_Does_Not_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await rootFolder.GetFileAsync("a.txt"));			
		}

		[TestMethod]
		public async Task When_GetFolder_Folder_Does_Not_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await rootFolder.GetFolderAsync("a"));
		}

		[TestMethod]
		public async Task When_GetFile_Folder_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(fileName);
				await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await rootFolder.GetFileAsync(fileName));
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

		[TestMethod]
		public async Task When_GetFolder_File_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(folderName);
				await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await rootFolder.GetFolderAsync(folderName));
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
		public async Task When_GetFile_File_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				var file = await rootFolder.GetFileAsync(fileName);
				Assert.AreEqual(fileName, file.Name);
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
		public async Task When_GetFolder_Folder_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var folder = await rootFolder.GetFolderAsync(folderName);
				Assert.AreEqual(folderName, folder.Name);
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

		[TestMethod]
		public async Task When_GetItem_File_Does_Not_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await rootFolder.GetItemAsync("a.txt"));
		}

		[TestMethod]
		public async Task When_GetItem_Folder_Does_Not_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await rootFolder.GetItemAsync("a"));
		}

		[TestMethod]
		public async Task When_GetItem_Folder_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var item = await rootFolder.GetItemAsync(folderName);
				Assert.IsTrue(item.IsOfType(StorageItemTypes.Folder));
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

		[TestMethod]
		public async Task When_GetItem_File_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				var item = await rootFolder.GetItemAsync(fileName);
				Assert.IsTrue(item.IsOfType(StorageItemTypes.File));
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
		public async Task When_TryGetItem_Folder_Does_Not_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			try
			{
				var item = await rootFolder.TryGetItemAsync("a");
				Assert.IsNull(item);
			}
			finally
			{
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_TryGetItem_File_Does_Not_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			try
			{
				var item = await rootFolder.TryGetItemAsync("a.txt");
				Assert.IsNull(item);
			}
			finally
			{
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_TryGetItem_Folder_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var item = await rootFolder.TryGetItemAsync(folderName);
				Assert.IsNotNull(item);
				Assert.IsTrue(item.IsOfType(StorageItemTypes.Folder));
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

		[TestMethod]
		public async Task When_TryGetItem_File_Exists()
		{
			var rootFolder = await GetRootFolderAsync();
			var fileName = GetRandomTextFileName();
			StorageFile? createdFile = null;
			try
			{
				createdFile = await rootFolder.CreateFileAsync(fileName);
				var item = await rootFolder.TryGetItemAsync(fileName);
				Assert.IsNotNull(item);
				Assert.IsTrue(item.IsOfType(StorageItemTypes.File));
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
		public async Task When_Delete_Folder_Empty()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			try
			{
				var createdFolder = await rootFolder.CreateFolderAsync(folderName);
				await createdFolder.DeleteAsync();
				Assert.IsNull(await rootFolder.TryGetItemAsync(folderName));
			}
			finally
			{
				await CleanupRootFolderAsync();
			}
		}

		[TestMethod]
		public async Task When_Delete_Folder_Nested()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			try
			{
				var createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var nestedFolder = await createdFolder.CreateFolderAsync(GetRandomFolderName());
				var nestedFile = await createdFolder.CreateFileAsync(GetRandomTextFileName());
				await createdFolder.DeleteAsync();
				Assert.IsNull(await rootFolder.TryGetItemAsync(folderName));
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
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var nestedFolder = await createdFolder.CreateFolderAsync(GetRandomFolderName());
				var basicProperties = await createdFolder.GetBasicPropertiesAsync();
				Assert.AreEqual(0UL, basicProperties.Size);
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

		[TestMethod]
		public async Task When_GetFiles_Empty()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var files = await createdFolder.GetFilesAsync();
				Assert.AreEqual(0, files.Count);
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

		[TestMethod]
		public async Task When_GetFiles_No_Files()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFolderAsync(GetRandomFolderName());
				}
				var files = await createdFolder.GetFilesAsync();
				Assert.AreEqual(0, files.Count);
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

		[TestMethod]
		public async Task When_GetFiles_Files_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFileAsync(GetRandomTextFileName());
				}
				var files = await createdFolder.GetFilesAsync();
				Assert.AreEqual(5, files.Count);
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

		[TestMethod]
		public async Task When_GetFiles_File_Folder_Mix()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFolderAsync(GetRandomFolderName());
				}
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFileAsync(GetRandomTextFileName());
				}
				var files = await createdFolder.GetFilesAsync();
				Assert.AreEqual(5, files.Count);
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

		[TestMethod]
		public async Task When_GetFolders_Empty()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var folders = await createdFolder.GetFoldersAsync();
				Assert.AreEqual(0, folders.Count);
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

		[TestMethod]
		public async Task When_GetFolders_No_Folders()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFileAsync(GetRandomTextFileName());
				}
				var folders = await createdFolder.GetFoldersAsync();
				Assert.AreEqual(0, folders.Count);
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

		[TestMethod]
		public async Task When_GetFolders_Folders_Exist()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFolderAsync(GetRandomTextFileName());
				}
				var folders = await createdFolder.GetFoldersAsync();
				Assert.AreEqual(5, folders.Count);
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

		[TestMethod]
		public async Task When_GetFolders_File_Folder_Mix()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFolderAsync(GetRandomFolderName());
				}
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFileAsync(GetRandomTextFileName());
				}
				var folders = await createdFolder.GetFoldersAsync();
				Assert.AreEqual(5, folders.Count);
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

		[TestMethod]
		public async Task When_GetItems_Empty()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var folders = await createdFolder.GetItemsAsync();
				Assert.AreEqual(0, folders.Count);
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

		[TestMethod]
		public async Task When_GetItems_Files_Only()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFileAsync(GetRandomTextFileName());
				}
				var items = await createdFolder.GetItemsAsync();
				Assert.AreEqual(5, items.Count);
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

		[TestMethod]
		public async Task When_GetItems_Folders_Only()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 5; i++)
				{
					await createdFolder.CreateFolderAsync(GetRandomTextFileName());
				}
				var items = await createdFolder.GetItemsAsync();
				Assert.AreEqual(5, items.Count);
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

		[TestMethod]
		public async Task When_GetItems_File_Folder_Mix()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				for (var i = 0; i < 3; i++)
				{
					await createdFolder.CreateFolderAsync(GetRandomFolderName());
				}
				for (var i = 0; i < 6; i++)
				{
					await createdFolder.CreateFileAsync(GetRandomTextFileName());
				}
				var items = await createdFolder.GetItemsAsync();
				Assert.AreEqual(9, items.Count);
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

		[TestMethod]
		public async Task When_GetFiles_Names_Match()
		{
			var rootFolder = await GetRootFolderAsync();
			var folderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(folderName);
				var createdFileNames = new List<string>();
				for (var i = 0; i < 6; i++)
				{
					var fileName = GetRandomTextFileName();
					createdFileNames.Add(fileName);
					await createdFolder.CreateFileAsync(fileName);
				}
				var files = await createdFolder.GetFilesAsync();
				var retrievedFileNames = files.Select(f => f.Name).ToArray();
				foreach (var fileName in createdFileNames)
				{
					CollectionAssert.Contains(retrievedFileNames, fileName);
				}
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

		[TestMethod]
		public async Task When_GetFolders_Names_Match()
		{
			var rootFolder = await GetRootFolderAsync();
			var containerFolderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(containerFolderName);
				var createdFolderNames = new List<string>();
				for (var i = 0; i < 6; i++)
				{
					var folderName = GetRandomFolderName();
					createdFolderNames.Add(folderName);
					await createdFolder.CreateFolderAsync(folderName);
				}
				var folders = await createdFolder.GetFoldersAsync();
				var retrievedFolderNames = folders.Select(f => f.Name).ToArray();
				foreach (var folderName in createdFolderNames)
				{
					CollectionAssert.Contains(retrievedFolderNames, folderName);
				}
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

		[TestMethod]
		public async Task When_GetItems_Names_Match()
		{
			var rootFolder = await GetRootFolderAsync();
			var containerFolderName = GetRandomFolderName();
			StorageFolder? createdFolder = null;
			try
			{
				createdFolder = await rootFolder.CreateFolderAsync(containerFolderName);
				var createdFolderNames = new List<string>();
				for (var i = 0; i < 6; i++)
				{
					var folderName = GetRandomFolderName();
					createdFolderNames.Add(folderName);
					await createdFolder.CreateFolderAsync(folderName);
				}
				var createdFileNames = new List<string>();
				for (var i = 0; i < 4; i++)
				{
					var fileName = GetRandomTextFileName();
					createdFileNames.Add(fileName);
					await createdFolder.CreateFileAsync(fileName);
				}
				var items = await createdFolder.GetItemsAsync();
				var retrievedItemNames = items.Select(f => f.Name).ToArray();
				foreach (var folderName in createdFolderNames)
				{
					CollectionAssert.Contains(retrievedItemNames, folderName);
				}
				foreach (var fileName in createdFileNames)
				{
					CollectionAssert.Contains(retrievedItemNames, fileName);
				}
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
				var nestedFolder = await createdFolder.CreateFolderAsync(GetRandomFolderName());
				var parent = await nestedFolder.GetParentAsync();
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
