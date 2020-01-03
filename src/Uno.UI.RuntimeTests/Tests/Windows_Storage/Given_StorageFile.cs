using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_StorageFile
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
		public async void When_DeleteFile()
		{
			
			var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
			Assert.IsNotNull(folder, "cannot get LocalFolder - error outside tested method");

			try
			{
				var file = await folder.CreateFileAsync( _filename, Windows.Storage.CreationCollisionOption.FailIfExists);
				Assert.IsNotNull(file, "cannot create file - error outside tested method");
			}
			catch
			{
			  Assert.IsNotNull(null, "CreateFile exception - error outside tested method");
			}

		  // try delete file
			try
			{
		  		await file.DeleteAsync();
			}
			catch
			{
				Assert.IsNotNull(null, "DeleteAsync exception - error in tested method");
			}

			// check if method works
			var fileAfter = await folder.TryGetItemAsync("test-deletingfile.txt");
			Assert.IsNull(file, "file is not deleted - tested method fails");
      
		}


	}
}
