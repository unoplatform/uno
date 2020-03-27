#if __ANDROID__
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage.Pickers;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Pickers
{
	[TestClass]
	public class Given_FileOpenPicker
	{
		[TestMethod]
		public void When_No_FileTypeFilter_For_PickSingleFileAsync()
		{
			var fileOpenPicker = new FileOpenPicker();
			Assert.ThrowsException<InvalidOperationException>(() => fileOpenPicker.PickSingleFileAsync());
		}

		[TestMethod]
		public void When_No_FileTypeFilter_For_PickMultipleFilesAsync()
		{
			var fileOpenPicker = new FileOpenPicker();
			Assert.ThrowsException<InvalidOperationException>(() => fileOpenPicker.PickMultipleFilesAsync());
		}
	}
}
#endif
