using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams;

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
				_file = await _folder.CreateFileAsync( _filename, Windows.Storage.CreationCollisionOption.FailIfExists);
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

		private string GetRandomFilePath()
			=> Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{Guid.NewGuid()}.txt");

		public async Task<StorageFile> GetFile(string filePath)
			=> await StorageFile.GetFileFromPathAsync(filePath);
	}
}
