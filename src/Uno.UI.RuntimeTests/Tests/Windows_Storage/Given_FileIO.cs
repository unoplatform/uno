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

// Testing Append, Write and Read in one test method

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_FileIO
	{

		[TestInitialize]
		public void Init()
		{
		}

		[TestCleanup]
		public void Cleanup()
		{
		}

		[TestMethod]
		public async Task When_WriteReadAppendRead()
		{
			var folderForTestFile = Windows.Storage.ApplicationData.Current.LocalFolder;
			Assert.IsNotNull(folderForTestFile, "cannot get LocalFolder - error outside tested method");

			var testFile = await folderForTestFile.CreateFileAsync("storage-read-write-append.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
			Assert.IsNotNull(testFile, "cannot create file - error outside tested method");

			string initialContent = "first line of file\n";
			await Windows.Storage.FileIO.WriteTextAsync(testFile, initialContent);

			string readedText = await Windows.Storage.FileIO.ReadTextAsync(testFile);
			Assert.AreEqual(readedText, initialContent, false, "error in WriteTextAsync or in ReadTextAsync - error in tested method");

			string appendContent = "next line\n";
			await Windows.Storage.FileIO.AppendTextAsync(testFile, appendContent);
			readedText = await Windows.Storage.FileIO.ReadTextAsync(testFile);
			Assert.AreEqual(readedText, initialContent, false, "error in AppendTextAsync (not appending?) - error in tested method");

			initialContent = "new first line of file\n";
			await Windows.Storage.FileIO.WriteTextAsync(testFile, initialContent);
			readedText = await Windows.Storage.FileIO.ReadTextAsync(testFile);
			Assert.AreEqual(readedText, initialContent, false, "error in WriteTextAsync (appending?) - error in tested method");
		}
	}
}
