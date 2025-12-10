using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[TestClass]
public class Given_BindableFoundationStructs
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Binding_To_Rect_Property()
	{
		var page = new BindableFoundationStructsTestPage();
		var rectTextBlock = page.RectTextBlock;

		// Verify binding to Rect.Width works
		page.TestRect = new Rect(10, 20, 100, 200);
		Assert.AreEqual(100.0, rectTextBlock.Tag);

		// Verify binding updates when property changes
		page.TestRect = new Rect(0, 0, 250, 300);
		Assert.AreEqual(250.0, rectTextBlock.Tag);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Binding_To_Size_Property()
	{
		var page = new BindableFoundationStructsTestPage();
		var sizeTextBlock = page.SizeTextBlock;

		// Verify binding to Size.Height works
		page.TestSize = new Size(100, 200);
		Assert.AreEqual(200.0, sizeTextBlock.Tag);

		// Verify binding updates when property changes
		page.TestSize = new Size(50, 150);
		Assert.AreEqual(150.0, sizeTextBlock.Tag);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Binding_To_Point_Property()
	{
		var page = new BindableFoundationStructsTestPage();
		var pointTextBlock = page.PointTextBlock;

		// Verify binding to Point.X works
		page.TestPoint = new Point(10, 20);
		Assert.AreEqual(10.0, pointTextBlock.Tag);

		// Verify binding updates when property changes
		page.TestPoint = new Point(50, 60);
		Assert.AreEqual(50.0, pointTextBlock.Tag);
	}
}
