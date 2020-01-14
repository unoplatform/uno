using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

// Given_StorageFile2 , as Given_StorageFile is used in PR#2407

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
        public async void When_DateCreated()
        {
            
            var _folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Assert.IsNotNull(_folder, "cannot get LocalFolder - error outside tested method");

            Windows.Storage.StorageFile _file = null;
            
            DateTimeOffset _dateBefore = DateTimeOffset.Now;
      
            try
            {
                _file = await _folder.CreateFileAsync( _filename, Windows.Storage.CreationCollisionOption.FailIfExists);
                Assert.IsNotNull(_file, "cannot create file - error outside tested method");
            }
            catch
            {
                  Assert.Fail("CreateFile exception - error outside tested method");
            }

            DateTimeOffset _dateAfter = DateTimeOffset.Now;

            // test of DateCreated

            // now, some wait - to be sure that returned date is not simply 'current date'
            // FAT has two seconds resolution - so wait should be longer
            await Task.Delay(5000);

            DateTimeOffset _dateCreated = DateTimeOffset.Now; // unneeded initialization - just to skip compiler error of using uninitialized variable
            try
            {
                  _dateCreated = _file.DateCreated;
            }
            catch
            {
                Assert.Fail("DateCreated exception - error in tested method");
            }

           _dateBefore = _dateBefore.AddSeconds(-2);
           _dateAfter = _dateAfter.AddSeconds(2);
      
            // check if method works
           if(_dateCreated < _dateBefore)
           {
                Assert.Fail("DateCreated: too early - method doesnt work");
           }

           if(_dateCreated > _dateAfter)
           {
                Assert.Fail("DateCreated: too late - method doesnt work");
           }

      
        }

    }
}
