using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_StorageFile
	{
		String _filename;

		[TestInitialize]
		public void Init()
		{
			_filename = "deletefile-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
		}

		[TestCleanup]
		public void Cleanup()
		{
		}

		[TestMethod]
		public async Task When_DeleteFile()
		{

			var _folder = Windows.Storage.ApplicationData.Current.LocalFolder;
			Assert.IsNotNull(_folder, "cannot get LocalFolder - error outside tested method");

			Windows.Storage.StorageFile _file = null;

			try
			{
				_file = await _folder.CreateFileAsync(_filename, Windows.Storage.CreationCollisionOption.FailIfExists);
				Assert.IsNotNull(_file, "cannot create file - error outside tested method");
			}
			catch
			{
				Assert.Fail("CreateFile exception - error outside tested method");
			}

			// try delete file
			try
			{
				await _file.DeleteAsync();
			}
			catch
			{
				Assert.Fail("DeleteAsync exception - error in tested method");
			}

			// check if method works
			var _fileAfter = await _folder.TryGetItemAsync("test-deletingfile.txt");
			Assert.IsNull(_fileAfter, "file is not deleted - tested method fails");
		}

		[TestMethod]
		public async Task When_OpenRead()
		{
			var path = GetRandomFilePath();
			var file = await (await GetFile(path)).OpenReadAsync();
			var direct = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			await StreamTestHelper.Test(writeTo: direct, readOn: file);

			try
			{
				await StreamTestHelper.WriteData(file);
				Assert.Fail("should have failed");
			}
			catch (NotSupportedException)
			{
			}
		}

		[TestMethod]
		public async Task When_Open_Read()
		{
			var path = GetRandomFilePath();
			var file = await (await GetFile(path)).OpenAsync(FileAccessMode.Read);
			var direct = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			await StreamTestHelper.Test(writeTo: direct, readOn: file);

			try
			{
				await StreamTestHelper.WriteData(file);
				Assert.Fail("should have failed");
			}
			catch (NotSupportedException)
			{
			}
		}

		[TestMethod]
		public async Task When_Open_ReadWrite()
		{
			var path = GetRandomFilePath();
			var file = await (await GetFile(path)).OpenAsync(FileAccessMode.ReadWrite);
			var direct = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			await StreamTestHelper.Test(writeTo: direct, readOn: file);
			await StreamTestHelper.Test(writeTo: file, readOn: direct);
		}

		[TestMethod]
		public async Task When_Open_Read_AndGetInputStream()
		{
			var path = GetRandomFilePath();
			var file = (await (await GetFile(path)).OpenAsync(FileAccessMode.Read)).GetInputStreamAt(0);
			var direct = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			await StreamTestHelper.Test(writeTo: direct, readOn: file);
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public async Task When_Open_Read_AndGetOutputStream()
		{
			var path = GetRandomFilePath();
			var file = (await (await GetFile(path)).OpenAsync(FileAccessMode.Read)).GetOutputStreamAt(0);
		}

		[TestMethod]
		public async Task When_Open_ReadWrite_AndGetInputStream()
		{
			var path = GetRandomFilePath();
			var file = (await (await GetFile(path)).OpenAsync(FileAccessMode.ReadWrite)).GetInputStreamAt(0);
			var direct = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			await StreamTestHelper.Test(writeTo: direct, readOn: file);
		}

		[TestMethod]
		public async Task When_Open_ReadWrite_AndGetOutputStream()
		{
			var path = GetRandomFilePath();
			var file = (await (await GetFile(path)).OpenAsync(FileAccessMode.ReadWrite)).GetOutputStreamAt(0);
			var direct = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			await StreamTestHelper.Test(writeTo: file, readOn: direct);
		}

		[TestMethod]
		public async Task When_CloneStream_Then_PositionAreNotShared()
		{
			var path = GetRandomFilePath();

			await StreamTestHelper.WriteData(File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite));

			var file1 = await (await GetFile(path)).OpenAsync(FileAccessMode.ReadWrite);
			var file2 = file1.CloneStream();

			file1.Seek(5);

			Assert.AreNotEqual(file1.Position, file2.Position);
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Not_Exists()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				// Create some other file
				await folder.CreateFileAsync("something.txt");

				var createdFile = await folder.CreateFileAsync("test.txt", CreationCollisionOption.GenerateUniqueName);

				// No suffix should be generated
				Assert.AreEqual("test.txt", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Exists()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFileAsync("test.txt");

				var createdFile = await folder.CreateFileAsync("test.txt", CreationCollisionOption.GenerateUniqueName);
				
				Assert.AreEqual("test (2).txt", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Exists_No_Extension()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFileAsync("test");

				var createdFile = await folder.CreateFileAsync("test", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (2)", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Exists_Increments()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFileAsync("test.txt");

				await folder.CreateFileAsync("test.txt", CreationCollisionOption.GenerateUniqueName);
				var createdFile = await folder.CreateFileAsync("test.txt", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (3).txt", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Multiple()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFileAsync("test.txt");

				for (int i = 2; i < 20; i++)
				{
					await folder.CreateFileAsync($"test ({i}).txt");
				}

				var createdFile = await folder.CreateFileAsync("test.txt", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (20).txt", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Multiple_With_Gap()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFileAsync("test.txt");

				for (int i = 2; i < 20; i++)
				{
					await folder.CreateFileAsync($"test ({i}).txt");
				}

				var gapFile = await folder.GetFileAsync("test (14).txt");
				await gapFile.DeleteAsync();

				var createdFile = await folder.CreateFileAsync("test.txt", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (14).txt", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Exists_Folder()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFileAsync("test.txt");
				await folder.CreateFolderAsync("test (2).txt");

				var createdFile = await folder.CreateFileAsync("test.txt", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (3).txt", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		private string GetRandomFilePath()
			=> Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{Guid.NewGuid()}.txt");

		private string GetRandomFolderPath()
		{
			var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, Guid.NewGuid().ToString());
			Directory.CreateDirectory(path);
			return path;
		}

		public async Task<StorageFile> GetFile(string filePath)
			=> await StorageFile.GetFileFromPathAsync(filePath);
	}
}
