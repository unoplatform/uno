#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Storage.Streams;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Tom_Geometry_Visual_Rect_Tracks_Inline_Object_After_Scroll()
	{
		const string pngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4AWJiZmT6DwAAAP//EKnFGgAAAAZJREFUAwABIQEIIJGZrwAAAABJRU5ErkJggg==";
		var editor = CreateGeometryEditor(width: 120, height: 60);
		try
		{
			await LoadGeometryEditor(editor, "prefix suffix");
			using (var stream = new MemoryStream(Convert.FromBase64String(pngBase64)).AsRandomAccessStream())
			{
				editor.Document.GetRange(7, 7).InsertImage(
					32,
					24,
					20,
					VerticalCharacterAlignment.Baseline,
					"geometry",
					stream);
			}
			await WindowHelper.WaitForIdle();
			var scrollViewer = GetGeometryScrollViewer(editor);
			scrollViewer.ChangeView(scrollViewer.ScrollableWidth, null, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();

			editor.Document.GetRange(7, 8).GetRect(
				PointOptions.ClientCoordinates | PointOptions.NoHorizontalScroll,
				out var rect,
				out var hit);
			var bitmap = await UITestHelper.ScreenShot(editor);

			Assert.AreEqual(0, hit);
			Assert.AreEqual(32, rect.Width, 1);
			Assert.IsTrue(rect.X >= 0);
			Assert.IsTrue(bitmap.Width > 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Tom_Geometry_Visual_Rect_Tracks_Wrapped_Structured_Math()
	{
		var editor = CreateGeometryEditor(width: 260, height: 160);
		editor.FontSize = 36;
		editor.TextWrapping = TextWrapping.Wrap;
		editor.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
		editor.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetMathMode(RichEditMathMode.MathOnly);
			editor.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mfrac><mi>abc</mi><mi>xyz</mi></mfrac></math>");
			await WindowHelper.WaitForIdle();

			var story = editor.Document.GetRange(0, int.MaxValue);
			story.GetRect(PointOptions.ClientCoordinates, out var rect, out var hit);
			var bitmap = await UITestHelper.ScreenShot(editor);

			Assert.AreEqual(0, hit);
			Assert.IsTrue(rect.Width > 20);
			Assert.IsTrue(rect.Height > editor.FontSize);
			Assert.IsTrue(CountGeometryDarkPixels(bitmap, rect) > 20);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static int CountGeometryDarkPixels(RawBitmap bitmap, Rect rect)
	{
		var left = Math.Clamp((int)Math.Floor(rect.Left), 0, bitmap.Width);
		var top = Math.Clamp((int)Math.Floor(rect.Top), 0, bitmap.Height);
		var right = Math.Clamp((int)Math.Ceiling(rect.Right), left, bitmap.Width);
		var bottom = Math.Clamp((int)Math.Ceiling(rect.Bottom), top, bitmap.Height);
		var count = 0;
		for (var y = top; y < bottom; y++)
		{
			for (var x = left; x < right; x++)
			{
				if (bitmap.GetPixel(x, y) is { A: > 200, R: < 90, G: < 90, B: < 90 })
				{
					count++;
				}
			}
		}

		return count;
	}
}
