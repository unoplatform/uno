using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls.Helpers;

[TestClass]
public class Given_OrientedSize
{
	[TestMethod]
	public void When_Default()
	{
		OrientedSize orientedSize = new();
		Assert.AreEqual(0.0, orientedSize.Width);
		Assert.AreEqual(0.0, orientedSize.Height);
		Assert.AreEqual(Orientation.Vertical, orientedSize.Orientation);
	}

	[TestMethod]
	public void When_Horizontal()
	{
		OrientedSize orientedSize = new(Orientation.Horizontal, 12, 16);
		Assert.AreEqual(12, orientedSize.Width);
		Assert.AreEqual(16, orientedSize.Height);
		Assert.AreEqual(Orientation.Horizontal, orientedSize.Orientation);
		Assert.AreEqual(12, orientedSize.Direct);
		Assert.AreEqual(16, orientedSize.Indirect);


		orientedSize.Direct = 93;
		orientedSize.Indirect = 11;

		Assert.AreEqual(93, orientedSize.Width);
		Assert.AreEqual(11, orientedSize.Height);
	}

	[TestMethod]
	public void When_Vertical()
	{
		OrientedSize orientedSize = new(Orientation.Vertical, 8, 23);
		Assert.AreEqual(8, orientedSize.Width);
		Assert.AreEqual(23, orientedSize.Height);
		Assert.AreEqual(Orientation.Vertical, orientedSize.Orientation);
		Assert.AreEqual(23, orientedSize.Direct);
		Assert.AreEqual(8, orientedSize.Indirect);

		orientedSize.Direct = 15;
		orientedSize.Indirect = 19;

		Assert.AreEqual(15, orientedSize.Height);
		Assert.AreEqual(19, orientedSize.Width);
	}
}
