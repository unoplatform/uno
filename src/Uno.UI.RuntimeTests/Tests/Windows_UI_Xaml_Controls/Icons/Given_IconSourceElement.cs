using System.Threading.Tasks;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
[RunsOnUIThread]
public class Given_IconSourceElement
{
	[TestMethod]
	public async Task When_Switch_Sources()
	{
		// SymbolIconSource
		var symbolIconSource = new SymbolIconSource() { Symbol = Symbol.Accept };
		var iconSourceElement = new IconSourceElement()
		{
			IconSource = symbolIconSource
		};
		TestServices.WindowHelper.WindowContent = iconSourceElement;
		await TestServices.WindowHelper.WaitForLoaded(iconSourceElement);

		var symbolChild = VisualTreeUtils.FindVisualChildByType<TextBlock>(iconSourceElement);
		Assert.IsNotNull(symbolChild);
		Assert.AreEqual("\uE10B", symbolChild.Text); //E10B represents Accept symbol

		symbolIconSource.Symbol = Symbol.Cancel;
		Assert.AreEqual("\uE10A", symbolChild.Text); //E10A represents Cancel symbol

		// PathIconSource
		var ellipseGeometry = new EllipseGeometry()
		{
			RadiusX = 20,
			RadiusY = 20
		};
		var pathIconSource = new PathIconSource() { Data = ellipseGeometry };
		iconSourceElement.IconSource = pathIconSource;

		Path pathChild = null;
		await TestServices.WindowHelper.WaitFor(
			() => (pathChild = VisualTreeUtils.FindVisualChildByType<Path>(iconSourceElement)) is not null);
		Assert.AreEqual(ellipseGeometry, pathChild.Data);

		var rectangleGeometry = new RectangleGeometry()
		{
			Rect = new Windows.Foundation.Rect(0, 0, 20, 20)
		};
		pathIconSource.Data = rectangleGeometry;
		Assert.AreEqual(rectangleGeometry, pathChild.Data);

		// FontIconSource
		var fontIconSource = new FontIconSource() { Glyph = "\uE909" };
		iconSourceElement.IconSource = fontIconSource;
		await TestServices.WindowHelper.WaitForIdle();

		TextBlock fontIconChild = null;
		await TestServices.WindowHelper.WaitFor(
			() => (fontIconChild = VisualTreeUtils.FindVisualChildByType<TextBlock>(iconSourceElement)) is not null);
		Assert.AreEqual("\uE909", fontIconChild.Text);

		fontIconSource.Glyph = "\uE890";
		Assert.AreEqual("\uE890", fontIconChild.Text);

		// BitmapIconSource
		var bitmapIconSource = new BitmapIconSource() { UriSource = new System.Uri("ms-appx:///Assets/Icons/search.png") };
		iconSourceElement.IconSource = bitmapIconSource;
		await TestServices.WindowHelper.WaitForIdle();

		Image bitmapIconChild = null;
		await TestServices.WindowHelper.WaitFor(
			() => (bitmapIconChild = VisualTreeUtils.FindVisualChildByType<Image>(iconSourceElement)) is not null);
		Assert.IsNotNull(bitmapIconChild.Source);

		var previousSource = bitmapIconChild.Source;
		bitmapIconSource.UriSource = new System.Uri("ms-appx:///Assets/Icon.png");
		Assert.AreNotEqual(previousSource, bitmapIconChild.Source);
	}
}
