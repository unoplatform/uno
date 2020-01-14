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
            var folderForTestFile = Windows.Storage.ApplicationData.Current.LocalFolder;
            Assert.IsNotNull(folderForTestFile, "cannot get LocalFolder - error outside tested method");

            Windows.Storage.StorageFile testFile = null;
            
            DateTimeOffset dateBeforeCreating = DateTimeOffset.Now;
      
            try
            {
                testFile = await folderForTestFile.CreateFileAsync( _filename, Windows.Storage.CreationCollisionOption.FailIfExists);
                Assert.IsNotNull(testFile, "cannot create file - error outside tested method");
            }
            catch
            {
                  Assert.Fail("CreateFile exception - error outside tested method");
            }

            DateTimeOffset dateAfterCreating = DateTimeOffset.Now;

            // test of DateCreated

            // first, some wait - to be sure that returned date is not simply 'current date'
            // e.g. FAT has two seconds resolution - so wait should be longer
            await Task.Delay(5000);

            DateTimeOffset dateOnCreating = DateTimeOffset.Now; // unneeded initialization - just to skip compiler error of using uninitialized variable
            try
            {
                  dateOnCreating = testFile.DateCreated;
            }
            catch
            {
                Assert.Fail("DateCreated exception - error in tested method");
            }

           dateBeforeCreating = dateBeforeCreating.AddSeconds(-2);
           dateAfterCreating = dateAfterCreating.AddSeconds(2);
      
            // check if method works
           if(dateOnCreating < dateBeforeCreating)
           {
                Assert.Fail("DateCreated: too early - method doesnt work");
           }

           if(dateOnCreating > dateAfterCreating)
           {
                Assert.Fail("DateCreated: too late - method doesnt work");
           }

        }
    }
}
