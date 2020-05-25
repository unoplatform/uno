#if __ANDROID__ || __MACOS__

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

// Testing Append, Write and Read in one test method

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_Clipboard
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
		public async Task When_PutAndGet()
		{
			string testString = "some text which should be intact";

			// setting clipboard
			var oClipCont = new Windows.ApplicationModel.DataTransfer.DataPackage();
			oClipCont.SetText(testString);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(oClipCont);

			// and reading from clipboard
			var clipView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
			string stringFromClipboard = await clipView.GetTextAsync();

			Assert.AreEqual(stringFromClipboard, testString, false, "text was changed while putting and reading from Clipboard - error in tested methods");

		}
	}
}

#endif
