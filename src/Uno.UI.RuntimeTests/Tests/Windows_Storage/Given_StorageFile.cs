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
            _filename = "deletefile-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public async void When_DeleteFile()
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


    }
}
