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

// Given_StorageFile2 , as Given_StorageFile is used in PR#2407
// Testing DateCreated and DateModified in one test method

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_StorageFile2
	{
		String _filename;

		[TestInitialize]
		public void Init()
		{
			_filename = DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
		}

		[TestCleanup]
		public void Cleanup()
		{
		}

		[TestMethod]
		public async Task When_DateCreated()
		{
			var folderForTestFile = Windows.Storage.ApplicationData.Current.LocalFolder;
			Assert.IsNotNull(folderForTestFile, "cannot get LocalFolder - error outside tested method");

			Windows.Storage.StorageFile testFile = null;

			DateTimeOffset dateBeforeCreating = DateTimeOffset.Now;

			try
			{
				testFile = await folderForTestFile.CreateFileAsync(_filename, Windows.Storage.CreationCollisionOption.FailIfExists);
				Assert.IsNotNull(testFile, "cannot create file - error outside tested method");
			}
			catch
			{
				Assert.Fail("CreateFile exception - error outside tested method");
			}

			DateTimeOffset dateAfterCreating = DateTimeOffset.Now;



			// tests of DateCreated

			DateTimeOffset dateOnCreating = DateTimeOffset.Now; // unneeded initialization - just to skip compiler error of using uninitialized variable
			try
			{
				dateOnCreating = testFile.DateCreated;
			}
			catch
			{
				Assert.Fail("DateCreated exception - error in tested method");
			}

			// while testing date, we should remember about filesystem date resolution.
			// FAT: 2 seconds, but we don't have FAT
			// NTFS: 100 ns
			// VFAT (SD cards): can be as small as 10 ms
			// ext4 (internal Android): can be below 1 ms
			// APFS (since iOS 10.3): 1 ns
			// HFS+ (before iOS 10.3): 1 s

			// first, date should not be year 1601 or something like that...
			if (dateOnCreating < dateBeforeCreating.AddSeconds(-2))
			{
				Assert.Fail("DateCreated: too early - method doesnt work");
			}

			// second, date should not be in future
			if (dateOnCreating > dateAfterCreating.AddSeconds(2))
			{
				Assert.Fail("DateCreated: too late - method doesnt work");
			}

			// third, it should not be "datetime.now"
			var initialTimeDiff = DateTimeOffset.Now - dateOnCreating;
			int loopGuard;
			for (loopGuard = 20; loopGuard > 0; loopGuard--) // wait for date change for max 5 seconds
			{
				await Task.Delay(250);
				dateOnCreating = testFile.DateCreated;
				if (DateTimeOffset.Now - dateOnCreating > initialTimeDiff)
				{
					break;
				}
			}
			if (loopGuard < 1)
			{
				Assert.Fail("DateCreated: probably == DateTime.Now, - method doesnt work");
			}



			// now, test Date Modified

			DateTimeOffset dateBeforeModify = DateTimeOffset.Now;
			// modify file
			Stream fileStream = await testFile.OpenStreamForWriteAsync();
			fileStream.Seek(0, SeekOrigin.End);
			StreamWriter streamWriter = new StreamWriter(fileStream);
			streamWriter.Write(_filename);  // anything - just write String
			streamWriter.Dispose();
			fileStream.Dispose();
			DateTimeOffset dateAfterModify = DateTimeOffset.Now;

			// get DateModified
			DateTimeOffset dateModified = DateTimeOffset.Now; // unneeded initialization - just to skip compiler error of using uninitialized variable
			try
			{
				dateModified = (await testFile.GetBasicPropertiesAsync()).DateModified;
			}
			catch
			{
				Assert.Fail("DateModified exception - error in tested method");
			}

			// first, date should not be year 1601 or something like that...
			if (dateModified < dateBeforeModify.AddSeconds(-2))
			{
				Assert.Fail("DateModified: too early - method doesnt work");
			}

			// second, date should not be in future
			if (dateModified > dateAfterModify.AddSeconds(2))
			{
				Assert.Fail("DateCreated: too late - method doesnt work");
			}

			// third, it should not be "datetime.now"
			initialTimeDiff = DateTimeOffset.Now - dateModified;
			for (loopGuard = 20; loopGuard > 0; loopGuard--) // wait for date change for max 5 seconds
			{
				await Task.Delay(250);
				var dateModifiedLoop = (await testFile.GetBasicPropertiesAsync()).DateModified;
				if (DateTimeOffset.Now - dateModifiedLoop > initialTimeDiff)
				{
					break;
				}
			}
			if (loopGuard < 1)
			{
				Assert.Fail("DateModified: probably == DateTime.Now, - method doesnt work");
			}

			// last test
			if (dateModified < dateOnCreating)
			{
				Assert.Fail("DateModified == DateCreated, - method doesnt work");
			}


		}
	}
}
