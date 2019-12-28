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
		[TestInitialize]
		public void Init()
		{
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

      var file = await folder.CreateFileAsync("test-deletingfile.txt", Windows.Storage.CreationCollisionOption.FailIfExists);
      Assert.IsNotNull(file, "cannot create file - error outside tested method");

      // try delete file
      await file.DeleteAsync();
      
      // check if method works
      var fileAfter = await folder.TryGetItemAsync("test-deletingfile.txt");
      Assert.IsNull(file, "file is not deleted - tested method fails");
      
		}


	}
}
