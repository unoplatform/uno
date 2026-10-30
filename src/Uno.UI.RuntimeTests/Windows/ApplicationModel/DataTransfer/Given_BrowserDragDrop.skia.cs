#nullable enable

using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_BrowserDragDrop
{
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	[DataRow("drawing.svg", "image/svg+xml", false)]
	[DataRow("picture.png", "image/png", true)]
	public async Task When_Single_Image_File_Is_Dropped(string name, string type, bool isBitmap)
	{
		// The Bitmap format is a raster image: a single dropped raster image file is also offered as the Bitmap,
		// any other image file, such as an SVG, as a file only.
		DataPackageView? dropped = null;
		var target = new Border { Width = 200, Height = 200, Background = new SolidColorBrush(Colors.Red), AllowDrop = true };
		target.DragEnter += (_, e) => e.AcceptedOperation = DataPackageOperation.Copy;
		target.DragOver += (_, e) => e.AcceptedOperation = DataPackageOperation.Copy;
		target.Drop += (_, e) => dropped = e.DataView;

		var bounds = await UITestHelper.Load(target);
		var x = (int)(bounds.X + bounds.Width / 2);
		var y = (int)(bounds.Y + bounds.Height / 2);

		// The browser raises these on the document for a file dragged in from outside the page.
		Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
			$$"""
			(function() {
				const dt = new DataTransfer();
				dt.items.add(new File(['content'], '{{name}}', { type: '{{type}}' }));
				dt.effectAllowed = 'copy';
				const init = { clientX: {{x}}, clientY: {{y}}, bubbles: true, cancelable: true, dataTransfer: dt };
				for (const type of ['dragenter', 'dragover', 'drop']) {
					document.dispatchEvent(new DragEvent(type, init));
				}
				return 'ok';
			})()
			""");

		await WindowHelper.WaitFor(() => dropped is not null, message: "The file was not dropped on the target.");

		Assert.IsTrue(dropped!.Contains(StandardDataFormats.StorageItems));
		Assert.AreEqual(isBitmap, dropped.Contains(StandardDataFormats.Bitmap));
	}
}
