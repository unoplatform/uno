using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using System.Linq;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given__StorageFolder_GetItems
	{

		readonly String[] _filenames = { "testfile1.txt", "testfile2.txt", "testfile3.txt", "testfile4.txt", "testfile5.txt" };
		readonly String[] _foldernames = { "testfolder1", "testfolder2", "testfolder3", "testfolder4", "testfolder5.555" };
		Windows.Storage.StorageFolder _folderForTestFiles;

		[TestInitialize]
		public async Task Init()
		{
			_folderForTestFiles = Windows.Storage.ApplicationData.Current.LocalFolder;
			Assert.IsNotNull(_folderForTestFiles, "cannot get LocalFolder - error outside tested method");

			_folderForTestFiles = await _folderForTestFiles.CreateFolderAsync("StorageFolder_GetItems");
			Assert.IsNotNull(_folderForTestFiles, "cannot create folder for test files - error outside tested method");


			foreach (var filename in _filenames)
			{
				try
				{
					var testFile = await _folderForTestFiles.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.FailIfExists);
					Assert.IsNotNull(testFile, "cannot create test file - error outside tested method");
				}
				catch
				{
					Assert.Fail("CreateFile exception - error outside tested method");
				}
			}

			foreach (var foldername in _foldernames)
			{
				try
				{
					var testFolder = await _folderForTestFiles.CreateFolderAsync(foldername, Windows.Storage.CreationCollisionOption.FailIfExists);
					Assert.IsNotNull(testFolder, "cannot create test folder - error outside tested method");
				}
				catch
				{
					Assert.Fail("CreateFolder exception - error outside tested method");
				}
			}


		}

		[TestCleanup]
		public async Task Cleanup()
		{
			foreach (var filename in _filenames)
			{
				try
				{
					var testFile = await _folderForTestFiles.GetFileAsync(filename);
					if (testFile != null)
					{
						await testFile.DeleteAsync();
					}
				}
				catch
				{
					Assert.Fail("DeleteAsync (file) exception - error outside tested method");
				}
			}

			foreach (var foldername in _foldernames)
			{
				try
				{
					var testFolder = await _folderForTestFiles.GetFolderAsync(foldername);
					if (testFolder != null)
					{
						await testFolder.DeleteAsync();
					}
				}
				catch
				{
					Assert.Fail("DeleteAsync (folder) exception - error outside tested method");
				}
			}

			await _folderForTestFiles.DeleteAsync();

		}

		[TestMethod]
		public async Task When_GetItems()
		{

			var realFileList = await _folderForTestFiles.GetFilesAsync();

			if (realFileList.Count != _filenames.Length)
			{
				Assert.Fail("Number of files mismatch");
			}

			foreach (string requestedFilename in _filenames)
			{
				bool fileOK = realFileList.Any(r => r.Name.Equals(requestedFilename, StringComparison.OrdinalIgnoreCase));
				Assert.IsTrue(fileOK, "Required file is missing - error in tested method");
			}

			var realFolderList = await _folderForTestFiles.GetFoldersAsync();

			if (realFolderList.Count != _foldernames.Length)
			{
				Assert.Fail("Number of folders mismatch");
			}

			foreach (var requestedFoldername in _foldernames)
			{
				bool fileOK = realFolderList.Any(r => r.Name.Equals(requestedFoldername, StringComparison.OrdinalIgnoreCase));
				Assert.IsTrue(fileOK, "Required folder is missing - error in tested method");
			}

		}

	}
}
