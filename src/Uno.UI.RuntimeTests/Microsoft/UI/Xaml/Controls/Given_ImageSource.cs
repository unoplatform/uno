#if !WINAPPSDK

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	/// <summary>
	/// UWP doesn't have implicit conversion from string to ImageSource.
	/// The behavior Uno have for the implicit conversion should match whatever
	/// UWP does when ImageSource is set from XAML, eg:
	/// <code><![CDATA[
	/// <Image Source="String" x:Name="myImage" />
	/// ]]>
	/// </code>
	/// </summary>
	[TestClass]
	public class Given_ImageSource
	{
		[TestMethod]
		public void When_ImageSource_Is_MsAppx()
		{
			ImageSource imageSource = "ms-appx:///Assets/File.png";

			var actual = ((BitmapImage)imageSource).UriSource.ToString();
			Assert.AreEqual("ms-appx:///Assets/File.png", actual);
		}

		[TestMethod]
		public void When_ImageSource_Is_Prefixed_With_Slash()
		{
			ImageSource imageSource = "/Assets/File.png";
			var actual = ((BitmapImage)imageSource).UriSource.ToString();
			Assert.AreEqual("ms-appx:///Assets/File.png", actual);
		}

		[TestMethod]
		public void When_ImageSource_Is_Not_Prefixed()
		{
			ImageSource imageSource = "Assets/File.png";
			var actual = ((BitmapImage)imageSource).UriSource.ToString();
			Assert.AreEqual("ms-appx:///Assets/File.png", actual);
		}

		[TestMethod]
		public void When_ImageSource_Is_Custom_Scheme()
		{
			ImageSource imageSource = "mysceheme:///Assets/File.png";
			var actual = ((BitmapImage)imageSource).UriSource.ToString();
			Assert.AreEqual("mysceheme:///Assets/File.png", actual);
		}

		[TestMethod]
		public void When_ImageSource_From_Relative_Uri()
		{
			ImageSource imageSource = null;
			Action act = () => imageSource = new Uri("/Assets/File.png", UriKind.Relative);

			act.Should().NotThrow();
			imageSource.Should().NotBeNull();

			var actual = ((BitmapImage)imageSource).UriSource.ToString();
			Assert.AreEqual("ms-appx:///Assets/File.png", actual);
		}
	}
}
#endif
