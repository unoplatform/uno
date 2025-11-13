using Windows.ApplicationModel.DataTransfer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_ApplicationModel_DataTransfer
{
	[TestClass]
	public class Given_FileUriHelper
	{
		[TestMethod]
		[DataRow("/home/user/C#/foo/bar.txt", "file:///home/user/C%23/foo/bar.txt")]
		[DataRow("/home/user/C#/foo/%79.txt", "file:///home/user/C%23/foo/%2579.txt")]
		[DataRow("/home/user/%51.txt/", "file:///home/user/%2551.txt/")]
		public void When_Encode_Linux_File_Path(string input, string expectedOutput)
		{
			var sut = FileUriHelper.UrlEncode(input);

			sut.Should().Be(expectedOutput);
		}
	}
}
