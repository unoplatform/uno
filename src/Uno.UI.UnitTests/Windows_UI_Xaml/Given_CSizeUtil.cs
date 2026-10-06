using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_CSizeUtil
{
	[TestMethod]
	public void When_Deflate_Size_By_Larger_Thickness_Then_Clamped_To_Zero()
	{
		var size = new Size(10, 5);

		CSizeUtil.Deflate(ref size, new Thickness(4, 3, 8, 3));

		Assert.AreEqual(new Size(0, 0), size);
	}

	[TestMethod]
	public void When_Deflate_Size_By_Larger_Size_Then_Clamped_To_Zero()
	{
		var size = new Size(10, 5);

		CSizeUtil.Deflate(ref size, new Size(12, 2));

		Assert.AreEqual(new Size(0, 3), size);
	}

	[TestMethod]
	public void When_Inflate_Negative_Size_Then_Clamped_To_Zero()
	{
		var size = new Size(-10, 5);

		CSizeUtil.Inflate(ref size, new Thickness(1));

		Assert.AreEqual(new Size(0, 7), size);
	}

	[TestMethod]
	public void When_Deflate_Infinite_Size_Then_Stays_Infinite()
	{
		var size = new Size(double.PositiveInfinity, 20);

		CSizeUtil.Deflate(ref size, new Thickness(2));

		Assert.AreEqual(new Size(double.PositiveInfinity, 16), size);
	}
}
