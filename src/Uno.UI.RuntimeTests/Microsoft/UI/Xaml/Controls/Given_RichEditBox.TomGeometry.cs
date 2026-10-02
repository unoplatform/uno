#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.Storage.Streams;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(32)]
	[DataRow(256)]
	[DataRow(512)]
	[DataRow(1024)]
	[DataRow(65536)]
	[DataRow(262144)]
	[DataRow(327936)]
	[DataRow(328448)]
	public async Task When_GetRect_Hit_Is_Zero_For_PointOptions(int optionValue)
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "abc\rdef");
			editor.Document.GetRange(0, 5).GetRect((PointOptions)(uint)optionValue, out var rect, out var hit);

			Assert.AreEqual(0, hit);
			Assert.IsTrue(rect.Height > 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_GetRect_Hit_Is_Zero_For_Text_Caret_FinalEop_And_Empty()
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "abc");
			AssertGeometryHit(editor.Document.GetRange(0, 1), PointOptions.ClientCoordinates, expectedHit: 0);
			AssertGeometryHit(editor.Document.GetRange(1, 1), PointOptions.ClientCoordinates, expectedHit: 0);

			var storyEnd = editor.Document.GetRange(0, int.MaxValue).EndPosition;
			var finalEop = editor.Document.GetRange(storyEnd - 1, storyEnd);
			var finalCaret = editor.Document.GetRange(storyEnd - 1, storyEnd - 1);
			finalEop.GetRect(PointOptions.ClientCoordinates, out var eopRect, out var eopHit);
			finalCaret.GetRect(PointOptions.ClientCoordinates, out var caretRect, out var caretHit);
			Assert.AreEqual(0, eopHit);
			Assert.AreEqual(0, caretHit);
			AssertRectsEqual(caretRect, eopRect);

			editor.Document.SetText(TextSetOptions.None, string.Empty);
			await WindowHelper.WaitForIdle();
			editor.Document.GetRange(0, 0).GetRect(PointOptions.ClientCoordinates, out var emptyRect, out var emptyHit);
			Assert.AreEqual(0, emptyHit);
			Assert.IsTrue(emptyRect.Height > 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_GetRect_On_Unloaded_Control_Returns_Zero_Hit()
	{
		var editor = CreateGeometryEditor();
		editor.Document.SetText(TextSetOptions.None, "unloaded");

		editor.Document.GetRange(0, 1).GetRect(PointOptions.ClientCoordinates, out _, out var hit);

		Assert.AreEqual(0, hit);
	}

	[TestMethod]
	public async Task When_GetRect_Start_Does_Not_Replace_Range_Geometry()
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "abc\rsecond");
			var range = editor.Document.GetRange(0, 5);
			range.GetRect(PointOptions.ClientCoordinates, out var expected, out var expectedHit);
			range.GetRect(PointOptions.ClientCoordinates | PointOptions.Start, out var actual, out var actualHit);

			Assert.AreEqual(0, expectedHit);
			Assert.AreEqual(0, actualHit);
			AssertRectsEqual(expected, actual);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_GetRect_NoScroll_Preserves_Viewport_And_OffClient_Geometry()
	{
		var editor = CreateGeometryEditor(width: 120, height: 60);
		try
		{
			await LoadGeometryEditor(
				editor,
				"line zero with a very long horizontal tail\rline one\rline two\rline three\rline four");
			var scrollViewer = GetGeometryScrollViewer(editor);
			scrollViewer.ChangeView(scrollViewer.ScrollableWidth, scrollViewer.ScrollableHeight, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			var horizontalOffset = scrollViewer.HorizontalOffset;
			var verticalOffset = scrollViewer.VerticalOffset;

			var options = PointOptions.ClientCoordinates
				| PointOptions.NoHorizontalScroll
				| PointOptions.NoVerticalScroll;
			var range = editor.Document.GetRange(0, 1);
			range.GetRect(options, out var clippedRect, out var clippedHit);
			range.GetRect(options | PointOptions.AllowOffClient, out var offClientRect, out var offClientHit);

			Assert.AreEqual(0, clippedHit);
			Assert.AreEqual(0, offClientHit);
			AssertRectsEqual(clippedRect, offClientRect);
			Assert.AreEqual(horizontalOffset, scrollViewer.HorizontalOffset, 0.5);
			Assert.AreEqual(verticalOffset, scrollViewer.VerticalOffset, 0.5);
			Assert.IsTrue(clippedRect.X >= 0);
			Assert.IsTrue(clippedRect.Y >= 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_GetRect_Selection_Spans_Visual_Lines_With_Zero_Hit()
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "first\rsecond\rthird");
			editor.Document.Selection.SetRange(0, 13);
			editor.Document.Selection.GetRect(
				PointOptions.ClientCoordinates | PointOptions.NoVerticalScroll,
				out var rect,
				out var hit);

			Assert.AreEqual(0, hit);
			Assert.IsTrue(rect.Height > editor.FontSize * 2);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_GetRect_Bidi_Logical_Edges_Match_Native()
	{
		var editor = CreateGeometryEditor(width: 300);
		try
		{
			await LoadGeometryEditor(editor, "abc אבג xyz");
			editor.Document.GetRange(4, 4).GetRect(PointOptions.ClientCoordinates, out var leading, out var leadingHit);
			editor.Document.GetRange(7, 7).GetRect(PointOptions.ClientCoordinates, out var trailing, out var trailingHit);
			editor.Document.GetRange(4, 7).GetRect(PointOptions.ClientCoordinates, out var span, out var spanHit);

			Assert.AreEqual(0, leadingHit);
			Assert.AreEqual(0, trailingHit);
			Assert.AreEqual(0, spanHit);
			Assert.AreEqual(leading.X, trailing.X, 1);
			Assert.IsTrue(span.Width > 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_GetRect_Inline_Object_Uses_Object_Bounds_With_Zero_Hit()
	{
		const string pngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4AWJiZmT6DwAAAP//EKnFGgAAAAZJREFUAwABIQEIIJGZrwAAAABJRU5ErkJggg==";
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "ab");
			using (var stream = new MemoryStream(Convert.FromBase64String(pngBase64)).AsRandomAccessStream())
			{
				editor.Document.GetRange(1, 1).InsertImage(
					32,
					24,
					20,
					VerticalCharacterAlignment.Baseline,
					"geometry",
					stream);
			}
			await WindowHelper.WaitForIdle();

			editor.Document.GetRange(1, 2).GetRect(PointOptions.ClientCoordinates, out var rect, out var hit);

			Assert.AreEqual(0, hit);
			Assert.AreEqual(32, rect.Width, 1);
			Assert.IsTrue(rect.Height >= 24);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_GetRect_Transform_Flag_Preserves_Native_Client_Geometry()
	{
		var editor = CreateGeometryEditor();
		editor.RenderTransform = new ScaleTransform { ScaleX = 1.5, ScaleY = 1.25 };
		editor.RenderTransformOrigin = new Point(0, 0);
		try
		{
			await LoadGeometryEditor(editor, "abc");
			var range = editor.Document.GetRange(0, 1);
			range.GetRect(PointOptions.ClientCoordinates, out var expected, out var expectedHit);
			range.GetRect(PointOptions.ClientCoordinates | PointOptions.Transform, out var actual, out var actualHit);

			Assert.AreEqual(0, expectedHit);
			Assert.AreEqual(0, actualHit);
			AssertRectsEqual(expected, actual);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_GetPoint_Start_Chooses_Start_And_Other_Options_Do_Not_Change_The_Point()
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "abc\rdef");
			var range = editor.Document.GetRange(1, 5);
			range.GetPoint(
				HorizontalCharacterAlignment.Left,
				VerticalCharacterAlignment.Top,
				PointOptions.ClientCoordinates,
				out var endPoint);
			range.GetPoint(
				HorizontalCharacterAlignment.Left,
				VerticalCharacterAlignment.Top,
				PointOptions.ClientCoordinates | PointOptions.AllowOffClient,
				out var offClientPoint);
			range.GetPoint(
				HorizontalCharacterAlignment.Left,
				VerticalCharacterAlignment.Top,
				PointOptions.ClientCoordinates | PointOptions.NoHorizontalScroll | PointOptions.NoVerticalScroll,
				out var noScrollPoint);
			range.GetPoint(
				HorizontalCharacterAlignment.Left,
				VerticalCharacterAlignment.Top,
				PointOptions.ClientCoordinates | PointOptions.Start,
				out var startPoint);

			AssertPointsEqual(endPoint, offClientPoint);
			AssertPointsEqual(endPoint, noScrollPoint);
			Assert.IsTrue(startPoint.Y < endPoint.Y);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_SetPoint_Extend_Matches_Native_Endpoint_Behavior()
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "abc\rdef");
			editor.Document.GetRange(5, 5).GetPoint(
				HorizontalCharacterAlignment.Left,
				VerticalCharacterAlignment.Top,
				PointOptions.ClientCoordinates,
				out var endPoint);
			var endRange = editor.Document.GetRange(2, 5);
			endRange.SetPoint(endPoint, PointOptions.ClientCoordinates, extend: true);
			Assert.AreEqual(5, endRange.StartPosition);
			Assert.AreEqual(5, endRange.EndPosition);

			editor.Document.GetRange(1, 1).GetPoint(
				HorizontalCharacterAlignment.Left,
				VerticalCharacterAlignment.Top,
				PointOptions.ClientCoordinates,
				out var startPoint);
			var startRange = editor.Document.GetRange(2, 5);
			startRange.SetPoint(startPoint, PointOptions.ClientCoordinates | PointOptions.Start, extend: true);
			Assert.AreEqual(1, startRange.StartPosition);
			Assert.AreEqual(5, startRange.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(512)]
	[DataRow(65536)]
	[DataRow(262144)]
	[DataRow(327936)]
	public async Task When_SetPoint_And_GetRangeFromPoint_Reject_Native_Invalid_Options(int optionValue)
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "abc\rdef");
			var options = PointOptions.ClientCoordinates | (PointOptions)(uint)optionValue;
			var point = new Point(10, 10);

			Assert.ThrowsExactly<ArgumentException>(
				() => editor.Document.GetRange(2, 5).SetPoint(point, options, extend: false));
			Assert.ThrowsExactly<ArgumentException>(
				() => editor.Document.GetRangeFromPoint(point, options));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_SetPoint_OffClient_Clamps_To_Nearest_Visual_Line_Position()
	{
		var editor = CreateGeometryEditor();
		try
		{
			await LoadGeometryEditor(editor, "abc\rdef");
			editor.Document.GetRange(1, 1).GetPoint(
				HorizontalCharacterAlignment.Left,
				VerticalCharacterAlignment.Top,
				PointOptions.ClientCoordinates,
				out var firstLinePoint);

			AssertSetPoint(editor, new Point(firstLinePoint.X, -100), expectedIndex: 1);
			AssertSetPoint(editor, new Point(firstLinePoint.X, 1000), expectedIndex: 5);
			AssertSetPoint(editor, new Point(-100, firstLinePoint.Y + 5), expectedIndex: 0);
			AssertSetPoint(editor, new Point(1000, firstLinePoint.Y + 5), expectedIndex: 4);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static RichEditBox CreateGeometryEditor(double width = 180, double height = 70)
		=> new()
		{
			Width = width,
			Height = height,
			FontSize = 24,
			TextWrapping = TextWrapping.NoWrap,
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0),
		};

	private static async Task LoadGeometryEditor(RichEditBox editor, string text)
	{
		WindowHelper.WindowContent = editor;
		await WindowHelper.WaitForLoaded(editor);
		editor.Document.SetText(TextSetOptions.None, text);
		await WindowHelper.WaitForIdle();
	}

	private static void AssertGeometryHit(ITextRange range, PointOptions options, int expectedHit)
	{
		range.GetRect(options, out var rect, out var hit);
		Assert.AreEqual(expectedHit, hit);
		Assert.IsTrue(rect.Height > 0);
	}

	private static void AssertSetPoint(RichEditBox editor, Point point, int expectedIndex)
	{
		var range = editor.Document.GetRange(2, 5);
		range.SetPoint(point, PointOptions.ClientCoordinates, extend: false);
		Assert.AreEqual(expectedIndex, range.StartPosition);
		Assert.AreEqual(expectedIndex, range.EndPosition);
	}

	private static ScrollViewer GetGeometryScrollViewer(RichEditBox editor)
		=> FindGeometryDescendant<ScrollViewer>(editor, viewer => viewer.Name == "ContentElement")
			?? throw new AssertFailedException("The RichEditBox ContentElement was not found.");

	private static T? FindGeometryDescendant<T>(DependencyObject root, Func<T, bool> predicate)
		where T : DependencyObject
	{
		for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
		{
			var child = VisualTreeHelper.GetChild(root, index);
			if (child is T match && predicate(match))
			{
				return match;
			}

			if (FindGeometryDescendant(child, predicate) is { } descendant)
			{
				return descendant;
			}
		}

		return default;
	}

	private static void AssertRectsEqual(Rect expected, Rect actual)
	{
		Assert.AreEqual(expected.X, actual.X, 0.5);
		Assert.AreEqual(expected.Y, actual.Y, 0.5);
		Assert.AreEqual(expected.Width, actual.Width, 0.5);
		Assert.AreEqual(expected.Height, actual.Height, 0.5);
	}

	private static void AssertPointsEqual(Point expected, Point actual)
	{
		Assert.AreEqual(expected.X, actual.X, 0.5);
		Assert.AreEqual(expected.Y, actual.Y, 0.5);
	}
}
